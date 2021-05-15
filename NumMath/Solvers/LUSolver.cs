using System;


namespace NumMath
{ 
    public partial class SoLESolver
    {
        public static void SolveLU(ProfileMatrix matrix, Vector vec)
        {
            int size = matrix.size;

            int[] ia = matrix.ia;
            double[] di = matrix.di;
            double[] al = matrix.al;
            double[] au = matrix.au;

            double[] b = vec.values;

            int i, j, index_i, index_j, k;

            // factorization
            for(i = 1; i < size; i++)
            {
                for(j = ia[i]; j < ia[i + 1]; j++)
                {
                    index_i = j - 1;
                    k = i - (ia[i + 1] - j);
                    index_j = ia[k + 1] - 1;
                    
                    for(; index_i >= ia[i] && index_j >= ia[k]; index_i--, index_j--)
                    {
                        al[j] -= al[index_i] * au[index_j];
                        au[j] -= al[index_j] * au[index_i];
                    }
                    au[j] /= di[k];

                    di[i] -= al[j] * au[j];
                }
            }

            // straight
            for(i = 0; i < size; i++)
            {
                for (j = ia[i]; j < ia[i + 1]; j++)
                    b[i] -= b[i - (ia[i + 1] - j)] * al[j];
                b[i] /= di[i];
            }

            // backward
            for(i = size - 1; i > 0; i--)
                for (j = ia[i]; j < ia[i + 1]; j++)
                    b[i - (ia[i + 1] - j)] -= b[i] * au[j];
        }
    }
}
