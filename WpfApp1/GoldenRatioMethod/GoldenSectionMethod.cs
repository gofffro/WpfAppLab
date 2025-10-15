using System;
using NCalc;

namespace WpfApp1
{
    public class GoldenSectionMethod
    {
        private readonly Expression _expression;
        private readonly double _goldenRatio = (Math.Sqrt(5) - 1) / 2; 

        public int IterationsCount { get; private set; }

        public GoldenSectionMethod(string function)
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

                if (result is double doubleResult)
                {
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
                if (result is float floatResult)
                {
                    return floatResult;
                }

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public double FindMinimum(double a, double b, double epsilon)
        {
            IterationsCount = 0;
            double x1, x2;

            // Проверяем границы интервала
            if (a >= b)
            {
                throw new ArgumentException("Левая граница a должна быть меньше правой границы b");
            }

            while (Math.Abs(b - a) > epsilon)
            {
                IterationsCount++;

                // Вычисляем точки золотого сечения
                x1 = b - (b - a) * _goldenRatio;
                x2 = a + (b - a) * _goldenRatio;

                double f1 = CalculateFunction(x1);
                double f2 = CalculateFunction(x2);

                if (f1 < f2)
                {
                    // Минимум в левой части
                    b = x2;
                }
                else
                {
                    // Минимум в правой части
                    a = x1;
                }

                // Защита от зацикливания
                if (IterationsCount > 1000)
                {
                    throw new InvalidOperationException("Превышено максимальное количество итераций. Возможно, функция не унимодальна на заданном интервале.");
                }
            }

            // Возвращаем середину конечного интервала
            return (a + b) / 2;
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
                    double sqrtArg = Convert.ToDouble(args.Parameters[0].Evaluate());
                    if (sqrtArg < 0)
                        throw new ArgumentException("Квадратный корень из отрицательного числа");
                    args.Result = Math.Sqrt(sqrtArg);
                    break;
                case "abs":
                    args.Result = Math.Abs(Convert.ToDouble(args.Parameters[0].Evaluate()));
                    break;
                case "log":
                    if (args.Parameters.Length == 1)
                    {
                        double logArg = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (logArg <= 0)
                            throw new ArgumentException("Логарифм определен только для положительных чисел");
                        args.Result = Math.Log(logArg);
                    }
                    else if (args.Parameters.Length == 2)
                    {
                        double logArg = Convert.ToDouble(args.Parameters[0].Evaluate());
                        double logBase = Convert.ToDouble(args.Parameters[1].Evaluate());
                        if (logArg <= 0 || logBase <= 0 || logBase == 1)
                            throw new ArgumentException("Логарифм определен только для положительных чисел с основанием ≠ 1");
                        args.Result = Math.Log(logArg, logBase);
                    }
                    else
                    {
                        throw new ArgumentException("Функция log требует 1 или 2 аргумента");
                    }
                    break;
                case "log10":
                    if (args.Parameters.Length == 1)
                    {
                        double logArg = Convert.ToDouble(args.Parameters[0].Evaluate());
                        if (logArg <= 0)
                            throw new ArgumentException("Логарифм определен только для положительных чисел");
                        args.Result = Math.Log10(logArg);
                    }
                    else
                    {
                        throw new ArgumentException("Функция log10 требует 1 аргумент");
                    }
                    break;
                case "pow":
                    if (args.Parameters.Length == 2)
                    {
                        double baseVal = Convert.ToDouble(args.Parameters[0].Evaluate());
                        double exponent = Convert.ToDouble(args.Parameters[1].Evaluate());
                        args.Result = Math.Pow(baseVal, exponent);
                    }
                    else
                    {
                        throw new ArgumentException("Функция pow требует 2 аргумента");
                    }
                    break;
                default:
                    throw new ArgumentException($"Неизвестная функция: {name}");
            }
        }

        public bool CheckUnimodality(double a, double b, int testPoints = 5)
        {
            if (testPoints < 3) testPoints = 3;

            double step = (b - a) / (testPoints - 1);
            double prevValue = CalculateFunction(a);
            bool foundExtremum = false;

            for (int i = 1; i < testPoints; i++)
            {
                double x = a + i * step;
                double currentValue = CalculateFunction(x);

                if (i > 1)
                {
                    double diff1 = currentValue - prevValue;
                    double diff2 = prevValue - CalculateFunction(a + (i - 2) * step);

                    if (diff1 * diff2 < 0)
                    {
                        if (foundExtremum)
                        {
                            return false;
                        }
                        foundExtremum = true;
                    }
                }

                prevValue = currentValue;
            }

            return true;
        }
    }
}