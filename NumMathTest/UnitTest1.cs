using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NumMath;

namespace NumMathTest
{
    [TestClass]
    public class BSGSolverTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            SparseMatrix matrix = new SparseMatrix(3, new int[] { 0, 0, 1, 3 }, new int[] { 0, 0, 1 }, new double[] { 5, 22, 73 }, new double[] { 10, 20, 50 }, new double[] { 8, 9, 28 });
            Vector b = new Vector(85, 202, 284);
            Vector start = new Vector(3);
            int a = SoLESolver.SolveBiCGM_LU(matrix, b, start);
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
