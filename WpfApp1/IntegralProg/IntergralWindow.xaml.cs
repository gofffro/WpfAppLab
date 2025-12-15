using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FontWeights = System.Windows.FontWeights;

namespace WpfApp1.IntegralProg
{
    public partial class IntergralWindow : Window
    {
        public PlotModel PlotModel { get; set; }
        private LineSeries functionSeries;
        private bool _drawPartitions = true;
        private const int MAX_PARTITIONS_TO_DRAW = 50;
        private bool _isCalculating = false;

        private IntegralCalculator _calculator;
        private bool _isStepByStepMode = false;
        private int _currentStep = 0;
        private List<IntegrationResult> _iterationHistory = new List<IntegrationResult>();

        public IntergralWindow()
        {
            InitializeComponent();
            InitializePlotModel();
            DataContext = this;

            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false;

            // Обновляем информацию о пределах
            UpdateBoundsInfo();
            txtA.TextChanged += (s, e) => UpdateBoundsInfo();
            txtB.TextChanged += (s, e) => UpdateBoundsInfo();
            txtFunction.TextChanged += (s, e) => UpdateFunctionInfo();
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel
            {
                Title = "График функции",
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
                Title = "f(x)",
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

                ResetStepMode();
                ShowProgress(true, "Начало вычислений...");

                var stopwatch = Stopwatch.StartNew();

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxN = int.Parse(txtN.Text);
                string function = PreprocessFunction(txtFunction.Text);

                // Обновляем статус
                UpdateBoundsInfo();
                UpdateFunctionInfo();
                lblIntegralInfo.Text = $"∫[{a:F2}, {b:F2}] {txtFunction.Text} dx";

                _calculator = new IntegralCalculator(function);

                // Собираем выбранные методы
                List<IntegrationMethod> methods = GetSelectedMethods();
                if (methods.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один метод интегрирования!", "Внимание",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowProgress(false);
                    return;
                }

                bool autoN = miAutoN.IsChecked == true;
                Dictionary<IntegrationMethod, IntegrationResult> results = new Dictionary<IntegrationMethod, IntegrationResult>();

                ShowProgress(true, "Выполняется расчет...");

                // Запускаем вычисление в отдельном потоке
                await Task.Run(() =>
                {
                    if (autoN)
                    {
                        // АВТОМАТИЧЕСКИЙ ВЫБОР N
                        results = _calculator.CalculateWithAutoN(a, b, epsilon, maxN, methods);
                    }
                    else
                    {
                        // ФИКСИРОВАННОЕ N
                        results = _calculator.CalculateWithFixedN(a, b, maxN, methods);
                    }
                });

                stopwatch.Stop();
                lblTime.Text = $"Время: {stopwatch.ElapsedMilliseconds} мс";

                // Отображаем результаты
                DisplayResults(results);

                // Строим график с разбиениями
                PlotIntegration(a, b, results);

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

        private List<IntegrationMethod> GetSelectedMethods()
        {
            List<IntegrationMethod> methods = new List<IntegrationMethod>();
            if (cbRectLeft.IsChecked == true) methods.Add(IntegrationMethod.RectangleLeft);
            if (cbRectRight.IsChecked == true) methods.Add(IntegrationMethod.RectangleRight);
            if (cbRectMid.IsChecked == true) methods.Add(IntegrationMethod.RectangleMidpoint);
            if (cbTrapezoid.IsChecked == true) methods.Add(IntegrationMethod.Trapezoidal);
            if (cbSimpson.IsChecked == true) methods.Add(IntegrationMethod.Simpson);
            return methods;
        }

        private void DisplayResults(Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            // Очищаем предыдущие результаты
            spResults.Children.Clear();

            // Добавляем заголовок
            spResults.Children.Add(new TextBlock
            {
                Text = "Результаты методов:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 14
            });

            if (results.Count == 0)
            {
                spResults.Children.Add(new TextBlock
                {
                    Text = "Нет результатов для отображения",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic
                });
                return;
            }

            // Создаем панель для результатов
            var resultsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

            foreach (var kvp in results)
            {
                var result = kvp.Value;
                var methodName = GetMethodName(result.Method);

                // Создаем карточку для каждого метода
                var card = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(225, 225, 225)),
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(10)
                };

                var cardContent = new StackPanel();

                // Название метода
                cardContent.Children.Add(new TextBlock
                {
                    Text = methodName,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                // Значение
                cardContent.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Значение: ",
                            FontWeight = FontWeights.SemiBold
                        },
                        new TextBlock
                        {
                            Text = $"{result.Value:F8}",
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.DarkGreen
                        }
                    }
                });

