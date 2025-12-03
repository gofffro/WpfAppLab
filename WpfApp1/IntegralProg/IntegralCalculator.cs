using System;
using System.Collections.Generic;
using System.Linq;
using NCalc;

namespace WpfApp1.IntegralProg
{
    public class IntegralCalculator
    {
        private readonly Expression _expression;

        public IntegralCalculator(string function)
        {
            _expression = new Expression(function.ToLower(), EvaluateOptions.IgnoreCase);
            _expression.Parameters["pi"] = Math.PI;
            _expression.Parameters["e"] = Math.E;
            _expression.EvaluateFunction += EvaluateFunction;
        }

        public double CalculateFunction(double x)
        {
            try
            {
                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result is double d)
                {
                    if (double.IsNaN(d) || double.IsInfinity(d))
                        throw new ArgumentException($"Неопределенное значение функции в точке x={x}");
                    return d;
                }
                if (result is int i) return i;
                if (result is decimal m) return (double)m;
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public Dictionary<IntegrationMethod, IntegrationResult> CalculateIntegral(
            double a, double b, double epsilon, int initialN,
            List<IntegrationMethod> methods, bool autoSelectN)
        {
            var results = new Dictionary<IntegrationMethod, IntegrationResult>();

            foreach (var method in methods)
            {
                var result = new IntegrationResult { Method = method };

                if (autoSelectN)
                {
                    // Автоматический подбор N
                    result = AutoSelectN(a, b, epsilon, method, initialN);
                }
                else
                {
                    // Фиксированное N
                    result.Value = CalculateWithFixedN(a, b, initialN, method);
                    result.Iterations = initialN;
                    result.ErrorEstimate = EstimateError(a, b, initialN, method, result.Value);
                }

                results[method] = result;
            }

            return results;
        }

        private IntegrationResult AutoSelectN(double a, double b, double epsilon, IntegrationMethod method, int initialN)
        {
            var result = new IntegrationResult { Method = method };
            int n = initialN;
            double prevValue = 0;
            double currentValue = 0;
            int maxIterations = 20;

            for (int i = 0; i < maxIterations; i++)
            {
                currentValue = CalculateWithFixedN(a, b, n, method);
                result.History.Add(currentValue);

                if (i > 0)
                {
                    double error = Math.Abs(currentValue - prevValue);
                    result.ErrorEstimate = error;

                    if (error < epsilon || n > 1000000)
                    {
                        result.Value = currentValue;
                        result.Iterations = n;
                        return result;
                    }
                }

                prevValue = currentValue;
                n *= 2; // Удваиваем количество разбиений

                // Для Симпсона делаем четным
                if (method == IntegrationMethod.Simpson && n % 2 != 0) n++;
            }

            result.Value = currentValue;
            result.Iterations = n;
            result.ErrorEstimate = Math.Abs(currentValue - prevValue);
            return result;
        }

        private double CalculateWithFixedN(double a, double b, int n, IntegrationMethod method)
        {
            // Для Симпсона делаем четным
            if (method == IntegrationMethod.Simpson && n % 2 != 0)
                n++;

            double h = (b - a) / n;

            return method switch
            {
                IntegrationMethod.RectangleLeft => RectangleLeft(a, h, n),
                IntegrationMethod.RectangleRight => RectangleRight(a, h, n),
                IntegrationMethod.RectangleMidpoint => RectangleMidpoint(a, h, n),
                IntegrationMethod.Trapezoidal => Trapezoidal(a, b, h, n),
                IntegrationMethod.Simpson => Simpson(a, b, h, n),
                _ => 0
            };
        }

        private double RectangleLeft(double a, double h, int n)
        {
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                double x = a + i * h;
                sum += CalculateFunction(x);
            }
            return h * sum;
        }

        private double RectangleRight(double a, double h, int n)
        {
            double sum = 0;
            for (int i = 1; i <= n; i++)
            {
                double x = a + i * h;
                sum += CalculateFunction(x);
            }
            return h * sum;
        }

        private double RectangleMidpoint(double a, double h, int n)
        {
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                double x = a + (i + 0.5) * h;
                sum += CalculateFunction(x);
            }
            return h * sum;
        }

        private double Trapezoidal(double a, double b, double h, int n)
        {
            double sum = 0.5 * (CalculateFunction(a) + CalculateFunction(b));

            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                sum += CalculateFunction(x);
            }

            return h * sum;
        }

        private double Simpson(double a, double b, double h, int n)
        {
            double sum = CalculateFunction(a) + CalculateFunction(b);

            // Нечетные точки
            for (int i = 1; i < n; i += 2)
            {
                double x = a + i * h;
                sum += 4 * CalculateFunction(x);
            }

            // Четные точки
            for (int i = 2; i < n; i += 2)
            {
                double x = a + i * h;
                sum += 2 * CalculateFunction(x);
            }

            return h * sum / 3.0;
        }

        private double EstimateError(double a, double b, int n, IntegrationMethod method, double value)
        {
            // Простая оценка погрешности путем сравнения с удвоенным количеством разбиений
            int doubleN = n * 2;
            if (method == IntegrationMethod.Simpson && doubleN % 2 != 0)
                doubleN++;

            double doubleValue = CalculateWithFixedN(a, b, doubleN, method);
            return Math.Abs(doubleValue - value);
        }

        private void EvaluateFunction(string name, FunctionArgs args)
        {
            switch (name.ToLower())
            {
                case "sin": args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "cos": args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "tan": args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "asin": args.Result = Math.Asin(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "acos": args.Result = Math.Acos(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "atan": args.Result = Math.Atan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "sinh": args.Result = Math.Sinh(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "cosh": args.Result = Math.Cosh(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "tanh": args.Result = Math.Tanh(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "exp": args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "sqrt":
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (v < 0) throw new ArgumentException("Квадратный корень из отрицательного числа");
                        args.Result = Math.Sqrt(v);
                        break;
                    }
                case "abs": args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "log":
                    if (args.Parameters.Length == 1)
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (v <= 0) throw new ArgumentException("Логарифм определен только для положительных чисел");
                        args.Result = Math.Log(v);
                    }
                    else if (args.Parameters.Length == 2)
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        double b = Convert.ToDouble(args.Parameters[1].Evaluate());
                        if (v <= 0 || b <= 0 || b == 1) throw new ArgumentException("Некорректные аргументы log");
                        args.Result = Math.Log(v, b);
                    }
                    else throw new ArgumentException("Функция log требует 1 или 2 аргумента");
                    break;
                case "log10":
                    if (args.Parameters.Length != 1) throw new ArgumentException("Функция log10 требует 1 аргумент");
                    {
                        double v = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (v <= 0) throw new ArgumentException("Логарифм определен только для положительных чисел");
                        args.Result = Math.Log10(v);
                        break;
                    }
                case "pow":
                    if (args.Parameters.Length != 2) throw new ArgumentException("Функция pow требует 2 аргумента");
                    {
                        double b = Convert.ToDouble(args.Parameters[0].Evaluate());
                        double p = Convert.ToDouble(args.Parameters[1].Evaluate());
                        args.Result = Math.Pow(b, p);
                        break;
                    }
                default:
                    throw new ArgumentException($"Неизвестная функция: {name}");
            }
        }
    }
}