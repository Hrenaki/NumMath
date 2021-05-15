using System;

namespace NumMath
{
    public partial class SoLESolver
    { 
        public static int SolveBiCGM(SparseMatrix mat, Vector vec, Vector start)
        {
            int size = mat.size;

            int[] ig = mat.ig;
            int[] jg = mat.jg;
            double[] di = mat.di;
            double[] ggl = mat.ggl;
            double[] ggu = mat.ggu;

            double[] v = vec.values;
            double[] xk = start.values;

            double[] rk = new double[size];
            double[] pk = new double[size];
            double[] zk = new double[size];
            double[] sk = new double[size];
            double[] temp = new double[size];

            double norm = vec.sqrMagnitude;
            double curNorm = 0;
            double prevScalar, curScalar = 0;
            double ak, bk;

            int i, j;
            int step;

            void mult(double[] b, double[] x) // Ab = x
            {
                for (i = 0; i < size; i++)
                    x[i] = 0;

                for(i = 0; i < size; i++)
                {
                    for(j = ig[i]; j < ig[i + 1]; j++)
                    {
                        x[i] += b[jg[j]] * ggl[j];
                        x[jg[j]] += b[i] * ggu[j];
                    }
                    x[i] += b[i] * di[i];
                }
            }
            void multTranspose(double[] b, double[] x) // AT * b = x
            {
                for (i = 0; i < size; i++)
                    x[i] = 0;

                for(i = 0; i < size; i++)
                {
                    for(j = ig[i]; j < ig[i + 1]; j++)
                    {
                        x[i] += b[jg[j]] * ggu[j];
                        x[jg[j]] += b[i] * ggl[j];
                    }
                    x[i] += b[i] * di[i];
                }
            }

            mult(xk, rk);
            for(i = 0; i < size; i++)
            {
                rk[i] = v[i] - rk[i];
                curNorm += rk[i] * rk[i];

                pk[i] = rk[i];
                curScalar += pk[i] * rk[i];

                zk[i] = rk[i];
                sk[i] = rk[i];
            }

            for(step = 0; step < size && curNorm / norm >= Epsilon * Epsilon; step++)
            {
                mult(zk, temp);
                ak = 0;
                for (i = 0; i < size; i++)
                    ak += sk[i] * temp[i];
                ak = curScalar / ak;

                curNorm = 0;
                for(i = 0; i < size; i++)
                {
                    xk[i] += ak * zk[i];
                    rk[i] -= ak * temp[i];
                    curNorm += rk[i] * rk[i];
                }

                multTranspose(sk, temp);
                prevScalar = curScalar;
                curScalar = 0;
                for(i = 0; i < size; i++)
                {
                    pk[i] -= ak * temp[i];
                    curScalar += pk[i] * rk[i];
                }

                bk = curScalar / prevScalar;
                for(i = 0; i < size; i++)
                {
                    zk[i] = rk[i] + bk * zk[i];
                    sk[i] = pk[i] + bk * sk[i];
                }
            }
            return step;
        }
        public static int SolveBiCGM_LU(SparseMatrix mat, Vector vec, Vector start)
        {
            int size = mat.size;

            int[] ig = mat.ig;
            int[] jg = mat.jg;
            double[] di = mat.di;
            double[] ggl = mat.ggl;
            double[] ggu = mat.ggu;
            double[] di_lu = new double[size];
            double[] ggl_lu = new double[ggl.Length];
            double[] ggu_lu = new double[ggu.Length];

            double[] v = vec.values;
            double[] xk = start.values;

            double[] rk = new double[size];
            double[] pk = new double[size];
            double[] zk = new double[size];
            double[] sk = new double[size];
            double[] w1 = new double[size];
            double[] w2 = new double[size];

            double norm = vec.sqrMagnitude;
            double curNorm;
            double prevScalar, curScalar;
            double ak, bk;

            int i, j, k, s, m, n;
            int step;

            void mult(double[] b, double[] x) // Ab = x
            {
                for (i = 0; i < size; i++)
                    x[i] = 0;

                for (i = 0; i < size; i++)
                {
                    for (j = ig[i]; j < ig[i + 1]; j++)
                    {
                        x[i] += b[jg[j]] * ggl[j];
                        x[jg[j]] += b[i] * ggu[j];
                    }
                    x[i] += b[i] * di[i];
                }
            }
            void mult_Transposed(double[] b, double[] x) // AT * b = x
            {
                for (i = 0; i < size; i++)
                    x[i] = 0;

                for (i = 0; i < size; i++)
                {
                    for (j = ig[i]; j < ig[i + 1]; j++)
                    {
                        x[i] += b[jg[j]] * ggu[j];
                        x[jg[j]] += b[i] * ggl[j];
                    }
                    x[i] += b[i] * di[i];
                }
            }
            void solveLU(double[] y) // LU * x = y
            {
                // straight
                for(i = 0; i < size; i++)
                {
                    for(j = ig[i]; j < ig[i + 1]; j++)
                        y[i] -= ggl_lu[j] * y[jg[j]];
                    y[i] /= di_lu[i];
                }

                // backward
                for(i = size - 1; i > 0; i--)
                {
                    for (j = ig[i]; j < ig[i + 1]; j++)
                        y[jg[j]] -= ggu_lu[j] * y[i];
                }
            }
            void solveLU_Transposed(double[] y)
            {
                // straight
                for (i = 0; i < size; i++)
                    for (j = ig[i]; j < ig[i + 1]; j++)
                        y[i] -= ggu_lu[j] * y[jg[j]];

                // backward
                for (i = size - 1; i >= 0; i--)
                {
                    y[i] /= di_lu[i];
                    for (j = ig[i]; j < ig[i + 1]; j++)
                        y[jg[j]] -= ggl_lu[j] * y[i];
                }
            }

            #region LU factorization
            for (i = 0; i < size; i++)
            {
                di_lu[i] = di[i];

                for (j = ig[i]; j < ig[i + 1]; j++)
                {
                    ggl_lu[j] = ggl[j];
                    ggu_lu[j] = ggu[j];

                    s = jg[j]; // Lij -> j, Uji -> j
                    m = ig[i];
                    for (k = ig[s]; k < ig[s + 1]; k++)
                    {
                        for (n = m; n < j; n++)
                            if (jg[k] == jg[n])
                            {
                                ggl_lu[j] -= ggl_lu[n] * ggu_lu[k];
                                ggu_lu[j] -= ggl_lu[k] * ggu_lu[n];
                                m = n + 1;
                                break;
                            }
                    }
                    ggu_lu[j] /= di_lu[jg[j]];

                    di_lu[i] -= ggl_lu[j] * ggu_lu[j];
                }
            }
            #endregion

            mult(xk, rk);

            curNorm = 0;
            for(i = 0; i < size; i++)
            {
                rk[i] = v[i] - rk[i];
                pk[i] = rk[i];

                w1[i] = rk[i];
                w2[i] = pk[i];

                curNorm += rk[i] * rk[i];
            }

            solveLU(w1);
            solveLU_Transposed(w2);

            prevScalar = 0;
            for(i = 0; i < size; i++)
            {
                zk[i] = w1[i];
                sk[i] = w2[i];

                prevScalar += w1[i] * pk[i];
            }

            for (step = 1; step < MaxSteps && curNorm / norm >= Epsilon * Epsilon; step++)
            {
                mult(zk, w1);
                mult_Transposed(sk, w2);

                ak = 0;
                for (i = 0; i < size; i++)
                    ak += w1[i] * sk[i];
                ak = prevScalar / ak;

                curNorm = 0;
                for(i = 0; i < size; i++)
                {
                    xk[i] += ak * zk[i];
                    rk[i] -= ak * w1[i];
                    pk[i] -= ak * w2[i];

                    curNorm += rk[i] * rk[i];

                    w1[i] = rk[i];
                    w2[i] = pk[i];
                }

                if (curNorm / norm < Epsilon * Epsilon)
                    break;

                solveLU(w1);
                solveLU_Transposed(w2);

                curScalar = 0;
                for (i = 0; i < size; i++)
                    curScalar += w1[i] * pk[i];
                bk = curScalar / prevScalar;
                for(i = 0; i < size; i++)
                {
                    zk[i] = w1[i] + bk * zk[i];
                    sk[i] = w2[i] + bk * sk[i];
                }

                prevScalar = curScalar;
            }
            return step;
        }
    }
}