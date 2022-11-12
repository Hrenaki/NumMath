using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath.Vectors
{
    public class Vector4 : Vector
    {
        public double X { get => values[0]; set => values[0] = value; }
        public double Y { get => values[1]; set => values[1] = value; }
        public double Z { get => values[2]; set => values[2] = value; }
        public double W { get => values[3]; set => values[3] = value; }

        public Vector4(double value) : base(4)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        public Vector4(double x, double y, double z, double w) : base(4)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }
    public class Vector4f
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public float this[int i]
        {
            get
            {  
                switch(i)
                {
                    case 1: return X;
                    case 2: return Y;
                    case 3: return Z;
                    case 4: return W;
                    default: throw new IndexOutOfRangeException("Vector4f index out of range!");
                } 
            }
            set
            {
                switch (i)
                {
                    case 1: X = value; break;
                    case 2: Y = value; break;
                    case 3: Z = value; break;
                    case 4: Z = value; break;
                    default: throw new IndexOutOfRangeException("Vector4f index out of range!");
                }
            }
        }
        public Vector4f(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }
        public Vector4f(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public static Vector4f operator*(float a, Vector4f b)
        {
            return new Vector4f(a * b.X, a * b.Y, a * b.Z, a * b.W);
        }
        public static Vector4f operator+(Vector4f left, Vector4f right)
        {
            return new Vector4f(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
        }
        public static Vector4f operator-(Vector4f left, Vector4f right)
        {
            return new Vector4f(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
        }
    }
}