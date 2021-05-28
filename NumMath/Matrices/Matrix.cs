using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public abstract class Matrix
    {
        public int size { get; private set; }
        public Matrix(int size)
        {
            this.size = size;
        }

        public abstract T Cast<T>() where T : Matrix;
    }
}
