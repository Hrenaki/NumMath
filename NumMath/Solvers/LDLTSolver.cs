using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public static partial class SoLESolver
    {
        public static void SolveLDLT(SymmProfileMatrix mat, Vector vec)
        {
            int[] ia = mat.ia;
            double[] a = mat.a;
            double[] di = mat.di;
            int size = mat.size;

            int i, j, index_cur, index_i, index_j, index_di;
            double capacitor;
            double di_capacitor;

            // factorization
            for (i = 1; i < size; i++)
            {
                index_cur = ia[i];
                di_capacitor = 0;
                for (j = i - ia[i + 1] + ia[i]; j < i; j++, index_cur++)
                {
                    index_i = index_cur - 1;
                    index_j = ia[j + 1] - 1;
                    index_di = j - 1;

                    capacitor = 0;

                    for (; index_j >= ia[j] && index_i >= ia[i]; index_j--, index_i--, index_di--)
                        capacitor -= a[index_i] * a[index_j] * di[index_di];
                    a[index_cur] += capacitor;
                    di_capacitor -= a[index_cur] * a[index_cur] / di[j];
                    a[index_cur] /= di[j];
                }
                di[i] += di_capacitor;
            }

            int curIndex;
            double[] v = vec.values;

            // forward
            for (i = 1; i < size; i++)
            {
                capacitor = 0;
                for (j = i - ia[i + 1] + ia[i], curIndex = ia[i]; j < i; j++, curIndex++)
                    capacitor -= v[j] * a[curIndex];
                v[i] += capacitor;
            }

            // diagonal
            for (i = 0; i < size; i++)
                v[i] /= di[i];

            // backward
            for (i = size - 1; i > 0; i--)
                for (j = i - ia[i + 1] + ia[i], curIndex = ia[i]; j < i; j++, curIndex++)
                    v[j] -= v[i] * a[curIndex];
        }
    }
}
