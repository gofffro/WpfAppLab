using System;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Data;

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

                double a = double.Parse(txtA.Text);
                double b = double.Parse(txtB.Text);
                double epsilon = double.Parse(txtEpsilon.Text);
                string function = txtFunction.Text;

                DihotomyMethod method = new DihotomyMethod(function);
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
            if (string.IsNullOrWhiteSpace(txtA.Text) || string.IsNullOrWhiteSpace(txtB.Text) || string.IsNullOrWhiteSpace(txtEpsilon.Text) || string.IsNullOrWhiteSpace(txtFunction.Text))
            {
                MessageBox.Show("В1се поля должны быть заполнены!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}