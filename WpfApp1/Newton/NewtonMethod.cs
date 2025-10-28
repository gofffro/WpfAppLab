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

                if (result is double doubleResult) return doubleResult;
                if (result is int intResult) return intResult;
                if (result is decimal decimalResult) return (double)decimalResult;

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        // Численное вычисление первой производной
        public double CalculateFirstDerivative(double x, double h = 1e-5)
        {
            return (CalculateFunction(x + h) - CalculateFunction(x - h)) / (2 * h);
        }

        // Численное вычисление второй производной
        public double CalculateSecondDerivative(double x, double h = 1e-5)
        {
            return (CalculateFunction(x + h) - 2 * CalculateFunction(x) + CalculateFunction(x - h)) / (h * h);
        }

        public double FindMinimum(double x0, double epsilon, int maxIterations)
        {
            IterationsCount = 0;
            double x = x0;
            double previousX = x0;

            for (int i = 0; i < maxIterations; i++)
            {
                double firstDerivative = CalculateFirstDerivative(x);
                double secondDerivative = CalculateSecondDerivative(x);

                // Проверка, что вторая производная положительна (минимум)
                if (Math.Abs(secondDerivative) < 1e-15)
                {
                    throw new InvalidOperationException("Вторая производная близка к нулю. Метод Ньютона не применим.");
                }

                if (secondDerivative < 0)
                {
                    throw new InvalidOperationException("Вторая производная отрицательна. Точка может быть максимумом.");
                }

                double delta = firstDerivative / secondDerivative;
                x = x - delta;

                IterationsCount++;

                // Критерий остановки
                if (Math.Abs(delta) < epsilon || Math.Abs(x - previousX) < epsilon)
                {
                    break;
                }

                previousX = x;

                // Защита от расходимости
                if (Math.Abs(x) > 1e10)
                {
                    throw new InvalidOperationException("Метод расходится. Попробуйте другую начальную точку.");
                }
            }

            return x;
        }

        public double NextIteration(double currentX)
        {
            double firstDerivative = CalculateFirstDerivative(currentX);
            double secondDerivative = CalculateSecondDerivative(currentX);

            if (Math.Abs(secondDerivative) < 1e-15)
            {
                throw new InvalidOperationException("Вторая производная близка к нулю.");
            }

            return currentX - firstDerivative / secondDerivative;
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
    }
}