using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath
{
    public class Vector
    {
        public double[] values;
        public double magnitude { get { double val = 0; foreach (double v in values) val += v * v; return Math.Sqrt(val); } }
        public double sqrMagnitude { get { double val = 0; foreach (double v in values) val += v * v; return val; } }
        public int size { get { return values.Length; } }
        public double this[int i]
        {
            get { return values[i]; }
            set { values[i] = value; }
        }
        public Vector(int size)
        {
            values = new double[size];
        }
        public Vector(params double[] values)
        {
            this.values = values;
        }
        public override string ToString()
        {
            return string.Join(" ", values.Select(value => value.ToString("F2")));
        }
        public static Vector Parse(string text)
        {
            return new Vector(text.Trim().Split(' ').Select((string word) => { return double.Parse(word); }).ToArray());
        }
        public static Vector GenerateSimpleVec(int size)
        {
            Vector v = new Vector(size);
            for (int i = 0; i < v.size; i++)
                v[i] = i + 1;
            return v;
        }
        public static Vector operator+(Vector lhs, Vector rhs)
        {
            if (lhs.size != rhs.size)
                throw new ArgumentOutOfRangeException();
            Vector res = new Vector(lhs.size);
            for (int i = 0; i < res.size; i++)
                res[i] = lhs[i] + rhs[i];
            return res;
        }
        public static void Sum(Vector lhs, Vector rhs)
        {
            if (lhs.size != rhs.size)
                throw new ArgumentOutOfRangeException();
            for (int i = 0; i < lhs.size; i++)
                lhs[i] += rhs[i];
        }
        public static Vector operator-(Vector lhs, Vector rhs)
        {
            if (lhs.size != rhs.size)
                throw new ArgumentOutOfRangeException();
            Vector res = new Vector(lhs.size);
            for (int i = 0; i < res.size; i++)
                res[i] = lhs[i] - rhs[i];
            return res;
        }
        public static void Sub(Vector lhs, Vector rhs)
        {
            if (lhs.size != rhs.size)
                throw new ArgumentOutOfRangeException();
            for (int i = 0; i < lhs.size; i++)
                lhs[i] -= rhs[i];
        }
        public static Vector operator*(double w, Vector vec)
        {
            Vector res = new Vector(vec.size);
            for (int i = 0; i < vec.size; i++)
                res[i] = w * vec[i];
            return res;
        }
        public static Vector operator/(Vector vec, double w)
        {
            Vector res = new Vector(vec.size);
            for (int i = 0; i < res.size; i++)
                res[i] = vec[i] / w;
            return res;
        }
    }
}