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
            SparseMatrix matrix = new SparseMatrix(3, new int[] { 0, 0, 1, 2 }, new int[] { 0, 1 }, new double[] { 1, 2, 3 }, new double[] { 5, 7 }, new double[] { 4, 6 });
            Vector b = new Vector(11, 29, 21);
            Vector start = new Vector(3);
            SoLESolver.SolveBSG(matrix, b, start);
        }
    }
}
