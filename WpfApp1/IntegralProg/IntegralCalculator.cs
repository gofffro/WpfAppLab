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

        // ФИКСИРОВАННОЕ N - используем точно указанное пользователем количество разбиений
        public Dictionary<IntegrationMethod, IntegrationResult> CalculateWithFixedN(
            double a, double b, int n, List<IntegrationMethod> methods)
        {
            var results = new Dictionary<IntegrationMethod, IntegrationResult>();

            foreach (var method in methods)
            {
                double value;
                int actualN = n;

                // Для метода Симпсона делаем N четным
                if (method == IntegrationMethod.Simpson && n % 2 != 0)
                {
                    actualN = n + 1; // Делаем четным
                }

                value = CalculateWithExactN(a, b, actualN, method);

                // Оцениваем погрешность (сравнивая с удвоенным N)
                double errorEstimate = EstimateError(a, b, actualN, method, value);

                results[method] = new IntegrationResult
                {
                    Method = method,
                    Value = value,
                    Iterations = actualN,
                    ErrorEstimate = errorEstimate
                };
            }

            return results;
        }

        // АВТОМАТИЧЕСКИЙ выбор N - находим оптимальное N для заданной точности
        public Dictionary<IntegrationMethod, IntegrationResult> CalculateWithAutoN(
            double a, double b, double epsilon, int initialN, List<IntegrationMethod> methods)
        {
            var results = new Dictionary<IntegrationMethod, IntegrationResult>();

            foreach (var method in methods)
            {
                var result = AutoSelectNForMethod(a, b, epsilon, initialN, method);
                results[method] = result;
            }

            return results;
        }

        private IntegrationResult AutoSelectNForMethod(double a, double b, double epsilon, int initialN, IntegrationMethod method)
        {
            int n = Math.Max(2, initialN);
            if (method == IntegrationMethod.Simpson && n % 2 != 0)
            {
                n++; // Делаем четным для Симпсона
            }

            double prevValue = CalculateWithExactN(a, b, n, method);
            double currentValue = prevValue; // Инициализируем значением prevValue

            var result = new IntegrationResult { Method = method };
            result.History.Add(prevValue);

            int maxIterations = 20;

            for (int i = 1; i <= maxIterations; i++)
            {
                n *= 2; // Удваиваем количество разбиений
                if (method == IntegrationMethod.Simpson && n % 2 != 0)
                {
                    n++; // Делаем четным
                }

                currentValue = CalculateWithExactN(a, b, n, method);
                result.History.Add(currentValue);

                double error = Math.Abs(currentValue - prevValue);

                if (error < epsilon || n > 1000000)
                {
                    result.Value = currentValue;
                    result.Iterations = n;
                    result.ErrorEstimate = error;
                    return result;
                }

                prevValue = currentValue;
            }

            result.Value = currentValue;
            result.Iterations = n;
            result.ErrorEstimate = Math.Abs(currentValue - prevValue);
            return result;
        }

        private double CalculateWithExactN(double a, double b, int n, IntegrationMethod method)
        {
            double h = (b - a) / n;

            return method switch
            {
                IntegrationMethod.RectangleLeft => RectangleLeft(a, h, n),
                IntegrationMethod.RectangleRight => RectangleRight(a, h, n),
                IntegrationMethod.RectangleMidpoint => RectangleMidpoint(a, h, n),
                IntegrationMethod.Trapezoidal => Trapezoidal(a, b, h, n),
                IntegrationMethod.Simpson => Simpson(a, b, h, n),
                _ => throw new ArgumentException("Неизвестный метод интегрирования")
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
            if (n < 2)
            {
                throw new ArgumentException("Для метода Симпсона N должно быть не менее 2");
            }

            if (n % 2 != 0)
            {
                throw new ArgumentException("Для метода Симпсона N должно быть четным");
            }

            // Формула Симпсона строго по математике:
            // ∫[a,b] f(x)dx ≈ (h/3)[f(x0) + 4∑f(x_нечет) + 2∑f(x_чет) + f(xn)]

            double sum = CalculateFunction(a); // f(x0)

            // Сумма нечетных индексов (умножается на 4)
            for (int i = 1; i < n; i += 2)
            {
                double x = a + i * h;
                sum += 4 * CalculateFunction(x);
            }

            // Сумма четных индексов (умножается на 2)
            for (int i = 2; i < n; i += 2)
            {
                double x = a + i * h;
                sum += 2 * CalculateFunction(x);
            }

            sum += CalculateFunction(b); // f(xn)

            return (h / 3.0) * sum;
        }

        private double EstimateError(double a, double b, int n, IntegrationMethod method, double value)
        {
            // Простая оценка погрешности путем сравнения с удвоенным количеством разбиений
            int doubleN = n * 2;
            if (method == IntegrationMethod.Simpson && doubleN % 2 != 0)
                doubleN++;

            double doubleValue = CalculateWithExactN(a, b, doubleN, method);
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