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
        private List<LineSeries> rectangleSeries;
        private List<LineSeries> trapezoidSeries;
        private List<LineSeries> simpsonSeries;

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

            // Инициализация цветов
            functionSeries = new LineSeries
            {
                Title = "f(x)",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };
        }

        private void InitializePlotModel()
        {
            PlotModel = new PlotModel
            {
                Title = "График функции и численное интегрирование",
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

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                ResetStepMode();

                var stopwatch = Stopwatch.StartNew();

                double a = double.Parse(txtA.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double b = double.Parse(txtB.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                double epsilon = double.Parse(txtEpsilon.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                int n = int.Parse(txtN.Text);
                string function = PreprocessFunction(txtFunction.Text);

                // Обновляем статус
                lblIntegralBounds.Text = $"∫[{a:F2}, {b:F2}] f(x)dx";

                _calculator = new IntegralCalculator(function);

                // Собираем выбранные методы
                List<IntegrationMethod> methods = new List<IntegrationMethod>();
                if (cbRectLeft.IsChecked == true) methods.Add(IntegrationMethod.RectangleLeft);
                if (cbRectRight.IsChecked == true) methods.Add(IntegrationMethod.RectangleRight);
                if (cbRectMid.IsChecked == true) methods.Add(IntegrationMethod.RectangleMidpoint);
                if (cbTrapezoid.IsChecked == true) methods.Add(IntegrationMethod.Trapezoidal);
                if (cbSimpson.IsChecked == true) methods.Add(IntegrationMethod.Simpson);

                if (methods.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один метод интегрирования!", "Внимание",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool autoN = miAutoN.IsChecked;
                var results = _calculator.CalculateIntegral(a, b, epsilon, n, methods, autoN);

                stopwatch.Stop();
                lblTime.Text = $"Время: {stopwatch.ElapsedMilliseconds} мс";

                // Отображаем результаты
                DisplayResults(results);

                // Строим график с разбиениями
                PlotIntegration(a, b, n, results);

                lblStatus.Text = "Вычисление завершено";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вычисления: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Ошибка вычисления";
            }
        }

        private void DisplayResults(Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            // Очищаем предыдущие результаты
            spResults.Children.Clear();

            // Обновляем значения в таблице
            tbRectLeft.Text = results.ContainsKey(IntegrationMethod.RectangleLeft) ?
                $"{results[IntegrationMethod.RectangleLeft].Value:F6}" : "-";
            tbRectRight.Text = results.ContainsKey(IntegrationMethod.RectangleRight) ?
                $"{results[IntegrationMethod.RectangleRight].Value:F6}" : "-";
            tbRectMid.Text = results.ContainsKey(IntegrationMethod.RectangleMidpoint) ?
                $"{results[IntegrationMethod.RectangleMidpoint].Value:F6}" : "-";
            tbTrapezoid.Text = results.ContainsKey(IntegrationMethod.Trapezoidal) ?
                $"{results[IntegrationMethod.Trapezoidal].Value:F6}" : "-";
            tbSimpson.Text = results.ContainsKey(IntegrationMethod.Simpson) ?
                $"{results[IntegrationMethod.Simpson].Value:F6}" : "-";

            // Находим оптимальный результат (наименьшая погрешность)
            if (results.Count > 0)
            {
                var optimal = results.Values.OrderBy(r => r.ErrorEstimate).First();
                tbOptimalMethod.Text = $"Метод: {GetMethodName(optimal.Method)}";
                tbOptimalValue.Text = $"Значение: {optimal.Value:F8}";
                tbOptimalN.Text = $"Разбиений: {optimal.Iterations}";
                tbOptimalError.Text = $"Погрешность: {optimal.ErrorEstimate:E2}";

                // Добавляем подробные результаты
                foreach (var result in results.Values)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{GetMethodName(result.Method)}: ",
                        FontWeight = FontWeights.Bold,
                        Width = 150
                    });
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{result.Value:F8} (N={result.Iterations}, err={result.ErrorEstimate:E2})"
                    });
                    spResults.Children.Add(panel);
                }
            }
        }

        private string GetMethodName(IntegrationMethod method)
        {
            return method switch
            {
                IntegrationMethod.RectangleLeft => "Прямоугольники (лев.)",
                IntegrationMethod.RectangleRight => "Прямоугольники (прав.)",
                IntegrationMethod.RectangleMidpoint => "Прямоугольники (сред.)",
                IntegrationMethod.Trapezoidal => "Трапеций",
                IntegrationMethod.Simpson => "Симпсона",
                _ => "Неизвестный"
            };
        }

        private void PlotIntegration(double a, double b, int n, Dictionary<IntegrationMethod, IntegrationResult> results)
        {
            PlotModel.Series.Clear();

            // Рисуем функцию
            PlotFunction(a, b);

            // Получаем выбранные методы для отображения
            bool showRectangles = cbRectLeft.IsChecked == true || cbRectRight.IsChecked == true || cbRectMid.IsChecked == true;
            bool showTrapezoids = cbTrapezoid.IsChecked == true;
            bool showSimpson = cbSimpson.IsChecked == true;

            // Рисуем разбиения для первого выбранного метода
            if (showRectangles && results.ContainsKey(IntegrationMethod.RectangleLeft))
            {
                PlotRectangles(a, b, n, IntegrationMethod.RectangleLeft);
            }
            else if (showTrapezoids && results.ContainsKey(IntegrationMethod.Trapezoidal))
            {
                PlotTrapezoids(a, b, n);
            }
            else if (showSimpson && results.ContainsKey(IntegrationMethod.Simpson))
            {
                PlotSimpson(a, b, n);
            }

            PlotModel.InvalidatePlot(true);
        }

        private void PlotFunction(double a, double b)
        {
            functionSeries = new LineSeries
            {
                Title = $"f(x) = {txtFunction.Text}",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            int pointsCount = 1000;
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

        private void PlotRectangles(double a, double b, int n, IntegrationMethod method)
        {
            double h = (b - a) / n;
            var rectangleSeries = new LineSeries
            {
                Title = $"Разбиение ({GetMethodName(method)})",
                Color = OxyColors.Red,
                StrokeThickness = 1
            };

            for (int i = 0; i < n; i++)
            {
                double x1 = a + i * h;
                double x2 = a + (i + 1) * h;
                double y;

                switch (method)
                {
                    case IntegrationMethod.RectangleLeft:
                        y = _calculator.CalculateFunction(x1);
                        break;
                    case IntegrationMethod.RectangleRight:
                        y = _calculator.CalculateFunction(x2);
                        break;
                    case IntegrationMethod.RectangleMidpoint:
                        y = _calculator.CalculateFunction((x1 + x2) / 2);
                        break;
                    default:
                        y = 0;
                        break;
                }

                // Рисуем прямоугольник
                rectangleSeries.Points.Add(new DataPoint(x1, 0));
                rectangleSeries.Points.Add(new DataPoint(x1, y));
                rectangleSeries.Points.Add(new DataPoint(x2, y));
                rectangleSeries.Points.Add(new DataPoint(x2, 0));

                // Разрыв между прямоугольниками
                rectangleSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            }

            PlotModel.Series.Add(rectangleSeries);
        }

        private void PlotTrapezoids(double a, double b, int n)
        {
            double h = (b - a) / n;
            var trapezoidSeries = new LineSeries
            {
                Title = "Разбиение трапеций",
                Color = OxyColors.Green,
                StrokeThickness = 1
            };

            for (int i = 0; i < n; i++)
            {
                double x1 = a + i * h;
                double x2 = a + (i + 1) * h;
                double y1 = _calculator.CalculateFunction(x1);
                double y2 = _calculator.CalculateFunction(x2);

                // Рисуем трапецию
                trapezoidSeries.Points.Add(new DataPoint(x1, 0));
                trapezoidSeries.Points.Add(new DataPoint(x1, y1));
                trapezoidSeries.Points.Add(new DataPoint(x2, y2));
                trapezoidSeries.Points.Add(new DataPoint(x2, 0));

                // Разрыв между трапециями
                trapezoidSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            }

            PlotModel.Series.Add(trapezoidSeries);
        }

        private void PlotSimpson(double a, double b, int n)
        {
            if (n % 2 != 0) n++; // Делаем четным

            double h = (b - a) / n;
            var simpsonSeries = new LineSeries
            {
                Title = "Разбиение Симпсона",
                Color = OxyColors.Purple,
                StrokeThickness = 1
            };

            for (int i = 0; i < n; i += 2)
            {
                double x0 = a + i * h;
                double x1 = a + (i + 1) * h;
                double x2 = a + (i + 2) * h;

                double y0 = _calculator.CalculateFunction(x0);
                double y1 = _calculator.CalculateFunction(x1);
                double y2 = _calculator.CalculateFunction(x2);

                // Аппроксимируем параболу
                for (int j = 0; j <= 20; j++)
                {
                    double t = j / 20.0;
                    double x = x0 + t * 2 * h;
                    // Интерполяция Лагранжа для параболы
                    double y = y0 * ((x - x1) * (x - x2)) / ((x0 - x1) * (x0 - x2)) +
                               y1 * ((x - x0) * (x - x2)) / ((x1 - x0) * (x1 - x2)) +
                               y2 * ((x - x0) * (x - x1)) / ((x2 - x0) * (x2 - x1));

                    simpsonSeries.Points.Add(new DataPoint(x, Math.Max(y, 0)));
                }

                // Разрыв между параболами
                simpsonSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            }

            PlotModel.Series.Add(simpsonSeries);
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
                int currentN = Math.Min(4 * _currentStep, maxN); // Увеличиваем N по шагам

                if (currentN % 2 != 0 && cbSimpson.IsChecked == true)
                    currentN++; // Делаем четным для Симпсона

                List<IntegrationMethod> methods = new List<IntegrationMethod>();
                if (cbRectLeft.IsChecked == true) methods.Add(IntegrationMethod.RectangleLeft);
                if (cbRectRight.IsChecked == true) methods.Add(IntegrationMethod.RectangleRight);
                if (cbRectMid.IsChecked == true) methods.Add(IntegrationMethod.RectangleMidpoint);
                if (cbTrapezoid.IsChecked == true) methods.Add(IntegrationMethod.Trapezoidal);
                if (cbSimpson.IsChecked == true) methods.Add(IntegrationMethod.Simpson);

                var results = _calculator.CalculateIntegral(a, b, epsilon, currentN, methods, false);

                // Сохраняем историю
                foreach (var result in results.Values)
                {
                    _iterationHistory.Add(result);
                }

                // Отображаем текущий шаг
                if (results.Count > 0)
                {
                    var firstResult = results.Values.First();
                    lblStepInfo.Text = $"Шаг {_currentStep}: N={currentN}, значение={firstResult.Value:F6}";

                    // Показываем разбиение для первого метода
                    PlotIntegration(a, b, currentN, results);

                    if (currentN >= maxN || firstResult.ErrorEstimate < epsilon)
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
            lblStepInfo.Text = "";
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
            if (n <= 0 || n > 100000)
            {
                MessageBox.Show("Количество разбиений N должно быть от 1 до 100000!", "Ошибка ввода",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка для метода Симпсона
            if (cbSimpson.IsChecked == true && n % 2 != 0)
            {
                var result = MessageBox.Show("Для метода Симпсона N должно быть четным. Исправить на четное?",
                                           "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    n = (n % 2 == 0) ? n : n + 1;
                    txtN.Text = n.ToString();
                }
            }

            return true;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtA.Text = "0";
            txtB.Text = "3.14159";
            txtEpsilon.Text = "0.0001";
            txtN.Text = "100";
            txtFunction.Text = "sin(x)";

            // Сброс чекбоксов
            cbRectLeft.IsChecked = true;
            cbRectRight.IsChecked = false;
            cbRectMid.IsChecked = false;
            cbTrapezoid.IsChecked = true;
            cbSimpson.IsChecked = true;

            // Очистка результатов
            spResults.Children.Clear();
            tbRectLeft.Text = tbRectRight.Text = tbRectMid.Text = tbTrapezoid.Text = tbSimpson.Text = "-";
            tbOptimalMethod.Text = "Метод: -";
            tbOptimalValue.Text = "Значение: -";
            tbOptimalN.Text = "Разбиений: -";
            tbOptimalError.Text = "Погрешность: -";

            lblStepInfo.Text = "";
            lblStatus.Text = "Готов к работе";
            lblTime.Text = "Время: 0 мс";
            lblIntegralBounds.Text = "∫[a,b] f(x)dx";

            // Очистка графика
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

        private void AboutProgram_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("IntegralProg v1.0\nВычисление определенных интегралов\n\n" +
                          "Направление подготовки: 09.03.03 – Прикладная информатика\n" +
                          "Кемерово, 2024\n\n" +
                          "Реализованные методы:\n" +
                          "- Метод прямоугольников (левый, правый, средний)\n" +
                          "- Метод трапеций\n" +
                          "- Метод Симпсона (парабол)",
                          "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoN_Click(object sender, RoutedEventArgs e)
        {
            txtN.IsEnabled = !miAutoN.IsChecked;
            if (miAutoN.IsChecked)
            {
                lblStatus.Text = "Включен автоматический выбор N";
            }
            else
            {
                lblStatus.Text = "Ручной ввод N";
            }
        }

        private void ShowGrid_Click(object sender, RoutedEventArgs e)
        {
            foreach (var axis in PlotModel.Axes)
            {
                axis.MajorGridlineStyle = miShowGrid.IsChecked ? LineStyle.Dash : LineStyle.None;
                axis.MinorGridlineStyle = miShowGrid.IsChecked ? LineStyle.Dot : LineStyle.None;
            }
            PlotModel.InvalidatePlot(true);
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
