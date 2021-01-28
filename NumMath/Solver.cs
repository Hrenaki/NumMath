using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public partial class SoLESolver
    {
        private readonly int maxSteps = 1000;
        public Matrix matrix { get; private set; }
        public Vector vec { get; private set; }
        public SoLESolver(Matrix mat, Vector vec)
        {
            matrix = mat;
            this.vec = vec;
        }
    }
}
