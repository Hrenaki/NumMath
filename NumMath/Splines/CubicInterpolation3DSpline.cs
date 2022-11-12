using NumMath.Vectors;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NumMath.Splines
{
   public class CubicInterpolation3DSpline
   {
      private const int functionCount = 16;

      private double[,] localMatrix;
      private double[] localVector = new double[functionCount];

      private const double epsilon = 1E-7;

      private Func<double, double>[] basicFunctions = new Func<double, double>[]
      {
         Phi_1,
         Phi_2,
         Phi_3,
         Phi_4
      };
      private double[] basicFunctionValues = new double[functionCount];

      private double[,] G_x, M_x;
      private double[,] G_y, M_y;

      private double[] coeffs;

      private double[] xTicks;
      private double[] yTicks;

      public CubicInterpolation3DSpline()
      {
         G_x = new double[4, 4];
         M_x = new double[4, 4];

         G_y = new double[4, 4];
         M_y = new double[4, 4];
      }

      public void CreateSpline(double[] x, double[] y, Vector3[] points, double[] w, double alpha)
      {
         if (localMatrix == null)
            localMatrix = new double[functionCount, functionCount];

         xTicks = x;
         yTicks = y;

         int nodeCount = x.Length * y.Length;
         int globalFunctionCount = nodeCount * 4;

         SparseMatrix globalMatrix = GenerateGlobalMatrix(x, y);
         Vector globalVector = new Vector(globalFunctionCount);
         coeffs = new double[globalFunctionCount];

         List<Vector3> visitedPoints = new List<Vector3>();
         double ksi_y, ksi_x;
         double h_y, h_x;
         int nodeIndex = 0;

         for (int i = 0; i < y.Length - 1; i++)
         {
            h_y = y[i + 1] - y[i];

            for (int j = 0; j < x.Length - 1; j++)
            {
               h_x = x[j + 1] - x[j];

               Clear(localMatrix);
               Clear(localVector);

               for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
               {
                  Vector3 point = points[pointIndex];
                  if (visitedPoints.Contains(point))
                     continue;
                  visitedPoints.Add(point);

                  if (!IsPointInsideElement(point.X, point.Y, x[j], x[j + 1], y[i], y[i + 1]))
                     continue;

                  ksi_x = Ksi(point.X, x[j], h_x);
                  ksi_y = Ksi(point.Y, y[i], h_y);

                  for (int functionNumber = 1; functionNumber <= functionCount; functionNumber++)
                  {
                     basicFunctionValues[functionNumber - 1] = basicFunctions[Mu(functionNumber)](ksi_x) *
                                                               basicFunctions[Nu(functionNumber)](ksi_y);
                  }

                  for (int row = 0; row < functionCount; row++)
                  {
                     localMatrix[row, row] += w[pointIndex] * basicFunctionValues[row] * basicFunctionValues[row];

                     for (int column = row + 1; column < functionCount; column++)
                     {
                        localMatrix[row, column] += w[pointIndex] * basicFunctionValues[row] * basicFunctionValues[column];
                        localMatrix[column, row] += w[pointIndex] * basicFunctionValues[column] * basicFunctionValues[row];
                     }

                     localVector[row] += w[pointIndex] * basicFunctionValues[row] * point.Z;
                  }
               }

               BuildLocalMatrices(h_x, G_x, M_x);
               BuildLocalMatrices(h_y, G_y, M_y);

               for (int row = 0; row < functionCount; row++)
               {
                  localMatrix[row, row] += alpha * (G_x[Mu(row + 1), Nu(row + 1)] * M_y[Mu(row + 1), Nu(row + 1)] +
                                                    M_x[Mu(row + 1), Nu(row + 1)] * G_y[Mu(row + 1), Nu(row + 1)]);

                  for (int column = row + 1; column < functionCount; column++)
                  {
                     localMatrix[row, column] += alpha * (G_x[Mu(row + 1), Nu(row + 1)] * M_y[Mu(column + 1), Nu(column + 1)] +
                                                          M_x[Mu(row + 1), Nu(row + 1)] * G_y[Mu(column + 1), Nu(column + 1)]);
                     localMatrix[column, row] += alpha * (G_x[Mu(column + 1), Nu(column + 1)] * M_y[Mu(row + 1), Nu(row + 1)] +
                                                          M_x[Mu(column + 1), Nu(column + 1)] * G_y[Mu(row + 1), Nu(row + 1)]);
                  }
               }

               AddLocalToGlobal(globalMatrix, localMatrix,
                                globalVector, localVector,
                                nodeIndex, (i + 1) * x.Length + 1);
            }
         }

         Vector vector = new Vector(coeffs);
         SoLESolver.SolveBiCGM(globalMatrix, globalVector, vector);
      }

      private static void AddLocalToGlobal(SparseMatrix globalMatrix, double[,] localMatrix,
                                           Vector globalVector, double[] localVector,
                                           int leftBottomIndex, int rightTopIndex)
      {
         int[] functionIndices = GetGlobalIndices(leftBottomIndex, rightTopIndex);

         for (int i = 0; i < 16; i++)
         {
            globalMatrix.di[functionIndices[i]] += localMatrix[i, i];
            globalVector[functionIndices[i]] += localVector[i];

            for (int j = i + 1; j < 16; j++)
            {
               for (int k = globalMatrix.ig[functionIndices[j] - 1]; k < globalMatrix.ig[functionIndices[j]]; k++)
               {
                  if (globalMatrix.jg[k] == i)
                  {
                     globalMatrix.ggu[k] += localMatrix[i, j];
                     globalMatrix.ggl[k] += localMatrix[j, i];
                     break;
                  }
               }
            }
         }
      }

      private static SparseMatrix GenerateGlobalMatrix(double[] x, double[] y)
      {
         int nodeCount = x.Length * y.Length;
         int size = 4 * nodeCount;

         List<int>[] adjacencyList = new List<int>[nodeCount];

         int nodeIndex = adjacencyList.Length - 1;
         for (int i = y.Length - 1; i > 0; i--)
         {
            for (int j = x.Length - 1; j > 0; j--)
            {
               if (adjacencyList[nodeIndex] == null)
                  adjacencyList[nodeIndex] = new List<int>();

               adjacencyList[nodeIndex].Add(nodeIndex - x.Length - 1);
               adjacencyList[nodeIndex].Add(nodeIndex - x.Length);
               adjacencyList[nodeIndex].Add(nodeIndex - 1);
               nodeIndex--;
            }

            adjacencyList[nodeIndex] = new List<int>() { nodeIndex - x.Length, nodeIndex - x.Length + 1 };
            nodeIndex--;
         }

         for (; nodeIndex > 0; nodeIndex--)
            adjacencyList[nodeIndex] = new List<int>() { nodeIndex - 1 };

         adjacencyList[0] = new List<int>();

         double[] di = new double[size];
         int[] ig = new int[size + 1];
         double[] ggl = new double[adjacencyList.Select(item => item.Count).Sum() * 16 + nodeCount * 6];
         double[] ggu = new double[ggl.Length];
         int[] jg = new int[ggl.Length];

         int index = 0;
         int currentColumn = 0;
         int columnIndex = 0;
         for (int i = 0; i < adjacencyList.Length; i++)
         {
            //ig[i] = ig[i - 1];

            for (int k = 0; k < 4; k++)
            {
               columnIndex = -1;

               for (int j = 0; j < adjacencyList[i].Count; j++)
               {
                  jg[index] = 4 * adjacencyList[i][j];
                  jg[index + 1] = jg[index] + 1;
                  jg[index + 2] = jg[index] + 2;
                  jg[index + 3] = jg[index] + 3;

                  index += 4;
                  ig[currentColumn] += 4;

                  columnIndex = 4 * adjacencyList[i][j] + 3;
               }

               for (int j = k; j > 0; j--)
               {
                  jg[index] = currentColumn - j;
                  index++;
                  ig[currentColumn]++;
               }

               ig[currentColumn + 1] = ig[currentColumn];
               currentColumn++;
            }
         }

         return new SparseMatrix(size, ig, jg, di, ggu, ggl);
      }

      private static void BuildLocalMatrices(double h, double[,] G, double[,] M)
      {
         BuildLocalFirmMatrix(h, G);
         BuildLocalMassMatrix(h, M);
      }

      private static void BuildLocalFirmMatrix(double h, double[,] G)
      {
         double coeff = 1 / (30 * h);
         G[0, 0] = 36;
         G[0, 1] = 3 * h;
         G[0, 2] = -36;
         G[0, 3] = 3 * h;

         G[1, 1] = 4 * h * h;
         G[1, 2] = -3 * h;
         G[1, 3] = -h * h;

         G[2, 2] = 36;
         G[2, 3] = -3 * h;

         G[3, 3] = 4 * h * h;

         for (int i = 0; i < 4; i++)
         {
            G[i, i] *= coeff;

            for (int j = i + 1; j < 4; j++)
            {
               G[i, j] *= coeff;
               G[j, i] = G[i, j];
            }
         }
      }

      private static void BuildLocalMassMatrix(double h, double[,] M)
      {
         double coeff = 1 / (420 * h);
         M[0, 0] = 156;
         M[0, 1] = 22 * h;
         M[0, 2] = 54;
         M[0, 3] = -13 * h;

         M[1, 1] = 4 * h * h;
         M[1, 2] = 13 * h;
         M[1, 3] = -3 * h * h;

         M[2, 2] = 156;
         M[2, 3] = -22 * h;

         M[3, 3] = 4 * h * h;

         for (int i = 0; i < 4; i++)
         {
            M[i, i] *= coeff;

            for (int j = i + 1; j < 4; j++)
            {
               M[i, j] *= coeff;
               M[j, i] = M[i, j];
            }
         }
      }

      private static void Clear(double[,] matrix)
      {
         for (int i = 0; i < matrix.GetLength(0); i++)
         {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
               matrix[i, j] = 0;
            }
         }
      }

      private static void Clear(double[] vector)
      {
         for (int i = 0; i < vector.Length; i++)
            vector[i] = 0;
      }

      private static int Mu(int i)
      {
         return 2 * (((i - 1) / 4) % 2) + ((i - 1) % 2);
      }

      private static int Nu(int i)
      {
         return 2 * ((i - 1) / 8) + (((i - 1) / 2) % 2);
      }

      private static bool IsPointInsideElement(double x, double y, double minX, double maxX, double minY, double maxY)
      {
         if (x < minX - epsilon || x > maxX + epsilon)
            return false;

         if (y < minY - epsilon || y > maxY + epsilon)
            return false;

         return true;
      }

      private static double Ksi(double arg, double arg0, double h) => (arg - arg0) / h;

      private static double Phi_1(double ksi)
      {
         return 1 - 3 * ksi * ksi + 2 * ksi * ksi * ksi;
      }

      private static double Phi_2(double ksi)
      {
         return ksi - 2 * ksi * ksi + ksi * ksi * ksi;
      }

      private static double Phi_3(double ksi)
      {
         return 3 * ksi * ksi - 2 * ksi * ksi * ksi;
      }

      private static double Phi_4(double ksi)
      {
         return -1 * ksi * ksi + ksi * ksi * ksi;
      }

      private static int[] GetGlobalIndices(int leftBottomIndex, int rightTopIndex)
      {
         int[] functionIndices = new int[]
         {
            4 * leftBottomIndex, 4 * leftBottomIndex + 1, 4 * leftBottomIndex + 2, 4 * leftBottomIndex + 3,
            4 * (leftBottomIndex + 1), 4 * (leftBottomIndex + 1) + 1, 4 * (leftBottomIndex + 1) + 2, 4 * (leftBottomIndex + 1) + 3,
            4 * (rightTopIndex - 1), 4 * (rightTopIndex - 1) + 1, 4 * (rightTopIndex - 1) + 2, 4 * (rightTopIndex - 1) + 3,
            4 * rightTopIndex, 4 * rightTopIndex + 1, 4 * rightTopIndex + 2, 4 * rightTopIndex + 3
         };
         return functionIndices;
      }

      public double GetValue(double x, double y)
      {
         for (int i = 0; i < yTicks.Length - 1; i++)
         {
            for (int j = 0; j < xTicks.Length - 1; j++)
            {
               if (!IsPointInsideElement(x, y, xTicks[j], xTicks[j + 1], yTicks[i], yTicks[i + 1]))
                  continue;

               double value = 0;
               double ksi_x = Ksi(x, xTicks[j], xTicks[j + 1] - xTicks[j]);
               double ksi_y = Ksi(y, yTicks[i], yTicks[i + 1] - yTicks[i]);

               int leftBottomIndex = i * xTicks.Length + j;
               int rightTopIndex = leftBottomIndex + xTicks.Length + 1;

               int[] functionIndices = GetGlobalIndices(leftBottomIndex, rightTopIndex);

               for (int functionIndex = 1; functionIndex <= 16; functionIndex++)
               {
                  value += coeffs[functionIndices[functionIndex - 1]] *
                           basicFunctions[Mu(functionIndex)](ksi_x) *
                           basicFunctions[Nu(functionIndex)](ksi_y);
               }

               return value;
            }
         }

         throw new ArgumentOutOfRangeException($"({x}, {y}) is out of mesh");
      }
   }
}