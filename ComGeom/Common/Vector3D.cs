﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ComGeom
{
    public struct Vector3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double Norm => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double SqrNorm => X * X + Y * Y + Z * Z;

        public Vector3D Zero => new Vector3D(0, 0, 0);
        public Vector3D UnitX => new Vector3D(1, 0, 0);
        public Vector3D UnitY => new Vector3D(0, 1, 0);
        public Vector3D UnitZ => new Vector3D(0, 0, 1);

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3D(params double[] values)
        {
            if(values == null || values.Length != 3)
                throw new ArgumentException("Parameter is invalid.", nameof(values));

            X = values[0];
            Y = values[1];
            Z = values[2];
        }

        public static Vector3D operator+(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3D operator-(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3D operator*(double number, Vector3D vector)
        {
            return new Vector3D(number * vector.X, number * vector.Y, number * vector.Z);
        }

        public static Vector3D operator*(Vector3D vector, double number)
        {
            return new Vector3D(number * vector.X, number * vector.Y, number * vector.Z);
        }

        public static double operator*(Vector3D a, Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public Vector3D Cross(Vector3D other)
        {
            return new Vector3D(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
        }

        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        public double SqrDistance(Vector3D other)
        {
            double dx = other.X - X;
            double dy = other.Y - Y;
            double dz = other.Z - Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public Vector3D Unify()
        {
            double norm = Norm;
            return new Vector3D(X / norm, Y / norm, Z / norm);
        }
    }
}
