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
    public partial class NewtonMethodWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public ChartValues<ObservablePoint> FunctionValues { get; set; }
        public ChartValues<ObservablePoint> MinimumPoint { get; set; }
        public ChartValues<ObservablePoint> StepPoints { get; set; }

        private NewtonMethod _newtonMethod;
        private bool _isStepByStepMode = false;
        private List<double> _iterationHistory = new List<double>();

        public NewtonMethodWindow()
        {
            InitializeComponent();
            DataContext = this;

            FunctionValues = new ChartValues<ObservablePoint>();
            MinimumPoint = new ChartValues<ObservablePoint>();
            StepPoints = new ChartValues<ObservablePoint>();

            miNextStep.IsEnabled = false;
            this.Closing += Window_Closing;
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                ResetStepMode();

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxIterations = int.Parse(txtMaxIterations.Text);
                string function = PreprocessFunction(txtFunction.Text);

                _newtonMethod = new NewtonMethod(function);

                double result = _newtonMethod.FindMinimum(a, b, epsilon, maxIterations);
                double functionValue = _newtonMethod.CalculateFunction(result);

                lblResult.Text = $"Найден минимум в точке: x = {result:F6}";
                lblFunctionValue.Text = $"f(min) = {functionValue:F6}";
                lblIterations.Text = $"Количество итераций: {_newtonMethod.IterationsCount}";

                PlotGraphWithMinimumInterval(a, b, result, functionValue);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка вычисления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartStepByStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                string function = PreprocessFunction(txtFunction.Text);

                _newtonMethod = new NewtonMethod(function);
                _iterationHistory.Clear();

                double x0 = 0.5 * (a + b); // старт из середины интервала
                _iterationHistory.Add(x0);

                _isStepByStepMode = true;
                miNextStep.IsEnabled = true;
                btnNextStep.IsEnabled = true;
                btnStepByStep.IsEnabled = false;
                btnCalculate.IsEnabled = false;

                lblStepInfo.Text = $"Шаг 1: x₀ = {x0:F6}";
                PlotFunction(a, b); // сразу рисуем всю функцию на [a,b]
                PlotStepByStep(x0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (!_isStepByStepMode || _newtonMethod == null) return;

            try
            {
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);

                double currentX = _iterationHistory[_iterationHistory.Count - 1];
                double nextX = _newtonMethod.NextIterationInterval(currentX, a, b);
                _iterationHistory.Add(nextX);

                double delta = Math.Abs(nextX - currentX);
                lblStepInfo.Text = $"Шаг {_iterationHistory.Count}: x = {nextX:F6}, Δ = {delta:F6}";

                PlotStepByStep(nextX);

                if (delta < epsilon || _iterationHistory.Count >= int.Parse(txtMaxIterations.Text))
                {
                    FinishStepByStep(nextX);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка на шаге: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                FinishStepByStep(_iterationHistory[_iterationHistory.Count - 1]);
            }
        }

        private void FinishStepByStep(double result)
        {
            _isStepByStepMode = false;
            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false;
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;

            double functionValue = _newtonMethod.CalculateFunction(result);
            lblResult.Text = $"Найден минимум в точке: x = {result:F6}";
            lblFunctionValue.Text = $"f(min) = {functionValue:F6}";
            lblIterations.Text = $"Количество итераций: {_iterationHistory.Count - 1}";
            lblStepInfo.Text = "Вычисление завершено";

            MinimumPoint.Clear();
            MinimumPoint.Add(new LiveCharts.Defaults.ObservablePoint(result, functionValue));
        }


        private void ResetStepMode()
        {
            _isStepByStepMode = false;
            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false; // Отключаем кнопку в панели
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;
            _iterationHistory.Clear();
            StepPoints.Clear();
            lblStepInfo.Text = "";
        }

        private void PlotStepByStep(double currentX)
        {
            StepPoints.Clear();
            foreach (double x in _iterationHistory)
            {
                double y = _newtonMethod.CalculateFunction(x);
                StepPoints.Add(new LiveCharts.Defaults.ObservablePoint(x, y));
            }
        }


        private void PlotGraphWithMinimumInterval(double a, double b, double xMin, double yMin)
        {
            PlotFunction(a, b);
            MinimumPoint.Clear();
            MinimumPoint.Add(new LiveCharts.Defaults.ObservablePoint(xMin, yMin));
            StepPoints.Clear();
        }

        private void PlotFunction(double a, double b)
        {
            FunctionValues.Clear();

            // Ширина «зоны разрыва» вокруг 0 и ограничение по Y для стабильного масштаба
            double gap = Math.Max(1e-6, 0.001 * (b - a));
            double YMAX = 1e6; // можно подобрать под задачу

            int pointsCount = 400;
            double step = (b - a) / pointsCount;

            for (double x = a; x <= b; x += step)
            {
                // Если близко к разрыву — вставляем «пробел» (NaN) и идём дальше
                if (Math.Abs(x) < gap)
                {
                    FunctionValues.Add(new LiveCharts.Defaults.ObservablePoint(x, double.NaN));
                    continue;
                }

                double y = _newtonMethod.CalculateFunction(x);

                // Отфильтровываем «заглушку» 1e10 и любые непригодные величины
                if (double.IsNaN(y) || double.IsInfinity(y) || Math.Abs(y) > YMAX || Math.Abs(y - 1e10) < 1)
                {
                    FunctionValues.Add(new LiveCharts.Defaults.ObservablePoint(x, double.NaN));
                    continue;
                }

                FunctionValues.Add(new LiveCharts.Defaults.ObservablePoint(x, y));
            }
        }


        private void ResetSteps_Click(object sender, RoutedEventArgs e)
        {
            ResetStepMode();
            lblIterationDetails.Text = "";
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtA.Text) ||
                string.IsNullOrWhiteSpace(txtB.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) ||
                string.IsNullOrWhiteSpace(txtMaxIterations.Text) ||
                string.IsNullOrWhiteSpace(txtFunction.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!double.TryParse(txtA.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(txtB.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double epsilon) ||
                !int.TryParse(txtMaxIterations.Text, out int maxIterations))
            {
                MessageBox.Show("Некорректные числовые значения!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (a >= b)
            {
                MessageBox.Show("Должно быть a < b!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (maxIterations <= 0 || maxIterations > 2000)
            {
                MessageBox.Show("Максимальное количество итераций должно быть от 1 до 2000!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "-1";
            txtB.Text = "3";
            txtEpsilon.Text = "0,0001";
            txtMaxIterations.Text = "100";
            txtFunction.Text = "x*x - 2*x + 1";

            lblResult.Text = "Результат: ";
            lblFunctionValue.Text = "f(min) = ";
            lblIterations.Text = "Количество итераций: ";
            lblStepInfo.Text = "";

            FunctionValues.Clear();
            MinimumPoint.Clear();
            StepPoints.Clear();
            ResetStepMode();
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
            StepPoints?.Clear();
            SeriesCollection?.Clear();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            ShowSyntaxHelp();
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

        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return function;

            string result = function;
            result = Regex.Replace(result, @"pow\(([^,]+),([^)]+)\)", "pow($1|SEPARATOR|$2)");
            result = Regex.Replace(result, @"log\(([^,]+),([^)]+)\)", "log($1|SEPARATOR|$2)");
            result = result.Replace(",", ".");
            result = result.Replace("|SEPARATOR|", ",");
            return result;
        }
    }
}