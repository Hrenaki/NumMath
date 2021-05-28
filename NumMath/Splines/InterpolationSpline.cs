using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumMath.Splines
{
    public class InterpolationSpline : ISpline2D
    {
        Vector2[] points;
    
        private double[] coeffs;
        private int size;
    
        private int i;
        private double h;
        private double ksi;
        private double ksi_2;
        private double ksi_3;
    
        // Matrix of second derivative coeffs in diagonal format
        private double[,] diagonals;
        private static int[] offsets = new int[] { -1, 0, 1 };
    
        private void build()
        {
            double l, r, c;
    
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
        public void CreateSpline(Vector2[] points)
        {
            this.points = points;
    
            size = points.Length;
            coeffs = new double[size];
            diagonals = new double[3, size];
    
            build();
        }
        public double GetValue(double x)
        {
            for (i = 0; i < size - 1; i++)
                if (points[i].X <= x && x <= points[i + 1].X)
                    break;
            if (i == size - 1)
                return double.NaN;
    
            h = points[i + 1].X - points[i].X;
            ksi = (x - points[i].X) / h;
            ksi_2 = ksi * ksi;
            ksi_3 = ksi_2 * ksi;
    
            return points[i].Y * (1.0 - 3.0 * ksi_2 + 2.0 * ksi_3) +
                coeffs[i] * h * (ksi - 2.0 * ksi_2 + ksi_3) +
                points[i + 1].Y * (3.0 * ksi_2 - 2.0 * ksi_3) +
                coeffs[i + 1] * h * (-1.0 * ksi_2 + ksi_3);
        }
    }

    //public class InterpolationSpline
    //{
    //    private double[] vertexes;
    //    private double[] values;
    //    private double[] coeffs;
    //    private int size;
    //
    //    private int i;
    //    private double h;
    //    private double ksi;
    //    private double ksi_2;
    //    private double ksi_3;
    //
    //    // Matrix of second derivative coeffs in diagonal format
    //    private double[,] diagonals;
    //    private static int[] offsets = new int[] { -1, 0, 1 };
    //
    //    public InterpolationSpline(double[] vertexes, double[] values)
    //    {
    //        if (vertexes.Length != values.Length)
    //            throw new IndexOutOfRangeException();
    //        else
    //        {
    //            this.vertexes = vertexes;
    //            this.values = values;
    //            size = vertexes.Length;
    //            coeffs = new double[vertexes.Length];
    //        }
    //
    //        diagonals = new double[3, vertexes.Length];
    //    }
    //
    //    public InterpolationSpline(double[] vertexes, Func<double, double> values)
    //    {
    //        this.vertexes = vertexes;
    //        size = vertexes.Length;
    //        coeffs = new double[size];
    //
    //        this.values = new double[size];
    //        for (int i = 0; i < size; i++)
    //            this.values[i] = values(vertexes[i]);
    //
    //        diagonals = new double[3, size];
    //    }
    //
    //    public void CreateSpline()
    //    {
    //        double l, r, c;
    //
    //        double next_h = vertexes[2] - vertexes[1]; // h1
    //        double prev_h = vertexes[1] - vertexes[0]; // h2
    //
    //        // f'0
    //        l = -1.0 * (3.0 * prev_h + 2.0 * next_h) /
    //            (prev_h * (prev_h + next_h));
    //        c = (2.0 * next_h + prev_h) /
    //            (prev_h * next_h);
    //        r = -1.0 * prev_h /
    //            (next_h * (prev_h + next_h));
    //        coeffs[0] = 0.5 * (values[0] * l + values[1] * c + values[2] * r);
    //
    //        // f'n
    //        next_h = vertexes[size - 1] - vertexes[size - 2];
    //        prev_h = vertexes[size - 2] - vertexes[size - 3];
    //        l = next_h / (prev_h * (prev_h + next_h));
    //        c = -1.0 * (next_h + 2.0 * prev_h) / (prev_h * next_h);
    //        r = (3.0 * next_h + 2.0 * prev_h) / (next_h * (prev_h + next_h));
    //        coeffs[size - 1] = 0.5 * (values[size - 3] * l + values[size - 2] * c + values[size - 1] * r);
    //
    //        diagonals[1, 0] = 1;
    //        diagonals[1, size - 1] = 1;
    //
    //        for (int i = 1; i < size - 1; i++)
    //        {
    //            next_h = vertexes[i + 1] - vertexes[i];
    //            prev_h = vertexes[i] - vertexes[i - 1];
    //
    //            diagonals[0, i] = 2.0 / prev_h;
    //            diagonals[1, i] = 4.0 * (1.0 / prev_h + 1.0 / next_h);
    //            diagonals[2, i] = 2.0 / next_h;
    //
    //            coeffs[i] = -6.0 * values[i - 1] / (prev_h * prev_h) +
    //                6.0 * values[i] * (1.0 / (prev_h * prev_h) - 1.0 / (next_h * next_h)) +
    //                6.0 * values[i + 1] / (next_h * next_h);
    //        }
    //
    //        DiagMatrix matrix = new DiagMatrix(size, offsets, diagonals);
    //        Vector vec = new Vector(coeffs);
    //        Vector x = new Vector(coeffs.Length);
    //
    //        SoLESolver.Epsilon = 1E-12;
    //        SoLESolver.SolveSeidel(matrix, vec, x, 1.5);
    //    }
    //
    //    public double getValue(double x)
    //    {
    //        for (i = 0; i < size - 1; i++)
    //            if (vertexes[i] <= x && x <= vertexes[i + 1])
    //                break;
    //        if (i == size - 1)
    //            return double.NaN;
    //
    //        h = vertexes[i + 1] - vertexes[i];
    //        ksi = (x - vertexes[i]) / h;
    //        ksi_2 = ksi * ksi;
    //        ksi_3 = ksi_2 * ksi;
    //
    //        return values[i] * (1.0 - 3.0 * ksi_2 + 2.0 * ksi_3) +
    //            coeffs[i] * h * (ksi - 2.0 * ksi_2 + ksi_3) +
    //            values[i + 1] * (3.0 * ksi_2 - 2.0 * ksi_3) +
    //            coeffs[i + 1] * h * (-1.0 * ksi_2 + ksi_3);
    //    }
    //}
}