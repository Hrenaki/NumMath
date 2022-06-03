using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Globalization;

using ComGeom;
using ComGeom.Common;
using ComGeom.Meshes;

using NUnit.Framework;
using System.IO;

namespace ComGeomTest.MeshMergeTests
{
    public class MeshMergeTests
    {
        private static IMeshIO meshIO;

        private static readonly double epsilon = 1E-7;
        private static readonly double sqrEpsilon = epsilon * epsilon;

        static MeshMergeTests()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            meshIO = MeshIOFactory.CreateObjectFormatIO();
        }

        private static bool MeshEquals(IMesh3D mesh1, IMesh3D mesh2)
        {
            if (ReferenceEquals(mesh1, mesh2))
                return true;

            if (mesh2 == null)
                return false;

            double sqrEpsilon = epsilon * epsilon;
            if (mesh1.Vertices.Count != mesh2.Vertices.Count)
                return false;

            for (int i = 0; i < mesh1.Vertices.Count; i++)
            {
                if (mesh1.Vertices[i].SqrDistance(mesh2.Vertices[i]) >= sqrEpsilon)
                    return false;
            }

            if (mesh1.Elements.Count != mesh2.Elements.Count || mesh1.Elements.Any(element => !mesh2.Elements.Any(otherElement => element.Equals(otherElement))))
                return false;

            foreach (var pair in mesh1.BoundaryMaterialNames)
            {
                if (mesh2.BoundaryMaterialNames[pair.Key] != pair.Value)
                    return false;
            }

            foreach (var pair in mesh1.VolumeMaterialNames)
            {
                if (mesh2.VolumeMaterialNames[pair.Key] != pair.Value)
                    return false;
            }

            return true;
        }

