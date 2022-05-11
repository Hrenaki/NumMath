using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ComGeom.Common;

namespace ComGeom.Meshes
{
    internal class Mesh3D : IMesh3D
    {
        private List<Vector3D> vertices;
        public IReadOnlyList<Vector3D> Vertices => vertices.AsReadOnly();

        private List<Element> elements;
        public IReadOnlyList<Element> Elements => elements.AsReadOnly();

        private IDictionary<int, string> boundaryMaterialNames;
        public IReadOnlyDictionary<int, string> BoundaryMaterialNames => new ReadOnlyDictionary<int, string>(boundaryMaterialNames);

        private IDictionary<int, string> volumeMaterialNames;
        public IReadOnlyDictionary<int, string> VolumeMaterialNames => new ReadOnlyDictionary<int, string>(volumeMaterialNames);

        public Mesh3D(IEnumerable<Vector3D> vertices, IEnumerable<Element> elements, IDictionary<int, string> boundaryMaterialNames, IDictionary<int, string> volumeMaterialNames)
        {
            Guard.AgainstNull(vertices, nameof(vertices));
            Guard.AgainstNull(elements, nameof(elements));
            Guard.AgainstNull(boundaryMaterialNames, nameof(boundaryMaterialNames));
            Guard.AgainstNull(volumeMaterialNames, nameof(volumeMaterialNames));

            this.vertices = vertices.ToList();
            this.elements = elements.ToList();
            this.boundaryMaterialNames = boundaryMaterialNames;
            this.volumeMaterialNames = volumeMaterialNames;
        }

