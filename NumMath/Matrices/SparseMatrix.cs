using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class SparseMatrix : Matrix
    {
        public double[] di, ggl, ggu;
        public int[] ig, jg;
        public double this[int i, int j]
        {
            get
            {
                if (i != j)
                {
                    for (int k = ig[i]; k < ig[i + 1]; k++)
                        if (jg[k] == j)
                            return i < j ? ggu[k] : ggl[k];
                    return 0;
                }
                else return di[i];
            }
        }
        public SparseMatrix(int size, int[] ig, int[] jg, double[] di, double[] ggu, double[] ggl) : base(size)
        {
            this.ig = ig;
            this.jg = jg;
            this.di = di;
            this.ggu = ggu;
            this.ggl = ggl;
        }
        public static SparseMatrix Parse(string size, string ig, string jg,
            string di, string ggu, string ggl)
        {
            return new SparseMatrix(int.Parse(size),
                ig.Split(' ').Select(value => int.Parse(value)).ToArray(),
                jg.Split(' ').Select(value => int.Parse(value)).ToArray(),
                di.Split(' ').Select(value => double.Parse(value)).ToArray(),
                ggu.Split(' ').Select(value => double.Parse(value)).ToArray(),
                ggl.Split(' ').Select(value => double.Parse(value)).ToArray());
        }
        public T Cast<T>() where T : Matrix
        {
            if (typeof(T) == typeof(ProfileMatrix))
            {
                int i, j, k;
                double[] profile_di = new double[size];
                int[] profile_ia = new int[size + 1];
                List<double> profile_au = new List<double>();
                List<double> profile_al = new List<double>();

                ProfileMatrix newMatrix = new ProfileMatrix(size, profile_ia, profile_di, null, null);

                for (i = 0; i < size; i++)
                    profile_di[i] = di[i];

                for (i = 0; i < size; i++)
                {
                    profile_ia[i + 1] = profile_ia[i];
                    
                    if(ig[i + 1] - ig[i] > 0)
                    {
                        for(j = ig[i]; j < ig[i + 1] - 1; j++)
                        {
                            profile_al.Add(ggl[j]);
                            profile_au.Add(ggu[j]);
                            profile_ia[i + 1]++;

                            k = jg[j];
                            while(k < jg[j + 1] - 1)
                            {
                                profile_al.Add(0.0);
                                profile_au.Add(0.0);
                                profile_ia[i + 1]++;
                                k++;
                            }
                        }

                        profile_al.Add(ggl[j]);
                        profile_au.Add(ggu[j]);
                        profile_ia[i + 1]++;

                        k = jg[j];
                        while(k < i - 1)
                        {
                            profile_al.Add(0.0);
                            profile_au.Add(0.0);
                            profile_ia[i + 1]++;
                            k++;
                        }
                    }
                }

                newMatrix.al = profile_al.ToArray();
                newMatrix.au = profile_au.ToArray();
                return newMatrix as T;
            }
            return null;
        }
        public void PrintPortait()
        {
            string str = "";
            int k = 0;
            int j;
            for(int i = 0; i < size; i++)
            {
                for(k = 0; k < i; k++)
                {
                    for(j = ig[i]; j < ig[i + 1]; j++)
                        if(jg[j] == k)
                        {
                            str += "* ";
                            break;
                        }
                    if (j == ig[i + 1])
                        str += "0 ";
                }
                str += "*\n";
            }

            File.WriteAllText("text.txt", str);
        }
        public static Vector operator *(SparseMatrix mat, Vector vec)
        {
            int size = mat.size;
            int[] ig = mat.ig;
            int[] jg = mat.jg;

            double[] di = mat.di;
            double[] ggu = mat.ggu;
            double[] ggl = mat.ggl;

            Vector result = new Vector(size);

            for (int i = 0; i < size; i++)
            {
                for (int j = ig[i]; j < ig[i + 1]; j++) 
                {
                    result[jg[j]] += vec[i] * ggu[j];
                    result[i] += vec[jg[j]] * ggl[j];
                }
                result[i] += di[i] * vec[i];
            }
            return result;
        }
        public override string ToString()
        {
            string text = "";
            int i, j, k;

            for(i = 0; i < size; i++)
            {
                for (j = 0; j < jg[i]; j++)
                    text += 0.ToString("E2") + " ";
                for(j = ig[i]; j < ig[i + 1]; j++)
                {
                    text += ggl[j].ToString("E2") + " ";
                    for (k = jg[j]; k < jg[j + 1]; k++)
                        text += 0.ToString("E2") + " ";
                }
                for(j = i + 1; j < size; j++)
                {
                    for(k = ig[j]; k < ig[j + 1]; k++)
                        if(jg[k] == i)
                            break;
                    text += (k == ig[j + 1] ? 0 : ggu[k]).ToString("E2") + " "; 
                }
                text += '\n';
            }
            return text;
        }
        public string ToString(string format = "E2")
        {
            string text = "";
            int i, j, k;

            for (i = 0; i < size; i++)
            {
                for (j = 0; j < jg[i]; j++)
                    text += 0.ToString(format) + " ";
                for (j = ig[i]; j < ig[i + 1]; j++)
                {
                    text += ggl[j].ToString(format) + " ";
                    for (k = jg[j]; k < jg[j + 1]; k++)
                        text += 0.ToString(format) + " ";
                }
                for (j = i + 1; j < size; j++)
                {
                    for (k = ig[j]; k < ig[j + 1]; k++)
                        if (jg[k] == i)
                            break;
                    text += (k == ig[j + 1] ? 0 : ggu[k]).ToString(format) + " ";
                }
                text += '\n';
            }
            return text;
        }
    }
    public class SymmSparseMatrix : Matrix
    {
        public double[] di, gg;
        public int[] ig, jg;
        public SymmSparseMatrix(int size, int[] ig, int[] jg, double[] di, double[] gg) : base(size)
        {
            this.ig = ig;
            this.jg = jg;
            this.di = di;
            this.gg = gg;
        }
        public static SymmSparseMatrix Parse(string size, string ig, string jg,
            string di, string gg)
        {
            return new SymmSparseMatrix(int.Parse(size),
                ig.Split(' ').Select(value => int.Parse(value)).ToArray(),
                jg.Split(' ').Select(value => int.Parse(value)).ToArray(),
                di.Split(' ').Select(value => double.Parse(value.Replace(".", ","))).ToArray(),
                gg.Split(' ').Select(value => double.Parse(value.Replace(".", ","))).ToArray());
        }
        public Matrix Cast<T>()
        {
            if(typeof(T) == typeof(SparseMatrix))
            {
                int i, j;
                
                double[] newMatrix_di = new double[size];
                int[] newMatrix_ig = new int[size + 1];
                int[] newMatrix_jg = new int[jg.Length];
                double[] newMatrix_ggl = new double[gg.Length];
                double[] newMatrix_ggu = new double[gg.Length];

                SparseMatrix newMatrix = new SparseMatrix(size, newMatrix_ig, newMatrix_jg, newMatrix_di, newMatrix_ggu, newMatrix_ggl);
                for(i = 0; i < size; i++)
                {
                    newMatrix_ig[i + 1] = ig[i + 1];
                    newMatrix_di[i] = di[i];

                    for (j = ig[i]; j < ig[i + 1]; j++)
                    {
                        newMatrix_ggl[j] = gg[j];
                        newMatrix_ggu[j] = gg[j];
                        newMatrix_jg[j] = jg[j];
                    }
                }
                return newMatrix;
            }
            return null;
        }
        public static Vector operator *(SymmSparseMatrix mat, Vector vec)
        {
            int size = mat.size;
            int[] ig = mat.ig;
            int[] jg = mat.jg;

            double[] di = mat.di;
            double[] gg = mat.gg;

            Vector result = new Vector(size);

            for (int i = 0; i < size; i++)
            {
                for (int j = ig[i]; j < ig[i + 1]; j++)
                {
                    result[jg[j]] += vec[i] * gg[j];
                    result[i] += vec[jg[j]] * gg[j];
                }
                result[i] += di[i] * vec[i];
            }
            return result;
        }
        public void ClearValues()
        {
            int i, j;
            for(i = 0; i < size; i++)
            {
                di[i] = 0;
                for (j = ig[i]; j < ig[i + 1]; j++)
                    gg[j] = 0;
            }
        }
        public override string ToString()
        {
            string text = "";
            int i, j, k;

            for (i = 0; i < size; i++)
            {
                for (j = 0; j < jg[i]; j++)
                    text += 0.ToString("E2") + " ";
                for (j = ig[i]; j < ig[i + 1]; j++)
                {
                    text += gg[j].ToString("E2") + " ";
                    for (k = jg[j]; k < jg[j + 1]; k++)
                        text += 0.ToString("E2") + " ";
                }
                for (j = i + 1; j < size; j++)
                {
                    for (k = ig[j]; k < ig[j + 1]; k++)
                        if (jg[k] == i)
                            break;
                    text += (k == ig[j + 1] ? 0 : gg[k]).ToString("E2") + " ";
                }
                text += '\n';
            }
            return text;
        }
        public string ToString(string format = "E2")
        {
            string text = "";
            int i, j, k;

            for (i = 0; i < size; i++)
            {
                for (j = 0; j < jg[i]; j++)
                    text += 0.ToString(format) + " ";
                for (j = ig[i]; j < ig[i + 1]; j++)
                {
                    text += gg[j].ToString(format) + " ";
                    for (k = jg[j]; k < jg[j + 1]; k++)
                        text += 0.ToString(format) + " ";
                }
                for (j = i + 1; j < size; j++)
                {
                    for (k = ig[j]; k < ig[j + 1]; k++)
                        if (jg[k] == i)
                            break;
                    text += (k == ig[j + 1] ? 0 : gg[k]).ToString(format) + " ";
                }
                text += '\n';
            }
            return text;
        }
    }
}