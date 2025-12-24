using ClosedXML.Excel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace WpfApp1.LeastSquareApp
{
    public partial class LeastSquare : Window
    {
        public ObservableCollection<PointRow> Points { get; } = new ObservableCollection<PointRow>();

        private PlotModel _plotModel = null!;
        private ScatterSeries _scatter = null!;
        private LineSeries _line1 = null!;
        private LineSeries _line2 = null!;

        private OxyColor _line1Color = OxyColors.Blue;
        private OxyColor _line2Color = OxyColors.DarkCyan;

        private LeastSquaresResult? _res1;
        private LeastSquaresResult? _res2;

        public LeastSquare()
        {
            InitializeComponent();
            DataContext = this;

            InitializePlot();
            UpdateStatus();
        }

        private void InitializePlot()
        {
            _plotModel = new PlotModel
            {
                Title = "Аппроксимация методом наименьших квадратов",
                TitleColor = OxyColors.DarkBlue,
                TextColor = OxyColors.DarkBlue,
                PlotAreaBorderColor = OxyColors.Gray,
                PlotAreaBorderThickness = new OxyThickness(1)
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "x",
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray
            };

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "y",
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray
            };

            _plotModel.Axes.Add(xAxis);
            _plotModel.Axes.Add(yAxis);

            _scatter = new ScatterSeries
            {
                Title = "Точки",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = OxyColors.Black
            };

            _line1 = new LineSeries
            {
                Title = "Аппроксимация n=1",
                Color = _line1Color,
                StrokeThickness = 2
            };

            _line2 = new LineSeries
            {
                Title = "Аппроксимация n=2",
                Color = _line2Color,
                StrokeThickness = 2
            };

            _plotModel.Series.Add(_scatter);
            _plotModel.Series.Add(_line1);
            _plotModel.Series.Add(_line2);

            plotView.Model = _plotModel;
        }

        // -------------------- MENU ACTIONS --------------------

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var data = ReadValidPoints();
                if (data.Length < 2)
                {
                    MessageBox.Show("Нужно минимум 2 корректные точки (X,Y).", "Ввод данных",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _res1 = null;
                _res2 = null;

                // n=1
                _res1 = LeastSquaresCalculator.FitPolynomial(data, degree: 1);

                // n=2 (только если есть минимум 3 точки)
                if (data.Length >= 3)
                    _res2 = LeastSquaresCalculator.FitPolynomial(data, degree: 2);

                RenderResults(data);
                lblStatus.Text = "Расчет выполнен";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка расчета", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Ошибка";
            }
            finally
            {
                sw.Stop();
                lblTime.Text = $"Время: {sw.ElapsedMilliseconds} мс";
                UpdateStatus();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Points.Clear();
            _res1 = null;
            _res2 = null;

            tbEq1.Text = "n=1: —";
            tbEq2.Text = "n=2: —";
            tbMetrics1.Text = "Метрики n=1: —";
            tbMetrics2.Text = "Метрики n=2: —";

            ClearPlot();
            lblStatus.Text = "Очищено";
            lblTime.Text = "Время: 0 мс";
            UpdateStatus();
        }

        private void FillSample_Click(object sender, RoutedEventArgs e)
        {
            Points.Clear();

            // Пример: y ≈ 1.2 + 0.7x - 0.15x^2 (с небольшим шумом)
            var rnd = new Random(1);
            double[] xs = { -4, -3, -2, -1, 0, 1, 2, 3, 4 };
            foreach (var x in xs)
            {
                var y = 1.2 + 0.7 * x - 0.15 * x * x + (rnd.NextDouble() - 0.5) * 0.6;
                Points.Add(new PointRow { X = x.ToString(CultureInfo.InvariantCulture), Y = y.ToString(CultureInfo.InvariantCulture) });
            }

            lblStatus.Text = "Пример заполнен";
            UpdateStatus();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = ParseInt(txtGenN.Text, 2, 2000, "N");
                double xmin = ParseDouble(txtGenXMin.Text, "Xmin");
                double xmax = ParseDouble(txtGenXMax.Text, "Xmax");
                if (xmin >= xmax) throw new ArgumentException("Должно быть Xmin < Xmax.");

                double sigma = ParseDouble(txtGenNoise.Text, "σ");
                if (sigma < 0) throw new ArgumentException("σ должно быть неотрицательным.");

                // Базовая “истинная” функция для генерации: y = 2 - 0.4x + 0.2x^2 + noise
                var rnd = new Random();
                Points.Clear();

                for (int i = 0; i < n; i++)
                {
                    double x = xmin + (xmax - xmin) * i / (n - 1.0);
                    double noise = NextGaussian(rnd) * sigma;
                    double y = 2.0 - 0.4 * x + 0.2 * x * x + noise;

                    Points.Add(new PointRow
                    {
                        X = x.ToString(CultureInfo.InvariantCulture),
                        Y = y.ToString(CultureInfo.InvariantCulture)
                    });
                }

                lblStatus.Text = "Данные сгенерированы";
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Генерация", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                Title = "Загрузка точек из Excel"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                Points.Clear();

                using var wb = new XLWorkbook(dlg.FileName);
                var ws = wb.Worksheets.First();

                // Ожидаем 2 колонки: X и Y. Заголовки допускаются.
                // Читаем до первого пустого ряда.
                int row = 1;
                while (true)
                {
                    var c1 = ws.Cell(row, 1).GetValue<string>()?.Trim();
                    var c2 = ws.Cell(row, 2).GetValue<string>()?.Trim();

                    if (string.IsNullOrWhiteSpace(c1) && string.IsNullOrWhiteSpace(c2))
                        break;

                    // пропуск возможной строки заголовка
                    if (row == 1 && !TryParseDoubleAny(c1, out _) && !TryParseDoubleAny(c2, out _))
                    {
                        row++;
                        continue;
                    }

                    Points.Add(new PointRow { X = c1 ?? "", Y = c2 ?? "" });
                    row++;
                }

                lblStatus.Text = "Excel загружен";
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Загрузка Excel", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Ошибка";
            }
        }

        private void SaveExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                Title = "Сохранение точек в Excel",
                FileName = "points.xlsx"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Points");

                ws.Cell(1, 1).Value = "X";
                ws.Cell(1, 2).Value = "Y";
                ws.Range(1, 1, 1, 2).Style.Font.Bold = true;

                int r = 2;
                foreach (var p in Points)
                {
                    ws.Cell(r, 1).Value = p.X ?? "";
                    ws.Cell(r, 2).Value = p.Y ?? "";
                    r++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);

                lblStatus.Text = "Excel сохранен";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Сохранение Excel", MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "Ошибка";
            }
            finally
            {
                UpdateStatus();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void ToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            bool on = miGrid.IsChecked == true;
            foreach (var axis in _plotModel.Axes)
            {
                axis.MajorGridlineStyle = on ? LineStyle.Dash : LineStyle.None;
                axis.MinorGridlineStyle = on ? LineStyle.Dot : LineStyle.None;
            }
            _plotModel.InvalidatePlot(true);
        }

        private void ColorLine1Blue_Click(object sender, RoutedEventArgs e) => SetLine1Color(OxyColors.Blue);
        private void ColorLine1Red_Click(object sender, RoutedEventArgs e) => SetLine1Color(OxyColors.Red);
        private void ColorLine1Green_Click(object sender, RoutedEventArgs e) => SetLine1Color(OxyColors.Green);

        private void ColorLine2Blue_Click(object sender, RoutedEventArgs e) => SetLine2Color(OxyColors.Blue);
        private void ColorLine2Red_Click(object sender, RoutedEventArgs e) => SetLine2Color(OxyColors.Red);
        private void ColorLine2Green_Click(object sender, RoutedEventArgs e) => SetLine2Color(OxyColors.Green);

        private void AboutLSQ_Click(object sender, RoutedEventArgs e)
        {
            string text =
@"Метод наименьших квадратов (МНК) подбирает коэффициенты полинома
f(x) = a0 + a1 x + ... + an x^n так, чтобы сумма квадратов отклонений была минимальной:

S = Σ (yi - f(xi))^2 → min.

В этой работе считаются:
• Полином 1-й степени (n=1): a0 + a1 x
• Полином 2-й степени (n=2): a0 + a1 x + a2 x^2

Также выводятся метрики:
• SSE = Σe^2
• RMSE = sqrt(SSE / m)
• R² = 1 - SSE/SST";
            MessageBox.Show(text, "О методе МНК", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutProgram_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("LeastSquares v1.0\nАппроксимация МНК для n=1 и n=2\n\n" +
                            "Направление: 09.03.03 – Прикладная информатика\nКемерово\n",
                            "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // -------------------- CORE --------------------

        private (double x, double y)[] ReadValidPoints()
        {
            var list = Points
                .Select(p => new { p.X, p.Y })
                .Where(p => !string.IsNullOrWhiteSpace(p.X) && !string.IsNullOrWhiteSpace(p.Y))
                .Select(p =>
                {
                    if (!TryParseDoubleAny(p.X!, out var x))
                        throw new ArgumentException($"Некорректный X: \"{p.X}\"");
                    if (!TryParseDoubleAny(p.Y!, out var y))
                        throw new ArgumentException($"Некорректный Y: \"{p.Y}\"");
                    if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                        throw new ArgumentException("В таблице обнаружены NaN/Infinity.");
                    return (x, y);
                })
                .ToArray();

            // Проверка на вырожденность по X (для n=1 и n=2 нужен разброс X)
            if (list.Length >= 2)
            {
                double minX = list.Min(t => t.x);
                double maxX = list.Max(t => t.x);
                if (Math.Abs(maxX - minX) < 1e-12)
                    throw new ArgumentException("Все X одинаковые — аппроксимация невозможна (вырождение).");
            }

            return list;
        }

        private void RenderResults((double x, double y)[] data)
        {
            // Тексты
            if (_res1 != null)
            {
                tbEq1.Text = $"n=1:  y = {FormatA(_res1.Coefficients[0])} + {FormatA(_res1.Coefficients[1])} * x";
                tbMetrics1.Text = $"Метрики n=1:  SSE={_res1.SSE:F6}   RMSE={_res1.RMSE:F6}   R²={_res1.R2:F6}";
            }
            else
            {
                tbEq1.Text = "n=1: —";
                tbMetrics1.Text = "Метрики n=1: —";
            }

            if (_res2 != null)
            {
                tbEq2.Text = $"n=2:  y = {FormatA(_res2.Coefficients[0])} + {FormatA(_res2.Coefficients[1])} * x + {FormatA(_res2.Coefficients[2])} * x^2";
                tbMetrics2.Text = $"Метрики n=2:  SSE={_res2.SSE:F6}   RMSE={_res2.RMSE:F6}   R²={_res2.R2:F6}";
            }
            else
            {
                tbEq2.Text = "n=2: — (нужно минимум 3 точки)";
                tbMetrics2.Text = "Метрики n=2: —";
            }

            // График
            PlotAll(data);
        }

        private void ClearPlot()
        {
            _scatter.Points.Clear();
            _line1.Points.Clear();
            _line2.Points.Clear();
            _plotModel.InvalidatePlot(true);
        }

        private void PlotAll((double x, double y)[] data)
        {
            _scatter.Points.Clear();
            _line1.Points.Clear();
            _line2.Points.Clear();

            foreach (var (x, y) in data)
                _scatter.Points.Add(new ScatterPoint(x, y));

            double minX = data.Min(t => t.x);
            double maxX = data.Max(t => t.x);

            // небольшой отступ по краям
            double pad = (maxX - minX) * 0.05;
            if (pad <= 0) pad = 1.0;

            double from = minX - pad;
            double to = maxX + pad;

            int samples = 400;
            double step = (to - from) / (samples - 1);

            if (_res1 != null)
            {
                for (int i = 0; i < samples; i++)
                {
                    double x = from + i * step;
                    double y = LeastSquaresCalculator.EvalPoly(_res1.Coefficients, x);
                    if (!double.IsNaN(y) && !double.IsInfinity(y))
                        _line1.Points.Add(new DataPoint(x, y));
                }
            }

            if (_res2 != null)
            {
                for (int i = 0; i < samples; i++)
                {
                    double x = from + i * step;
                    double y = LeastSquaresCalculator.EvalPoly(_res2.Coefficients, x);
                    if (!double.IsNaN(y) && !double.IsInfinity(y))
                        _line2.Points.Add(new DataPoint(x, y));
                }
            }

            _line1.Color = _line1Color;
            _line2.Color = _line2Color;

            _plotModel.InvalidatePlot(true);
        }

        private void SetLine1Color(OxyColor c)
        {
            _line1Color = c;
            _line1.Color = c;
            _plotModel.InvalidatePlot(true);
        }

        private void SetLine2Color(OxyColor c)
        {
            _line2Color = c;
            _line2.Color = c;
            _plotModel.InvalidatePlot(true);
        }

        private void UpdateStatus()
        {
            int countFilled = Points.Count(p =>
                !string.IsNullOrWhiteSpace(p.X) &&
                !string.IsNullOrWhiteSpace(p.Y));

            lblInfo.Text = $"Точек: {countFilled}";
        }

        // -------------------- HELPERS --------------------

        private static bool TryParseDoubleAny(string s, out double value)
        {
            s = (s ?? "").Trim().Replace(" ", "");

            // пробуем Invariant (точка)
            if (double.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;

            // пробуем текущую культуру
            return double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        private static double ParseDouble(string s, string name)
        {
            if (!TryParseDoubleAny(s, out var v))
                throw new ArgumentException($"Некорректное значение {name}: \"{s}\"");
            return v;
        }

        private static int ParseInt(string s, int min, int max, string name)
        {
            if (!int.TryParse((s ?? "").Trim(), out int v))
                throw new ArgumentException($"Некорректное значение {name}: \"{s}\"");
            if (v < min || v > max)
                throw new ArgumentException($"{name} должно быть в диапазоне [{min}; {max}]");
            return v;
        }

        private static double NextGaussian(Random rnd)
        {
            // Box–Muller
            double u1 = 1.0 - rnd.NextDouble();
            double u2 = 1.0 - rnd.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static string FormatA(double v)
        {
            // компактно, но читаемо
            if (Math.Abs(v) < 1e-12) v = 0;
            return v.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
