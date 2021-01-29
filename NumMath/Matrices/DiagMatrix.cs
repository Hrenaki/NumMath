using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class DiagMatrix : Matrix
    {
        public double[,] values;
        public int[] offsets;
        public int size { get; private set; }
        public int diagCount { get; private set; }
        public int mainDiagSize { get; private set; }
        public double this[int i, int j]
        {
            get
            {
                int diagIndex = Array.IndexOf(offsets, j - i);
                return diagIndex == -1 ? 0 : values[diagIndex, i];
            }
        }
        public DiagMatrix(int size, int[] diagOffsets, double[,] values)
        {
            this.size = size;
            diagCount = diagOffsets.Length;
            offsets = diagOffsets;
            this.values = values;

            mainDiagSize = 1;
            int mainDiagIndex = Array.IndexOf(offsets, 0);
            for (int i = 1; mainDiagIndex - i >= 0 && mainDiagIndex + i < diagCount; i++, mainDiagSize++)
            {
                if (!offsets.Contains(-i) || !offsets.Contains(i))
                    break;
            }
        }
        public static DiagMatrix ParseMatrixDiag(string size_string, string i_string, string[] diagonals)
        {
            int size = int.Parse(size_string);
            int[] diagoffsets = i_string.Split(' ').Select(value => int.Parse(value)).ToArray();

            double[] diag = diagonals[0].Split(' ').Select(value => double.Parse(value)).ToArray();
            double[,] values = new double[diagonals.Length, diag.Length];

            for (int i = 0; i < diagonals.Length; i++)
            {
                diag = diagonals[i].Split(' ').Select(value => double.Parse(value)).ToArray();
                for (int j = 0; j < diag.Length; j++)
                    values[i, j] = diag[j];
            }

            return new DiagMatrix(size, diagoffsets, values);
        }
        public static Vector operator *(DiagMatrix mat, Vector vec)
        {
            int size = mat.size;
            int i, j, k;
            int diagCount = mat.diagCount;
            double[,] values = mat.values;
            int[] offsets = mat.offsets;
            double[] x = vec.values;

            Vector res = new Vector(size);

            for (i = 0; i < diagCount; i++)
                for (j = Math.Max(-offsets[i], 0), k = Math.Max(offsets[i], 0); j < size && k < size; j++, k++)
                    res[j] += x[k] * values[i, j];
            return res;
        }
    }
}
