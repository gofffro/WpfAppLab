using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    public partial class GoldenRatio : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public ChartValues<ObservablePoint> FunctionValues { get; set; }
        public ChartValues<ObservablePoint> MinimumPoint { get; set; }

        private MainWindow _mainWindow;

        public GoldenRatio()
        {
            InitializeComponent();
            DataContext = this;

            FunctionValues = new ChartValues<ObservablePoint>();
            MinimumPoint = new ChartValues<ObservablePoint>();

            this.Closing += Window_Closing;
        }

        public GoldenRatio(MainWindow mainWindow) : this()
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

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                string function = txtFunction.Text;

                function = PreprocessFunction(function);

                // Автоматическая коррекция интервала для проблемных функций
                if (function.Contains("/x") && a <= 0 && b >= 0)
                {
                    double newA = a <= 0 ? 0.001 : a;
                    double newB = b <= 0 ? -0.001 : b;

                    if (a <= 0 && b > 0)
                    {
                        // Разделяем интервал на две части, избегая x=0
                        MessageBox.Show("Функция содержит деление на x. Исключаю точку x=0 из интервала.",
                                      "Корректировка интервала", MessageBoxButton.OK, MessageBoxImage.Warning);
                        a = 0.001;
                        txtA.Text = "0.001";
                    }
                }

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

                // Используем метод золотого сечения для поиска минимума
                GoldenSectionMethod method = new GoldenSectionMethod(function);

                // ПРОВЕРКА УНИМОДАЛЬНОСТИ ПЕРЕД ВЫЧИСЛЕНИЕМ
                bool isUnimodal = method.CheckUnimodality(a, b, 10); // 10 точек для точности

                if (!isUnimodal)
                {
                    var result = MessageBox.Show("Функция может иметь несколько экстремумов на заданном интервале.\n" +
                                               "Метод золотого сечения найдет только один из них.\n\n" +
                                               "Продолжить вычисления?",
                                               "Предупреждение",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                double minimumX = method.FindMinimum(a, b, epsilon);
                double minimumY = method.CalculateFunction(minimumX);

                // ДОБАВЛЯЕМ ИНФОРМАЦИЮ О РЕЗУЛЬТАТЕ
                string modalityInfo = isUnimodal ? "Функция унимодальна" : "Функция имеет несколько экстремумов";
                lblResult.Text = $"Найден минимум в точке: x = {minimumX:F6}\n({modalityInfo})";
                lblFunctionValue.Text = $"f(min) = {minimumY:F6}";
                lblIterations.Text = $"Количество итераций: {method.IterationsCount}";

                PlotGraphWithMinimum(a, b, minimumX, minimumY, method);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlotGraphWithMinimum(double a, double b, double minX, double minY, GoldenSectionMethod method)
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

                    // Проверяем, что значение не бесконечное и не слишком большое для графика
                    if (!double.IsInfinity(y) && !double.IsNaN(y) && Math.Abs(y) < 1e10)
                    {
                        FunctionValues.Add(new ObservablePoint(x, y));
                    }
                    else
                    {
                        // Пропускаем точки с бесконечными или слишком большими значениями
                        // Можно добавить точку с ограниченным значением для визуализации разрыва
                        if (y > 0 && y >= double.MaxValue / 2)
                        {
                            FunctionValues.Add(new ObservablePoint(x, 1e10)); // Большое положительное значение
                        }
                        else if (y < 0 && y <= -double.MaxValue / 2)
                        {
                            FunctionValues.Add(new ObservablePoint(x, -1e10)); // Большое отрицательное значение
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но не крашим программу
                    System.Diagnostics.Debug.WriteLine($"Ошибка при вычислении точки x={x}: {ex.Message}");
                }
            }

            // Добавляем точку минимума на график, только если она валидная
            try
            {
                if (!double.IsInfinity(minY) && !double.IsNaN(minY) && Math.Abs(minY) < 1e10)
                {
                    MinimumPoint.Add(new ObservablePoint(minX, minY));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении точки минимума: {ex.Message}");
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

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "0.1"; // Избегаем 0
            txtB.Text = "2";
            txtEpsilon.Text = "0,0001";
            txtFunction.Text = "pow(x,2)";
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

            return result;
        }
    }
}