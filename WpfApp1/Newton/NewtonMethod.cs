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
                // Защита от особых точек
                if (Math.Abs(x) < 1e-15)
                {
                    // Для x=0 возвращаем большое число, но не бесконечность
                    return 1e10;
                }

                _expression.Parameters["x"] = x;
                var result = _expression.Evaluate();

                if (result is double doubleResult)
                {
                    if (double.IsInfinity(doubleResult) || double.IsNaN(doubleResult))
                    {
                        return 1e10; // Возвращаем большое число вместо бесконечности
                    }
                    return doubleResult;
                }
                if (result is int intResult) return intResult;
                if (result is decimal decimalResult) return (double)decimalResult;

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                // Возвращаем большое число при ошибке вычисления
                return 1e10;
            }
        }

        public double CalculateFirstDerivative(double x, double h = 1e-5)
        {
            try
            {
                // Защита от вычисления в особых точках
                if (Math.Abs(x) < h)
                {
                    h = Math.Abs(x) * 0.1 + 1e-10;
                }

                double f_plus = CalculateFunction(x + h);
                double f_minus = CalculateFunction(x - h);

                return (f_plus - f_minus) / (2 * h);
            }
            catch
            {
                return 0; // Возвращаем 0 при ошибке
            }
        }

        public double CalculateSecondDerivative(double x, double h = 1e-5)
        {
            try
            {
                // Защита от вычисления в особых точках
                if (Math.Abs(x) < h)
                {
                    h = Math.Abs(x) * 0.1 + 1e-10;
                }

                double f_plus = CalculateFunction(x + h);
                double f_curr = CalculateFunction(x);
                double f_minus = CalculateFunction(x - h);

                return (f_plus - 2 * f_curr + f_minus) / (h * h);
            }
            catch
            {
                return 1; // Возвращаем положительное число при ошибке
            }
        }

        public double NextIteration(double currentX)
        {
            try
            {
                double firstDerivative = CalculateFirstDerivative(currentX);
                double secondDerivative = CalculateSecondDerivative(currentX);

                // Защита от деления на ноль и отрицательной второй производной
                if (Math.Abs(secondDerivative) < 1e-15)
                {
                    // Если вторая производная почти нулевая, делаем небольшой шаг
                    return currentX - Math.Sign(firstDerivative) * 0.1;
                }

                if (secondDerivative < 0)
                {
                    // Если вторая производная отрицательная, идем против градиента
                    return currentX - firstDerivative * 0.1;
                }

                return currentX - firstDerivative / secondDerivative;
            }
            catch (Exception ex)
            {
                // При любой ошибке возвращаем текущую точку (останавливаем метод)
                return currentX;
            }
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