using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath.Splines
{
    public class InterpolationSpline : ISpline1D
    {
        Vector2[] points;
    
        private double[] coeffs;
        private int size;

        public InterpolationSpline(Vector2[] points)
        {
            this.points = points;
            size = points.Length;
            build();
        }

        private void build()
        {
            double l, r, c;

            coeffs = new double[size];
            double[,] diagonals = new double[3, size];
            int[] offsets = new int[] { -1, 0, 1 };

            double next_h = points[2].X - points[1].X; // h1
            double prev_h = points[1].X - points[0].X; // h2
    
            // f'0
            l = -1.0 * (3.0 * prev_h + 2.0 * next_h) /
                (prev_h * (prev_h + next_h));
            c = (2.0 * next_h + prev_h) /
                (prev_h * next_h);
            r = -1.0 * prev_h /
                (next_h * (prev_h + next_h));
            coeffs[0] = 0.5 * (points[0].Y * l + points[1].Y * c + points[2].Y * r);
    
            // f'n
            next_h = points[size - 1].X - points[size - 2].X;
            prev_h = points[size - 2].X - points[size - 3].X;
            l = next_h / (prev_h * (prev_h + next_h));
            c = -1.0 * (next_h + 2.0 * prev_h) / (prev_h * next_h);
            r = (3.0 * next_h + 2.0 * prev_h) / (next_h * (prev_h + next_h));
            coeffs[size - 1] = 0.5 * (points[size - 3].Y * l + points[size - 2].Y * c + points[size - 1].Y * r);
    
            diagonals[1, 0] = 1;
            diagonals[1, size - 1] = 1;
    
            for (int i = 1; i < size - 1; i++)
            {
                next_h = points[i + 1].X - points[i].X;
                prev_h = points[i].X - points[i - 1].X;
    
                diagonals[0, i] = 2.0 / prev_h;
                diagonals[1, i] = 4.0 * (1.0 / prev_h + 1.0 / next_h);
                diagonals[2, i] = 2.0 / next_h;
    
                coeffs[i] = -6.0 * points[i - 1].Y / (prev_h * prev_h) +
                    6.0 * points[i].Y * (1.0 / (prev_h * prev_h) - 1.0 / (next_h * next_h)) +
                    6.0 * points[i + 1].Y / (next_h * next_h);
            }
    
            DiagMatrix matrix = new DiagMatrix(size, offsets, diagonals);
            Vector vec = new Vector(coeffs);
            Vector x = new Vector(coeffs.Length);
    
            SoLESolver.Epsilon = 1E-12;
            SoLESolver.SolveSeidel(matrix, vec, x, 1.5);

            coeffs = x.values;
        }

        public double GetValue(double x)
        {
            int i;

            for (i = 0; i < size - 1; i++)
                if (points[i].X <= x && x <= points[i + 1].X)
                    break;
            if (i == size - 1)
                return double.NaN;
    
            double h = points[i + 1].X - points[i].X;
            double ksi = (x - points[i].X) / h;
            double ksi_2 = ksi * ksi;
            double ksi_3 = ksi_2 * ksi;
    
            return points[i].Y * (1.0 - 3.0 * ksi_2 + 2.0 * ksi_3) +
                coeffs[i] * h * (ksi - 2.0 * ksi_2 + ksi_3) +
                points[i + 1].Y * (3.0 * ksi_2 - 2.0 * ksi_3) +
                coeffs[i + 1] * h * (-1.0 * ksi_2 + ksi_3);
        }
    }
}