        public IMesh3D MergeWith(IMesh3D other, int boundaryMaterialIndex, int otherBoundaryMaterialIndex)
        {
            Guard.AgainstNull(other, nameof(other));

            if (!BoundaryMaterialNames.ContainsKey(boundaryMaterialIndex))
                throw new ArgumentException($"First mesh doesn't contain material with index {boundaryMaterialIndex}", nameof(boundaryMaterialIndex));

            if (!other.BoundaryMaterialNames.ContainsKey(otherBoundaryMaterialIndex))
                throw new ArgumentException($"Second mesh doesn't contain material with index {otherBoundaryMaterialIndex}", nameof(otherBoundaryMaterialIndex));

            int i;
            double epsilon = 1E-7;

            LinkedList<Element> elements = new LinkedList<Element>(Elements);
            LinkedList<Element> otherElements = new LinkedList<Element>(other.Elements);

            List<Element> boundaryTriangles = elements.Where(element => element.Type == ElementType.Triangle && element.MaterialNumber == boundaryMaterialIndex).ToList();
            List<Element> boundaryTetrahedrons = elements.Where(element => element.Type == ElementType.Tetrahedron && boundaryTriangles.Any(triangle => triangle.Indices.Except(element.Indices).Count() < 2)).ToList();

            List<Element> otherBoundaryTriangles = otherElements.Where(element => element.Type == ElementType.Triangle && element.MaterialNumber == otherBoundaryMaterialIndex).ToList();
            List<Element> otherBoundaryTetrahedrons = otherElements.Where(element => element.Type == ElementType.Tetrahedron && otherBoundaryTriangles.Any(triangle => triangle.Indices.Except(element.Indices).Count() < 2)).ToList();

            List<Vector3D> vertices = Vertices.ToList();
            List<Vector3D> otherVertices = other.Vertices.ToList();

            Dictionary<int, int> equalVertexIndices = new Dictionary<int, int>();
            Dictionary<Element, Element> equalTriangles = new Dictionary<Element, Element>();

            var triangles = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>();
            foreach (var tetrahedron in boundaryTetrahedrons)
            {
                var faces = elements.Where(triangle => triangle.Type == ElementType.Triangle &&
                                                                  !triangle.Indices.Except(tetrahedron.Indices).Any() &&
                                                                  boundaryTriangles.Any(btriangle => btriangle.Indices.Except(triangle.Indices).Count() < 2)).ToList();

                elements.Remove(tetrahedron);
                foreach (var face in faces)
                {
                    triangles.Add((Triangle: face, Tetrahedron: tetrahedron, FromSplitted: false));
                    elements.Remove(face);
                }
            }

            var otherTriangles = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>();
            foreach (var tetrahedron in otherBoundaryTetrahedrons)
            {
                var faces = otherElements.Where(triangle => triangle.Type == ElementType.Triangle &&
                                                                       !triangle.Indices.Except(tetrahedron.Indices).Any() &&
                                                                       otherBoundaryTriangles.Any(btriangle => btriangle.Indices.Except(triangle.Indices).Count() < 2)).ToList();

                otherElements.Remove(tetrahedron);
                foreach (var face in faces)
                {
                    otherTriangles.Add((Triangle: face, Tetrahedron: tetrahedron, FromSplitted: false));
                    otherElements.Remove(face);
                }
            }

            var resultElements = new List<Element>(elements);
            GetSplittedBoundaryTetrahedrons(triangles, vertices, otherTriangles, otherVertices, equalVertexIndices, equalTriangles, epsilon);

            for (i = 0; i < otherVertices.Count; i++)
            {
                if (!equalVertexIndices.ContainsKey(i))
                    vertices.Add(otherVertices[i]);
            }

            IDictionary<int, string> volumeMaterialNames, boundaryMaterialNames;
            IDictionary<int, int> otherOldToNewVolumeMaterialIndices, equalVolumeMaterials;
            IDictionary<int, int> otherOldToNewBoundaryMaterialIndices, equalBoundaryMaterials;
            MergeAllMaterials(other,
                              out volumeMaterialNames,
                              out otherOldToNewVolumeMaterialIndices, out equalVolumeMaterials,
                              out boundaryMaterialNames,
                              out otherOldToNewBoundaryMaterialIndices, out equalBoundaryMaterials);

            var equalTrianglesValues = equalTriangles.Values;
            for (i = 0; i < triangles.Count; i++)
            {
                var tuple = triangles[i];
                if (!equalTrianglesValues.Contains(tuple.Triangle))
                    resultElements.Add(tuple.Triangle);
                if (!resultElements.Contains(tuple.Tetrahedron))
                    resultElements.Add(tuple.Tetrahedron);
            }

            for (i = 0; i < otherTriangles.Count; i++)
            {
                var tuple = otherTriangles[i];

                if (!equalTriangles.ContainsKey(tuple.Triangle))
                    otherElements.AddLast(tuple.Triangle);

                if (!otherElements.Contains(tuple.Tetrahedron))
                    otherElements.AddLast(tuple.Tetrahedron);
            }

            for(int k = 0; k < otherElements.Count; k++)
            {
                Element element = otherElements.ElementAt(k);
                for (i = 0; i < element.Indices.Length; i++)
                {
                    element.Indices[i] = equalVertexIndices.ContainsKey(element.Indices[i]) ? equalVertexIndices[element.Indices[i]] :
                                                                                              vertices.IndexOf(otherVertices[element.Indices[i]]);
                }

                switch (element.Type)
                {
                    case ElementType.Triangle:
                        element.MaterialNumber = equalBoundaryMaterials.ContainsKey(element.MaterialNumber) ? equalBoundaryMaterials[element.MaterialNumber] :
                                                                                                              otherOldToNewBoundaryMaterialIndices[element.MaterialNumber];
                        break;
                    case ElementType.Tetrahedron:
                        element.MaterialNumber = equalVolumeMaterials.ContainsKey(element.MaterialNumber) ? equalVolumeMaterials[element.MaterialNumber] :
                                                                                                            otherOldToNewVolumeMaterialIndices[element.MaterialNumber];
                        break;
                }

                resultElements.Add(element);
            }

            resultElements.Sort((x, y) =>
            {
                return x.Type.CompareTo(y.Type);
            });

            IMesh3D mesh = MeshFactory.Create3DMesh(vertices, resultElements, boundaryMaterialNames, volumeMaterialNames);
            return mesh;
        }

