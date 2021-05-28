using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class ProfileMatrix : Matrix
    {
        public double[] di, au, al;
        public int[] ia;
        public double this[int i, int j]
        {
            get 
            {
                if (i != j)
                {
                    if (i - (ia[i + 1] - ia[i]) > j)
                        return 0;
                    for (int k = ia[i]; k < ia[i + 1]; k++)
                        if (i - (ia[i + 1] - k) == j)
                            return i > j ? au[k] : al[k];
                    return 0;
                }
                else return di[i];
            }
        }

        public ProfileMatrix(int size, int[] ia, double[] di, double[] au, double[] al) : base(size)
        {
            this.ia = ia;
            this.di = di;
            this.au = au;
            this.al = al;
        }
        public static ProfileMatrix Parse(string size, string ia, 
            string di, string au, string al)
        {
            return new ProfileMatrix(int.Parse(size), ia.Split(' ').Select(value => int.Parse(value)).ToArray(),
                di.Split(' ').Select(value => double.Parse(value)).ToArray(), 
                au.Split(' ').Select(value => double.Parse(value)).ToArray(),
                al.Split(' ').Select(value => double.Parse(value)).ToArray());
        }
        public static Vector operator *(ProfileMatrix mat, Vector vec)
        {
            int size = mat.size;           
            int[] ia = mat.ia;
            double[] di = mat.di;
            double[] au = mat.au;
            double[] al = mat.al;

            Vector res = new Vector(size);

            int i, j;
            for (i = 0; i < size; i++)
            {
                for(j = ia[i]; j < ia[i + 1]; j++)
                {
                    res[i] += al[j] * vec[i - (ia[i + 1] - j)];
                    res[i - (ia[i + 1] - j)] += au[j] * vec[i];
                }
                res[i] += di[i] * vec[i];
            }
            return res;
        }
        public void PrintPotrait()
        {
            int i, j;
            string str = "";
            for(i = 0; i < size; i++)
            {
                for (j = 0; j < i - ia[i + 1] + ia[i]; j++)
                    str += "0 ";
                for (j = ia[i]; j < ia[i + 1]; j++)
                    str += "* ";
                str += "*\n";
            }
            File.WriteAllText("text2.txt", str);
        }
        public override string ToString()
        {
            string text = "";
            int i, j, k;
            for(i = 0; i < size; i++)
            {
                for (j = 0; j < i - ia[i + 1] + ia[i]; j++)
                    text += 0.ToString("E2") + " ";
                for (j = ia[i]; j < ia[i + 1]; j++)
                    text += au[j].ToString("E2") + " ";
                text += di[i].ToString("E2") + " ";
                for(j = i + 1; j < size; j++)
                    for (k = ia[j]; k < ia[j + 1]; k++)
                    {
                        if (j - ia[j + 1] + k >= j)
                        {
                            text += (j - ia[j + 1] + k == j ? al[k] : 0).ToString("E2") + " ";
                            break;
                        } 
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
                for (j = 0; j < i - ia[i + 1] + ia[i]; j++)
                    text += 0.ToString(format) + " ";
                for (j = ia[i]; j < ia[i + 1]; j++)
                    text += au[j].ToString(format) + " ";
                text += di[i].ToString(format) + " ";
                for (j = i + 1; j < size; j++)
                    for (k = ia[j]; k < ia[j + 1]; k++)
                    {
                        if (j - ia[j + 1] + k >= j)
                        {
                            text += (j - ia[j + 1] + k == j ? al[k] : 0).ToString(format) + " ";
                            break;
                        }
                    }
                text += '\n';
            }
            return text;
        }

        public override T Cast<T>()
        {
            throw new NotImplementedException();
        }
    }
    public class SymmProfileMatrix : Matrix
    {
        public double[] di, a;
        public int[] ia;
        public SymmProfileMatrix(int size, int[] ia, double[] di, double[] a) : base(size)
        {
            this.ia = ia;
            this.di = di;
            this.a = a;
        }
        public static SymmProfileMatrix Parse(string size, string ia, string di, string a)
        {
            return new SymmProfileMatrix(int.Parse(size), ia.Split(' ').Select(value => int.Parse(value)).ToArray(),
                di.Split(' ').Select(value => double.Parse(value)).ToArray(), 
                a.Split(' ').Select(value => double.Parse(value)).ToArray());
        }
        public static Vector operator *(SymmProfileMatrix mat, Vector vec)
        {
            Vector result = new Vector(mat.size);
            for (int i = 0; i < mat.size; i++)
            {
                for (int j = i - mat.ia[i + 1] + mat.ia[i]; j < i; j++)
                {
                    result[i] += vec[j] * mat.a[mat.ia[i + 1] - i + j];
                    result[j] += vec[i] * mat.a[mat.ia[i + 1] - i + j];
                }
                result[i] += mat.di[i] * vec[i];
            }
            return result;
        }
        public override string ToString()
        {
            string text = "";
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (j > i)
                    {
                        text += (j - i <= ia[j + 1] - ia[j] ? a[ia[j + 1] - j + i] : 0).ToString("E2") + ' ';
                    }
                    else if (i == j)
                    {
                        text += di[i].ToString("E2") + ' ';
                    }
                    else
                    {
                        text += (i - j <= ia[i + 1] - ia[i] ? a[ia[i + 1] - i + j] : 0).ToString("E2") + ' ';
                    }
                }
                text += '\n';
            }
            return text;
        }
        public string ToString(string format = "E2")
        {
            string text = "";
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (j > i)
                    {
                        text += (j - i <= ia[j + 1] - ia[j] ? a[ia[j + 1] - j + i] : 0).ToString(format) + ' ';
                    }
                    else if (i == j)
                    {
                        text += di[i].ToString(format) + ' ';
                    }
                    else
                    {
                        text += (i - j <= ia[i + 1] - ia[i] ? a[ia[i + 1] - i + j] : 0).ToString(format) + ' ';
                    }
                }
                text += '\n';
            }
            return text;
        }

        public override T Cast<T>()
        {
            throw new NotImplementedException();
        }
    }
}
