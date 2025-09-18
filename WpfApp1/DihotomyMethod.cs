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
                    else
                    {
                        args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()), Convert.ToDouble(args.Parameters[1].Evaluate()));
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

                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка вычисления функции в точке x={x}: {ex.Message}");
            }
        }

        public double FindMinimum(double a, double b, double epsilon)
        {
            if (a >= b)
            {
                throw new ArgumentException("Интервал [a, b] задан неверно: a должно быть меньше b");
            }

            if (epsilon <= 0)
            {
                throw new ArgumentException("Точность epsilon должна быть положительным числом");
            }

            IterationsCount = 0;
            double delta = epsilon / 3;

            while (Math.Abs(b - a) > epsilon)
            {
                double x1 = (a + b - delta) / 2;
                double x2 = (a + b + delta) / 2;

                double f1 = CalculateFunction(x1);
                double f2 = CalculateFunction(x2);

                if (f1 < f2)
                {
                    b = x2;
                }
                else
                {
                    a = x1;
                }

                IterationsCount++;

                if (IterationsCount > 1000)
                {
                    throw new Exception("Превышено максимальное количество итераций (1000). " + "Возможно, функция не имеет минимума на заданном интервале.");
                }
            }

            return (a + b) / 2;
        }
    }
}