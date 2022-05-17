using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComGeom;

namespace ComGeomTest.MeshMergeTests
{
    public class GetIntersectionWindowTests
    {
        private static readonly double epsilon = 1E-7;
        private static readonly double sqrEpsilon = epsilon * epsilon;

        private static Vector3D[] GetEdges(Vector3D[] vertices)
        {
            return new Vector3D[]
            {
                vertices[1] - vertices[0],
                vertices[2] - vertices[1],
                vertices[0] - vertices[2]
            };
        }

        private static void GenerateData(Vector3D[] vertices, Vector3D[] otherVertices, 
                                         out (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[] vertex1Infos,
                                         out (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[] vertex2Infos,
                                         out List<Vector3D> intersectionPoints)
        {
            Vector3D[] edges = GetEdges(vertices);
            Vector3D[] otherEdges = GetEdges(otherVertices);

            vertex1Infos = new (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[3];
            vertex2Infos = new (int vertexLocalIndex, (bool belongs, int belongsToEdge, int equalToVertex) info)[3];

            for(int i = 0; i < vertices.Length; i++)
            {
                vertex1Infos[i] = (i, ComGeomAlgorithms.PointBelongsToTriangleInfo(vertices[i], otherVertices, otherEdges, epsilon));
                vertex2Infos[i] = (i, ComGeomAlgorithms.PointBelongsToTriangleInfo(otherVertices[i], vertices, edges, epsilon));
            }

            intersectionPoints = new List<Vector3D>();
            for (int n = 0; n < vertices.Length; n++)
            {
                for (int m = 0; m < vertices.Length; m++)
                {
                    var point = ComGeomAlgorithms.GetIntersectionPointOfSegments(otherVertices[n], otherEdges[n], vertices[m], edges[m], epsilon, out _);

                    if (point.HasValue && !intersectionPoints.Exists(p => p.SqrDistance(point.Value) < sqrEpsilon))
                    {
                        intersectionPoints.Add(point.Value);
                    }
                }
            }
        }

        private static bool IsIntersectionWindowCorrect(List<Vector3D> actual, List<Vector3D> theory)
        {
            int startIndex = theory.FindIndex(vertex => vertex.SqrDistance(actual[0]) < sqrEpsilon);
            int pointCount = theory.Count;
            if (startIndex < 0 || actual.Count != pointCount)
                return false;

            if (actual.Count < 2)
                return true;

            if (actual[1].SqrDistance(theory[(startIndex + 1) % pointCount]) >= sqrEpsilon)
            {
                theory.Reverse();
                startIndex = pointCount - startIndex - 1;
            }

            bool pass = true;
            for (int i = 0; i < pointCount; i++)
            {
                if (actual[i].SqrDistance(theory[(startIndex + i) % pointCount]) >= sqrEpsilon)
                {
                    pass = false;
                    break;
                }
            }

            return pass;
        }

        [Test]
        public void TrianglesDontIntersect()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(2, 0, 0),
                new Vector3D(3, 0, 0),
                new Vector3D(2, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> intersectionWindow = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            Assert.IsEmpty(intersectionWindow);
        }

        [Test]
        public void NoVertexIsInsideTwoEdgesIntersect()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0.5, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(-0.5, 0, 0),
                new Vector3D(1, 0.6, 0),
                new Vector3D(-0.5, 0.6, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);
            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { new Vector3D(0.125, 0.25, 0), new Vector3D(0.75, 0.5, 0), new Vector3D(0.7, 0.6, 0), new Vector3D(0.3, 0.6, 0) };
            Assert.True(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void NoVertexIsInsideThreeEdgesIntersect()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0.5, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, -0.4, 0),
                new Vector3D(1, 0.6, 0),
                new Vector3D(0, 0.6, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);
            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { new Vector3D(0.3, 0, 0), new Vector3D(0.7, 0, 0), 
                                                           new Vector3D(0.85, 0.3, 0), new Vector3D(0.7, 0.6, 0),
                                                           new Vector3D(0.3, 0.6, 0), new Vector3D(0.15, 0.3, 0) };
            Assert.True(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexIsEqualToOther()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(1, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            Assert.True(actual.Count == 1 && actual[0].SqrDistance(otherVertices[0]) < sqrEpsilon);
        }

        [Test]
        public void OneVertexBelongsToEdge()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0.5, 0),
                new Vector3D(1.5, 0.5, 0),
                new Vector3D(0.5, 1.5, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0] };
            Assert.True(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexIsInsideAndEdgesIntersectOneOther()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.25, 0.25, 0),
                new Vector3D(1.25, 0.25, 0),
                new Vector3D(0.25, 1.25, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], new Vector3D(0.75, 0.25, 0), new Vector3D(0.25, 0.75, 0) };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexIsInsideAndEdgesIntersectDifferentOther()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.25, 0.25, 0),
                new Vector3D(1.5, -0.25, 0),
                new Vector3D(0.25, 1.25, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], new Vector3D(0.875, 0, 0), new Vector3D(1, 0, 0), new Vector3D(0.25, 0.75, 0) };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexEqualsToOtherAndOneEdgeIntersectsOther()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0, 1, 0),
                new Vector3D(1.2, -0.5, 0),
                new Vector3D(1.25, 1.25, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], new Vector3D(0.8, 0, 0), new Vector3D(1, 0, 0) };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesEqualToOther()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0, 1, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(1, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], otherVertices[1] };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesIsOnOtherEdge()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.2, 0.8, 0),
                new Vector3D(0.8, 0.2, 0),
                new Vector3D(1, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], otherVertices[1] };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesIsOnDifferentOtherEdges()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 1, 0),
                new Vector3D(1.5, 1, 0),
                new Vector3D(1, 3, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], otherVertices[1], vertices[2] };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesIsOnOppositeOtherEdge()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(0, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0, 0.5, 0),
                new Vector3D(0, 1.5, 0),
                new Vector3D(1, 1.5, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], otherVertices[1], new Vector3D(0.5, 1.5, 0), new Vector3D(0.75, 1.25, 0) };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesIsOnOppositeOtherEdgeAndEdgesIntersectDifferentOther()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0, 0),
                new Vector3D(1.5, 0, 0),
                new Vector3D(1, 3, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], new Vector3D(0.75, 1.5, 0), new Vector3D(1, 2, 0), new Vector3D(1.25, 1.5, 0), otherVertices[1] };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesIsOnOppositeDifferentOtherEdges()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(0, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1.5, 0),
                new Vector3D(1, 1.5, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>() { otherVertices[0], otherVertices[1], new Vector3D(0.5, 1.5, 0), new Vector3D(1, 1, 0) };

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TrianglesAreEqual()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(0, 2, 0)
            };

            GenerateData(vertices, vertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, vertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(vertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesAreEqualAndOneIsOnOtherEdge()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(0.5, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexIsEqualAndTwoAreOnOtherEdge()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0, 0),
                new Vector3D(1.5, 0, 0),
                new Vector3D(1, 2, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexIsEqualAndTwoAreOnDifferentOtherEdges()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 1, 0),
                new Vector3D(1.5, 1, 0),
                new Vector3D(1, 2, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void AllVerticesAreInsideAndTwoAreOnOtherEdge()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0, 0),
                new Vector3D(1.5, 0, 0),
                new Vector3D(0.5, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void AllVerticesAreOnDifferentOtherEdges()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(1, 0, 0),
                new Vector3D(1.5, 1, 0),
                new Vector3D(0.5, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesAreEqualAndOneVertexIsInside()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesAreOnOtherEdgeAndOneVertexIsInside()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0, 0),
                new Vector3D(1.5, 0, 0),
                new Vector3D(1, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void TwoVerticesAreOnDifferentOtherEdgeAndOneVertexIsInside()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 1, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(1, 1, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void OneVertexIsOnOtherEdgeAndTwoVertexIsInside()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0.5, 0),
                new Vector3D(1.5, 0.5, 0),
                new Vector3D(1, 2, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }

        [Test]
        public void AllVerticesAreInside()
        {
            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(0, 0, 0),
                new Vector3D(2, 0, 0),
                new Vector3D(1, 2, 0)
            };

            Vector3D[] otherVertices = new Vector3D[]
            {
                new Vector3D(0.5, 0.5, 0),
                new Vector3D(1.5, 0.5, 0),
                new Vector3D(1, 1.5, 0)
            };

            GenerateData(vertices, otherVertices, out var vertexInfo, out var otherVertexInfo, out List<Vector3D> intersectionPoints);

            List<Vector3D> actual = ComGeomAlgorithms.GetIntersectionWindow(vertices, vertexInfo, otherVertices, otherVertexInfo, intersectionPoints, epsilon);
            List<Vector3D> theory = new List<Vector3D>(otherVertices);

            Assert.IsTrue(IsIntersectionWindowCorrect(actual, theory));
        }
    }
}