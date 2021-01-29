using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public partial class SoLESolver
    {
        public int SolveCGM(Vector start, double epsilon)
        {
            if(matrix as SymmSparseMatrix != null)
            {
                SymmSparseMatrix mat = matrix as SymmSparseMatrix;

                int size = mat.size;

                int[] ig = mat.ig;
                int[] jg = mat.jg;

                double[] di = mat.di;
                double[] gg = mat.gg;

                int i, j;

                void mult(double[] b, double[] x) // x = Ab
                {
                    for (i = 0; i < size; i++)
                        x[i] = 0;

                    for (i = 0; i < size; i++)
                    {
                        for (j = ig[i]; j < ig[i + 1]; j++)
                        {
                            x[jg[j]] += b[i] * gg[j];
                            x[i] += b[jg[j]] * gg[j];
                        }
                        x[i] += di[i] * b[i];
                    }
                }

                double[] temp = new double[size];
                double norm = vec.sqrMagnitude; // норма вектора b
                double prevNorm, curNorm = 0;

                double[] v = vec.values;
                double ak, bk;
                double[] xk = start.values; // x0

                //rk = vec - mat * start
                double[] rk = new double[size];
                double[] zk = new double[size];
                mult(xk, rk);
                for (i = 0; i < size; i++)
                {
                    rk[i] = v[i] - rk[i];
                    curNorm += rk[i] * rk[i];
                    zk[i] = rk[i];
                }

                int step;

                for (step = 1; step < maxSteps && curNorm / norm >= epsilon * epsilon; step++)
                {
                    mult(zk, temp);

                    #region ak = (r_k-1, r_k-1) / (A * z_k-1, z_k-1)
                    prevNorm = curNorm;
                    curNorm = 0;
                    for (i = 0; i < size; i++)
                        curNorm += temp[i] * zk[i];
                    ak = prevNorm / curNorm;
                    #endregion

                    curNorm = 0;
                    // вычисление x_k и r_k
                    for (i = 0; i < size; i++)
                    {
                        xk[i] += ak * zk[i];
                        rk[i] -= ak * temp[i];
                        curNorm += rk[i] * rk[i];
                    }

                    Console.WriteLine("X" + step + " = " + start.ToString() + ". Residual^2 = " + Math.Sqrt(curNorm / norm).ToString("E4"));

                    // b_k = (r_k, r_k) / (r_k-1, r_k-1)
                    bk = curNorm / prevNorm;

                    // z_k = r_k + b_k * z_k-1
                    for (i = 0; i < size; i++)
                        zk[i] = rk[i] + bk * zk[i];
                }

                return --step;
            }
            throw new NotImplementedException();
        }
        public int SolveCGM_D(Vector start, double epsilon)
        {
            if(matrix as SymmSparseMatrix != null)
            {
                SymmSparseMatrix mat = matrix as SymmSparseMatrix;

                int size = mat.size;

                int[] ig = mat.ig;
                int[] jg = mat.jg;

                double[] di = mat.di;
                double[] gg = mat.gg;

                int i, j;

                void mult(double[] b, double[] x) // x = Ab
                {
                    for (i = 0; i < size; i++)
                        x[i] = 0;

                    for (i = 0; i < size; i++)
                    {
                        for (j = ig[i]; j < ig[i + 1]; j++)
                        {
                            x[jg[j]] += b[i] * gg[j];
                            x[i] += b[jg[j]] * gg[j];
                        }
                        x[i] += di[i] * b[i];
                    }
                }

                double[] temp = new double[size];
                double norm = vec.sqrMagnitude; // норма вектора b
                double numerator, denominator = 0, curNorm = 0;

                double[] v = vec.values;
                double ak, bk;
                double[] xk = start.values; // x0

                //rk = vec - mat * start
                double[] rk = new double[size];
                double[] zk = new double[size];
                mult(xk, rk);
                for (i = 0; i < size; i++)
                {
                    rk[i] = v[i] - rk[i];
                    zk[i] = rk[i] / di[i];
                    curNorm += rk[i] * rk[i];
                    denominator += rk[i] * rk[i] / di[i];
                }

                int step;
                for (step = 1; step < maxSteps && curNorm / norm >= epsilon * epsilon; step++)
                {
                    mult(zk, temp);

                    #region ak = (M^(-1) * r_k-1, r_k-1) / (A * z_k-1, z_k-1)
                    numerator = denominator;
                    denominator = 0;
                    for (i = 0; i < size; i++)
                        denominator += temp[i] * zk[i];
                    ak = numerator / denominator;
                    #endregion

                    denominator = 0;
                    // x_k и r_k
                    for (i = 0; i < size; i++)
                    {
                        xk[i] += ak * zk[i]; // x_k = x_k-1 + a_k * z_k-1
                        rk[i] -= ak * temp[i]; // r_k = r_k-1 - a_k * A * z_k-1
                        denominator += rk[i] * rk[i];
                    }
                    curNorm = denominator;

                    Console.WriteLine("X" + step + " = " + start.ToString() + 
                        ". Residual = " + Math.Sqrt(curNorm / norm).ToString("E4"));

                    // b_k
                    denominator = 0; // (M^(-1) * r_k, r_k)
                    for (i = 0; i < size; i++)
                        denominator += rk[i] * rk[i] / di[i];

                    bk = denominator / numerator;

                    // zk = M^(-1) * rk + beta * z_k-1
                    for (i = 0; i < size; i++)
                        zk[i] = rk[i] / di[i] + bk * zk[i];
                }
                return --step;
            }
            throw new NotImplementedException();
        }
        public int SolveCGM_LLT(Vector start, double epsilon)
        {
            if (matrix as SymmSparseMatrix != null)
            {
                SymmSparseMatrix mat = matrix as SymmSparseMatrix;

                int size = mat.size;

                int[] ig = mat.ig;
                int[] jg = mat.jg;

                double[] di = mat.di;
                double[] gg = mat.gg;

                int i, j, k, s, m, n;

                void mult(double[] b, double[] x) // x = Ab
                {
                    for (i = 0; i < size; i++)
                        x[i] = 0;

                    for (i = 0; i < size; i++)
                    {
                        for (j = ig[i]; j < ig[i + 1]; j++)
                        {
                            x[jg[j]] += b[i] * gg[j];
                            x[i] += b[jg[j]] * gg[j];
                        }
                        x[i] += di[i] * b[i];
                    }
                }
                double[] diLLT = new double[size];
                double[] ggLLT = new double[gg.Length];

                #region LLT factorization    
                for (i = 0; i < size; i++)
                {
                    diLLT[i] = 0;

                    for (j = ig[i]; j < ig[i + 1]; j++) // Lij
                    {
                        ggLLT[j] = gg[j];

                        s = jg[j]; // Lij -> j
                        m = ig[i];
                        for (k = ig[s]; k < ig[s + 1]; k++)
                        {
                            for (n = m; n < j; n++)
                                if (jg[n] == jg[k])
                                {
                                    ggLLT[j] -= ggLLT[n] * ggLLT[k];
                                    m = n + 1;
                                    break;
                                }
                        }
                        // L43 = 1 / L33 * (A43 - L41 * L31 - L42 * L32)

                        ggLLT[j] /= diLLT[jg[j]];
                        diLLT[i] -= ggLLT[j] * ggLLT[j]; // -Lij ^ 2
                    }

                    diLLT[i] = Math.Sqrt(di[i] + diLLT[i]);
                }
                #endregion

                double[] temp = new double[size];
                double norm = vec.sqrMagnitude;
                double numerator, denominator = 0, curNorm = 0;
                double[] q = new double[size];

                double[] v = vec.values;
                double ak, bk;
                double[] xk = start.values; // x0

                //rk = vec - mat * start
                double[] rk = new double[size];
                double[] zk = new double[size];
                mult(xk, rk);
                for (i = 0; i < size; i++)
                {
                    rk[i] = v[i] - rk[i];
                    zk[i] = rk[i];
                    curNorm += rk[i] * rk[i];
                    q[i] = rk[i];
                }
                solveLLT(q);
                for (i = 0; i < size; i++)
                    denominator += q[i] * rk[i];


                // LLT * x = y 
                void solveLLT(double[] y)
                {
                    // straight
                    for (i = 0; i < size; i++)
                    {
                        for (j = ig[i]; j < ig[i + 1]; j++)
                            y[i] -= ggLLT[j] * y[jg[j]];
                        y[i] /= diLLT[i];
                    }
                    // backward
                    for (i = size - 1; i >= 0; i--)
                    {
                        y[i] /= diLLT[i];
                        for (j = ig[i]; j < ig[i + 1]; j++)
                            y[jg[j]] -= ggLLT[j] * y[i];
                    }
                }

                solveLLT(zk);

                int step;
                for (step = 1; step < maxSteps && curNorm / norm >= epsilon * epsilon; step++)
                {
                    mult(zk, temp);

                    #region ak = (q, r_k-1) / (A * z_k-1, z_k-1)
                    numerator = denominator;
                    denominator = 0;
                    for (i = 0; i < size; i++)
                        denominator += temp[i] * zk[i];
                    ak = numerator / denominator;
                    #endregion

                    curNorm = 0;
                    for (i = 0; i < size; i++)
                    {
                        xk[i] += ak * zk[i]; // x_k = x_k-1 + a_k * z_k-1
                        rk[i] -= ak * temp[i]; // r_k = r_k-1 - a_k * A * z_k-1
                        curNorm += rk[i] * rk[i];
                    }

                    Console.WriteLine("X" + step + " = " + start.ToString() + ". Residual^2 = " + Math.Sqrt(curNorm / norm).ToString("E4"));

                    // q = M^(-1) * r_k => LLT * q = r_k
                    for (i = 0; i < size; i++)
                        q[i] = rk[i];
                    solveLLT(q);

                    denominator = 0;
                    for (i = 0; i < size; i++)
                        denominator += q[i] * rk[i]; // (M^(-1) * r_k, r_k) => (q, r_k)
                                                     // b_k = (M^(-1) * r_k, r_k) / (M^(-1) * r_k-1, r_k-1)
                    bk = denominator / numerator;

                    for (i = 0; i < size; i++)
                        zk[i] = q[i] + bk * zk[i]; // z_k = q + b_k * z_k-1
                }

                return --step;
            }
            throw new NotImplementedException();
        }
    }
}
