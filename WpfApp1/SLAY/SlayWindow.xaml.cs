using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using WpfApp1.SLAY;
namespace WpfApp1.SLAY
{
    public partial class SlayWindow : Window
    {
        private int matrixSize = 3;
        private Random random = new Random();
        private Dictionary<string, TimeSpan> methodTimes = new Dictionary<string, TimeSpan>();

        private MathMethods solver = new MathMethods();

        public SlayWindow()
        {
            InitializeComponent();
            InitializeDataGrids();
            UpdateStatus("Готов к работе");
        }

        private void InitializeDataGrids()
        {
            CreateMatrixA();
            CreateVectorB();
            CreateVectorX();
        }

        private void CreateMatrixA()
        {
            MatrixADataGrid.Columns.Clear();
            MatrixADataGrid.ItemsSource = null; // Добавить очистку

            for (int i = 0; i < matrixSize; i++)
            {
                MatrixADataGrid.Columns.Add(new DataGridTextColumn()
                {
                    Header = $"a{i + 1}",
                    Binding = new System.Windows.Data.Binding($"[{i}]")
                });
            }

            var matrixData = new List<List<double>>();
            for (int i = 0; i < matrixSize; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < matrixSize; j++)
                {
                    row.Add(0); // Заполняем нулями вместо пустых массивов
                }
                matrixData.Add(row);
            }
            MatrixADataGrid.ItemsSource = matrixData; // Убрать Select().ToList()
        }