        private void MergeAllMaterials(IMesh3D other,
                                       out IDictionary<int, string> volumeMaterialNames,
                                       out IDictionary<int, int> otherOldToNewVolumeMaterialIndices, out IDictionary<int, int> equalVolumeMaterials,
                                       out IDictionary<int, string> boundaryMaterialNames,
                                       out IDictionary<int, int> otherOldToNewBoundaryMaterialIndices, out IDictionary<int, int> equalBoundaryMaterials)
        {
            MergeMaterials(VolumeMaterialNames,
                           other.VolumeMaterialNames,
                           out volumeMaterialNames,
                           out otherOldToNewVolumeMaterialIndices,
                           out equalVolumeMaterials);

            MergeMaterials(BoundaryMaterialNames,
                           other.BoundaryMaterialNames,
                           out boundaryMaterialNames,
                           out otherOldToNewBoundaryMaterialIndices,
                           out equalBoundaryMaterials);
        }

        private static void MergeMaterials(IReadOnlyDictionary<int, string> materialNames,
                                    IReadOnlyDictionary<int, string> otherMaterialNames,
                                    out IDictionary<int, string> mergedMaterialNames,
                                    out IDictionary<int, int> otherOldToNewMaterialIndices, out IDictionary<int, int> equalMaterials)
        {
            mergedMaterialNames = new Dictionary<int, string>();
            otherOldToNewMaterialIndices = new Dictionary<int, int>();
            equalMaterials = new Dictionary<int, int>();

            mergedMaterialNames.AddRange(materialNames);

            int materialIndex = 0;
            foreach (KeyValuePair<int, string> volumeMaterialPair in otherMaterialNames)
            {
                if (materialNames.Values.Contains(volumeMaterialPair.Value))
                {
                    int index = materialNames.FirstOrDefault(pair => pair.Value == volumeMaterialPair.Value).Key;
                    equalMaterials.Add(volumeMaterialPair.Key, index);
                    continue;
                }

                while (materialNames.ContainsKey(materialIndex))
                {
                    materialIndex++;
                }

                otherOldToNewMaterialIndices.Add(volumeMaterialPair.Key, materialIndex);

                mergedMaterialNames.Add(materialIndex, volumeMaterialPair.Value);
            }
        }

        private static Vector3D? GetIntersectionPointOfSegments(Vector3D point1, Vector3D line1, Vector3D point2, Vector3D line2, double epsilon, out bool multipleIntersection)
        {
            double sqrEpsilon = epsilon * epsilon;
            double t1, t2;
            double t;

            var g = point2 - point1;
            var fe = Vector3D.Cross(line1, line2);
            var fg = Vector3D.Cross(g, line2);

            multipleIntersection = false;

            if (g.SqrNorm >= sqrEpsilon && fe.SqrNorm >= sqrEpsilon && Math.Abs(g * fe) >= sqrEpsilon)
                return null;

            if (fe.SqrNorm < sqrEpsilon)
            {
                if (fg.SqrNorm >= sqrEpsilon)
                {
                    return null;
                }

                if (line1 * line2 < 0)
                {
                    point2 += line2;
                    line2 = -line2;
                }

                t = line1 * line1;
                t1 = line1 * (point2 - point1);
                t2 = line1 * (point2 + line2 - point1);

                if (t2 <= -sqrEpsilon || t1 >= t + sqrEpsilon)
                {
                    return null;
                }

                if (Math.Abs(t2) < sqrEpsilon)
                {
                    return point1;
                }

                if (Math.Abs(t1 - t) < sqrEpsilon)
                {
                    return point2;
                }

                multipleIntersection = true;
                return null;
            }

            var point = point1 + Math.Sign(fg * fe) * Math.Sqrt(fg.SqrNorm / fe.SqrNorm) * line1;

            var v1 = point - point1;
            var v2 = point - point2;

            t1 = v1 * line1;
            t2 = v2 * line2;

            if (t1 > -sqrEpsilon && t1 < line1 * line1 + sqrEpsilon &&
                t2 > -sqrEpsilon && t2 < line2 * line2 + sqrEpsilon)
            {
                return point;
            }

            return null;
        }

