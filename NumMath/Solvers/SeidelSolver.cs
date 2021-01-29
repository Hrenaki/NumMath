using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public partial class SoLESolver
    {
        public int SolveSeidel(Vector start, double epsilon, double weight)
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
                int i, j;
                double collector;
                int step;
                double norm = 0;
                double curNorm;
                int offset;
                double residual;
                for (i = 0; i < size; i++)
                    norm += v[i] * v[i];

                for (step = 0; step < maxSteps; step++)
                {
                    curNorm = 0;
                    for (i = 0; i < size; i++)
                    {
                        collector = 0;
                        for (j = 0; j < diagCount; j++)
                        {
                            offset = i + offsets[j];
                            if (offset < 0)
                                continue;
                            if (offset >= size)
                                break;
                            collector += values[j, i] * x[offset];
                        }
                        x[i] += weight * (v[i] - collector) / values[mainDiagIndex, i];
                        curNorm += (v[i] - collector) * (v[i] - collector);
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
