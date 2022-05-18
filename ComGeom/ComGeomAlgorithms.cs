using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComGeom
{
    public static class ComGeomAlgorithms
    {
        public static Vector3D? GetIntersectionPointOfSegments(Vector3D point1, Vector3D line1, Vector3D point2, Vector3D line2, double epsilon, out bool multipleIntersection)
        {
            double sqrEpsilon = epsilon * epsilon;
            double t1, t2;
            double t;

            multipleIntersection = false;

            if (line1.IsZero(epsilon) && line2.IsZero(epsilon))
            {
                return point1.SqrDistance(point2) < sqrEpsilon ? point1 : null;
            }

            var g = point2 - point1;
            var fe = Vector3D.Cross(line1, line2);
            var fg = Vector3D.Cross(g, line2);

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

        public static (bool Belongs, int BelongsToEdge, int EqualToVertex) PointBelongsToTriangleInfo(Vector3D point,
                                                                                                      Vector3D[] triangleVertices,
                                                                                                      Vector3D[] triangleEdges,
                                                                                                      double epsilon)
        {
            var vertexCount = triangleVertices.Length;
            if (vertexCount != 3)
                throw new ArgumentException("Vertices length isn't equal to 3");

            if (triangleEdges.Length != vertexCount)
                throw new ArgumentException("Edges length isn't equal to 3");

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

        public static List<Vector3D> GetIntersectionWindow(Vector3D[] vertices1, (int VertexLocalIndex, (bool Belongs, int BelongsToEdge, int EqualToVertex) info)[] vertex1Infos,
                                                           Vector3D[] vertices2, (int VertexLocalIndex, (bool Belongs, int BelongsToEdge, int EqualToVertex) info)[] vertex2Infos,
                                                           List<Vector3D> intersectionPoints,
                                                           double epsilon)
        {
            double sqrEpsilon = epsilon * epsilon;

            void AddUniqueInsideVertices(Vector3D[] vertices,
                                         (int VertexLocalIndex, (bool Belongs, int BelongsToEdge, int EqualToVertex) info)[] vertexInfos,
                                         List<Vector3D> destination)
            {
                foreach (var vertexInfo in vertexInfos)
                {
                    var vertex = vertices[vertexInfo.VertexLocalIndex];
                    if (vertexInfo.info.Belongs && !destination.Exists(v => v.SqrDistance(vertex) < sqrEpsilon))
                    {
                        destination.Add(vertex);
                    }
                }
            }

            if (vertex1Infos.All(vertexInfo => vertexInfo.info.Belongs))
            {
                return vertices1.ToList();
            }

            if (vertex2Infos.All(vertexInfo => vertexInfo.info.Belongs))
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

        private static List<(int LocalIndex, int IntersectionWindowIndex, Vector3D Line)> GetNewEdges(List<Vector3D> intersectionWindow,
                                                                                                      (int StartIndex, int EndIndex, Vector3D Line)[] intersectionWindowEdges,
                                                                                                      Vector3D[] localVertices,
                                                                                                      double epsilon)
        {
            double sqrEpsilon = epsilon * epsilon;

            int localVertexCount = localVertices.Length;
            int intersectionWindowCount = intersectionWindow.Count;
            int i;

            bool multipleIntersection;

            var additionalEdges = new List<(int LocalIndex, int FigureIndex, Vector3D Line)>();

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
                        var intersectPoint = GetIntersectionPointOfSegments(currentPoint, edge, localVertices[additionalEdge.LocalIndex], additionalEdge.Line, epsilon, out multipleIntersection);

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

        private static Element CopyElementWithNewIndices(Element element, int[] indices)
        {
            return new Element(element.Type, element.MaterialNumber, indices);
        }

        public static List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> TriangulateTriangle(Element triangle, Element tetrahedron,
                                                                                                           Vector3D[] triangleVertices,
                                                                                                           List<Vector3D> meshVertices,
                                                                                                           List<Vector3D> intersectionWindow,
                                                                                                           (int StartIndex, int EndIndex, Vector3D Line)[] intersectionWindowEdges,
                                                                                                           double epsilon)
        {
            double sqrEpsilon = epsilon * epsilon;
            int index;

            int intersectionWindowCount = intersectionWindow.Count;

            if(intersectionWindowCount < 1)
                return new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>() { new(triangle, tetrahedron, false) };

            List<int> windowIndices = intersectionWindow.Select(windowVertex => meshVertices.FindIndex(0, meshVertices.Count, vertex => vertex.SqrDistance(windowVertex) < sqrEpsilon)).ToList();

            if(intersectionWindowCount <= 3 && !windowIndices.Except(triangle.Indices).Any())
                return new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> { new(triangle, tetrahedron, false) };

            var newEdges = GetNewEdges(intersectionWindow, intersectionWindowEdges, triangleVertices, epsilon);

            int tetrahedronTopVertexIndex = tetrahedron.Indices.Except(triangle.Indices).Single();
            var newElements = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>();

            for (index = 1; index < intersectionWindowCount - 1; index++)
            {
                newElements.Add((CopyElementWithNewIndices(triangle, new int[] { windowIndices[0], windowIndices[index], windowIndices[index + 1] }),
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

                        newElements.Add((CopyElementWithNewIndices(triangle, new int[] { index1, triangle.Indices[k], index2 }),
                                         CopyElementWithNewIndices(tetrahedron, new int[] { index1, triangle.Indices[k], index2, tetrahedronTopVertexIndex }),
                                         true));
                    }
                }
            }

            Vector3D[] tempTriangleVertices = new Vector3D[3];
            Vector3D[] tempTriangleEdges = new Vector3D[3];

            for (int k = 0; k < triangle.Indices.Length; k++)
            {
                Vector3D edge = triangleVertices[(k + 1) % 3] - triangleVertices[k];

                for (index = 0; index < intersectionWindowCount; index++)
                {
                    if (newEdges.Exists(edge => edge.LocalIndex == k && edge.IntersectionWindowIndex == index) &&
                        newEdges.Exists(edge => edge.LocalIndex == (k + 1) % 3 && edge.IntersectionWindowIndex == index))
                    {
                        // Add new triangle
                        int index1 = windowIndices[index];
                        if (Vector3D.Cross(meshVertices[index1] - triangleVertices[k], triangleVertices[(k + 1) % 3] - meshVertices[index1]).SqrNorm < sqrEpsilon ||
                            triangle.Indices.Contains(index1))
                            continue;

                        if (newElements.Exists(element => element.Triangle.Indices.Contains(k) && element.Triangle.Indices.Contains((k + 1) % 3)))
                            continue;

                        if (intersectionWindow.Exists(point => Vector3D.Cross(point - meshVertices[k], edge).SqrNorm < sqrEpsilon))
                            continue;

                        tempTriangleVertices[0] = triangleVertices[k];
                        tempTriangleVertices[1] = meshVertices[index1];
                        tempTriangleVertices[2] = triangleVertices[(k + 1) % 3];

                        tempTriangleEdges[0] = tempTriangleVertices[1] - tempTriangleVertices[0];
                        tempTriangleEdges[1] = tempTriangleVertices[2] - tempTriangleVertices[1];
                        tempTriangleEdges[2] = tempTriangleVertices[0] - tempTriangleVertices[2];

                        if (intersectionWindow.Any(point =>
                                                            {
                                                                var info = PointBelongsToTriangleInfo(point, tempTriangleVertices, tempTriangleEdges, epsilon);
                                                                return info.Belongs && info.EqualToVertex == -1;
                                                            }
                                                  )
                            )
                        {
                            continue;
                        }

                        newElements.Add((CopyElementWithNewIndices(triangle, new int[] { triangle.Indices[k], index1, triangle.Indices[(k + 1) % 3] }),
                                         CopyElementWithNewIndices(tetrahedron, new int[] { triangle.Indices[k], index1, triangle.Indices[(k + 1) % 3], tetrahedronTopVertexIndex }),
                                         true));
                    }
                }
            }

            return newElements;
        }
    }
}