        private static (bool belongs, int belongsToEdge, int equalToVertex) PointBelongsToTriangleInfo(Vector3D point,
                                                                                                       Vector3D[] triangleVertices,
                                                                                                       Vector3D[] triangleEdges,
                                                                                                       double epsilon)
        {
            var vertexCount = triangleVertices.Length;

            Vector3D prevVectorMult = Vector3D.Zero;
            Vector3D curVectorMult;

            double sqrEpsilon = epsilon * epsilon;
            double dotMult;

            // Point doesn't belong to triangle plane
            if (Math.Abs((point - triangleVertices[0]) * Vector3D.Cross(triangleEdges[0], triangleEdges[1])) >= epsilon)
                return new(false, -1, -1);

            for (int i = 0; i < vertexCount; i++)
            {
                var edgeVector = triangleEdges[i];
                var vertexToPointVector = point - triangleVertices[i];

                if (vertexToPointVector.SqrNorm < sqrEpsilon)
                {
                    return new(true, -1, i);
                }

                curVectorMult = Vector3D.Cross(edgeVector, vertexToPointVector);
                if (curVectorMult.SqrNorm < sqrEpsilon)
                {
                    dotMult = vertexToPointVector * edgeVector;

                    if (dotMult > sqrEpsilon && dotMult < edgeVector.SqrNorm - sqrEpsilon)
                    {
                        return new(true, i, -1);
                    }

                    if (Math.Abs(dotMult - edgeVector.SqrNorm) < sqrEpsilon)
                    {
                        return new(true, -1, (i + 1) % vertexCount);
                    }

                    return new(false, -1, -1);
                }

                dotMult = prevVectorMult * curVectorMult;
                var vectorMult = Vector3D.Cross(prevVectorMult, curVectorMult);
                if (vectorMult.SqrNorm >= sqrEpsilon || dotMult < 0)
                {
                    return new(false, -1, -1);
                }

                prevVectorMult = curVectorMult;
            }

            return new(true, -1, -1);
        }