        private void CreateVectorB()
        {
            VectorBDataGrid.Columns.Clear();
            VectorBDataGrid.ItemsSource = null; // Добавить очистку

            VectorBDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "b",
                Binding = new System.Windows.Data.Binding("[0]")
            });

            var vectorData = new List<List<double>>();
            for (int i = 0; i < matrixSize; i++)
            {
                vectorData.Add(new List<double> { 0 }); // Заполняем нулями
            }
            VectorBDataGrid.ItemsSource = vectorData; // Убрать Select().ToList()
        }

        private void CreateVectorX()
        {
            VectorXDataGrid.Columns.Clear();
            VectorXDataGrid.ItemsSource = null;

            VectorXDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "x",
                Binding = new System.Windows.Data.Binding("[0]")
            });
        }

        // Обработчики событий меню
        private async void GaussMethod_Click(object sender, RoutedEventArgs e)
        {
            await SolveWithMethod("Гаусс", SolveGaussAsync);
        }

        private async void JordanGaussMethod_Click(object sender, RoutedEventArgs e)
        {
            await SolveWithMethod("Жардан-Гаусс", SolveJordanGaussAsync);
        }

        private async void CramerMethod_Click(object sender, RoutedEventArgs e)
        {
            await SolveWithMethod("Крамер", SolveCramerAsync);
        }

        private async void AllMethods_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputData()) return;

            UpdateStatus("Выполняются все методы...");

            await SolveGaussAsync();
            await SolveJordanGaussAsync();
            await SolveCramerAsync();

            UpdateStatus("Все методы завершены");
            ShowMethodComparison();
        }

        private async Task SolveWithMethod(string methodName, Func<Task<double[]>> method)
        {
            if (!ValidateInputData()) return;

            UpdateStatus($"Выполняется метод {methodName}...");

            var stopwatch = Stopwatch.StartNew();
            var result = await method();
            stopwatch.Stop();

            methodTimes[methodName] = stopwatch.Elapsed;
            DisplayResult(result, methodName);
            UpdateStatus($"Метод {methodName} завершен");
        }

        // Валидация данных
        private bool ValidateInputData()
        {
            try
            {
                var matrixA = GetMatrixA();
                var vectorB = GetVectorB();

                if (matrixA == null || vectorB == null)
                {
                    MessageBox.Show("Ошибка в данных матрицы или вектора", "Ошибка");
                    return false;
                }

                // Проверка на вырожденность матрицы
                if (Math.Abs(solver.Determinant(matrixA)) < 1e-10)
                {
                    MessageBox.Show("Матрица вырождена (определитель близок к нулю)", "Ошибка");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка валидации: {ex.Message}", "Ошибка");
                return false;
            }
        }

        // Методы решения СЛАУ
        public async Task<double[]> SolveGaussAsync()
        {
            return await Task.Run(() =>
            {
                var A = GetMatrixA();
                var B = GetVectorB();
                return solver.SolveByGauss(A, B);
            });
        }

        public async Task<double[]> SolveJordanGaussAsync()
        {
            return await Task.Run(() =>
            {
                var A = GetMatrixA();
                var B = GetVectorB();
                return solver.SolveByJordanGauss(A, B);
            });
        }

        private async Task<double[]> SolveCramerAsync()
        {
            return await Task.Run(() =>
            {
                var A = GetMatrixA();
                var B = GetVectorB();
                return solver.SolveByCramer(A, B);
            });
        }

        private double[,] GetMatrixA()
        {
            try
            {
                var items = MatrixADataGrid.ItemsSource as List<List<double>>;
                if (items == null)
                { 
                    return null;
                }

                double[,] matrix = new double[matrixSize, matrixSize];
                for (int i = 0; i < matrixSize; i++)
                {
                    for (int j = 0; j < matrixSize; j++)
                    {
                        matrix[i, j] = items[i][j];
                    }
                }
                return matrix;
            }
            catch
            {
                return null;
            }
        }

        private double[] GetVectorB()
        {
            try
            {
                var items = VectorBDataGrid.ItemsSource as List<List<double>>;
                if (items == null) return null;

                double[] vector = new double[matrixSize];
                for (int i = 0; i < matrixSize; i++)
                {
                    vector[i] = items[i][0];
                }
                return vector;
            }
            catch
            {
                return null;
            }
        }

        private void DisplayResult(double[] result, string methodName)
        {
            var resultData = new List<List<double>>();
            foreach (var value in result)
            {
                resultData.Add(new List<double> { Math.Round(value, 6) });
            }
            VectorXDataGrid.ItemsSource = resultData;

            CalculationDetailsTextBox.Text = $"Метод: {methodName}\n" +
                                           $"Время: {methodTimes[methodName].TotalMilliseconds:F4} мс\n" +
                                           $"Результат:\n{string.Join("\n", result.Select((x, i) => $"x{i + 1} = {x:F6}"))}";
        }

        // Генерация случайных данных
        private void GenerateRandom_Click(object sender, RoutedEventArgs e)
        {
            GenerateWithRange_Click(sender, e);
        }

        private void GenerateWithRange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double min = double.Parse(MinValueTextBox.Text);
                double max = double.Parse(MaxValueTextBox.Text);

                var matrixData = new List<List<double>>();
                var vectorData = new List<List<double>>();

                for (int i = 0; i < matrixSize; i++)
                {
                    var row = new List<double>();
                    for (int j = 0; j < matrixSize; j++)
                    {
                        row.Add(Math.Round(random.NextDouble() * (max - min) + min, 2));
                    }
                    matrixData.Add(row);

                    vectorData.Add(new List<double> {
                        Math.Round(random.NextDouble() * (max - min) + min, 2)
                    });
                }

                MatrixADataGrid.ItemsSource = matrixData;
                VectorBDataGrid.ItemsSource = vectorData;

                UpdateStatus("Данные сгенерированы");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка");
            }
        }

        // Обработчики изменения размера матрицы
        private void MatrixSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(MatrixSizeTextBox.Text, out int size) && size >= 2 && size <= 50)
            {
                matrixSize = size;
            }
        }

        private void ApplySize_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MatrixSizeTextBox.Text, out int size) && size >= 2 && size <= 50)
            {
                matrixSize = size;
                InitializeDataGrids();
                UpdateStatus($"Размер матрицы изменен на {size}x{size}");
            }
            else
            {
                MessageBox.Show("Размер матрицы должен быть от 2 до 50", "Ошибка");
                MatrixSizeTextBox.Text = matrixSize.ToString();
            }
        }

        // Очистка данных
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            InitializeDataGrids();
            VectorXDataGrid.ItemsSource = null;
            CalculationDetailsTextBox.Text = "";
            UpdateStatus("Все данные очищены");
        }

        private void ClearMatrix_Click(object sender, RoutedEventArgs e)
        {
            CreateMatrixA();
            UpdateStatus("Матрица A очищена");
        }

        private void ClearVectorB_Click(object sender, RoutedEventArgs e)
        {
            CreateVectorB();
            UpdateStatus("Вектор B очищен");
        }

        // Заглушки для импорта/экспорта
        private void LoadFromExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция импорта из Excel будет реализована в будущем", "Информация");
        }

        private void LoadFromGoogle_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция импорта из Google Tables будет реализована в будущем", "Информация");
        }

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция экспорта в Excel будет реализована в будущем", "Информация");
        }

        // Вспомогательные методы
        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void ShowMethodComparison()
        {
            string comparison = "Сравнение методов:\n";
            foreach (var kvp in methodTimes)
            {
                comparison += $"{kvp.Key}: {kvp.Value.TotalMilliseconds:F4} мс\n";
            }
            CalculationDetailsTextBox.Text = comparison;
        }

        private void ValidateData_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputData())
            {
                MessageBox.Show("Данные корректны", "Проверка");
            }
        }

        private void MatrixDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void VectorDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}