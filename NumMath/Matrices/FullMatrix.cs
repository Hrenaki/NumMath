using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class FullMatrix : Matrix
    {
        public double[,] values;
        public double this[int i, int j]
        {
            get { return values[i, j]; }
            set { values[i, j] = value; }
        }
        public FullMatrix(int size) : base(size)
        {
            values = new double[size, size];
        }
        public FullMatrix(double[,] values) : base((int)Math.Sqrt(values.Length))
        {
            this.values = values;
        }
        public static FullMatrix Parse(string str)
        {
            string[] lines = str.Split('\n');
            FullMatrix matrix = new FullMatrix(lines.Length);
            for(int i = 0; i < matrix.size; i++)
            {
                string[] row = lines[i].Split(' ');
                for (int j = 0; j < matrix.size; j++)
                    matrix[i, j] = double.Parse(row[j]);
            }
            return matrix;
        }
        public static Vector operator*(FullMatrix mat, Vector vec)
        {
            if (mat.size != vec.size)
                return null;
            else
            {
                Vector res = new Vector(vec.size);
                for(int i = 0; i < mat.size; i++)
                    for (int j = 0; j < mat.size; j++)
                        res[i] += mat[i, j] * vec[j];
                return res;
            }
        }
        public override string ToString()
        {
            string text = "";
            for(int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                    text += values[i, j].ToString("E2") + " ";
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
                    text += values[i, j].ToString(format) + " ";
                text += '\n';
            }
            return text;
        }

        public override T Cast<T>()
        {
            Type curType = typeof(T);
            if(curType == typeof(SparseMatrix))
            {
                double[] di = new double[size];
                double[] ggl = new double[size * (size - 1) / 2];
                double[] ggu = new double[ggl.Length];

                int[] ig = new int[size + 1];
                int[] jg = new int[ggl.Length];

                for(int i = 0; i < size; i++)
                {
                    ig[i + 1] = ig[i];

                    for(int j = 0; j < i; j++, ig[i + 1]++)
                    {
                        ggl[ig[i + 1]] = values[i, j];
                        ggu[ig[i + 1]] = values[j, i];
                        jg[ig[i + 1]] = j;
                    }
                    di[i] = values[i, i];
                }

                return (new SparseMatrix(size, ig, jg, di, ggu, ggl)) as T;
            }
            return null;
        }
    }
}
