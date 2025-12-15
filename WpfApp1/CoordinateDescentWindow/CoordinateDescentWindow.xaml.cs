using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using org.mariuszgromada.math.mxparser;
using Expression = org.mariuszgromada.math.mxparser.Expression;
using Function = org.mariuszgromada.math.mxparser.Function;

namespace WpfApp1.CoordinateDescentWindow
{
    public partial class CoordinateDescentWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private bool _drawTrajectory = true;
        private bool _findMinimum = true;
        private bool _findMaximum = true;
        private PlotStyle _plotStyle = PlotStyle.Contour;

        private enum PlotStyle
        {
            Contour,
            Surface3D,
            HeatMap
        }

        private CoordinateDescentCalculator _calculator;
        private bool _isCalculating = false;

        public CoordinateDescentWindow()
        {
            InitializeComponent();
            InitializePlotModel();
            DataContext = this;

            // Устанавливаем значения по умолчанию
            UpdateFunctionInfo();
            UpdateSearchAreaInfo();
            UpdateStartPointInfo();
            UpdateEpsilonInfo();

            // Подписываемся на изменения
            txtFunction.TextChanged += (s, e) => UpdateFunctionInfo();
            txtXMin.TextChanged += (s, e) => UpdateSearchAreaInfo();
            txtXMax.TextChanged += (s, e) => UpdateSearchAreaInfo();
            txtYMin.TextChanged += (s, e) => UpdateSearchAreaInfo();
            txtYMax.TextChanged += (s, e) => UpdateSearchAreaInfo();
            txtStartX.TextChanged += (s, e) => UpdateStartPointInfo();
            txtStartY.TextChanged += (s, e) => UpdateStartPointInfo();
            txtEpsilon.TextChanged += (s, e) => UpdateEpsilonInfo();

            // Настройка меню типа спуска
            UpdateDescentTypeMenu();
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel
            {
                Title = "График функции двух переменных",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                PlotAreaBorderColor = OxyColors.Gray,
                PlotAreaBorderThickness = new OxyThickness(1)
            };

            // Настройка осей
            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                AxislineColor = OxyColors.Gray,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dot
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "y",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                AxislineColor = OxyColors.Gray,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotView.Model = PlotModel;
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            if (_isCalculating) return;

            try
            {
                if (!ValidateInput())
                    return;

                ShowProgress(true, "Начало вычислений...");

                var stopwatch = Stopwatch.StartNew();

                // Получаем параметры
                double xMin = double.Parse(txtXMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double xMax = double.Parse(txtXMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double yMin = double.Parse(txtYMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double yMax = double.Parse(txtYMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double startX = double.Parse(txtStartX.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double startY = double.Parse(txtStartY.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxIterations = int.Parse(txtMaxIterations.Text);
                string function = PreprocessFunction(txtFunction.Text);

                // Обновляем статус
                UpdateAllInfo();
                lblCurrentPoint.Text = $"Точка: ({startX:F2}, {startY:F2})";

                _calculator = new CoordinateDescentCalculator(function, xMin, xMax, yMin, yMax);

                // Выполняем вычисления
                DescentResult minResult = null;
                DescentResult maxResult = null;

                ShowProgress(true, "Выполняется поиск экстремумов...");

                await Task.Run(() =>
                {
                    // Поиск минимума
                    if (_findMinimum)
                    {
                        minResult = _calculator.FindMinimum(startX, startY, epsilon, maxIterations);
                    }

                    // Поиск максимума
                    if (_findMaximum)
                    {
                        maxResult = _calculator.FindMaximum(startX, startY, epsilon, maxIterations);
                    }
                });

                stopwatch.Stop();
                lblTime.Text = $"Время: {stopwatch.ElapsedMilliseconds} мс";

                // Отображаем результаты
                DisplayResults(minResult, maxResult, stopwatch.ElapsedMilliseconds);

                // Строим график
                PlotFunctionAndTrajectory(minResult, maxResult);

                lblStatus.Text = "Вычисление завершено";
                ShowProgress(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вычисления: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Ошибка вычисления";
                ShowProgress(false);
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private void DisplayResults(DescentResult minResult, DescentResult maxResult, long elapsedTime)
        {
            // Обновляем информацию о минимуме
            if (minResult != null)
            {
                tbMinPoint.Text = $"({minResult.X:F6}, {minResult.Y:F6})";
                tbMinValue.Text = $"{minResult.Value:F8}";
                tbMinIterations.Text = $"{minResult.Iterations}";
                tbMinTime.Text = $"{elapsedTime} мс";
                lblCurrentPoint.Text = $"Минимум: ({minResult.X:F4}, {minResult.Y:F4})";
                lblFunctionValue.Text = $"f(x,y) = {minResult.Value:F6}";
                lblIterationCount.Text = $"Итераций: {minResult.Iterations}";

                // Заполняем историю итераций
                UpdateIterationHistory(minResult.History);
            }
            else
            {
                tbMinPoint.Text = "(-, -)";
                tbMinValue.Text = "-";
                tbMinIterations.Text = "-";
                tbMinTime.Text = "-";
            }

            // Обновляем информацию о максимуме
            if (maxResult != null)
            {
                tbMaxPoint.Text = $"({maxResult.X:F6}, {maxResult.Y:F6})";
                tbMaxValue.Text = $"{maxResult.Value:F8}";
                tbMaxIterations.Text = $"{maxResult.Iterations}";
                tbMaxTime.Text = $"{elapsedTime} мс";

                // Если минимум не искали, используем максимум для отображения
                if (minResult == null)
                {
                    lblCurrentPoint.Text = $"Максимум: ({maxResult.X:F4}, {maxResult.Y:F4})";
                    lblFunctionValue.Text = $"f(x,y) = {maxResult.Value:F6}";
                    lblIterationCount.Text = $"Итераций: {maxResult.Iterations}";
                    UpdateIterationHistory(maxResult.History);
                }
            }
            else
            {
                tbMaxPoint.Text = "(-, -)";
                tbMaxValue.Text = "-";
                tbMaxIterations.Text = "-";
                tbMaxTime.Text = "-";
            }
        }

        private void UpdateIterationHistory(List<IterationInfo> history)
        {
            lvIterations.Items.Clear();
            if (history == null || history.Count == 0) return;

            // Показываем только последние 20 итераций для производительности
            int displayCount = Math.Min(history.Count, 20);
            int startIndex = Math.Max(0, history.Count - displayCount);

            for (int i = startIndex; i < history.Count; i++)
            {
                lvIterations.Items.Add(new
                {
                    Step = i + 1,
                    X = history[i].X,
                    Y = history[i].Y,
                    Value = history[i].Value,
                    Delta = history[i].Delta
                });
            }
        }

        private void PlotFunctionAndTrajectory(DescentResult minResult, DescentResult maxResult)
        {
            PlotModel.Series.Clear();

            double xMin = double.Parse(txtXMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double xMax = double.Parse(txtXMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double yMin = double.Parse(txtYMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double yMax = double.Parse(txtYMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);

            // Рисуем контурный график
            PlotContourGraph(xMin, xMax, yMin, yMax);

            // Рисуем траектории спуска
            if (_drawTrajectory)
            {
                if (minResult != null && minResult.History != null && minResult.History.Count > 1)
                    PlotTrajectory(minResult.History, OxyColors.Blue, "Путь к минимуму");

                if (maxResult != null && maxResult.History != null && maxResult.History.Count > 1)
                    PlotTrajectory(maxResult.History, OxyColors.Red, "Путь к максимуму");
            }

            // Рисуем начальную и конечные точки
            PlotPoints(minResult, maxResult);

            PlotModel.InvalidatePlot(true);

            // Обновляем информацию о графике
            UpdatePlotInfo();
        }

        private void PlotContourGraph(double xMin, double xMax, double yMin, double yMax)
        {
            if (_calculator == null) return;

            // Уменьшаем размер сетки для производительности
            int gridSize = 25;
            double[,] values = new double[gridSize, gridSize];
            double xStep = (xMax - xMin) / (gridSize - 1);
            double yStep = (yMax - yMin) / (gridSize - 1);

            // Вычисляем значения функции на сетке
            for (int i = 0; i < gridSize; i++)
            {
                double x = xMin + i * xStep;
                for (int j = 0; j < gridSize; j++)
                {
                    double y = yMin + j * yStep;
                    try
                    {
                        values[i, j] = _calculator.CalculateFunction(x, y);
                    }
                    catch
                    {
                        values[i, j] = double.NaN;
                    }
                }
            }

            // Создаем контурные линии
            var contourSeries = new ContourSeries
            {
                Title = $"f(x,y) = {txtFunction.Text}",
                ColumnCoordinates = Enumerable.Range(0, gridSize).Select(i => xMin + i * xStep).ToArray(),
                RowCoordinates = Enumerable.Range(0, gridSize).Select(i => yMin + i * yStep).ToArray(),
                Data = values,
                LabelStep = 10,
                ContourLevelStep = 5.0,
                ContourColors = new[]
                {
                    OxyColors.Blue,
                    OxyColors.Cyan,
                    OxyColors.Green,
                    OxyColors.Yellow,
                    OxyColors.Orange,
                    OxyColors.Red
                }
            };

            PlotModel.Series.Add(contourSeries);
        }

        private void PlotTrajectory(List<IterationInfo> history, OxyColor color, string title)
        {
            if (history == null || history.Count < 2) return;

            var lineSeries = new LineSeries
            {
                Title = title,
                Color = color,
                StrokeThickness = 1,
                LineStyle = LineStyle.Solid,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = color
            };

            // Берем каждую 2-ю точку для производительности если точек много
            int step = history.Count > 50 ? 2 : 1;
            for (int i = 0; i < history.Count; i += step)
            {
                lineSeries.Points.Add(new DataPoint(history[i].X, history[i].Y));
            }

            PlotModel.Series.Add(lineSeries);
        }

        private void PlotPoints(DescentResult minResult, DescentResult maxResult)
        {
            // Начальная точка
            double startX = double.Parse(txtStartX.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double startY = double.Parse(txtStartY.Text.Replace(",", "."), CultureInfo.InvariantCulture);

            var startPointSeries = new ScatterSeries
            {
                Title = "Начальная точка",
                MarkerType = MarkerType.Star,
                MarkerSize = 8,
                MarkerFill = OxyColors.Purple
            };
            startPointSeries.Points.Add(new ScatterPoint(startX, startY));
            PlotModel.Series.Add(startPointSeries);

            // Конечные точки
            if (minResult != null)
            {
                var minPointSeries = new ScatterSeries
                {
                    Title = "Минимум",
                    MarkerType = MarkerType.Triangle,
                    MarkerSize = 7,
                    MarkerFill = OxyColors.Blue
                };
                minPointSeries.Points.Add(new ScatterPoint(minResult.X, minResult.Y));
                PlotModel.Series.Add(minPointSeries);
            }

            if (maxResult != null)
            {
                var maxPointSeries = new ScatterSeries
                {
                    Title = "Максимум",
                    MarkerType = MarkerType.Diamond,
                    MarkerSize = 7,
                    MarkerFill = OxyColors.Red
                };
                maxPointSeries.Points.Add(new ScatterPoint(maxResult.X, maxResult.Y));
                PlotModel.Series.Add(maxPointSeries);
            }
        }

        private void ShowProgress(bool show, string message = "")
        {
            if (show)
            {
                _isCalculating = true;
                progressBar.Visibility = Visibility.Visible;
                lblProgress.Visibility = Visibility.Visible;
                lblProgress.Text = message;
                progressBar.IsIndeterminate = true;
                btnCalculate.IsEnabled = false;
                lblStatus.Text = message;
            }
            else
            {
                _isCalculating = false;
                progressBar.Visibility = Visibility.Collapsed;
                lblProgress.Visibility = Visibility.Collapsed;
                progressBar.IsIndeterminate = false;
                btnCalculate.IsEnabled = true;
            }
        }

        private void UpdateAllInfo()
        {
            UpdateFunctionInfo();
            UpdateSearchAreaInfo();
            UpdateStartPointInfo();
            UpdateEpsilonInfo();
        }

        private void UpdateFunctionInfo()
        {
            tbFunction.Text = txtFunction.Text;
        }

        private void UpdateSearchAreaInfo()
        {
            try
            {
                double xMin = double.Parse(txtXMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double xMax = double.Parse(txtXMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double yMin = double.Parse(txtYMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double yMax = double.Parse(txtYMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                tbSearchArea.Text = $"x∈[{xMin:F1}, {xMax:F1}], y∈[{yMin:F1}, {yMax:F1}]";
            }
            catch
            {
                tbSearchArea.Text = "x∈[?, ?], y∈[?, ?]";
            }
        }

        private void UpdateStartPointInfo()
        {
            try
            {
                double startX = double.Parse(txtStartX.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double startY = double.Parse(txtStartY.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                tbStartPoint.Text = $"({startX:F2}, {startY:F2})";
            }
            catch
            {
                tbStartPoint.Text = "(?, ?)";
            }
        }

        private void UpdateEpsilonInfo()
        {
            tbEpsilon.Text = txtEpsilon.Text;
        }

        private void UpdatePlotInfo()
        {
            lblPlotType.Text = _plotStyle switch
            {
                PlotStyle.Contour => "Контурный график",
                PlotStyle.Surface3D => "3D поверхность",
                PlotStyle.HeatMap => "Тепловая карта",
                _ => "Контурный график"
            };

            double xMin = double.Parse(txtXMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double xMax = double.Parse(txtXMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double yMin = double.Parse(txtYMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            double yMax = double.Parse(txtYMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
            lblPlotInfo.Text = $"Область: x∈[{xMin:F1}, {xMax:F1}], y∈[{yMin:F1}, {yMax:F1}]";
        }

        private void UpdateDescentTypeMenu()
        {
            miDescentType.Header = _findMinimum ? "Поиск минимума ✓" : "Поиск максимума";
            miDescentType.ToolTip = _findMinimum ?
                "Сейчас ищется минимум. Кликните для переключения на поиск максимума" :
                "Сейчас ищется максимум. Кликните для переключения на поиск минимума";
        }

        private bool ValidateInput()
        {
            // Проверка заполненности полей
            if (string.IsNullOrWhiteSpace(txtFunction.Text) ||
                string.IsNullOrWhiteSpace(txtXMin.Text) ||
                string.IsNullOrWhiteSpace(txtXMax.Text) ||
                string.IsNullOrWhiteSpace(txtYMin.Text) ||
                string.IsNullOrWhiteSpace(txtYMax.Text) ||
                string.IsNullOrWhiteSpace(txtStartX.Text) ||
                string.IsNullOrWhiteSpace(txtStartY.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) ||
                string.IsNullOrWhiteSpace(txtMaxIterations.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка числовых значений
            if (!double.TryParse(txtXMin.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double xMin) ||
                !double.TryParse(txtXMax.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double xMax) ||
                !double.TryParse(txtYMin.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double yMin) ||
                !double.TryParse(txtYMax.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double yMax) ||
                !double.TryParse(txtStartX.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double startX) ||
                !double.TryParse(txtStartY.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double startY) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double epsilon) ||
                !int.TryParse(txtMaxIterations.Text, out int maxIterations))
            {
                MessageBox.Show("Некорректные числовые значения!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка интервалов
            if (xMin >= xMax)
            {
                MessageBox.Show("Должно быть xMin < xMax!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (yMin >= yMax)
            {
                MessageBox.Show("Должно быть yMin < yMax!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка начальной точки
            if (startX < xMin || startX > xMax || startY < yMin || startY > yMax)
            {
                MessageBox.Show($"Начальная точка должна быть в области поиска!\n" +
                              $"x ∈ [{xMin}, {xMax}], y ∈ [{yMin}, {yMax}]", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка точности
            if (epsilon <= 0)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка максимального количества итераций
            if (maxIterations <= 0 || maxIterations > 1000)
            {
                MessageBox.Show("Максимальное количество итераций должно быть от 1 до 1000!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка что выбран хотя бы один тип поиска
            if (!_findMinimum && !_findMaximum)
            {
                MessageBox.Show("Выберите хотя бы один тип поиска (минимум или максимум)!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка функции (попробуем вычислить в одной точке)
            try
            {
                string testFunction = PreprocessFunction(txtFunction.Text);
                var testCalc = new CoordinateDescentCalculator(testFunction, xMin, xMax, yMin, yMax);
                double testValue = testCalc.CalculateFunction(startX, startY);
                if (double.IsNaN(testValue) || double.IsInfinity(testValue))
                {
                    MessageBox.Show("Функция возвращает некорректное значение в начальной точке!", "Ошибка ввода",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в функции: {ex.Message}", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем значения полей
            txtFunction.Text = "x^2 + y^2";
            txtXMin.Text = "-5";
            txtXMax.Text = "5";
            txtYMin.Text = "-5";
            txtYMax.Text = "5";
            txtStartX.Text = "2";
            txtStartY.Text = "3";
            txtEpsilon.Text = "0.001";
            txtMaxIterations.Text = "100";

            // Сбрасываем чекбоксы
            cbFindMinimum.IsChecked = true;
            cbFindMaximum.IsChecked = true;
            _findMinimum = true;
            _findMaximum = true;
            UpdateDescentTypeMenu();

            // Сбрасываем стиль графика
            _plotStyle = PlotStyle.Contour;
            if (miContour != null) miContour.IsChecked = true;

            // Очищаем результаты
            tbMinPoint.Text = "(-, -)";
            tbMinValue.Text = "-";
            tbMinIterations.Text = "-";
            tbMinTime.Text = "-";

            tbMaxPoint.Text = "(-, -)";
            tbMaxValue.Text = "-";
            tbMaxIterations.Text = "-";
            tbMaxTime.Text = "-";

            lvIterations.Items.Clear();

            // Очищаем статус
            lblStatus.Text = "Готов к работе";
            lblCurrentPoint.Text = "Точка: (-, -)";
            lblFunctionValue.Text = "f(x,y) = -";
            lblIterationCount.Text = "Итераций: 0";
            lblTime.Text = "Время: 0 мс";

            // Очищаем график
            PlotModel.Series.Clear();
            InitializePlotModel();
            UpdatePlotInfo();

            // Обновляем информацию
            UpdateAllInfo();
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
            string helpText = @"Поддерживаемые математические функции для двух переменных:

Базовые операции: + - * / ^
Степень: x^2, y^3, (x+y)^2
Тригонометрические: sin(x), cos(y), tan(x+y)
Обратные тригонометрические: asin(x), acos(y), atan(x/y)
Экспонента и логарифмы: exp(x), log(y), log10(x+y)
Корни: sqrt(x^2+y^2)
Модуль: abs(x), abs(y)
Константы: pi, e

Примеры корректных функций:
• x^2 + y^2                    (параболоид)
• sin(x) + cos(y)              (периодическая функция)
• exp(-(x^2+y^2))              (гауссова функция)
• x^2 - y^2                    (седловая точка)
• x*sin(y) + y*cos(x)
• 1/(1+x^2+y^2)

ВАЖНО: Используйте точку как разделитель дробей: 0.5 а не 0,5";

            MessageBox.Show(helpText, "Справка по синтаксису функций",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutMethod_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = @"Метод покоординатного спуска (Coordinate Descent Method)

Назначение:
Поиск локальных минимумов и максимумов функций двух переменных f(x,y).

Алгоритм:
1. Выбирается начальная точка (x₀, y₀)
2. Фиксируется y, ищется экстремум по x (одномерный поиск)
3. Фиксируется найденное x, ищется экстремум по y
4. Шаги 2-3 повторяются до достижения точности ε

Особенности:
• Простая реализация
• Может застревать в локальных экстремумах
• Чувствителен к выбору начальной точки

Одномерный поиск выполняется методом золотого сечения.

Критерии остановки:
1. |xₖ₊₁ - xₖ| < ε и |yₖ₊₁ - yₖ| < ε
2. Достигнуто максимальное число итераций
3. Изменение функции меньше ε";

            MessageBox.Show(aboutText, "О методе покоординатного спуска",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutProgram_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Метод покоординатного спуска v1.0\n\n" +
                          "Поиск экстремумов функций двух переменных\n\n" +
                          "Направление подготовки: 09.03.03 – Прикладная информатика\n" +
                          "Кемерово, 2025\n\n" +
                          "Реализованные возможности:\n" +
                          "- Поиск минимума функции\n" +
                          "- Поиск максимума функции\n" +
                          "- Визуализация контурного графика\n" +
                          "- Отображение траектории спуска\n" +
                          "- История итераций",
                          "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowGrid_Click(object sender, RoutedEventArgs e)
        {
            foreach (var axis in PlotModel.Axes)
            {
                axis.MajorGridlineStyle = (miShowGrid.IsChecked == true) ? LineStyle.Dash : LineStyle.None;
                axis.MinorGridlineStyle = (miShowGrid.IsChecked == true) ? LineStyle.Dot : LineStyle.None;
            }
            PlotModel.InvalidatePlot(true);
        }

        private void DescentType_Click(object sender, RoutedEventArgs e)
        {
            _findMinimum = !_findMinimum;
            _findMaximum = !_findMaximum;
            UpdateDescentTypeMenu();
            cbFindMinimum.IsChecked = _findMinimum;
            cbFindMaximum.IsChecked = _findMaximum;

            lblStatus.Text = _findMinimum ?
                "Установлен поиск минимума" :
                "Установлен поиск максимума";
        }

        private void FindMinimum_Checked(object sender, RoutedEventArgs e)
        {
            _findMinimum = true;
            UpdateDescentTypeMenu();
        }

        private void FindMinimum_Unchecked(object sender, RoutedEventArgs e)
        {
            _findMinimum = false;
            UpdateDescentTypeMenu();
        }

        private void FindMaximum_Checked(object sender, RoutedEventArgs e)
        {
            _findMaximum = true;
            UpdateDescentTypeMenu();
        }

        private void FindMaximum_Unchecked(object sender, RoutedEventArgs e)
        {
            _findMaximum = false;
            UpdateDescentTypeMenu();
        }

        private void ContourStyle_Click(object sender, RoutedEventArgs e)
        {
            _plotStyle = PlotStyle.Contour;
            UpdatePlotInfo();
            // Перерисовываем график если есть данные
            if (_calculator != null && PlotModel.Series.Count > 0)
            {
                try
                {
                    double xMin = double.Parse(txtXMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                    double xMax = double.Parse(txtXMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                    double yMin = double.Parse(txtYMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                    double yMax = double.Parse(txtYMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);

                    PlotModel.Series.Clear();
                    PlotContourGraph(xMin, xMax, yMin, yMax);
                    PlotModel.InvalidatePlot(true);
                }
                catch { }
            }
        }

        private void Surface3D_Click(object sender, RoutedEventArgs e)
        {
            _plotStyle = PlotStyle.Surface3D;
            UpdatePlotInfo();
            MessageBox.Show("3D визуализация в разработке. Используется контурный график.",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HeatMapStyle_Click(object sender, RoutedEventArgs e)
        {
            _plotStyle = PlotStyle.HeatMap;
            UpdatePlotInfo();
            MessageBox.Show("Тепловая карта в разработке. Используется контурный график.",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return function;

            string result = function.ToLower();

            // Заменяем запятые на точки
            result = result.Replace(",", ".");

            // mXparser понимает ^ напрямую, но нужно убедиться в правильности скобок
            // Заменяем неправильные формы степеней
            result = Regex.Replace(result, @"([a-zA-Z0-9]+)\^([a-zA-Z0-9]+)", "($1)^($2)");
            result = Regex.Replace(result, @"([a-zA-Z0-9]+)\^\(([^)]+)\)", "($1)^($2)");
            result = Regex.Replace(result, @"\(([^)]+)\)\^([a-zA-Z0-9]+)", "($1)^($2)");

            // mXparser понимает и pow(x,2) и x^2
            // Оставляем как есть, mXparser разберется

            // Убедимся, что функция содержит x и y
            if (!result.Contains("x") || !result.Contains("y"))
            {
                MessageBox.Show("Функция должна содержать обе переменные x и y!", "Предупреждение",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return result;
        }
    }

    // Классы для хранения данных
    public class IterationInfo
    {
        public int Step { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
        public double Delta { get; set; } // Изменение от предыдущей точки
    }

    public class DescentResult
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
        public int Iterations { get; set; }
        public bool Converged { get; set; }
        public List<IterationInfo> History { get; set; } = new List<IterationInfo>();
    }

    // Класс для вычислений методом покоординатного спуска
    public class CoordinateDescentCalculator
    {
        private readonly Function _function;
        private readonly double _xMin;
        private readonly double _xMax;
        private readonly double _yMin;
        private readonly double _yMax;
        private const double DefaultDelta = 1e-7;

        public CoordinateDescentCalculator(string functionExpression, double xMin, double xMax, double yMin, double yMax)
        {
            // Очищаем выражение
            string cleanExpression = functionExpression.Trim();

            // Добавляем f(x,y) = если его нет
            if (!cleanExpression.StartsWith("f(x,y) = ", StringComparison.OrdinalIgnoreCase))
            {
                cleanExpression = "f(x,y) = " + cleanExpression;
            }

            _function = new Function(cleanExpression);

            // Проверяем синтаксис
            if (!_function.checkSyntax())
            {
                throw new ArgumentException($"Некорректный синтаксис функции: {_function.getErrorMessage()}");
            }

            _xMin = xMin;
            _xMax = xMax;
            _yMin = yMin;
            _yMax = yMax;
        }

        public double CalculateFunction(double x, double y)
        {
            try
            {
                // Создаем выражение с подставленными значениями
                string expression = $"f({x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
                var expr = new Expression(expression, _function);

                if (!expr.checkSyntax())
                {
                    throw new ArgumentException($"Ошибка вычисления: {expr.getErrorMessage()}");
                }

                double result = expr.calculate();

                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    // Возвращаем большое число вместо NaN/Infinity
                    return 1e10;
                }

                return result;
            }
            catch
            {
                return 1e10;
            }
        }

        public DescentResult FindMinimum(double startX, double startY, double epsilon, int maxIterations)
        {
            return CoordinateDescent(startX, startY, epsilon, maxIterations, true);
        }

        public DescentResult FindMaximum(double startX, double startY, double epsilon, int maxIterations)
        {
            return CoordinateDescent(startX, startY, epsilon, maxIterations, false);
        }

        private DescentResult CoordinateDescent(double startX, double startY, double epsilon, int maxIterations, bool findMinimum)
        {
            double x = startX;
            double y = startY;
            double prevX, prevY;
            int iteration = 0;
            bool converged = false;

            var history = new List<IterationInfo>();

            // Добавляем начальную точку в историю
            history.Add(new IterationInfo
            {
                Step = 0,
                X = x,
                Y = y,
                Value = CalculateFunction(x, y),
                Delta = 0
            });

            do
            {
                iteration++;
                prevX = x;
                prevY = y;

                // Фиксируем y, ищем экстремум по x
                x = FindExtremumAlongX(y, x, findMinimum, epsilon);

                // Фиксируем x, ищем экстремум по y
                y = FindExtremumAlongY(x, y, findMinimum, epsilon);

                // Вычисляем изменение
                double deltaX = Math.Abs(x - prevX);
                double deltaY = Math.Abs(y - prevY);
                double currentValue = CalculateFunction(x, y);
                double prevValue = CalculateFunction(prevX, prevY);
                double valueDelta = Math.Abs(currentValue - prevValue);

                // Добавляем в историю
                history.Add(new IterationInfo
                {
                    Step = iteration,
                    X = x,
                    Y = y,
                    Value = currentValue,
                    Delta = Math.Max(deltaX, deltaY)
                });

                // Проверяем критерий остановки
                if (deltaX < epsilon && deltaY < epsilon && valueDelta < epsilon)
                {
                    converged = true;
                    break;
                }

            } while (iteration < maxIterations);

            return new DescentResult
            {
                X = x,
                Y = y,
                Value = CalculateFunction(x, y),
                Iterations = iteration,
                Converged = converged,
                History = history
            };
        }

        private double FindExtremumAlongX(double fixedY, double startX, bool findMinimum, double epsilon)
        {
            // Создаем функцию одной переменной f(x) = f(x, fixedY)
            // Для этого создаем новое выражение с подставленным y
            string originalFunc = _function.getFunctionExpressionString()
                .Replace("f(x,y) = ", "");

            // Заменяем y на фиксированное значение
            string xFunc = originalFunc.Replace("y", fixedY.ToString(CultureInfo.InvariantCulture));
            xFunc = xFunc.Replace("Y", fixedY.ToString(CultureInfo.InvariantCulture)); // На случай если Y заглавное

            Function func = new Function($"g(x) = {xFunc}");

            return GoldenSectionSearch(func, _xMin, _xMax, startX, epsilon, findMinimum);
        }

        private double FindExtremumAlongY(double fixedX, double startY, bool findMinimum, double epsilon)
        {
            // Создаем функцию одной переменной f(y) = f(fixedX, y)
            string originalFunc = _function.getFunctionExpressionString()
                .Replace("f(x,y) = ", "");

            // Заменяем x на фиксированное значение
            string yFunc = originalFunc.Replace("x", fixedX.ToString(CultureInfo.InvariantCulture));
            yFunc = yFunc.Replace("X", fixedX.ToString(CultureInfo.InvariantCulture)); // На случай если X заглавное

            Function func = new Function($"h(y) = {yFunc}");

            return GoldenSectionSearch(func, _yMin, _yMax, startY, epsilon, findMinimum);
        }

        private double GoldenSectionSearch(Function function, double a, double b, double start, double epsilon, bool findMinimum)
        {
            double phi = (1 + Math.Sqrt(5)) / 2;

            // Если start вне границ, используем середину
            if (start < a || start > b)
                start = (a + b) / 2;

            double left = a;
            double right = b;

            // Начинаем поиск в окрестности start
            left = Math.Max(a, start - (b - a) / 4);
            right = Math.Min(b, start + (b - a) / 4);

            int maxIter = 50;
            int iter = 0;

            while (Math.Abs(right - left) > epsilon && iter < maxIter)
            {
                iter++;

                double x1 = right - (right - left) / phi;
                double x2 = left + (right - left) / phi;

                double f1 = EvaluateFunction(function, x1);
                double f2 = EvaluateFunction(function, x2);

                if (findMinimum)
                {
                    if (f1 >= f2)
                        left = x1;
                    else
                        right = x2;
                }
                else
                {
                    if (f1 <= f2)
                        left = x1;
                    else
                        right = x2;
                }
            }

            return (left + right) / 2;
        }

        private double EvaluateFunction(Function function, double x)
        {
            try
            {
                var expr = new Expression($"g({x.ToString(CultureInfo.InvariantCulture)})", function);
                double result = expr.calculate();
                return double.IsNaN(result) || double.IsInfinity(result) ? 1e10 : result;
            }
            catch
            {
                return 1e10;
            }
        }
    }
}