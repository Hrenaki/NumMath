using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class EigenValues
    {
        private int maxSteps;
        private double eps;
        private int size;
        private FullMatrix mat;

        public double MinValue { get; private set; }
        public double MaxValue { get; private set; }
        public double[] MaxEigenValueVector { get; private set; }
        public double[] MinEigenValueVector { get; private set; }
        public double MinValueSteps { get; private set; }
        public double MaxValueSteps { get; private set; }

        public EigenValues(FullMatrix mat, int maxSteps, double eps)
        {
            this.maxSteps = maxSteps;
            this.eps = eps;
            this.mat = mat;

            size = mat.size;
            MaxEigenValueVector = new double[size];
            MinEigenValueVector = new double[size];

            findMaxValue();
            findMinValue();
        }

        private void findMaxValue()
        {
            double[] xk = MaxEigenValueVector;
            double[] temp = new double[size];

            double curNorm, prevNorm = 0;
            double curEigenValue;
            double m;

            void Mult(double[] t, double[] res)
            {
                double sum;
                int k, s;
                for (k = 0; k < size; k++)
                {
                    sum = 0;
                    for (s = 0; s < size; s++)
                        sum += mat[k, s] * t[s];
                    res[k] = sum;
                }
            }

            int i;
            for (i = 0; i < size; i++)
            {
                xk[i] = 1;
                prevNorm += xk[i] * xk[i];
            }

            MaxValue = 0;
            int step = 0;
            do
            {
                step++;

                Mult(xk, temp);

                curNorm = 0;
                for (i = 0; i < size; i++)
                    curNorm += temp[i] * temp[i];

                curEigenValue = curNorm / prevNorm;
                m = Math.Abs((MaxValue - curEigenValue) / curEigenValue);
                MaxValue = curEigenValue;

                if (step % 5 == 0)
                {
                    curNorm = Math.Sqrt(curNorm);
                    for (i = 0; i < size; i++)
                        xk[i] = temp[i] / curNorm;
                    prevNorm = 1;
                }
                else
                {
                    prevNorm = curNorm;
                    for (i = 0; i < size; i++)
                        xk[i] = temp[i];
                }

            } while (step < maxSteps && m >= eps);

            MaxValue = Math.Sqrt(MaxValue);
            MaxValueSteps = step;
        }
        private void findMinValue()
        {
            int i, j, k;
            double sumLower, sumUpper;

            #region LU-факторизация
            for (i = 0; i < size; i++)
            {
                for (j = 0; j < i; j++)
                {
                    sumLower = 0;
                    sumUpper = 0;
                    for (k = 0; k < j; k++)
                    {
                        sumLower += mat[i, k] * mat[k, j];
                        sumUpper += mat[j, k] * mat[k, i];
                    }

                    mat[i, j] -= sumLower;
                    mat[j, i] = 1.0 / mat[j, j] * (mat[j, i] - sumUpper);
                    mat[i, i] -= mat[i, j] * mat[j, i];
                }
            }
            #endregion

            void solveLU(double[] t)
            {
                for (i = 0; i < size; i++)
                {
                    for (j = 0; j < i; j++)
                        t[i] -= mat[i, j] * t[j];
                    t[i] /= mat[i, i];
                }

                for (i = size - 1; i >= 0; i--)
                {
                    for (j = 0; j < i; j++)
                        t[j] -= mat[j, i] * t[i];
                }
            }

            double[] xk = MinEigenValueVector;

            double curNorm, prevNorm = 0;
            double curEigenValue;
            double m;

            for (i = 0; i < size; i++)
            {
                xk[i] = 1;
                prevNorm += xk[i] * xk[i];
            }

            MinValue = 0;
            int step = 0;
            do
            {
                step++;

                solveLU(xk);

                curNorm = 0;
                for (i = 0; i < size; i++)
                    curNorm += xk[i] * xk[i];

                curEigenValue = curNorm / prevNorm;
                m = Math.Abs((MinValue - curEigenValue) / curEigenValue);

                MinValue = curEigenValue;

                if (step % 5 == 0)
                {
                    curNorm = Math.Sqrt(curNorm);
                    for (i = 0; i < size; i++)
                        xk[i] /= curNorm;
                    prevNorm = 1;
                }
                else prevNorm = curNorm;

            } while (step < maxSteps && m >= eps);

            MinValue = 1.0 / Math.Sqrt(MinValue);
            MinValueSteps = step;
        }
    }
}
