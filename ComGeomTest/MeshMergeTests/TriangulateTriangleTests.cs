using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using System.IO;
using System.Reflection;
using System.Globalization;

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

        static TriangulateTriangleTests()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

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

        private static List<(Element Triangle, Element Tetrahedron)> GenerateTheory(int tetrahedronTopVertexIndex, int materialIndex, params int[][] triangleIndices)
        {
            List<(Element Triangle, Element Tetrahedron)> theory = new List<(Element Triangle, Element Tetrahedron)>();

            int indicesLenght = 3;
            for (int i = 0; i < triangleIndices.Length; i++)
            {
                int[] indices = triangleIndices[i];

                int[] tetrahedronIndices = new int[indicesLenght + 1];
                indices.CopyTo(tetrahedronIndices, 0);
                tetrahedronIndices[indicesLenght] = tetrahedronTopVertexIndex;

                theory.Add(new(new Element(ElementType.Triangle, materialIndex, indices),
                               new Element(ElementType.Tetrahedron, materialIndex, tetrahedronIndices)));
            }

            return theory;
        }

        private static List<(Element Triangle, Element Tetrahedron)>  GetActualResult(Vector3D[] triangle, Vector3D tetrahedronTopVertex, List<Vector3D> intersectionWindow, out List<Vector3D> meshVertices)
        {
            GenerateData(triangle, tetrahedronTopVertex, intersectionWindow,
                         out Element triangleElement, out Element tetrahedronElement, out Vector3D[] triangleVertices,
                         out meshVertices, out var intersectionWindowEdges);

            return ComGeomAlgorithms.TriangulateTriangle(triangleElement, tetrahedronElement,
                                                         triangleVertices,
                                                         meshVertices,
                                                         intersectionWindow,
                                                         intersectionWindowEdges,
                                                         epsilon);
        }

        private static bool IsActualEqualToTheory(List<(Element Triangle, Element Tetrahedron)> actualResult,
                                                  List<(Element Triangle, Element Tetrahedron)> theoryResult)
        {
            return actualResult.Count == theoryResult.Count && actualResult.All(actualTuple => theoryResult.Exists(theoryTuple => actualTuple.Triangle.Equals(theoryTuple.Triangle) &&
                                                                                                                                  actualTuple.Tetrahedron.Equals(theoryTuple.Tetrahedron)));
        }

        private static void DumpTest(string testname, Vector3D[] triangle, List<Vector3D> intersectionWindow,
                                     List<Vector3D> meshVertices,
                                     List<(Element Triangle, Element Tetrahedron)> theory,
                                     List<(Element Triangle, Element Tetrahedron)> actual)
        {
            string filename = $"C:\\Users\\shikh\\Desktop\\telma\\triangulateTriangleTests\\Tests\\{testname}.txt";
            using(StreamWriter sw = new StreamWriter(filename))
            {
                foreach(var vertex in triangle)
                    sw.WriteLine(vertex);

                sw.WriteLine(intersectionWindow.Count);
                foreach(var vertex in intersectionWindow)
                    sw.WriteLine(vertex);

                sw.WriteLine(meshVertices.Count);
                foreach (var vertex in meshVertices)
                    sw.WriteLine(vertex);

                sw.WriteLine(theory.Count);
                foreach (var tuple in theory)
                    sw.WriteLine(string.Join(" ", tuple.Triangle.Indices));

                foreach (var tuple in actual)
                    sw.WriteLine(string.Join(" ", tuple.Triangle.Indices));
            }
        }

        [Test]
        public void PointOnEdge()
        {
            List<Vector3D> intersectionWindow = new List<Vector3D>() { new Vector3D(0, 0.5, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 4 }, new int[] { 4, 1, 2 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, intersectionWindow, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, intersectionWindow, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void PointEqualsToVertex()
        {
            List<Vector3D> intersectionWindow = new List<Vector3D>() { masterTriangle[2] };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 2 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, intersectionWindow, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, intersectionWindow, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void PointIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.2, 0.2, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 4 }, new int[] { 1, 2, 4 }, new int[] { 2, 0, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnOneEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.3, 0.7, 0), new Vector3D(0.7, 0.3, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 5 }, new int[] { 0, 4, 5 }, new int[] { 0, 4, 2 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void OnePointEqualsToVertexAnotherOneIsOnOppositeEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.3, 0.7, 0), masterTriangle[0] };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 2, 4 }, new int[] { 0, 1, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // --
        [Test]
        public void OnePointEqualsToVertexAnotherOneIsOnSameEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.3, 0.7, 0), masterTriangle[2] };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 2, 4 }, new int[] { 0, 1, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }
        // --

        [Test]
        public void TwoPointsAreOnDifferentEdges()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.6, 0.4, 0), new Vector3D(0, 0.4, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 4 }, new int[] { 0, 4, 5 }, new int[] { 2, 4, 5 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void OnePointEqualsToVertexAndAnotherOneIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 1, 0), new Vector3D(0.2, 0.2, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 2 }, new int[] { 0, 1, 4 }, new int[] { 1, 2, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsEqualToVertices()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[0], masterTriangle[1] };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 2 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void PointsEqualToVertices()
        {
            List<Vector3D> window = new List<Vector3D>(masterTriangle);
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 2 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // 19.05.2022
        [Test]
        public void TwoPointsEqualToVerticesThirdIsOnEdge()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[0], new Vector3D(0.5, 0.5, 0), masterTriangle[2] };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 2 }, new int[] { 0, 1, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsEqualToVerticesThirdIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[0], new Vector3D(0.4, 0.4, 0), masterTriangle[2] };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 4 }, new int[] { 1, 2, 4 }, new int[] { 2, 0, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }
        
        // не оформлен
        [Test]
        public void OnePointEqualToVertexTwoAreOnEdges()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[2], new Vector3D(0, 0.5, 0), new Vector3D(0.5, 0.5, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 5 }, new int[] { 2, 4, 5 }, new int[] { 0, 4, 5 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // не оформлен
        [Test]
        public void OnePointEqualToVertexSecondIsOnEdgeThirdIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[2], new Vector3D(0, 0.5, 0), new Vector3D(0.2, 0.4, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 5 }, 
                                                          new int[] { 1, 2, 5 },
                                                          new int[] { 2, 4, 5 },
                                                          new int[] { 0, 4, 5 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // не оформлен
        [Test]
        public void TwoPointsAreOnOneEdgeThirdIsOnAnother()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 0.7, 0), new Vector3D(0, 0.3, 0), new Vector3D(0.5, 0.5, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 6 },
                                                          new int[] { 2, 4, 6 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 0, 5, 6 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // не оформлен
        [Test]
        public void TwoPointsAreOnOneEdgeThirdIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 0.7, 0), new Vector3D(0, 0.3, 0), new Vector3D(0.2, 0.5, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 1, 6 },
                                                          new int[] { 1, 6, 4 },
                                                          new int[] { 1, 2, 4 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 0, 5, 6 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void ThreePointsAreOnDifferentEdges()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 0.5, 0), new Vector3D(0.5, 0, 0), new Vector3D(0.5, 0.5, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 5 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 2, 4, 6 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnDifferentEdgesAnotherOneIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 0.5, 0), new Vector3D(0.3, 0.2, 0), new Vector3D(0.5, 0.5, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 5 },
                                                          new int[] { 0, 1, 5 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 2, 4, 6 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // не оформлен
        [Test]
        public void OnePointIsEqualToVertexOtherTwoAreInside()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[2], new Vector3D(0.2, 0.2, 0), new Vector3D(0.4, 0.2, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 5 },
                                                          new int[] { 0, 1, 5 },
                                                          new int[] { 1, 2, 5 },
                                                          new int[] { 2, 4, 5 },
                                                          new int[] { 2, 0, 4 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TriangleIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.2, 0.2, 0), new Vector3D(0.4, 0.2, 0), new Vector3D(0.2, 0.4, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 6 },
                                                          new int[] { 0, 4, 5 },
                                                          new int[] { 0, 1, 5 },
                                                          new int[] { 0, 2, 6 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 1, 2, 6 },
                                                          new int[] { 4, 5, 6 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        // не оформлен
        [Test]
        public void OnePointIsEqualToVertexTwoAreOnDifferentEdgesOneIsInside()
        {
            List<Vector3D> window = new List<Vector3D>() { masterTriangle[0], new Vector3D(0, 0.3, 0), new Vector3D(0.3, 0.3, 0), new Vector3D(0.3, 0, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 5 },
                                                          new int[] { 0, 5, 6 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 1, 2, 5 },
                                                          new int[] { 2, 4, 5 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnOneEdgeTwoAreOnAnother()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 0.2, 0), new Vector3D(0.8, 0.2, 0), new Vector3D(0.4, 0.6, 0), new Vector3D(0, 0.6, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 5 },
                                                          new int[] { 0, 1, 5 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 4, 6, 7 },
                                                          new int[] { 2, 6, 7 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnOneEdgeTwoAreOnDifferent()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0, 0.2, 0), new Vector3D(0.4, 0, 0), new Vector3D(0.4, 0.6, 0), new Vector3D(0, 0.6, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 5 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 4, 6, 7 },
                                                          new int[] { 2, 6, 7 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void TwoPointsAreOnOneEdgeTwoAreOnDifferentOnePointEqualsToVertex()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.2, 0, 0), new Vector3D(0.4, 0, 0), new Vector3D(0.4, 0.6, 0), masterTriangle[2], new Vector3D(0, 0.6, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 7 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 4, 6, 2 },
                                                          new int[] { 4, 2, 7 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }

        [Test]
        public void ThreePairsOfPointsAreOnDifferentEdges()
        {
            List<Vector3D> window = new List<Vector3D>() { new Vector3D(0.2, 0, 0), new Vector3D(0.6, 0, 0), 
                                                           new Vector3D(0.8, 0.2, 0), new Vector3D(0.4, 0.6, 0), 
                                                           new Vector3D(0, 0.6, 0), new Vector3D(0, 0.2, 0) };
            var theory = GenerateTheory(3, materialIndex, new int[] { 0, 4, 9 },
                                                          new int[] { 1, 5, 6 },
                                                          new int[] { 4, 5, 6 },
                                                          new int[] { 4, 6, 7 },
                                                          new int[] { 4, 7, 8 },
                                                          new int[] { 4, 8, 9 },
                                                          new int[] { 2, 7, 8 });

            var actual = GetActualResult(masterTriangle, tetrahedronTopVertex, window, out List<Vector3D> meshVertices);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, masterTriangle, window, meshVertices, theory, actual);

            Assert.IsTrue(IsActualEqualToTheory(actual, theory));
        }
    }
}