                // Количество разбиений
                cardContent.Children.Add(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Разбиений: ",
                            FontWeight = FontWeights.SemiBold
                        },
                        new TextBlock
                        {
                            Text = $"{result.Iterations}",
                            Foreground = Brushes.DarkBlue
                        }
                    }
                });

                // Погрешность (если есть)
                if (result.ErrorEstimate > 0)
                {
                    cardContent.Children.Add(new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                    {
                        new TextBlock
                        {
                            Text = "Погрешность: ",
                            FontWeight = FontWeights.SemiBold
                        },
                        new TextBlock
                        {
                            Text = $"{result.ErrorEstimate:E2}",
                            Foreground = Brushes.DarkRed
                        }
                    }
                    });
                }

                card.Child = cardContent;
                resultsPanel.Children.Add(card);
            }

            spResults.Children.Add(resultsPanel);

            // Информация о графике
            var firstResult = results.Values.First();
            if (firstResult.Iterations > MAX_PARTITIONS_TO_DRAW && _drawPartitions)
            {
                lblPlotWarning.Text = $"График построен для {MAX_PARTITIONS_TO_DRAW} разбиений (из {firstResult.Iterations}) для лучшей видимости";
            }
            else
            {
                lblPlotWarning.Text = "";
            }

            lblIterationInfo.Text = $"Всего разбиений: {firstResult.Iterations}";
        }

        private string GetMethodName(IntegrationMethod method)
        {
            return method switch
            {
                IntegrationMethod.RectangleLeft => "Метод левых прямоугольников",
                IntegrationMethod.RectangleRight => "Метод правых прямоугольников",
                IntegrationMethod.RectangleMidpoint => "Метод средних прямоугольников",
                IntegrationMethod.Trapezoidal => "Метод трапеций",
                IntegrationMethod.Simpson => "Метод Симпсона",
                _ => "Неизвестный метод"
            };
        }

        private void PlotIntegration(double a, double b, Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            PlotModel.Series.Clear();
            lblPlotWarning.Text = "";

            // Рисуем функцию
            PlotFunction(a, b);

            // Если рисование разбиений отключено или слишком много разбиений, не рисуем
            if (!_drawPartitions || results.Count == 0)
                return;

            // Определяем какое N использовать для отрисовки
            var firstResult = results.Values.First();
            int nToPlot = firstResult.Iterations;

            // Если разбиений слишком много, ограничиваем для визуализации
            if (nToPlot > MAX_PARTITIONS_TO_DRAW)
            {
                nToPlot = MAX_PARTITIONS_TO_DRAW;
                lblPlotWarning.Text = $"График построен для {nToPlot} разбиений (из {firstResult.Iterations}) для лучшей видимости";
            }

            // Рисуем разбиения для ВСЕХ выбранных методов
            foreach (var method in results.Keys)
            {
                if (method == IntegrationMethod.Simpson)
                {
                    // Для метода Симпсона убедимся, что n четное
                    int simpsonN = (nToPlot % 2 == 0) ? nToPlot : nToPlot - 1;
                    if (simpsonN >= 2)
                        PlotMethodPartitions(a, b, simpsonN, method);
                }
                else
                {
                    if (nToPlot >= 2)
                        PlotMethodPartitions(a, b, nToPlot, method);
                }
            }

            PlotModel.InvalidatePlot(true);
        }

        private void PlotFunction(double a, double b)
        {
            if (_calculator == null) return;

            functionSeries = new LineSeries
            {
                Title = $"f(x) = {txtFunction.Text}",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            int pointsCount = 500;
            double step = (b - a) / pointsCount;

            for (int i = 0; i <= pointsCount; i++)
            {
                double x = a + i * step;
                try
                {
                    double y = _calculator.CalculateFunction(x);
                    if (!double.IsNaN(y) && !double.IsInfinity(y))
                    {
                        functionSeries.Points.Add(new DataPoint(x, y));
                    }
                }
                catch { }
            }

            PlotModel.Series.Add(functionSeries);
        }

        private void PlotMethodPartitions(double a, double b, int n, IntegrationMethod method)
        {
            if (n <= 0 || _calculator == null) return;

            // Для Симпсона убедимся, что n четное
            if (method == IntegrationMethod.Simpson && n % 2 != 0)
            {
                n++;
            }

            double h = (b - a) / n;
            OxyColor color;
            string title;

            switch (method)
            {
                case IntegrationMethod.RectangleLeft:
                    color = OxyColors.Red;
                    title = "Прямоуг. (лев.)";
                    break;
                case IntegrationMethod.RectangleRight:
                    color = OxyColors.DarkOrange;
                    title = "Прямоуг. (прав.)";
                    break;
                case IntegrationMethod.RectangleMidpoint:
                    color = OxyColors.Purple;
                    title = "Прямоуг. (сред.)";
                    break;
                case IntegrationMethod.Trapezoidal:
                    color = OxyColors.Green;
                    title = "Трапеций";
                    break;
                case IntegrationMethod.Simpson:
                    color = OxyColors.DarkCyan;
                    title = "Симпсона";
                    break;
                default:
                    return;
            }

            if (method == IntegrationMethod.Simpson)
            {
                // Рисуем параболы для метода Симпсона
                PlotSimpsonPartitions(a, b, n, color, title);
            }
            else
            {
                // Для остальных методов рисуем прямоугольники/трапеции
                PlotSimplePartitions(a, b, n, h, method, color, title);
            }
        }

        private void PlotSimplePartitions(double a, double b, int n, double h, IntegrationMethod method, OxyColor color, string title)
        {
            if (n < 2 || _calculator == null)
            {
                // Не рисуем, если недостаточно разбиений
                return;
            }

            var partitionSeries = new LineSeries
            {
                Title = title,
                Color = color,
                StrokeThickness = 1
            };

            for (int i = 0; i < n; i++)
            {
                double x1 = a + i * h;
                double x2 = a + (i + 1) * h;

                double y1, y2;

                switch (method)
                {
                    case IntegrationMethod.RectangleLeft:
                        y1 = _calculator.CalculateFunction(x1);
                        y2 = y1;
                        break;
                    case IntegrationMethod.RectangleRight:
                        y2 = _calculator.CalculateFunction(x2);
                        y1 = y2;
                        break;
                    case IntegrationMethod.RectangleMidpoint:
                        double xMid = (x1 + x2) / 2;
                        y1 = _calculator.CalculateFunction(xMid);
                        y2 = y1;
                        break;
                    case IntegrationMethod.Trapezoidal:
                        y1 = _calculator.CalculateFunction(x1);
                        y2 = _calculator.CalculateFunction(x2);
                        break;
                    default:
                        y1 = y2 = 0;
                        break;
                }

                // Рисуем фигуру (прямоугольник или трапецию)
                partitionSeries.Points.Add(new DataPoint(x1, 0));
                partitionSeries.Points.Add(new DataPoint(x1, y1));

                if (method == IntegrationMethod.Trapezoidal)
                {
                    partitionSeries.Points.Add(new DataPoint(x2, y2));
                }
                else
                {
                    partitionSeries.Points.Add(new DataPoint(x2, y2));
                }

                partitionSeries.Points.Add(new DataPoint(x2, 0));

                // Разрыв между фигурами
                partitionSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            }

            PlotModel.Series.Add(partitionSeries);
        }

        private void PlotSimpsonPartitions(double a, double b, int n, OxyColor color, string title)
        {
            if (n < 2 || _calculator == null) return;

            var simpsonSeries = new LineSeries
            {
                Title = title,
                Color = color,
                StrokeThickness = 1
            };

            double h = (b - a) / n;

            // Рисуем параболы на каждом отрезке [xᵢ, xᵢ₊₂]
            for (int i = 0; i < n; i += 2)
            {
                if (i + 2 > n) break;

                double x0 = a + i * h;
                double x1 = a + (i + 1) * h;
                double x2 = a + (i + 2) * h;

                double y0 = _calculator.CalculateFunction(x0);
                double y1 = _calculator.CalculateFunction(x1);
                double y2 = _calculator.CalculateFunction(x2);

                // Рисуем параболу через три точки
                for (int j = 0; j <= 20; j++)
                {
                    double t = j / 20.0;
                    double x = x0 + t * 2 * h;

                    // Интерполяция Лагранжа для параболы
                    double y = y0 * ((x - x1) * (x - x2)) / ((x0 - x1) * (x0 - x2)) +
                               y1 * ((x - x0) * (x - x2)) / ((x1 - x0) * (x1 - x2)) +
                               y2 * ((x - x0) * (x - x1)) / ((x2 - x0) * (x2 - x1));

                    simpsonSeries.Points.Add(new DataPoint(x, y));
                }

                // Разрыв между параболами
                simpsonSeries.Points.Add(new DataPoint(double.NaN, double.NaN));

                // Рисуем вертикальные линии в точках разбиения
                var verticalSeries = new LineSeries
                {
                    Color = color,
                    StrokeThickness = 0.5,
                    LineStyle = LineStyle.Dash
                };

                verticalSeries.Points.Add(new DataPoint(x0, 0));
                verticalSeries.Points.Add(new DataPoint(x0, y0));
                verticalSeries.Points.Add(new DataPoint(double.NaN, double.NaN));

                verticalSeries.Points.Add(new DataPoint(x1, 0));
                verticalSeries.Points.Add(new DataPoint(x1, y1));
                verticalSeries.Points.Add(new DataPoint(double.NaN, double.NaN));

                verticalSeries.Points.Add(new DataPoint(x2, 0));
                verticalSeries.Points.Add(new DataPoint(x2, y2));

                PlotModel.Series.Add(verticalSeries);
            }

            PlotModel.Series.Add(simpsonSeries);
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

        private void UpdateBoundsInfo()
        {
            try
            {
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                tbBounds.Text = $"[{a:F2}, {b:F2}]";
            }
            catch
            {
                tbBounds.Text = "[?, ?]";
            }
        }

        private void UpdateFunctionInfo()
        {
            tbFunction.Text = txtFunction.Text;
        }

        // Пошаговый режим
        private void StartStepByStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                string function = PreprocessFunction(txtFunction.Text);

                _calculator = new IntegralCalculator(function);
                _iterationHistory.Clear();
                _currentStep = 0;

                _isStepByStepMode = true;
                miNextStep.IsEnabled = true;
                btnNextStep.IsEnabled = true;
                btnStepByStep.IsEnabled = false;
                btnCalculate.IsEnabled = false;

                lblStepInfo.Text = $"Шаг 1: Начало интервала [{a:F2}, {b:F2}]";
                PlotFunction(a, b);
                lblStatus.Text = "Пошаговый режим: готов к первому шагу";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (!_isStepByStepMode || _calculator == null) return;

            try
            {
                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int maxN = int.Parse(txtN.Text);

                _currentStep++;
                int currentN = Math.Min(4 * _currentStep, maxN);

                List<IntegrationMethod> methods = GetSelectedMethods();
                if (methods.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один метод интегрирования!", "Внимание",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool autoN = miAutoN.IsChecked == true;
                Dictionary<IntegrationMethod, IntegrationResult> results;

                if (autoN)
                {
                    results = _calculator.CalculateWithAutoN(a, b, epsilon, currentN, methods);
                }
                else
                {
                    results = _calculator.CalculateWithFixedN(a, b, currentN, methods);
                }

                // Сохраняем историю
                foreach (var result in results.Values)
                {
                    _iterationHistory.Add(result);
                }

                // Отображаем текущий шаг
                if (results.Count > 0)
                {
                    var firstResult = results.Values.First();
                    string nInfo = autoN ? $"авто N={firstResult.Iterations}" : $"N={currentN}";
                    lblStepInfo.Text = $"Шаг {_currentStep}: {nInfo}, значение={firstResult.Value:F6}";

                    // Показываем разбиение
                    PlotIntegration(a, b, results);

                    if ((!autoN && currentN >= maxN) ||
                        (autoN && firstResult.ErrorEstimate < epsilon))
                    {
                        FinishStepByStep(results);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка на шаге: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                FinishStepByStep(new Dictionary<IntegrationMethod, IntegrationResult>());
            }
        }

        private void FinishStepByStep(Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            _isStepByStepMode = false;
            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false;
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;

            if (results.Count > 0)
            {
                DisplayResults(results);
                lblStatus.Text = $"Пошаговый режим завершен. Выполнено шагов: {_currentStep}";
            }
            else
            {
                lblStatus.Text = "Пошаговый режим прерван";
            }
        }

        private void ResetStepMode()
        {
            _isStepByStepMode = false;
            miNextStep.IsEnabled = false;
            btnNextStep.IsEnabled = false;
            btnStepByStep.IsEnabled = true;
            btnCalculate.IsEnabled = true;
            _iterationHistory.Clear();
            _currentStep = 0;
            lblStepInfo.Text = "Готов к расчету";
            lblStatus.Text = "Готов к работе";
        }

        private void ResetSteps_Click(object sender, RoutedEventArgs e)
        {
            ResetStepMode();
        }

        private bool ValidateInput()
        {
            // Проверка заполненности полей
            if (string.IsNullOrWhiteSpace(txtA.Text) ||
                string.IsNullOrWhiteSpace(txtB.Text) ||
                string.IsNullOrWhiteSpace(txtEpsilon.Text) ||
                string.IsNullOrWhiteSpace(txtN.Text) ||
                string.IsNullOrWhiteSpace(txtFunction.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка числовых значений
            if (!double.TryParse(txtA.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(txtB.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double b) ||
                !double.TryParse(txtEpsilon.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double epsilon) ||
                !int.TryParse(txtN.Text, out int n))
            {
                MessageBox.Show("Некорректные числовые значения!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка интервала
            if (a >= b)
            {
                MessageBox.Show("Должно быть a < b!", "Ошибка ввода",
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

            // Проверка N
            if (n <= 0 || n > 1000000)
            {
                MessageBox.Show("Количество разбиений N должно быть от 1 до 1000000!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "0";
            txtB.Text = "3.14159";
            txtEpsilon.Text = "0.0001";
            txtN.Text = "1000";
            txtFunction.Text = "sin(x)";

            // Сброс чекбоксов
            cbRectLeft.IsChecked = true;
            cbRectRight.IsChecked = false;
            cbRectMid.IsChecked = false;
            cbTrapezoid.IsChecked = true;
            cbSimpson.IsChecked = true;
            miDrawPartitions.IsChecked = true;
            _drawPartitions = true;

            // Очистка результатов
            spResults.Children.Clear();

            lblStepInfo.Text = "Готов к расчету";
            lblIterationInfo.Text = "";
            lblStatus.Text = "Готов к работе";
            lblTime.Text = "Время: 0 мс";
            lblIntegralInfo.Text = "∫[a,b] f(x)dx";
            lblPlotWarning.Text = "";
            lblDrawingInfo.Text = "Рисование: включено";

            // Очистка графика
            PlotModel.Series.Clear();
            InitializePlotModel();
            ResetStepMode();

            UpdateBoundsInfo();
            UpdateFunctionInfo();
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

Базовые операции: + - * / ^
Степень: x^2 или pow(x,2)
Тригонометрические: sin(x), cos(x), tan(x)
Обратные тригонометрические: asin(x), acos(x), atan(x)
Экспонента и логарифмы: exp(x), ln(x), log10(x), log(x,основание)
Корни: sqrt(x), x^(1/2)
Модуль: abs(x)
Гиперболические: sinh(x), cosh(x), tanh(x)

Константы: pi, e

Примеры функций для интегрирования:
• sin(x)
• x^2 + 2*x + 1
• exp(-x^2)
• 1/(1+x^2)
• sqrt(4-x^2)";

            MessageBox.Show(helpText, "Справка по синтаксису функций",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutMethods_Click(object sender, RoutedEventArgs e)
        {
            string aboutText = @"Методы численного интегрирования:

1. Метод прямоугольников:
   - Левые: ∫f(x)dx ≈ h·Σf(xᵢ)
   - Правые: ∫f(x)dx ≈ h·Σf(xᵢ₊₁)
   - Средние: ∫f(x)dx ≈ h·Σf((xᵢ+xᵢ₊₁)/2)
   Погрешность: O(h)

2. Метод трапеций:
   ∫f(x)dx ≈ h/2·[f(a)+2Σf(xᵢ)+f(b)]
   Погрешность: O(h²)

3. Метод Симпсона (парабол):
   ∫f(x)dx ≈ h/3·[f(a)+4Σf(x_нечет)+2Σf(x_чет)+f(b)]
   Погрешность: O(h⁴)
   Требует четного N

Автоматический выбор N: удваивает количество разбиений до достижения заданной точности.";

            MessageBox.Show(aboutText, "О методах интегрирования",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoNCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            miAutoN.IsChecked = true;
            txtEpsilon.IsEnabled = true;
            txtEpsilon.Background = System.Windows.Media.Brushes.White;
            txtN.IsEnabled = true;
            txtN.Background = System.Windows.Media.Brushes.White;
            txtN.ToolTip = "Максимальное количество разбиений";
            tbMode.Text = "Автоматический выбор N";
            lblCurrentMode.Text = "Режим: автоматический выбор N";
            lblStatus.Text = "Включен автоматический выбор N (удваивает N до достижения точности)";
        }

        private void AutoNCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            miAutoN.IsChecked = false;
            txtEpsilon.IsEnabled = false;
            txtEpsilon.Background = System.Windows.Media.Brushes.LightGray;
            txtN.IsEnabled = true;
            txtN.Background = System.Windows.Media.Brushes.White;
            txtN.ToolTip = "Количество разбиений";
            tbMode.Text = $"Фиксированное N: {txtN.Text}";
            lblCurrentMode.Text = "Режим: фиксированное N";
            lblStatus.Text = "Используется указанное количество разбиений";
        }

        private void AboutProgram_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("IntegralProg v1.0\nВычисление определенных интегралов\n\n" +
                          "Направление подготовки: 09.03.03 – Прикладная информатика\n" +
                          "Кемерово, 2025\n\n" +
                          "Реализованные методы:\n" +
                          "- Метод прямоугольников (левый, правый, средний)\n" +
                          "- Метод трапеций\n" +
                          "- Метод Симпсона (парабол)",
                          "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoN_Click(object sender, RoutedEventArgs e)
        {
            txtN.IsEnabled = !(miAutoN.IsChecked == true);
            if (miAutoN.IsChecked == true)
            {
                lblStatus.Text = "Включен автоматический выбор N (удваивает N пока не достигнет точности)";
                txtEpsilon.IsEnabled = true;
            }
            else
            {
                lblStatus.Text = "Ручной ввод N (используется указанное количество разбиений)";
                txtEpsilon.IsEnabled = false;
            }
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

        private void DrawPartitions_Click(object sender, RoutedEventArgs e)
        {
            _drawPartitions = miDrawPartitions.IsChecked == true;
            lblDrawingInfo.Text = _drawPartitions ? "Рисование: включено" : "Рисование: выключено";

            // Если есть предыдущие результаты, перерисовываем график
            if (_calculator != null && PlotModel.Series.Count > 0)
            {
                try
                {
                    double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                    double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);

                    // Сохраняем текущие результаты
                    var currentMethods = GetSelectedMethods();
                    if (currentMethods.Count > 0)
                    {
                        // Получаем последние результаты
                        var results = new Dictionary<IntegrationMethod, IntegrationResult>();
                        foreach (var method in currentMethods)
                        {
                            var n = int.Parse(txtN.Text);
                            if (method == IntegrationMethod.Simpson && n % 2 != 0) n++;
                            var result = _calculator.CalculateWithFixedN(a, b, n, new List<IntegrationMethod> { method });
                            if (result.ContainsKey(method))
                                results[method] = result[method];
                        }

                        // Перерисовываем график
                        PlotIntegration(a, b, results);
                    }

                    lblStatus.Text = _drawPartitions ?
                        "Рисование разбиений включено" :
                        "Рисование разбиений выключено";
                }
                catch { }
            }
        }

        private void ColorFunctionBlue_Click(object sender, RoutedEventArgs e)
        {
            if (functionSeries != null) functionSeries.Color = OxyColors.Blue;
            PlotModel.InvalidatePlot(true);
        }

        private void ColorFunctionRed_Click(object sender, RoutedEventArgs e)
        {
            if (functionSeries != null) functionSeries.Color = OxyColors.Red;
            PlotModel.InvalidatePlot(true);
        }

        private void ColorFunctionGreen_Click(object sender, RoutedEventArgs e)
        {
            if (functionSeries != null) functionSeries.Color = OxyColors.Green;
            PlotModel.InvalidatePlot(true);
        }

        private string PreprocessFunction(string function)
        {
            if (string.IsNullOrWhiteSpace(function))
                return function;

            string result = function;

            // Заменяем ^ на pow
            result = Regex.Replace(result, @"(\w+)\^(\d+)", "pow($1,$2)");
            result = Regex.Replace(result, @"(\d+)\^(\w+)", "pow($1,$2)");
            result = Regex.Replace(result, @"(\w+)\^\(([^)]+)\)", "pow($1,$2)");
            result = Regex.Replace(result, @"\(([^)]+)\)\^(\w+)", "pow($1,$2)");

            // Стандартные замены
            result = result.Replace("ln", "log");
            result = result.Replace(",", ".");

            return result;
        }
    }

    public enum IntegrationMethod
    {
        RectangleLeft,
        RectangleRight,
        RectangleMidpoint,
        Trapezoidal,
        Simpson
    }

    public class IntegrationResult
    {
        public IntegrationMethod Method { get; set; }
        public double Value { get; set; }
        public int Iterations { get; set; }
        public double ErrorEstimate { get; set; }
        public List<double> History { get; set; } = new List<double>();
    }
}