using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class Vector2 : Vector
    {
        public double X { get { return values[0]; } set { values[0] = value; } }
        public double Y { get { return values[1]; } set { values[1] = value; } }

        public Vector2() : base (2)
        {

        }
        public Vector2(double x, double y)
        {
            values = new double[2];
            values[0] = x;
            values[1] = y;
        }
        public Vector2(double[] t) : base(t) { }
        public double Distance(Vector2 to)
        {
            return (values[0] - to.values[0]) * (values[0] - to.values[0]) + (values[1] - to.values[1]) * (values[1] - to.values[1]);
        }
    }
}