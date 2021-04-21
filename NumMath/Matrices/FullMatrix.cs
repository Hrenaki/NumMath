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
        public int size { get; private set; }
        public double this[int i, int j]
        {
            get { return values[i, j]; }
            set { values[i, j] = value; }
        }
        public FullMatrix(int size)
        {
            this.size = size;
            values = new double[size, size];
        }
        public FullMatrix(double[,] values)
        {
            this.size = values.Length / 2;
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
    }
}
