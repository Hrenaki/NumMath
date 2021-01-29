using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public partial class SoLESolver
    {
        public int SolveJacobi(Vector start, double epsilon, double weight)
        {
            if(matrix as DiagMatrix != null)
            {
                DiagMatrix mat = matrix as DiagMatrix;

                int size = mat.size;
                int diagCount = mat.diagCount;
                int mainDiagIndex = Array.IndexOf(mat.offsets, 0);
                double[,] values = mat.values;
                int[] offsets = mat.offsets;
                double[] v = vec.values;
                double[] x = start.values;
                int i, j, k;
                double[] collector = new double[size];
                int step;
                double norm = 0;
                double curNorm;
                double residual;

                for (i = 0; i < size; i++)
                    norm += v[i] * v[i];

                for (step = 0; step < maxSteps; step++)
                {
                    curNorm = 0;
                    for (i = 0; i < diagCount; i++)
                        for (j = Math.Max(-offsets[i], 0), k = Math.Max(offsets[i], 0); j < size && k < size; j++, k++)
                            collector[j] += x[k] * values[i, j];
                    for (i = 0; i < size; i++)
                    {
                        curNorm += (v[i] - collector[i]) * (v[i] - collector[i]);
                        x[i] += weight * (v[i] - collector[i]) / values[mainDiagIndex, i];
                        collector[i] = 0;
                    }
                    residual = Math.Sqrt(curNorm / norm);
                    //Console.WriteLine("step = " + (step + 1) + ", residual = " + residual);
                    if (residual < epsilon)
                        break;
                }
                return step + 1;
            }
            throw new NotImplementedException();
        }
    }
}
