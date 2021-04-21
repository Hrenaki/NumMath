using System;

namespace NumMath
{
    public partial class SoLESolver
    { 
        public static int SolveBSG(SparseMatrix mat, Vector vec, Vector start)
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

            for(step = 0; step < MaxSteps && curNorm / norm >= Epsilon * Epsilon; step++)
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
    }
}