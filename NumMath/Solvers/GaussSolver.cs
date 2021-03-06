using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public static partial class SoLESolver
    {
        public static void SolveGauss(FullMatrix mat, Vector vec)
        {
            for (int i = 0; i < mat.size - 1; i++)
            {
                double maxValue = mat[i, i];
                int maxValueIndex = i;
                for (int j = i + 1; j < mat.size; j++)
                    if (mat[j, i] > maxValue)
                    {
                        maxValue = mat[j, i];
                        maxValueIndex = j;
                    }
                if (maxValueIndex != i)
                {
                    double tmp = vec[i];
                    vec[i] = vec[maxValueIndex];
                    vec[maxValueIndex] = tmp;
                    for (int j = 0; j < mat.size; j++)
                    {
                        tmp = mat[i, j];
                        mat[i, j] = mat[maxValueIndex, j];
                        mat[maxValueIndex, j] = tmp;
                    }
                }
                for (int j = i + 1; j < mat.size; j++)
                {
                    double coef = mat[j, i] / mat[i, i];
                    for (int k = i; k < mat.size; k++)
                        mat[j, k] -= mat[i, k] * coef;
                    vec[j] -= vec[i] * coef;
                }
            }

            for (int i = mat.size - 1; i >= 0; i--)
            {
                for (int j = i + 1; j < mat.size; j++)
                    vec[i] -= mat[i, j] * vec[j];
                vec[i] /= mat[i, i];
            }
        }
    }
}
