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
                        vertexInfos[index] = (index, ComGeomAlgorithms.PointBelongsToTriangleInfo(localVertices[index], otherLocalVertices, otherTriangleEdges, epsilon));
                        otherVertexInfos[index] = (index, ComGeomAlgorithms.PointBelongsToTriangleInfo(otherLocalVertices[index], localVertices, triangleEdges, epsilon));
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
                            var point = ComGeomAlgorithms.GetIntersectionPointOfSegments(otherLocalVertices[n], otherTriangleEdges[n], localVertices[m], triangleEdges[m], epsilon, out _);

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

                    var intersectionWindow = ComGeomAlgorithms.GetIntersectionWindow(localVertices, vertexInfos,
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

                    if (shouldBreakOtherTriangle && (newElements = ComGeomAlgorithms.TriangulateTriangle(otherTriangle, otherTetrahedron,
                                                                                                         otherLocalVertices, otherVertices,
                                                                                                         intersectionWindow, intersectionWindowEdges,
                                                                                                         epsilon)).Count != 0)
                    {
                            otherTriangles.RemoveAt(j);
                            otherTriangles.AddRange(newElements);
                            j--;
                    }

                    if (shouldBreakTriangle && (newElements = ComGeomAlgorithms.TriangulateTriangle(triangle, tetrahedron,
                                                                                                    localVertices, vertices,
                                                                                                    intersectionWindow, intersectionWindowEdges,
                                                                                                    epsilon)).Count != 0)
                    {
                            triangles.RemoveAt(i);
                            triangles.AddRange(newElements);
                            i--;
                            break;
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