using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using ComGeom;
using ComGeom.Meshes;

namespace ComGeomTest.MeshMergeTests
{
    public class TriangulateTriangleTests
    {
        private static readonly double epsilon = 1E-7;
        private static readonly double sqrEpsilon = epsilon * epsilon;

        private static readonly Vector3D[] masterTriangle = new Vector3D[]
        {
            Vector3D.Zero,
            Vector3D.UnitX,
            Vector3D.UnitY
        };

        private static readonly Vector3D tetrahedronTopVertex = Vector3D.UnitZ;

        private static readonly int materialIndex = 5;

        private static void GenerateData(Vector3D[] triangle, Vector3D tetrahedronTopVertex, List<Vector3D> intersectionWindow,
                                         out Element triangleElement, out Element tetrahedronElement, out Vector3D[] triangleVertices, 
                                         out List<Vector3D> meshVertices, out (int StartIndex, int EndIndex, Vector3D Line)[] intersectionWindowEdges)
        {
            meshVertices = new List<Vector3D>() { triangle[0], triangle[1], triangle[2], tetrahedronTopVertex };
            
            foreach(var point in intersectionWindow)
            {
                if(!meshVertices.Exists(vertex => vertex.SqrDistance(point) < sqrEpsilon))
                    meshVertices.Add(point);
            }

            triangleElement = new Element(ElementType.Triangle, materialIndex, new int[] { 0, 1, 2 });
            tetrahedronElement = new Element(ElementType.Tetrahedron, materialIndex, new int[] { 0, 1, 2, 3 });

            triangleVertices = triangle;

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
                default:
                    throw new ArgumentException(nameof(intersectionWindowCount));
            }
        }

        private static List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> GetActualResult(Vector3D[] triangle, Vector3D tetrahedronTopVertex, List<Vector3D> intersectionWindow)
        {
            GenerateData(triangle, tetrahedronTopVertex, intersectionWindow,
                         out Element triangleElement, out Element tetrahedronElement, out Vector3D[] triangleVertices,
                         out List<Vector3D> meshVertices, out var intersectionWindowEdges);

            return ComGeomAlgorithms.TriangulateTriangle(triangleElement, tetrahedronElement,
                                                         triangleVertices,
                                                         meshVertices,
                                                         intersectionWindow,
                                                         intersectionWindowEdges,
                                                         epsilon);
        }

        private static bool IsActualEqualToTheory(List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> actualResult,
                                                  List<(Element Triangle, Element Tetrahedron, bool FromSplitted)> theoryResult)
        {
            return actualResult.Count == theoryResult.Count && actualResult.All(actualTuple => theoryResult.Exists(theoryTuple => actualTuple.Triangle.Equals(theoryTuple.Triangle) &&
                                                                                                                                  actualTuple.Tetrahedron.Equals(theoryTuple.Tetrahedron) &&
                                                                                                                                  actualTuple.FromSplitted == theoryTuple.FromSplitted));
        }

        [Test]
        public void PointOnEdge()
        {
            List<Vector3D> intersectionWindow = new List<Vector3D>() { new Vector3D(0, 0.5, 0) };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 4, 1, 2 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 4, 1, 2, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, intersectionWindow);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void PointEqualsToVertex()
        {
            List<Vector3D> intersectionWindow = new List<Vector3D>() { masterTriangle[2] };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 2 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 2, 3 }), false)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, intersectionWindow);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void PointIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.2, 0.2, 0) };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 1, 2, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 1, 2, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 2, 0, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 2, 0, 4, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnOneEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.3, 0.7, 0), new Vector3D(0.7, 0.3, 0) };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 5 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 5, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 4, 5 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 4, 5, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 4, 2 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 4, 2, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void OnePointEqualsToVertexAnotherOneIsOnOppositeEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.3, 0.7, 0), masterTriangle[0] };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 2, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 2, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 4, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void OnePointEqualsToVertexAnotherOneIsOnSameEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.3, 0.7, 0), masterTriangle[2] };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 2, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 2, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 4, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnDifferentEdges()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.6, 0.4, 0), new Vector3D(0, 0.4, 0) };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 4, 5 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 4, 5, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 2, 4, 5 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 2, 4, 5, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void OnePointEqualsToVertexAndAnotherOneIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 1, 0), new Vector3D(0.2, 0.2, 0) };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 4, 2 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 4, 2, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 4, 3 }), true),
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 1, 2, 4 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 1, 2, 4, 3 }), true)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsEqualToVertices()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[0], masterTriangle[1] };
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 2 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 2, 3 }), false)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void PointsEqualToVertices()
        {
            List<Vector3D> window = new List<Vector3D>(masterTriangle);
            var theory = new List<(Element Triangle, Element Tetrahedron, bool FromSplitted)>()
            {
                new(new Element(ElementType.Triangle, materialIndex, new int[]{ 0, 1, 2 }), new Element(ElementType.Tetrahedron, materialIndex, new int[]{ 0, 1, 2, 3 }), false)
            };

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }
    }
}
