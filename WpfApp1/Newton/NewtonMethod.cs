using System;
using NCalc;

namespace WpfApp1
{
    public class NewtonMethod
    {
        private readonly Expression _expression;
        public int IterationsCount { get; private set; }

        public NewtonMethod(string function)
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
                    if (double.IsNaN(d) || double.IsInfinity(d)) return 1e10;
                    return d;
                }
                if (result is int i) return i;
                if (result is decimal m) return (double)m;
                return Convert.ToDouble(result);
            }
            catch
            {
                return 1e10;
            }
        }

        public double CalculateFirstDerivative(double x, double h = 1e-5)
        {
            try
            {
                if (Math.Abs(x) < h) h = Math.Abs(x) * 0.1 + 1e-10;
                double f_plus = CalculateFunction(x + h);
                double f_minus = CalculateFunction(x - h);
                return (f_plus - f_minus) / (2 * h);
            }
            catch { return 0; }
        }

        public double CalculateSecondDerivative(double x, double h = 1e-5)
        {
            try
            {
                if (Math.Abs(x) < h) h = Math.Abs(x) * 0.1 + 1e-10;
                double f_plus = CalculateFunction(x + h);
                double f_curr = CalculateFunction(x);
                double f_minus = CalculateFunction(x - h);
                return (f_plus - 2 * f_curr + f_minus) / (h * h);
            }
            catch { return 1; }
        }

        /// <summary>
        /// Следующий шаг Ньютона с удержанием внутри [a,b] и бэктрекингом.
        /// </summary>
        public double NextIterationInterval(double currentX, double a, double b)
        {
            double g = CalculateFirstDerivative(currentX);
            double H = CalculateSecondDerivative(currentX);

            // Если вторая производная «плохая», сделаем градиентный шаг малого размера.
            double step;
            if (Math.Abs(H) < 1e-15 || H <= 0)
                step = -Math.Sign(g) * 0.1;
            else
                step = -g / H;

            // Бэктрекинг: удерживаем внутри [a,b] и уменьшаем шаг, если значение ухудшается.
            double xNew = currentX + step;
            double fCurr = CalculateFunction(currentX);
            int shrink = 0;
            while ((xNew < a || xNew > b || CalculateFunction(xNew) > fCurr) && shrink < 25)
            {
                step *= 0.5;
                xNew = currentX + step;
                // проекция в интервал, если всё ещё вылетает
                if (xNew < a) xNew = a + 1e-9;
                if (xNew > b) xNew = b - 1e-9;
                shrink++;
            }

            return xNew;
        }

        /// <summary>
        /// Поиск минимума на интервале: Ньютон с подстраховкой + фолбэк на золотое сечение.
        /// </summary>
        public double FindMinimum(double a, double b, double epsilon, int maxIterations)
        {
            IterationsCount = 0;

            // начальная точка — середина интервала
            double x = 0.5 * (a + b);
            double prev = x;

            bool newtonOk = true;

            for (int i = 0; i < maxIterations; i++)
            {
                double H = CalculateSecondDerivative(x);
                if (double.IsNaN(H) || double.IsInfinity(H))
                {
                    newtonOk = false; break;
                }

                double xNext = NextIterationInterval(x, a, b);
                IterationsCount++;

                if (Math.Abs(xNext - x) < epsilon)
                {
                    x = xNext;
                    return x;
                }

                prev = x;
                x = xNext;

                // защита от «выстрела»
                if (double.IsNaN(x) || double.IsInfinity(x))
                {
                    newtonOk = false; break;
                }
            }

            // Если Ньютон не дал устойчивой сходимости — используем золотое сечение
            if (!newtonOk)
            {
                double xmin = GoldenSection(a, b, epsilon, maxIterations);
                // Считаем, что это «итерации» метода в целом
                return xmin;
            }

            return x;
        }

        /// <summary>
        /// Классическое золотое сечение на [a,b].
        /// </summary>
        private double GoldenSection(double a, double b, double eps, int maxIter)
        {
            IterationsCount = 0;
            double phi = (Math.Sqrt(5) - 1) / 2.0; // ~0.618
            double x1 = b - phi * (b - a);
            double x2 = a + phi * (b - a);
            double f1 = CalculateFunction(x1);
            double f2 = CalculateFunction(x2);

            for (int i = 0; i < maxIter && Math.Abs(b - a) > eps; i++)
            {
                if (f1 > f2)
                {
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = a + phi * (b - a);
                    f2 = CalculateFunction(x2);
                }
                else
                {
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = b - phi * (b - a);
                    f1 = CalculateFunction(x1);
                }
                IterationsCount++;
            }

            double xMin = 0.5 * (a + b);
            return xMin;
        }

        private void EvaluateFunction(string name, FunctionArgs args)
        {
            switch (name.ToLower())
            {
                case "sin": args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "cos": args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
                case "tan": args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate())); break;
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
                        if (v <= 0) throw new ArgumentException("Логарифм определён только для положительных чисел");
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
                        if (v <= 0) throw new ArgumentException("Логарифм определён только для положительных чисел");
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
