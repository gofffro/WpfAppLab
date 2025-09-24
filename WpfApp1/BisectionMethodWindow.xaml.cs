using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    public partial class BisectionMethodWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public ChartValues<ObservablePoint> FunctionValues { get; set; }
        public ChartValues<ObservablePoint> MinimumPoint { get; set; }

        private MainWindow _mainWindow;

        public BisectionMethodWindow()
        {
            InitializeComponent();
            DataContext = this;

            FunctionValues = new ChartValues<ObservablePoint>();
            MinimumPoint = new ChartValues<ObservablePoint>();

            this.Closing += Window_Closing;
        }

        public BisectionMethodWindow(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                double a = ParseNumber(txtA.Text);
                double b = ParseNumber(txtB.Text);
                double epsilon = ParseNumber(txtEpsilon.Text);
                string function = txtFunction.Text;

                function = PreprocessFunction(function);

                if (function.ToLower().Contains("log") || function.ToLower().Contains("log10"))
                {
                    if (a <= 0)
                    {
                        MessageBox.Show("Внимание: логарифм не определен для x ≤ 0.\n" +
                                      "Автоматически корректирую начало интервала на 0.001",
                                      "Корректировка интервала", MessageBoxButton.OK, MessageBoxImage.Warning);
                        a = 0.001;
                        txtA.Text = "0.001";
                    }
                }

                if (function.Contains("^"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо оператора ^.\n\nПример: x^2 -> pow(x,2)",
                                  "Неподдерживаемый оператор",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                else if (function.Contains("**"))
                {
                    MessageBox.Show("Пожалуйста, используйте функцию pow(x,y) вместо оператора **. \n\nПример: x**2 -> pow(x,2)",
                        "Неподдерживаемый оператор",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                DihotomyMethod method = new DihotomyMethod(function);


                if (method.IsConstantFunction(a, b))
                {
                    MessageBox.Show("Функция является константой. Любая точка на интервале является решением.\n" +
                                  "Алгоритм вернет середину интервала.",
                                  "Особый случай", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                double minimum = method.FindMinimum(a, b, epsilon);
                double minValue = method.CalculateFunction(minimum);

                lblResult.Text = $"Минимум: x = {minimum:F6}";
                lblFunctionValue.Text = $"f(min) = {minValue:F6}";
                lblIterations.Text = $"Количество итераций: {method.IterationsCount}";

                PlotGraph(a, b, minimum, method);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtA.Text) || string.IsNullOrWhiteSpace(txtB.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) || string.IsNullOrWhiteSpace(txtFunction.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(txtA.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(txtB.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double b) ||
                !double.TryParse(txtEpsilon.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double epsilon))
            {
                MessageBox.Show("Параметры a, b и epsilon должны быть числами!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Math.Abs(a) > 1e15 || Math.Abs(b) > 1e15)
            {
                MessageBox.Show("Значения a и b не должны превышать 10^15 по модулю!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Math.Abs(b - a) > 1e10)
            {
                MessageBox.Show("Интервал [a, b] слишком большой! Максимальная длина: 10^10",
                    "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon < 1e-15)
            {
                MessageBox.Show("Точность epsilon не должна быть меньше 10^-15!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (a >= b)
            {
                MessageBox.Show("Значение a должно быть меньше b!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void PlotGraph(double a, double b, double minimum, DihotomyMethod method)
        {
            FunctionValues.Clear();
            MinimumPoint.Clear();

            int pointsCount = 100;
            double step = (b - a) / pointsCount;

            for (double x = a; x <= b; x += step)
            {
                try
                {
                    double y = method.CalculateFunction(x);
                    FunctionValues.Add(new ObservablePoint(x, y));
                }
                catch
                {
             
                }
            }

            double minY = method.CalculateFunction(minimum);
            MinimumPoint.Add(new ObservablePoint(minimum, minY));
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "1";
            txtB.Text = "2";
            txtEpsilon.Text = "0,001";
            txtFunction.Text = "x*x - 2*x + 1";
            lblResult.Text = "Результат: ";
            lblFunctionValue.Text = "f(min) = ";
            lblIterations.Text = "Количество итераций: ";
            FunctionValues.Clear();
            MinimumPoint.Clear();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (chart != null)
            {
                chart.Series.Clear();
                chart = null;
            }

            FunctionValues?.Clear();
            MinimumPoint?.Clear();
            SeriesCollection?.Clear();
        }

        private void ShowSyntaxHelp()
        {
            string helpText = @"Поддерживаемые математические функции:

Базовые операции: + - * /
Возведение в степень: pow(x,y)  (например: pow(x,2))
Тригонометрические: sin(x), cos(x), tan(x)
Экспонента и логарифмы: exp(x), log(x), log10(x)
Корни: sqrt(x)
Модуль: abs(x)

Константы: pi, e

Примеры:
• x^2 + 3*x + 1 → pow(x,2) + 3*x + 1
• sin(x)^2 → pow(sin(x),2)
• e^(2*x) → pow(e,2*x)";

            MessageBox.Show(helpText, "Справка по синтаксису", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            ShowSyntaxHelp();
        }

        private double ParseNumber(string numberText)
        {
            if (string.IsNullOrWhiteSpace(numberText))
            {
                throw new ArgumentException("Пустая строка");
            }

            // замена точки на запятую
            string normalizedText = numberText.Replace(".", ",");

            if (double.TryParse(normalizedText, NumberStyles.Any, CultureInfo.GetCultureInfo("ru-RU"), out double result))
            {
                return result;
            }

            // навсякий доп попытка
            if (double.TryParse(numberText, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            throw new ArgumentException($"Некорректный формат числа: {numberText}");
        }

        // сложная регулярка я её своровал
        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
            {
                return function;
            }

            string result = function;

            // заменяем запятые в функциях на специальные маркеры
            result = Regex.Replace(result, @"pow\(([^,]+),([^)]+)\)", "pow($1|SEPARATOR|$2)");
            result = Regex.Replace(result, @"log\(([^,]+),([^)]+)\)", "log($1|SEPARATOR|$2)");

            result = result.Replace(",", ".");

            // возвращаем запятые в функциях обратно
            result = result.Replace("|SEPARATOR|", ",");

            // теперь в функции поддерживается и точка и запятая
            return result;
        }
    }
}