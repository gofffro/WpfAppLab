using System;
using System.Linq;

namespace WpfApp1.LeastSquareApp
{
    public static class LeastSquaresCalculator
    {
        public static LeastSquaresResult FitPolynomial((double x, double y)[] data, int degree)
        {
            if (degree != 1 && degree != 2)
                throw new ArgumentException("Поддерживаются только степени 1 и 2.");

            if (degree == 1 && data.Length < 2)
                throw new ArgumentException("Для n=1 нужно минимум 2 точки.");

            if (degree == 2 && data.Length < 3)
                throw new ArgumentException("Для n=2 нужно минимум 3 точки.");

            double[] a = degree == 1 ? FitLine(data) : FitQuadratic(data);

            var metrics = ComputeMetrics(data, a);

            return new LeastSquaresResult
            {
                Degree = degree,
                Coefficients = a,
                SSE = metrics.sse,
                RMSE = metrics.rmse,
                R2 = metrics.r2
            };
        }

        // y = a0 + a1 x
        private static double[] FitLine((double x, double y)[] data)
        {
            int n = data.Length;

            double Sx = data.Sum(p => p.x);
            double Sy = data.Sum(p => p.y);
            double Sxx = data.Sum(p => p.x * p.x);
            double Sxy = data.Sum(p => p.x * p.y);

            double D = n * Sxx - Sx * Sx;
            if (Math.Abs(D) < 1e-18)
                throw new ArgumentException("Вырожденная система для n=1 (проверьте значения X).");

            double a0 = (Sy * Sxx - Sx * Sxy) / D;
            double a1 = (n * Sxy - Sx * Sy) / D;

            return new[] { a0, a1 };
        }

        // y = a0 + a1 x + a2 x^2
        private static double[] FitQuadratic((double x, double y)[] data)
        {
            int n = data.Length;

            double Sx = data.Sum(p => p.x);
            double Sxx = data.Sum(p => p.x * p.x);
            double Sxxx = data.Sum(p => p.x * p.x * p.x);
            double Sxxxx = data.Sum(p => p.x * p.x * p.x * p.x);

            double Sy = data.Sum(p => p.y);
            double Sxy = data.Sum(p => p.x * p.y);
            double Sxxy = data.Sum(p => p.x * p.x * p.y);

            // Нормальные уравнения:
            // [ n    Sx     Sxx  ] [a0] = [ Sy   ]
            // [ Sx   Sxx    Sxxx ] [a1] = [ Sxy  ]
            // [ Sxx  Sxxx   Sxxxx] [a2] = [ Sxxy ]
            double[,] A = new double[3, 4]
            {
                { n,   Sx,   Sxx,   Sy   },
                { Sx,  Sxx,  Sxxx,  Sxy  },
                { Sxx, Sxxx, Sxxxx, Sxxy }
            };

            var sol = SolveGaussian3x3(A);
            return sol;
        }

        private static double[] SolveGaussian3x3(double[,] aug)
        {
            // aug: 3x4
            int N = 3;

            for (int col = 0; col < N; col++)
            {
                // поиск ведущей строки
                int pivot = col;
                double maxAbs = Math.Abs(aug[col, col]);
                for (int r = col + 1; r < N; r++)
                {
                    double v = Math.Abs(aug[r, col]);
                    if (v > maxAbs)
                    {
                        maxAbs = v;
                        pivot = r;
                    }
                }

                if (maxAbs < 1e-18)
                    throw new ArgumentException("Вырожденная система для n=2 (проверьте значения X).");

                // swap
                if (pivot != col)
                {
                    for (int c = col; c < N + 1; c++)
                    {
                        double tmp = aug[col, c];
                        aug[col, c] = aug[pivot, c];
                        aug[pivot, c] = tmp;
                    }
                }

                // нормировка ведущей строки
                double div = aug[col, col];
                for (int c = col; c < N + 1; c++)
                    aug[col, c] /= div;

                // зануление остальных строк
                for (int r = 0; r < N; r++)
                {
                    if (r == col) continue;
                    double factor = aug[r, col];
                    if (Math.Abs(factor) < 1e-18) continue;

                    for (int c = col; c < N + 1; c++)
                        aug[r, c] -= factor * aug[col, c];
                }
            }

            return new[] { aug[0, 3], aug[1, 3], aug[2, 3] };
        }

        public static double EvalPoly(double[] a, double x)
        {
            // Horner
            double res = 0;
            for (int i = a.Length - 1; i >= 0; i--)
                res = res * x + a[i];
            return res;
        }

        private static (double sse, double rmse, double r2) ComputeMetrics((double x, double y)[] data, double[] a)
        {
            int n = data.Length;

            double sse = 0;
            double meanY = data.Average(p => p.y);
            double sst = 0;

            foreach (var (x, y) in data)
            {
                double yhat = EvalPoly(a, x);
                double e = y - yhat;
                sse += e * e;

                double d = y - meanY;
                sst += d * d;
            }

            double rmse = Math.Sqrt(sse / n);
            double r2 = (Math.Abs(sst) < 1e-18) ? 1.0 : (1.0 - sse / sst);

            return (sse, rmse, r2);
        }
    }
}
