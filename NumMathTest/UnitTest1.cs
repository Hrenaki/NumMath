using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NumMath;
using HarmonicFEM;
using EParser;

namespace NumMathTest
{
    [TestClass]
    public class BSGSolverTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Mesh mesh = new Mesh(new double[] { 0, 2 }, new double[] { 0, 2 },
                new DischargeInfo[] { new DischargeInfo(1, 2) }, new DischargeInfo[] { new DischargeInfo(1, 2) },
                new int[] { }, new double[] { 1 }, new double[] { 1 }, new double[] { 1 }, 1,
                new EParser.Func[,] { { t => -2, t => 0 } }, new BoundaryCondition[] { new BoundaryCondition(BoundaryConditionType.First, new int[] { 0, 1, 3 }, t => 1, t => 1) ,
                                                                                        new BoundaryCondition(BoundaryConditionType.Second, new int[]{ 3, 2, 0}, t => 0, t => 0)});
            HarmonicProblem p = new HarmonicProblem(mesh);
            p.build();

            ProfileMatrix profileMatrix = p.globalMatrix.Cast<ProfileMatrix>();
            p.globalMatrix.PrintPortait();
            profileMatrix.PrintPotrait();

            Vector s = new Vector(p.globalB);
            Vector st = new Vector(p.globalB.size);

            SoLESolver.SolveBiCGM(p.globalMatrix, p.globalB, st);
            SoLESolver.SolveLU(profileMatrix, s);
        }
        [TestMethod]
        public void TestLU_FullMatrix()
        {
            ProfileMatrix matrix = new ProfileMatrix(3, new int[] { 0, 0, 1, 3 }, new double[] { 5, 22, 73 }, new double[] { 10, 20, 50 }, new double[] { 8, 9, 28 });
            Vector b = new Vector(85, 202, 284);
            SoLESolver.SolveLU(matrix, b);
        }
    }
}