        private static List<Vector3D> GetIntersectionWindow(Vector3D[] vertices1, (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[] vertex1Infos,
                                                            Vector3D[] vertices2, (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[] vertex2Infos,
                                                            List<Vector3D> intersectionPoints,
                                                            double epsilon)
        {
            double sqrEpsilon = epsilon * epsilon;

            void AddUniqueInsideVertices(Vector3D[] vertices,
                                         (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[] vertexInfos,
                                         List<Vector3D> destination)
            {
                foreach (var vertexInfo in vertexInfos)
                {
                    var vertex = vertices[vertexInfo.vertexLocalIndex];
                    if (vertexInfo.info.belongs && !destination.Exists(v => v.SqrDistance(vertex) < sqrEpsilon))
                    {
                        destination.Add(vertex);
                    }
                }
            }

            if (vertex1Infos.All(vertexInfo => vertexInfo.info.belongs))
            {
                return vertices1.ToList();
            }

            if (vertex2Infos.All(vertexInfo => vertexInfo.info.belongs))
            {
                return vertices2.ToList();
            }

            var figure = new List<Vector3D>(intersectionPoints);
            AddUniqueInsideVertices(vertices1, vertex1Infos, figure);
            AddUniqueInsideVertices(vertices2, vertex2Infos, figure);

            if (figure.Count < 3)
            {
                return figure;
            }

            Vector3D normal = Vector3D.Cross(figure[1] - figure[0], figure[2] - figure[1]);
            var window = new List<Vector3D>(figure.Count);
            window.Add(figure[0]);
            figure.RemoveAt(0);

            int count = figure.Count;
            int currentEndIndex, index;
            Vector3D baseVector, current, start;
            for (int i = 1; i < count; i++)
            {
                currentEndIndex = 0;
                start = window[i - 1];
                baseVector = figure[currentEndIndex] - start;

                for (index = 1; index < figure.Count; index++)
                {
                    current = figure[index] - start;
                    if (Vector3D.Cross(current, baseVector) * normal > 0)
                    {
                        baseVector = current;
                        currentEndIndex = index;
                    }
                }

                window.Add(figure[currentEndIndex]);
                figure.RemoveAt(currentEndIndex);
            }

            if (figure.Count > 0)
            {
                window.Add(figure[0]);
            }

            return window;
        }

        private static List<(int localIndex, int figureIndex, Vector3D line)> GetAdditionalEdges(List<Vector3D> intersectionWindow,
                                                                                                 (int StartIndex, int EndIndex, Vector3D Line)[] intersectionWindowEdges,
                                                                                                 Vector3D[] localVertices,
                                                                                                 double epsilon)
        {
            double sqrEpsilon = epsilon * epsilon;

            int localVertexCount = localVertices.Length;
            int intersectionWindowCount = intersectionWindow.Count;
            int i;

            bool multipleIntersection;

            var additionalEdges = new List<(int localIndex, int figureIndex, Vector3D line)>();

            bool intersectionFound;
            int pointIndex;
            for (i = 0; i < localVertexCount; i++)
            {
                var currentPoint = localVertices[i];

                for (pointIndex = 0; pointIndex < intersectionWindowCount; pointIndex++)
                {
                    intersectionFound = false;

                    var point = intersectionWindow[pointIndex];
                    var edge = point - currentPoint;

                    if (edge.SqrNorm < sqrEpsilon)
                        continue;

                    foreach (var windowEdge in intersectionWindowEdges)
                    {
                        var intersectPoint = GetIntersectionPointOfSegments(currentPoint, edge, intersectionWindow[windowEdge.StartIndex], windowEdge.Line, epsilon, out multipleIntersection);

                        if (multipleIntersection || intersectPoint.HasValue && intersectPoint.Value.SqrDistance(point) >= sqrEpsilon &&
                                                                               intersectPoint.Value.SqrDistance(currentPoint) >= sqrEpsilon)
                        {
                            intersectionFound = true;
                            break;
                        }
                    }

                    if (intersectionFound)
                        continue;

                    foreach (var additionalEdge in additionalEdges)
                    {
                        var intersectPoint = GetIntersectionPointOfSegments(currentPoint, edge, localVertices[additionalEdge.localIndex], additionalEdge.line, epsilon, out multipleIntersection);

                        if (multipleIntersection || intersectPoint.HasValue && intersectPoint.Value.SqrDistance(point) >= sqrEpsilon &&
                                                                               intersectPoint.Value.SqrDistance(currentPoint) >= sqrEpsilon)
                        {
                            intersectionFound = true;
                            break;
                        }
                    }

                    if (!intersectionFound)
                    {
                        additionalEdges.Add(new(i, pointIndex, edge));
                    }
                }
            }

            return additionalEdges;
        }

        private static List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> GetNewElements(Element triangle, Element tetrahedron, List<Vector3D> vertices,
                                                                                                       List<Vector3D> intersectionWindow,
                                                                                                       (int StartIndex, int EndIndex, Vector3D Line)[] intersectionWindowEdges,
                                                                                                       List<(int LocalIndex, int IntersectionWindowIndex, Vector3D Line)> newEdges,
                                                                                                       double epsilon)
        {
            double sqrEpsilon = epsilon * epsilon;
            int index;

            List<int> windowIndices = intersectionWindow.Select(windowVertex => vertices.FindIndex(0, vertices.Count, vertex => vertex.SqrDistance(windowVertex) < sqrEpsilon)).ToList();

            int tetrahedronTopVertexIndex = tetrahedron.Indices.Except(triangle.Indices).Single();
            var additionalElements = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>();
            for (index = 1; index < intersectionWindow.Count - 1; index++)
            {
                additionalElements.Add((CopyElementWithNewIndices(triangle, new int[] { windowIndices[0], windowIndices[index], windowIndices[index + 1] }),
                                        CopyElementWithNewIndices(tetrahedron, new int[] { windowIndices[0], windowIndices[index], windowIndices[index + 1], tetrahedronTopVertexIndex }),
                                        true));
            }

            foreach (var windowEdge in intersectionWindowEdges)
            {
                for (int k = 0; k < triangle.Indices.Length; k++)
                {
                    if (newEdges.Exists(edge => edge.LocalIndex == k && edge.IntersectionWindowIndex == windowEdge.StartIndex) &&
                        newEdges.Exists(edge => edge.LocalIndex == k && edge.IntersectionWindowIndex == windowEdge.EndIndex))
                    {
                        // Add new triangle
                        int index1 = windowIndices[windowEdge.StartIndex];
                        int index2 = windowIndices[windowEdge.EndIndex];

                        additionalElements.Add((CopyElementWithNewIndices(triangle, new int[] { index1, triangle.Indices[k], index2 }),
                                                CopyElementWithNewIndices(tetrahedron, new int[] { index1, triangle.Indices[k], index2, tetrahedronTopVertexIndex }),
                                                true));
                    }
                }
            }

            for (int k = 0; k < triangle.Indices.Length; k++)
            {
                for (index = 0; index < intersectionWindow.Count; index++)
                {
                    if (newEdges.Exists(edge => edge.LocalIndex == k && edge.IntersectionWindowIndex == index) &&
                       newEdges.Exists(edge => edge.LocalIndex == (k + 1) % 3 && edge.IntersectionWindowIndex == index))
                    {
                        // Add new triangle
                        int index1 = windowIndices[index];
                        if (Vector3D.Cross(vertices[index1] - vertices[triangle.Indices[k]], vertices[triangle.Indices[(k + 1) % 3]] - vertices[index1]).SqrNorm < sqrEpsilon ||
                            triangle.Indices.Contains(index1))
                            continue;

                        if (additionalElements.Exists(element => element.Triangle.Indices.Contains(k) && element.Triangle.Indices.Contains((k + 1) % 3)))
                            continue;

                        Vector3D edge = vertices[(k + 1) % 3] - vertices[k];
                        if (intersectionWindow.Exists(point => Vector3D.Cross(point - vertices[k], edge).SqrNorm < sqrEpsilon))
                            continue;

                        additionalElements.Add((CopyElementWithNewIndices(triangle, new int[] { triangle.Indices[k], index1, triangle.Indices[(k + 1) % 3] }),
                                                CopyElementWithNewIndices(tetrahedron, new int[] { triangle.Indices[k], index1, triangle.Indices[(k + 1) % 3], tetrahedronTopVertexIndex }),
                                                true));
                    }
                }
            }

            return additionalElements;
        }

        private static Element CopyElementWithNewIndices(Element element, int[] indices)
        {
            return new Element(element.Type, element.MaterialNumber, indices);
        }

        private static void GetSplittedBoundaryTetrahedrons(List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> triangles, List<Vector3D> vertices,
                                                            List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> otherTriangles, List<Vector3D> otherVertices,
                                                            Dictionary<int, int> equalVertexIndices, Dictionary<Element, Element> equalTriangles,
                                                            double epsilon)
        {
            int i, j;
            int n, m;
            int index;

            double sqrEpsilon = epsilon * epsilon;
            int triangleVertexCount = 3;

            Element triangle, tetrahedron;
            Element otherTriangle, otherTetrahedron;
            bool fromSplitted, otherFromSplitted;

            Vector3D[] localVertices = new Vector3D[triangleVertexCount];
            Vector3D[] triangleEdges = new Vector3D[triangleVertexCount];

            Vector3D[] otherLocalVertices = new Vector3D[triangleVertexCount];
            Vector3D[] otherTriangleEdges = new Vector3D[triangleVertexCount];

            var vertexInfos = new (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[triangleVertexCount];
            var otherVertexInfos = new (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[triangleVertexCount];
            (int StartIndex, int EndIndex, Vector3D Line)[] intersectionWindowEdges;

            List<(int localIndex, int figureIndex, Vector3D line)> additionalEdges;
            List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> newElements;

            // OtherTriangle - Triangle
            List<(int OtherTriangle, int Triangle)> notIntersectTriangles = new List<(int OtherTriangle, int Triangle)>(100);

            for (i = 0; i < triangles.Count; i++)
            {
                (triangle, tetrahedron, fromSplitted) = triangles[i];
                if (equalTriangles.Values.Contains(triangle))
                    continue;

                int tetrahedronTopVertexIndex = tetrahedron.Indices.Except(triangle.Indices).Single();

                localVertices[0] = vertices[triangle.Indices[0]];
                localVertices[1] = vertices[triangle.Indices[1]];
                localVertices[2] = vertices[triangle.Indices[2]];

                triangleEdges[0] = localVertices[1] - localVertices[0];
                triangleEdges[1] = localVertices[2] - localVertices[1];
                triangleEdges[2] = localVertices[0] - localVertices[2];

                for (j = 0; j < otherTriangles.Count; j++)
                {
                    (otherTriangle, otherTetrahedron, otherFromSplitted) = otherTriangles[j];
                    if (equalTriangles.ContainsKey(otherTriangle))
                        continue;

                    otherLocalVertices[0] = otherVertices[otherTriangle.Indices[0]];
                    otherLocalVertices[1] = otherVertices[otherTriangle.Indices[1]];
                    otherLocalVertices[2] = otherVertices[otherTriangle.Indices[2]];

                    otherTriangleEdges[0] = otherLocalVertices[1] - otherLocalVertices[0];
                    otherTriangleEdges[1] = otherLocalVertices[2] - otherLocalVertices[1];
                    otherTriangleEdges[2] = otherLocalVertices[0] - otherLocalVertices[2];

                    for (index = 0; index < triangleVertexCount; index++)
                    {
                        vertexInfos[index] = (index, PointBelongsToTriangleInfo(localVertices[index], otherLocalVertices, otherTriangleEdges, epsilon));
                        otherVertexInfos[index] = (index, PointBelongsToTriangleInfo(otherLocalVertices[index], localVertices, triangleEdges, epsilon));
                    }

                    foreach (var vertexInfo in vertexInfos)
                    {
                        var equalVertexIndex = vertexInfo.info.equalToVertex;
                        if (equalVertexIndex != -1 && !equalVertexIndices.ContainsKey(otherTriangle.Indices[equalVertexIndex]))
                        {
                            equalVertexIndices.Add(otherTriangle.Indices[equalVertexIndex], triangle.Indices[vertexInfo.vertexLocalIndex]);
                        }
                    }

                    var intersectionPoints = new List<Vector3D>();
                    for (n = 0; n < triangleVertexCount; n++)
                    {
                        for (m = 0; m < triangleVertexCount; m++)
                        {
                            var point = GetIntersectionPointOfSegments(otherLocalVertices[n], otherTriangleEdges[n], localVertices[m], triangleEdges[m], epsilon, out _);

                            if (point.HasValue && !intersectionPoints.Exists(p => p.SqrDistance(point.Value) < sqrEpsilon))
                            {
                                intersectionPoints.Add(point.Value);
                            }
                        }
                    }

                    //if (intersectionPoints.Count == 0)
                    //{
                    //    continue;
                    //}

                    var insideVertices = vertexInfos.Where(vertexInfo => vertexInfo.info.belongs);
                    var otherInsideVertices = otherVertexInfos.Where(vertexInfo => vertexInfo.info.belongs);

                    var shouldBreakTriangle = intersectionPoints.Any(point => localVertices.All(vertex => vertex.SqrDistance(point) >= sqrEpsilon)) ||
                                              otherInsideVertices.Any(vertexInfo => vertexInfo.info.equalToVertex == -1);

                    var shouldBreakOtherTriangle = intersectionPoints.Any(point => otherLocalVertices.All(vertex => vertex.SqrDistance(point) >= sqrEpsilon)) ||
                                                   insideVertices.Any(vertexInfo => vertexInfo.info.equalToVertex == -1);

                    // triangles dont break each other
                    if (!shouldBreakTriangle && !shouldBreakOtherTriangle)
                    {
                        if (intersectionPoints.Count == triangleVertexCount && !equalTriangles.ContainsKey(otherTriangle))
                            equalTriangles.Add(otherTriangle, triangle);
                        continue;
                    }

                    var intersectionWindow = GetIntersectionWindow(localVertices, vertexInfos,
                                                                   otherLocalVertices, otherVertexInfos,
                                                                   intersectionPoints, epsilon);
                    foreach (Vector3D point in intersectionWindow)
                    {
                        if (!otherVertices.Any(vertex => vertex.SqrDistance(point) < sqrEpsilon))
                            otherVertices.Add(point);

                        if (!vertices.Any(vertex => vertex.SqrDistance(point) < sqrEpsilon))
                            vertices.Add(point);
                    }

                    int intersectionWindowCount = intersectionWindow.Count;
                    switch (intersectionWindowCount)
                    {
                        case 1:
                            intersectionWindowEdges = Array.Empty<(int StartIndex, int EndIndex, Vector3D Line)>();
                            break;
                        case 2:
                            intersectionWindowEdges = new (int StartIndex, int EndIndex, Vector3D Line)[] { (0, 1, intersectionWindow[1] - intersectionWindow[0]) };
                            break;
                        case > 2:
                            intersectionWindowEdges = new (int StartIndex, int EndIndex, Vector3D Line)[intersectionWindowCount];
                            for (int k = 0; k < intersectionWindowCount; k++)
                            {
                                intersectionWindowEdges[k] = (k, (k + 1) % intersectionWindowCount, intersectionWindow[(k + 1) % intersectionWindowCount] - intersectionWindow[k]);
                            }
                            break;
                        default: continue;
                    }

                    if (shouldBreakOtherTriangle)
                    {
                        additionalEdges = GetAdditionalEdges(intersectionWindow, intersectionWindowEdges, otherLocalVertices, epsilon);
                        newElements = GetNewElements(otherTriangle, otherTetrahedron, otherVertices, intersectionWindow, intersectionWindowEdges, additionalEdges, epsilon);
                        if (newElements.Count != 0)
                        {
                            otherTriangles.RemoveAt(j);
                            otherTriangles.AddRange(newElements);
                            j--;
                        }
                    }

                    if (shouldBreakTriangle)
                    {
                        additionalEdges = GetAdditionalEdges(intersectionWindow, intersectionWindowEdges, localVertices, epsilon);
                        newElements = GetNewElements(triangle, tetrahedron, vertices, intersectionWindow, intersectionWindowEdges, additionalEdges, epsilon);
                        if (newElements.Count != 0)
                        {
                            triangles.RemoveAt(i);
                            triangles.AddRange(newElements);
                            i--;
                            break;
                        }
                    }
                }
            }

            FindNewTetrahedronsForEntireFaces(triangles);
            FindNewTetrahedronsForEntireFaces(otherTriangles);
        }

        private static void FindNewTetrahedronsForEntireFaces(List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> triangles)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                var triangleTuple = triangles[i];
                if (triangleTuple.FromSplitted)
                    continue;

                int tetrahedronIndex = triangles.FindIndex(0, triangles.Count, triangle => triangle.FromSplitted && !triangleTuple.Triangle.Indices.Except(triangle.Tetrahedron.Indices).Any());
                if (tetrahedronIndex >= 0)
                    triangles[i] = (triangleTuple.Triangle, triangles[tetrahedronIndex].Tetrahedron, false);
            }
        }
    }
}