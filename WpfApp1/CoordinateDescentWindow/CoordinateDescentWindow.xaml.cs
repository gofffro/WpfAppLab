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
        public PlotModel ConvergencePlotModel { get; set; }
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
            InitializePlotModels();
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

        private void InitializePlotModels()
        {
            // Основной график функции
            PlotModel = new PlotModel
            {
                Title = "График функции двух переменных",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                PlotAreaBorderColor = OxyColors.Gray,
                PlotAreaBorderThickness = new OxyThickness(1)
            };

            // Настройка осей основного графика
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

            // График сходимости
            ConvergencePlotModel = new PlotModel
            {
                Title = "График сходимости метода",
                TitleColor = OxyColors.DarkGreen,
                TextColor = OxyColors.DarkGreen,
                PlotAreaBorderColor = OxyColors.Gray,
                PlotAreaBorderThickness = new OxyThickness(1)
            };

            // Настройка осей графика сходимости
            ConvergencePlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Номер итерации",
                TitleColor = OxyColors.DarkGreen,
                TextColor = OxyColors.DarkGreen,
                AxislineColor = OxyColors.Gray,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash
            });

            ConvergencePlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Значение функции f(x,y)",
                TitleColor = OxyColors.DarkGreen,
                TextColor = OxyColors.DarkGreen,
                AxislineColor = OxyColors.Gray,
                MajorGridlineColor = OxyColors.LightGray,
                MajorGridlineStyle = LineStyle.Dash
            });

            plotConvergenceView.Model = ConvergencePlotModel;
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

                // Строим графики
                PlotFunctionAndTrajectory(minResult, maxResult);
                PlotConvergenceGraph(minResult, maxResult);

                lblStatus.Text = "Вычисление завершено";
                ShowProgress(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вычисления: {ex.Message}\n\nДетали: {ex.InnerException?.Message}", "Ошибка",
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
                tbMinConvergence.Text = minResult.Converged ? "Сошёлся ✓" : "Не сошёлся ✗";
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
                tbMinConvergence.Text = "-";
            }

            // Обновляем информацию о максимуме
            if (maxResult != null)
            {
                tbMaxPoint.Text = $"({maxResult.X:F6}, {maxResult.Y:F6})";
                tbMaxValue.Text = $"{maxResult.Value:F8}";
                tbMaxIterations.Text = $"{maxResult.Iterations}";
                tbMaxTime.Text = $"{elapsedTime} мс";
                tbMaxConvergence.Text = maxResult.Converged ? "Сошёлся ✓" : "Не сошёлся ✗";

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
                tbMaxConvergence.Text = "-";
            }

            // Если оба результата есть, показываем информацию о минимуме
            if (minResult != null && maxResult != null)
            {
                lblCurrentPoint.Text = $"Минимум: ({minResult.X:F4}, {minResult.Y:F4}), Максимум: ({maxResult.X:F4}, {maxResult.Y:F4})";
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

        private void PlotConvergenceGraph(DescentResult minResult, DescentResult maxResult)
        {
            ConvergencePlotModel.Series.Clear();

            // Линии для минимального значения функции на каждой итерации
            if (minResult != null && minResult.History != null && minResult.History.Count > 1)
            {
                var minConvergenceSeries = new LineSeries
                {
                    Title = "Сходимость к минимуму",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3,
                    MarkerFill = OxyColors.Blue
                };

                for (int i = 0; i < minResult.History.Count; i++)
                {
                    minConvergenceSeries.Points.Add(new DataPoint(i, minResult.History[i].Value));
                }

                ConvergencePlotModel.Series.Add(minConvergenceSeries);

                // Добавляем линию целевого значения (минимальное значение)
                var minTargetSeries = new LineSeries
                {
                    Title = $"Минимум: {minResult.Value:F4}",
                    Color = OxyColors.DarkBlue,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash,
                    MarkerType = MarkerType.None
                };

                minTargetSeries.Points.Add(new DataPoint(0, minResult.Value));
                minTargetSeries.Points.Add(new DataPoint(minResult.History.Count - 1, minResult.Value));

                ConvergencePlotModel.Series.Add(minTargetSeries);
            }

            // Линии для максимального значения функции на каждой итерации
            if (maxResult != null && maxResult.History != null && maxResult.History.Count > 1)
            {
                var maxConvergenceSeries = new LineSeries
                {
                    Title = "Сходимость к максимуму",
                    Color = OxyColors.Red,
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    MarkerType = MarkerType.Diamond,
                    MarkerSize = 3,
                    MarkerFill = OxyColors.Red
                };

                for (int i = 0; i < maxResult.History.Count; i++)
                {
                    maxConvergenceSeries.Points.Add(new DataPoint(i, maxResult.History[i].Value));
                }

                ConvergencePlotModel.Series.Add(maxConvergenceSeries);

                // Добавляем линию целевого значения (максимальное значение)
                var maxTargetSeries = new LineSeries
                {
                    Title = $"Максимум: {maxResult.Value:F4}",
                    Color = OxyColors.DarkRed,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash,
                    MarkerType = MarkerType.None
                };

                maxTargetSeries.Points.Add(new DataPoint(0, maxResult.Value));
                maxTargetSeries.Points.Add(new DataPoint(maxResult.History.Count - 1, maxResult.Value));

                ConvergencePlotModel.Series.Add(maxTargetSeries);
            }

            // Если есть только один результат, настраиваем оси для лучшего отображения
            if (minResult != null && maxResult == null)
            {
                var values = minResult.History.Select(h => h.Value).ToList();
                double minValue = values.Min();
                double maxValue = values.Max();
                double range = Math.Max(Math.Abs(maxValue - minValue), 0.1);

                ConvergencePlotModel.Axes[1].Minimum = minValue - range * 0.1;
                ConvergencePlotModel.Axes[1].Maximum = maxValue + range * 0.1;
            }
            else if (maxResult != null && minResult == null)
            {
                var values = maxResult.History.Select(h => h.Value).ToList();
                double minValue = values.Min();
                double maxValue = values.Max();
                double range = Math.Max(Math.Abs(maxValue - minValue), 0.1);

                ConvergencePlotModel.Axes[1].Minimum = minValue - range * 0.1;
                ConvergencePlotModel.Axes[1].Maximum = maxValue + range * 0.1;
            }

            ConvergencePlotModel.InvalidatePlot(true);
        }

        private void PlotContourGraph(double xMin, double xMax, double yMin, double yMax)
        {
            if (_calculator == null) return;

            // Уменьшаем размер сетки для производительности, но достаточно для детализации
            int gridSize = 30;
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
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = color
            };

            // Берем каждую 2-ю точку для производительности если точек много
            int step = history.Count > 50 ? 2 : 1;
            for (int i = 0; i < history.Count; i += step)
            {
                lineSeries.Points.Add(new DataPoint(history[i].X, history[i].Y));
            }

            // Добавляем последнюю точку
            if (step > 1 && history.Count > 0)
            {
                lineSeries.Points.Add(new DataPoint(history.Last().X, history.Last().Y));
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
                MarkerSize = 10,
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
                    MarkerSize = 8,
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
                    MarkerSize = 8,
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

            try
            {
                double xMin = double.Parse(txtXMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double xMax = double.Parse(txtXMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double yMin = double.Parse(txtYMin.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double yMax = double.Parse(txtYMax.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                lblPlotInfo.Text = $"Область: x∈[{xMin:F1}, {xMax:F1}], y∈[{yMin:F1}, {yMax:F1}]";
            }
            catch
            {
                lblPlotInfo.Text = "Область: не определена";
            }
        }

        private void UpdateDescentTypeMenu()
        {
            if (_findMinimum && _findMaximum)
            {
                miDescentType.Header = "Поиск минимума и максимума ✓";
                miDescentType.ToolTip = "Сейчас ищется и минимум, и максимум";
            }
            else if (_findMinimum)
            {
                miDescentType.Header = "Поиск минимума ✓";
                miDescentType.ToolTip = "Сейчас ищется только минимум";
            }
            else if (_findMaximum)
            {
                miDescentType.Header = "Поиск максимума ✓";
                miDescentType.ToolTip = "Сейчас ищется только максимум";
            }
            else
            {
                miDescentType.Header = "Выберите тип поиска";
                miDescentType.ToolTip = "Выберите хотя бы один тип поиска";
            }
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
                MessageBox.Show("Некорректные числовые значения!\nИспользуйте точку как разделитель дробей: 0.5 а не 0,5", "Ошибка ввода",
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
            if (epsilon <= 0 || epsilon > 1)
            {
                MessageBox.Show("Точность epsilon должна быть положительным числом от 0 до 1!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка максимального количества итераций
            if (maxIterations <= 0 || maxIterations > 10000)
            {
                MessageBox.Show("Максимальное количество итераций должно быть от 1 до 10000!", "Ошибка ввода",
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

            // Проверка функции (попробуем вычислить в начальной точке)
            try
            {
                string testFunction = PreprocessFunction(txtFunction.Text);

                // Проверка на деление на ноль в начальной точке
                string functionLower = testFunction.ToLower();
                if (functionLower.Contains("/x") && Math.Abs(startX) < 1e-10)
                {
                    MessageBox.Show("Функция содержит деление на x, но начальное значение x слишком близко к нулю!", "Ошибка ввода",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (functionLower.Contains("/y") && Math.Abs(startY) < 1e-10)
                {
                    MessageBox.Show("Функция содержит деление на y, но начальное значение y слишком близко к нулю!", "Ошибка ввода",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                var testCalc = new CoordinateDescentCalculator(testFunction, xMin, xMax, yMin, yMax);
                double testValue = testCalc.CalculateFunction(startX, startY);
                if (double.IsNaN(testValue) || double.IsInfinity(testValue))
                {
                    MessageBox.Show("Функция возвращает некорректное значение в начальной точке!\n" +
                                  "Проверьте деление на ноль или другие математические ошибки.", "Ошибка ввода",
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

            // Сбрасываем меню
            if (miShowTrajectory != null)
                miShowTrajectory.IsChecked = true;
            _drawTrajectory = true;

            UpdateDescentTypeMenu();

            // Сбрасываем стиль графика
            _plotStyle = PlotStyle.Contour;
            if (miContour != null) miContour.IsChecked = true;

            // Очищаем результаты
            tbMinPoint.Text = "(-, -)";
            tbMinValue.Text = "-";
            tbMinIterations.Text = "-";
            tbMinTime.Text = "-";
            tbMinConvergence.Text = "-";

            tbMaxPoint.Text = "(-, -)";
            tbMaxValue.Text = "-";
            tbMaxIterations.Text = "-";
            tbMaxTime.Text = "-";
            tbMaxConvergence.Text = "-";

            lvIterations.Items.Clear();

            // Очищаем статус
            lblStatus.Text = "Готов к работе";
            lblCurrentPoint.Text = "Точка: (-, -)";
            lblFunctionValue.Text = "f(x,y) = -";
            lblIterationCount.Text = "Итераций: 0";
            lblTime.Text = "Время: 0 мс";

            // Очищаем графики
            PlotModel.Series.Clear();
            ConvergencePlotModel.Series.Clear();
            InitializePlotModels();
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

            if (plotConvergenceView != null)
            {
                plotConvergenceView.Model = null;
            }

            PlotModel?.Series.Clear();
            ConvergencePlotModel?.Series.Clear();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            ShowSyntaxHelp();
        }

        private void ShowSyntaxHelp()
        {
            string helpText = @"ВНИМАНИЕ: Тригонометрические функции требуют скобок!

ПРАВИЛЬНО:
• sin(x) + cos(y)
• sin(2*x) + cos(3*y)
• sin(x^2) * cos(y^2)

НЕПРАВИЛЬНО:
• sin x + cos y
• sin + cos
• sin2x + cos3y

Другие функции:
• x^2 + y^2
• exp(-x^2 - y^2)
• 1/(1 + x^2 + y^2)
• x*sin(y) + y*cos(x)
• (x+y)/(x^2 + y^2 + 1)

Обязательно ставьте скобки после sin, cos и других функций!";

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
3. Изменение функции меньше ε

График сходимости показывает изменение значения функции на каждой итерации.";

            MessageBox.Show(aboutText, "О методе покоординатного спуска",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutProgram_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Метод покоординатного спуска v1.2\n\n" +
                          "Поиск экстремумов функций двух переменных\n\n" +
                          "Упрощен парсинг функций\n" +
                          "Исправлены ошибки с sin и cos\n" +
                          "Добавлен график сходимости\n\n" +
                          "Направление подготовки: 09.03.03 – Прикладная информатика\n" +
                          "Кемерово, 2025\n\n" +
                          "Реализованные возможности:\n" +
                          "- Поиск минимума функции\n" +
                          "- Поиск максимума функции\n" +
                          "- Визуализация контурного графика\n" +
                          "- График сходимости метода\n" +
                          "- Отображение траектории спуска\n" +
                          "- История итераций\n" +
                          "- Проверка на корректность ввода",
                          "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowGrid_Click(object sender, RoutedEventArgs e)
        {
            foreach (var axis in PlotModel.Axes)
            {
                axis.MajorGridlineStyle = (miShowGrid.IsChecked == true) ? LineStyle.Dash : LineStyle.None;
                axis.MinorGridlineStyle = (miShowGrid.IsChecked == true) ? LineStyle.Dot : LineStyle.None;
            }

            foreach (var axis in ConvergencePlotModel.Axes)
            {
                axis.MajorGridlineStyle = (miShowGrid.IsChecked == true) ? LineStyle.Dash : LineStyle.None;
            }

            PlotModel.InvalidatePlot(true);
            ConvergencePlotModel.InvalidatePlot(true);
        }

        private void ShowTrajectory_Checked(object sender, RoutedEventArgs e)
        {
            _drawTrajectory = true;
        }

        private void ShowTrajectory_Unchecked(object sender, RoutedEventArgs e)
        {
            _drawTrajectory = false;
        }

        private void DescentType_Click(object sender, RoutedEventArgs e)
        {
            // Переключаем между тремя состояниями: оба, только минимум, только максимум
            if (_findMinimum && _findMaximum)
            {
                _findMinimum = true;
                _findMaximum = false;
            }
            else if (_findMinimum && !_findMaximum)
            {
                _findMinimum = false;
                _findMaximum = true;
            }
            else
            {
                _findMinimum = true;
                _findMaximum = true;
            }

            UpdateDescentTypeMenu();
            cbFindMinimum.IsChecked = _findMinimum;
            cbFindMaximum.IsChecked = _findMaximum;

            lblStatus.Text = _findMinimum && _findMaximum ? "Поиск минимума и максимума" :
                            _findMinimum ? "Поиск минимума" : "Поиск максимума";
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

        // УПРОЩЕННЫЙ метод предобработки - только минимальные изменения
        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return function;

            string result = function.Trim();

            // Только заменяем запятые на точки и убираем лишние пробелы
            result = result.Replace(",", ".");
            result = result.Replace(" ", "");

            // Простая проверка на очевидные ошибки с sin и cos
            // Проверяем, что sin и cos имеют скобки
            if ((result.Contains("sin") && !result.Contains("sin(")) ||
                (result.Contains("cos") && !result.Contains("cos(")))
            {
                throw new ArgumentException("Функции sin и cos должны иметь скобки: sin(x), cos(y)");
            }

            // Проверяем баланс скобок
            int openBrackets = result.Count(c => c == '(');
            int closeBrackets = result.Count(c => c == ')');
            if (openBrackets != closeBrackets)
            {
                throw new ArgumentException($"Несбалансированные скобки: открывающих {openBrackets}, закрывающих {closeBrackets}");
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
            // Создаем функцию напрямую с минимальной обработкой
            string cleanExpression = functionExpression.Trim();

            // mxparser ожидает функцию в формате "f(x,y) = выражение"
            if (!cleanExpression.StartsWith("f(x,y) = ", StringComparison.OrdinalIgnoreCase))
            {
                cleanExpression = "f(x,y) = " + cleanExpression;
            }

            _function = new Function(cleanExpression);

            if (!_function.checkSyntax())
            {
                // Попробуем добавить скобки вокруг всего выражения
                string expr = functionExpression.Trim();
                if (!expr.StartsWith("(") || !expr.EndsWith(")"))
                {
                    expr = "(" + expr + ")";
                }

                cleanExpression = "f(x,y) = " + expr;
                _function = new Function(cleanExpression);

                if (!_function.checkSyntax())
                {
                    throw new ArgumentException($"Некорректный синтаксис функции:\n{_function.getErrorMessage()}\n\n" +
                                              "Проверьте:\n" +
                                              "1. Функции должны иметь скобки: sin(x), cos(y)\n" +
                                              "2. Используйте x и y как переменные\n" +
                                              "3. Проверьте баланс скобок");
                }
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
                // Простой и надежный способ вычисления
                string exprStr = $"f({x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
                var expr = new Expression(exprStr, _function);

                if (!expr.checkSyntax())
                {
                    return 1e10;
                }

                double result = expr.calculate();

                if (double.IsNaN(result) || double.IsInfinity(result))
                {
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
            // Простой метод золотого сечения без сложных замен
            return GoldenSectionSearchForX(fixedY, startX, findMinimum, epsilon);
        }

        private double FindExtremumAlongY(double fixedX, double startY, bool findMinimum, double epsilon)
        {
            // Простой метод золотого сечения без сложных замен
            return GoldenSectionSearchForY(fixedX, startY, findMinimum, epsilon);
        }

        private double GoldenSectionSearchForX(double y, double startX, bool findMinimum, double epsilon)
        {
            double phi = (1 + Math.Sqrt(5)) / 2;
            double a = _xMin;
            double b = _xMax;

            // Корректируем начальную точку
            if (startX < a) startX = a;
            if (startX > b) startX = b;

            // Начинаем с небольшой области вокруг начальной точки
            double range = Math.Max(0.1, (b - a) * 0.2);
            double left = Math.Max(a, startX - range);
            double right = Math.Min(b, startX + range);

            int maxIter = 30;

            for (int iter = 0; iter < maxIter; iter++)
            {
                if (Math.Abs(right - left) < epsilon)
                    break;

                double x1 = right - (right - left) / phi;
                double x2 = left + (right - left) / phi;

                double f1 = CalculateFunction(x1, y);
                double f2 = CalculateFunction(x2, y);

                if (findMinimum)
                {
                    if (f1 < f2)
                        right = x2;
                    else
                        left = x1;
                }
                else
                {
                    if (f1 > f2)
                        right = x2;
                    else
                        left = x1;
                }
            }

            return (left + right) / 2;
        }

        private double GoldenSectionSearchForY(double x, double startY, bool findMinimum, double epsilon)
        {
            double phi = (1 + Math.Sqrt(5)) / 2;
            double a = _yMin;
            double b = _yMax;

            // Корректируем начальную точку
            if (startY < a) startY = a;
            if (startY > b) startY = b;

            // Начинаем с небольшой области вокруг начальной точки
            double range = Math.Max(0.1, (b - a) * 0.2);
            double left = Math.Max(a, startY - range);
            double right = Math.Min(b, startY + range);

            int maxIter = 30;

            for (int iter = 0; iter < maxIter; iter++)
            {
                if (Math.Abs(right - left) < epsilon)
                    break;

                double y1 = right - (right - left) / phi;
                double y2 = left + (right - left) / phi;

                double f1 = CalculateFunction(x, y1);
                double f2 = CalculateFunction(x, y2);

                if (findMinimum)
                {
                    if (f1 < f2)
                        right = y2;
                    else
                        left = y1;
                }
                else
                {
                    if (f1 > f2)
                        right = y2;
                    else
                        left = y1;
                }
            }

            return (left + right) / 2;
        }
    }
}