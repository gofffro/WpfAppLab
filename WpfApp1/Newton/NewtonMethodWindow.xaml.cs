using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfApp1
{
    public partial class NewtonMethodWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private LineSeries functionSeries;
        private ScatterSeries minimumSeries;
        private ScatterSeries stepSeries;

        private NewtonMethod _newtonMethod;
        private bool _isStepByStepMode = false;
        private List<double> _iterationHistory = new List<double>();

        public NewtonMethodWindow()
        {
            InitializeComponent();
            InitializePlotModel();
            DataContext = this;

            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false;
            this.Closing += Window_Closing;
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel
            {
                Title = "График функции и поиск минимума",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                PlotAreaBorderColor = OxyColors.DarkBlue
            };

            // Настройка осей
            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                AxislineColor = OxyColors.DarkBlue
            });
            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "f(x)",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                AxislineColor = OxyColors.DarkBlue
            });

            // Инициализация серий
            functionSeries = new LineSeries
            {
                Title = "Функция",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            minimumSeries = new ScatterSeries
            {
                Title = "Минимум",
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerFill = OxyColors.Red,
                MarkerStroke = OxyColors.DarkRed,
                MarkerStrokeThickness = 2
            };

            stepSeries = new ScatterSeries
            {
                Title = "Шаги метода",
                MarkerType = MarkerType.Triangle,
                MarkerSize = 5,
                MarkerFill = OxyColors.Green,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerStrokeThickness = 2
            };

            PlotModel.Series.Add(functionSeries);
            PlotModel.Series.Add(minimumSeries);
            PlotModel.Series.Add(stepSeries);

            plotView.Model = PlotModel;
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

            minimumSeries.Points.Clear();
            minimumSeries.Points.Add(new ScatterPoint(result, functionValue));
            PlotModel.InvalidatePlot(true);
        }

        private void ResetStepMode()
        {
            _isStepByStepMode = false;
            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false;
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;
            _iterationHistory.Clear();
            stepSeries.Points.Clear();
            lblStepInfo.Text = "";
            PlotModel.InvalidatePlot(true);
        }

        private void PlotStepByStep(double currentX)
        {
            stepSeries.Points.Clear();
            foreach (double x in _iterationHistory)
            {
                double y = _newtonMethod.CalculateFunction(x);
                stepSeries.Points.Add(new ScatterPoint(x, y));
            }
            PlotModel.InvalidatePlot(true);
        }

        private void PlotGraphWithMinimumInterval(double a, double b, double xMin, double yMin)
        {
            PlotFunction(a, b);
            minimumSeries.Points.Clear();
            minimumSeries.Points.Add(new ScatterPoint(xMin, yMin));
            stepSeries.Points.Clear();
            PlotModel.InvalidatePlot(true);
        }

        private void PlotFunction(double a, double b)
        {
            // Очищаем предыдущие серии
            PlotModel.Series.Clear();

            // Создаем новые серии
            functionSeries = new LineSeries
            {
                Title = "Функция",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            minimumSeries = new ScatterSeries
            {
                Title = "Минимум",
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerFill = OxyColors.Red,
                MarkerStroke = OxyColors.DarkRed,
                MarkerStrokeThickness = 2
            };

            stepSeries = new ScatterSeries
            {
                Title = "Шаги метода",
                MarkerType = MarkerType.Triangle,
                MarkerSize = 5,
                MarkerFill = OxyColors.Green,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerStrokeThickness = 2
            };

            PlotModel.Series.Add(functionSeries);
            PlotModel.Series.Add(minimumSeries);
            PlotModel.Series.Add(stepSeries);

            int pointsCount = 1000;
            double step = (b - a) / pointsCount;

            // Определяем порог для обнаружения разрывов
            double discontinuityThreshold = 1000;

            double prevY = double.NaN;
            bool inSegment = false;
            LineSeries currentSegment = null;

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                double y = _newtonMethod.CalculateFunction(x);

                // Проверяем на корректность значения
                bool isValid = !double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 1e10;

                if (isValid)
                {
                    // Проверяем на разрыв
                    if (!double.IsNaN(prevY) && inSegment)
                    {
                        double diff = Math.Abs(y - prevY);
                        if (diff > discontinuityThreshold && Math.Abs(prevY) > 1 && Math.Abs(y) > 1)
                        {
                            // Обнаружен разрыв - завершаем текущий сегмент и начинаем новый
                            if (currentSegment != null && currentSegment.Points.Count > 1)
                            {
                                PlotModel.Series.Add(currentSegment);
                            }
                            currentSegment = new LineSeries
                            {
                                Color = OxyColors.Blue,
                                StrokeThickness = 2
                            };
                            inSegment = true;
                        }
                    }

                    if (currentSegment == null)
                    {
                        currentSegment = new LineSeries
                        {
                            Color = OxyColors.Blue,
                            StrokeThickness = 2
                        };
                        inSegment = true;
                    }

                    currentSegment.Points.Add(new DataPoint(x, y));
                    prevY = y;
                }
                else
                {
                    // Некорректное значение - завершаем сегмент если он есть
                    if (currentSegment != null && currentSegment.Points.Count > 1)
                    {
                        PlotModel.Series.Add(currentSegment);
                        currentSegment = null;
                    }
                    inSegment = false;
                    prevY = double.NaN;
                }
            }

            // Добавляем последний сегмент если он есть
            if (currentSegment != null && currentSegment.Points.Count > 1)
            {
                PlotModel.Series.Add(currentSegment);
            }

            // Устанавливаем заголовок только для первой серии
            if (PlotModel.Series.Count > 0 && PlotModel.Series[0] is LineSeries firstSeries)
            {
                firstSeries.Title = "Функция";
            }

            PlotModel.InvalidatePlot(true);
        }

        private void ResetSteps_Click(object sender, RoutedEventArgs e)
        {
            ResetStepMode();
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

            PlotModel.Series.Clear();
            InitializePlotModel();
            ResetStepMode();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (plotView != null)
            {
                plotView.Model = null;
            }

            PlotModel?.Series.Clear();
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