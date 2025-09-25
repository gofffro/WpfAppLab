using System;
using NCalc;

namespace WpfApp1
{
    public class DihotomyMethod
    {
        private readonly Expression _expression;
        public int IterationsCount { get; private set; }

        public DihotomyMethod(string function)
        {
            _expression = new Expression(function.ToLower(), EvaluateOptions.IgnoreCase);

            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;

            _expression.EvaluateFunction += EvaluateFunction;
        }

        private void EvaluateFunction(string name, FunctionArgs args)
        {
            switch (name.ToLower())
            {
                case "sin":
                    args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "cos":
                    args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "tan":
                    args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "atan":
                    args.Result = Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "exp":
                    args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "sqrt":
                    args.Result = Math.Sqrt(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "abs":
                    args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "log":
                    if (args.Parameters.Length == 1)
                    {
                        args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    }
                    else if (args.Parameters.Length == 2)
                    {
                        args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()),
                                             Convert.ToDouble(args.Parameters[1].Evaluate()));
                    }
                    else
                    {
                        throw new ArgumentException("Функция log требует 1 или 2 аргумента");
                    }
                    break;
                case "log10":
                    if (args.Parameters.Length == 1)
                    {
                        args.Result = Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    }
                    else
                    {
                        throw new ArgumentException("Функция log10 требует 1 аргумент");
                    }
                    break;
                case "pow":
                    args.Result = Math.Pow(Convert.ToDouble(args.Parameters[0].Evaluate()), Convert.ToDouble(args.Parameters[1].Evaluate()));
                    break;
                default:
                    throw new ArgumentException($"Неизвестная функция: {name}");
            }
        }

        public double CalculateFunction(double x)
        {
            try
            {
                if (Math.Abs(x) > 1e10)
                {
                    return double.MaxValue / 1000;
                }

                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result is double doubleResult)
                {
                    if (double.IsInfinity(doubleResult) || double.IsNaN(doubleResult))
                    {
                        return double.MaxValue;
                    }

                    return doubleResult;
                }

                if (result is int intResult)
                {
                    return intResult;
                }

                if (result is decimal decimalResult)
                {
                    return (double)decimalResult;
                }

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public List<double> FindRoots(double a, double b, double epsilon, int maxRoots = 10)
        {
            if (a >= b)
            {
                throw new ArgumentException("Интервал [a, b] задан неверно");
            }

            if (epsilon <= 0)
            {
                throw new ArgumentException("Точность epsilon должна быть положительной");
            }

            List<double> roots = new List<double>();
            IterationsCount = 0;

            int segments = 100; 
            double segmentStep = (b - a) / segments;

            for (int i = 0; i < segments; ++i)
            {
                double segmentStart = a + i * segmentStep;
                double segmentEnd = segmentStart + segmentStep;

                double fStart = CalculateFunction(segmentStart);
                double fEnd = CalculateFunction(segmentEnd);

                if (Math.Abs(fStart) < epsilon)
                {
                    AddRootIfNew(roots, segmentStart, epsilon);
                    continue;
                }

                if (fStart * fEnd < 0)
                {
                    double root = FindSingleRoot(segmentStart, segmentEnd, epsilon);
                    AddRootIfNew(roots, root, epsilon);
                }

                else if (Math.Abs(fStart) < epsilon * 10 && Math.Abs(fEnd) < epsilon * 10)
                {
                    double mid = (segmentStart + segmentEnd) / 2;
                    if (Math.Abs(CalculateFunction(mid)) < epsilon)
                    {
                        AddRootIfNew(roots, mid, epsilon);
                    }
                }

                if (roots.Count >= maxRoots)
                {
                    break;
                }
            }

            return roots;
        }

        private void AddRootIfNew(List<double> roots, double newRoot, double epsilon)
        {
            if (!roots.Any(root => Math.Abs(root - newRoot) < epsilon))
            {
                roots.Add(newRoot);
            }
        }

        private double FindSingleRoot(double a, double b, double epsilon)
        {
            double fa = CalculateFunction(a);
            double fb = CalculateFunction(b);

            if (Math.Abs(fa) < epsilon)
            {
                return a;
            }

            if (Math.Abs(fb) < epsilon)
            {
                return b;
            }

            if (fa * fb >= 0)
            {
                throw new ArgumentException("На интервале нет смены знака");
            }

            while (Math.Abs(b - a) > epsilon)
            {
                double mid = (a + b) / 2;
                double fmid = CalculateFunction(mid);

                IterationsCount++;

                if (Math.Abs(fmid) < epsilon)
                {
                    return mid;
                }

                if (fa * fmid < 0)
                {
                    b = mid;
                    fb = fmid;
                }
                else
                {
                    a = mid;
                    fa = fmid;
                }

                if (IterationsCount > 1000)
                {
                    break;
                }
            }

            return (a + b) / 2;
        }

        public bool IsConstantFunction(double a, double b)
        {
            double[] testPoints = { a, (a + b) / 2, b, a + (b - a) / 4, a + 3 * (b - a) / 4 };
            double firstValue = CalculateFunction(testPoints[0]);

            foreach (double point in testPoints)
            {
                if (Math.Abs(CalculateFunction(point) - firstValue) > 1e-15)
                {
                    return false;
                }
            }
            return true;
        }
    }
}