        private static void DumpTest(string testname, IMesh3D mesh1, IMesh3D mesh2, IMesh3D actual, IMesh3D theory)
        {
            string directory = $"C:\\Users\\shikh\\Desktop\\telma\\meshMergeTests\\{testname}";

            if(!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            meshIO.Write(mesh1, Path.Combine(directory, "mesh1.txt"));
            meshIO.Write(mesh2, Path.Combine(directory, "mesh2.txt"));
            meshIO.Write(actual, Path.Combine(directory, "actual.txt"));
            meshIO.Write(theory, Path.Combine(directory, "theory.txt"));
        }

        [Test]
        public void VertexEqualsToVertex()
        {
            IMesh3D mesh1 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, -1, 0), new Vector3D(1, -1, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D mesh2 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(0, 0, 1), new Vector3D(-1, -1, 2), new Vector3D(1, -1, 2), new Vector3D(0, 1, 2) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D theory = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, -1, 0), new Vector3D(1, -1, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1),
                                                                             new Vector3D(-1, -1, 2), new Vector3D(1, -1, 2), new Vector3D(0, 1, 2) },
                                                      new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 4, 5, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 3, 4, 5 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 3, 4, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 3, 5, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 5, 6 })},
                                                      new Dictionary<int, string>() { { 0, "steel" } },
                                                      new Dictionary<int, string>() { { 0, "steel" } });
            IMesh3D actual = mesh1.MergeWith(mesh2, 0, 0);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, mesh1, mesh2, actual, theory);

            Assert.IsTrue(MeshEquals(actual, theory));
        }

        [Test]
        public void VertexInsideTriangle()
        {
            IMesh3D mesh1 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, -1, 1), new Vector3D(1, -1, 1), new Vector3D(0, 1, 1), new Vector3D(0, 0, 0) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D mesh2 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(0, 0, 1), new Vector3D(-1, -1, 2), new Vector3D(1, -1, 2), new Vector3D(0, 1, 2) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D theory = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, -1, 1), new Vector3D(1, -1, 1), new Vector3D(0, 1, 1), new Vector3D(0, 0, 0),
                                                                             new Vector3D(0, 0, 1), new Vector3D(-1, -1, 2), new Vector3D(1, -1, 2), new Vector3D(0, 1, 2) },
                                                      new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 3, 4 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 1, 2, 3, 4 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 0, 2, 3, 4 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 4, 5, 6, 7 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 5, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 6, 7 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 5, 7 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 5, 6, 7 })
                                                                            },
                                                      new Dictionary<int, string>() { { 0, "steel" } },
                                                      new Dictionary<int, string>() { { 0, "steel" } });
            IMesh3D actual = mesh1.MergeWith(mesh2, 0, 0);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, mesh1, mesh2, actual, theory);

            Assert.IsTrue(MeshEquals(actual, theory));
        }

        [Test]
        public void ThreeEdgesIntersectDifferentOther()
        {
            IMesh3D mesh1 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, 0, 1), new Vector3D(1, 0, 1), new Vector3D(0, 2, 1), new Vector3D(0, 0, 0) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D mesh2 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(0, -0.8, 1), new Vector3D(1, 1.2, 1), new Vector3D(-1, 1.2, 1), new Vector3D(0, 0, 2) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D theory = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, 0, 1), new Vector3D(1, 0, 1), new Vector3D(0, 2, 1), new Vector3D(0, 0, 0),
                                                                             new Vector3D(0.4, 0, 1), new Vector3D(0.7, 0.6, 1), new Vector3D(0.4, 1.2, 1), new Vector3D(-0.4, 1.2, 1),
                                                                             new Vector3D(-0.7, 0.6, 1), new Vector3D(-0.4, 0, 1), new Vector3D(0, -0.8, 1), new Vector3D(1, 1.2, 1),
                                                                             new Vector3D(-1, 1.2, 1), new Vector3D(0, 0, 2)},
                                                      new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 3, 0, 8, 9 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 1, 4, 5 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 4, 5, 6 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 2, 6, 7 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 4, 6, 7 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 4, 7, 8 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 4, 8, 9 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 4, 5, 6 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 5, 6, 11 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 4, 6, 7 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 4, 7, 8 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 7, 8, 12 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 4, 8, 9 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 13, 4, 9, 10 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 3, 5 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 5, 3, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 6, 3, 2 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 2, 3, 7 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 7, 3, 8 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 8, 3, 0 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 3, 9 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 9, 3, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 3, 1 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 4, 5 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 11, 5, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 2, 6, 7 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 12, 7, 8 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 8, 9 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 10, 9, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 10, 13, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 13, 5 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 5, 13, 11 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 11, 13, 6 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 6, 13, 7 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 7, 13, 12 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 12, 13, 8 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 8, 13, 9 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 9, 13, 10 })
                                                                            },
                                                      new Dictionary<int, string>() { { 0, "steel" } },
                                                      new Dictionary<int, string>() { { 0, "steel" } });
            IMesh3D actual = mesh1.MergeWith(mesh2, 0, 0);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, mesh1, mesh2, actual, theory);

            Assert.IsTrue(MeshEquals(actual, theory));
        }

        [Test]
        public void TouchByTriangles()
        {
            IMesh3D mesh1 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, 0, 1), new Vector3D(1, 0, 1), new Vector3D(0, 2, 1), new Vector3D(0, 1, 0) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D mesh2 = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, 0, 1), new Vector3D(0, 0, 1), new Vector3D(0, 2, 1), new Vector3D(0, 1, 2) },
                                                     new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 0, 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 2 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 1, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 3 }),
                                                                           new Element(ElementType.Triangle, 0, new int[]{ 0, 2, 3 })},
                                                     new Dictionary<int, string>() { { 0, "steel" } },
                                                     new Dictionary<int, string>() { { 0, "steel" } });

            IMesh3D theory = MeshFactory.Create3DMesh(new List<Vector3D>() { new Vector3D(-1, 0, 1), new Vector3D(1, 0, 1), new Vector3D(0, 2, 1), new Vector3D(0, 1, 0),
                                                                             new Vector3D(0, 0, 1), new Vector3D(0, 1, 2) },
                                                      new List<Element>() { new Element(ElementType.Tetrahedron, 0, new int[] { 3, 0, 2, 4 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 3, 1, 2, 4 }),
                                                                            new Element(ElementType.Tetrahedron, 0, new int[] { 5, 0, 2, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 3, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 3, 1 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 3, 2 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 2, 3, 0 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 1, 2, 4 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 4, 5, 2 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 2, 5, 0 }),
                                                                            new Element(ElementType.Triangle, 0, new int[]{ 0, 5, 4 })
                                                                            },
                                                      new Dictionary<int, string>() { { 0, "steel" } },
                                                      new Dictionary<int, string>() { { 0, "steel" } });
            IMesh3D actual = mesh1.MergeWith(mesh2, 0, 0);

            DumpTest(MethodBase.GetCurrentMethod()!.Name, mesh1, mesh2, actual, theory);

            Assert.IsTrue(MeshEquals(actual, theory));
        }
    }
}