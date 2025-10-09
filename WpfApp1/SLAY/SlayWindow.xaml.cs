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
using OfficeOpenXml;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

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
            ExcelPackage.License.SetNonCommercialPersonal("SLAY Solver App");
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
            MatrixADataGrid.ItemsSource = null; 

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
                    row.Add(0); 
                }
                matrixData.Add(row);
            }
            MatrixADataGrid.ItemsSource = matrixData; 
        }

        private void CreateVectorB()
        {
            VectorBDataGrid.Columns.Clear();
            VectorBDataGrid.ItemsSource = null; 

            VectorBDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "b",
                Binding = new System.Windows.Data.Binding("[0]")
            });

            var vectorData = new List<List<double>>();
            for (int i = 0; i < matrixSize; i++)
            {
                vectorData.Add(new List<double> { 0 }); // zapolnenie nylyami
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
            if (!ValidateInputData())
            {
                return;
            }

            UpdateStatus("Выполняются все методы...");
            methodTimes.Clear();

            var stopwatch = Stopwatch.StartNew();

            var gaussWatch = Stopwatch.StartNew();
            var gaussResult = await SolveGaussAsync();
            gaussWatch.Stop();
            methodTimes["Гаусс"] = gaussWatch.Elapsed;
            DisplayResult(gaussResult, "Гаусс");

            var jordanWatch = Stopwatch.StartNew();
            var jordanResult = await SolveJordanGaussAsync();
            jordanWatch.Stop();
            methodTimes["Жардан-Гаусс"] = jordanWatch.Elapsed;
            DisplayResult(jordanResult, "Жардан-Гаусс");

            var cramerWatch = Stopwatch.StartNew();
            var cramerResult = await SolveCramerAsync();
            cramerWatch.Stop();
            methodTimes["Крамер"] = cramerWatch.Elapsed;
            DisplayResult(cramerResult, "Крамер");

            stopwatch.Stop();

            ShowMethodComparison();
            UpdateStatus("Все методы завершены");
        }

        private async Task SolveWithMethod(string methodName, Func<Task<double[]>> method)
        {
            if (!ValidateInputData())
            {
                return;
            }

            UpdateStatus($"Выполняется метод {methodName}...");

            var stopwatch = Stopwatch.StartNew();
            var result = await method();
            stopwatch.Stop();

            methodTimes[methodName] = stopwatch.Elapsed;
            DisplayResult(result, methodName);
            UpdateStatus($"Метод {methodName} завершен");
        }

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
                if (items == null)
                { 
                    return null;
                }

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

        private void LoadFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*";
                openFileDialog.Title = "Выберите файл Excel";

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadDataFromExcel(openFileDialog.FileName);
                    UpdateStatus($"Данные загружены из {System.IO.Path.GetFileName(openFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки из Excel: {ex.Message}", "Ошибка");
            }
        }

        private void LoadDataFromExcel(string filePath)
        {
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                if (package.Workbook.Worksheets.Count == 0)
                { 
                    throw new Exception("В файле нет листов");
                }

                var worksheet = package.Workbook.Worksheets[0];

                int rows = 0;
                for (int i = 2; i <= 51; i++)
                {
                    if (worksheet.Cells[i, 1].Value == null || string.IsNullOrEmpty(worksheet.Cells[i, 1].Value.ToString()))
                    { 
                        break;
                    }
                    rows++;
                }

                if (rows == 0)
                {
                    throw new Exception("Не найдены данные матрицы A");
                }


                matrixSize = rows;
                MatrixSizeTextBox.Text = rows.ToString();

                var matrixData = new List<List<double>>();
                for (int i = 0; i < rows; i++)
                {
                    var row = new List<double>();
                    for (int j = 0; j < rows; j++)
                    {
                        double value = 0;
                        if (worksheet.Cells[i + 2, j + 1].Value != null)
                        { 
                            double.TryParse(worksheet.Cells[i + 2, j + 1].Value.ToString(), out value);
                        }
                        row.Add(value);
                    }
                    matrixData.Add(row);
                }

                var vectorData = new List<List<double>>();
                for (int i = 0; i < rows; i++)
                {
                    double value = 0;
                    if (worksheet.Cells[i + 2, rows + 1].Value != null)
                    { 
                        double.TryParse(worksheet.Cells[i + 2, rows + 1].Value.ToString(), out value);
                    }
                    vectorData.Add(new List<double> { value });
                }

                InitializeDataGrids();
                MatrixADataGrid.ItemsSource = matrixData;
                VectorBDataGrid.ItemsSource = vectorData;

                if (worksheet.Cells[1, rows + 2]?.Value?.ToString() == "Вектор X")
                {
                    var resultData = new List<List<double>>();
                    for (int i = 0; i < rows; i++)
                    {
                        double value = 0;
                        if (worksheet.Cells[i + 2, rows + 2].Value != null)
                            double.TryParse(worksheet.Cells[i + 2, rows + 2].Value.ToString(), out value);
                        resultData.Add(new List<double> { value });
                    }
                    VectorXDataGrid.ItemsSource = resultData;
                }
            }
        }

        private async void LoadFromGoogle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadFromGoogleSheetsSimplified();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки из Google Tables: {ex.Message}\n\nДля работы с Google Tables необходимо:\n1. Создать проект в Google Cloud Console\n2. Включить Google Sheets API\n3. Создать API ключ", "Ошибка");
            }
        }

        private async Task LoadFromGoogleSheetsSimplified()
        {
            var inputDialog = new Window
            {
                Title = "Импорт из Google Tables",
                Width = 500,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var linkLabel = new TextBlock { Text = "Ссылка на Google таблицу:" };
            var linkTextBox = new TextBox { Height = 25, Margin = new Thickness(0, 5, 0, 15) };

            var apiKeyLabel = new TextBlock { Text = "API ключ:" };
            var apiKeyTextBox = new TextBox { Height = 25, Margin = new Thickness(0, 5, 0, 15) };

            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "Загрузить", Width = 100, Margin = new Thickness(10) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(10) };

            okButton.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(linkTextBox.Text))
                {
                    MessageBox.Show("Введите ссылку на Google таблицу", "Ошибка");
                    return;
                }

                if (string.IsNullOrEmpty(apiKeyTextBox.Text))
                {
                    MessageBox.Show("Введите API ключ", "Ошибка");
                    return;
                }

                inputDialog.DialogResult = true;
                inputDialog.Close();

                await LoadDataFromGoogleSheetsByLink(linkTextBox.Text, apiKeyTextBox.Text);
            };

            cancelButton.Click += (s, e) =>
            {
                inputDialog.DialogResult = false;
                inputDialog.Close();
            };

            buttonStack.Children.Add(okButton);
            buttonStack.Children.Add(cancelButton);

            stackPanel.Children.Add(linkLabel);
            stackPanel.Children.Add(linkTextBox);
            stackPanel.Children.Add(apiKeyLabel);
            stackPanel.Children.Add(apiKeyTextBox);
            stackPanel.Children.Add(buttonStack);

            inputDialog.Content = stackPanel;

            if (inputDialog.ShowDialog() == true)
            {
                UpdateStatus("Данные загружены из Google Tables");
            }
        }

        private async Task LoadDataFromGoogleSheetsByLink(string sheetLink, string apiKey)
        {
            try
            {
                string spreadsheetId = ExtractSheetIdFromLink(sheetLink);

                if (string.IsNullOrEmpty(spreadsheetId))
                { 
                    throw new Exception("Неверная ссылка на Google таблицу");
                }

                string range = "A:AZ";

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    ApiKey = apiKey,
                    ApplicationName = "SLAU Solver"
                });

                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();

                var values = response.Values;

                if (values == null || values.Count == 0)
                { 
                    throw new Exception("В таблице нет данных");
                }

                ProcessGoogleSheetsData(values);
            }
            catch (Google.GoogleApiException ex) when (ex.Error.Code == 403)
            {
                throw new Exception("Неверный API ключ или нет доступа к таблице");
            }
            catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
            {
                throw new Exception("Таблица не найдена. Проверьте ссылку и доступность таблицы");
            }
            catch (Google.GoogleApiException ex)
            {
                throw new Exception($"Ошибка Google API: {ex.Error.Message}");
            }
        }

        private string ExtractSheetIdFromLink(string sheetLink)
        {
            if (string.IsNullOrEmpty(sheetLink))
                return null;

            var patterns = new[]
            {
        @"\/spreadsheets\/d\/([a-zA-Z0-9-_]+)",
        @"\/d\/([a-zA-Z0-9-_]+)",
        @"key=([a-zA-Z0-9-_]+)",
        @"id=([a-zA-Z0-9-_]+)"
    };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(sheetLink, pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        private void ProcessGoogleSheetsData(IList<IList<object>> values)
        {
            int rows = 0;
            for (int i = 1; i < values.Count && i <= 51; i++)
            {
                if (values[i].Count == 0 || string.IsNullOrEmpty(values[i][0]?.ToString()))
                { 
                    break;
                }
                rows++;
            }

            if (rows == 0)
            {
                throw new Exception("Не найдены данные матрицы A");
            }

            matrixSize = rows;
            MatrixSizeTextBox.Text = rows.ToString();

            var matrixData = new List<List<double>>();
            for (int i = 0; i < rows; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < rows; j++)
                {
                    double value = 0;
                    if (values[i + 1].Count > j && values[i + 1][j] != null)
                    { 
                        double.TryParse(values[i + 1][j].ToString(), out value);
                    }
                    row.Add(value);
                }
                matrixData.Add(row);
            }

            var vectorData = new List<List<double>>();
            for (int i = 0; i < rows; i++)
            {
                double value = 0;
                if (values[i + 1].Count > rows && values[i + 1][rows] != null)
                { 
                    double.TryParse(values[i + 1][rows].ToString(), out value);
                }
                vectorData.Add(new List<double> { value });
            }

            InitializeDataGrids();
            MatrixADataGrid.ItemsSource = matrixData;
            VectorBDataGrid.ItemsSource = vectorData;

            if (values[0].Count > rows + 1 && values[0][rows + 1]?.ToString() == "Вектор X")
            {
                var resultData = new List<List<double>>();
                for (int i = 0; i < rows; i++)
                {
                    double value = 0;
                    if (values[i + 1].Count > rows + 1 && values[i + 1][rows + 1] != null)
                    { 
                        double.TryParse(values[i + 1][rows + 1].ToString(), out value);
                    }
                    resultData.Add(new List<double> { value });
                }
                VectorXDataGrid.ItemsSource = resultData;
            }
        }

        private void ExportResultsExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                saveFileDialog.Title = "Сохранить результаты в Excel";
                saveFileDialog.FileName = "SLAU_Results.xlsx";

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToExcel(saveFileDialog.FileName);
                    UpdateStatus($"Результаты сохранены в {System.IO.Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Excel: {ex.Message}", "Ошибка");
            }
        }

        private void ExportToExcel(string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("СЛАУ Данные");

                worksheet.Cells[1, 1].Value = "Матрица A";
                worksheet.Cells[1, matrixSize + 1].Value = "Вектор B";

                var matrixA = GetMatrixA();
                for (int i = 0; i < matrixSize; i++)
                {
                    for (int j = 0; j < matrixSize; j++)
                    {
                        worksheet.Cells[i + 2, j + 1].Value = matrixA[i, j];
                    }
                }

                var vectorB = GetVectorB();
                for (int i = 0; i < matrixSize; i++)
                {
                    worksheet.Cells[i + 2, matrixSize + 1].Value = vectorB[i];
                }

                if (VectorXDataGrid.ItemsSource != null)
                {
                    worksheet.Cells[1, matrixSize + 2].Value = "Вектор X";
                    var items = VectorXDataGrid.ItemsSource as List<List<double>>;
                    for (int i = 0; i < matrixSize && i < items.Count; i++)
                    {
                        worksheet.Cells[i + 2, matrixSize + 2].Value = items[i][0];
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private async void ExportResultsGoogle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ExportToGoogleTables();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Google Tables: {ex.Message}", "Ошибка");
            }
        }

        private async Task ExportToGoogleTables()
        {
            var inputDialog = new Window
            {
                Title = "Экспорт в Google Tables",
                Width = 500,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var linkLabel = new TextBlock { Text = "Ссылка на Google таблицу:" };
            var linkTextBox = new TextBox { Height = 25, Margin = new Thickness(0, 5, 0, 15) };

            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var exportButton = new Button { Content = "Экспортировать", Width = 120, Margin = new Thickness(10) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(10) };

            exportButton.Click += async (s, e) =>
            {
                if (string.IsNullOrEmpty(linkTextBox.Text))
                {
                    MessageBox.Show("Введите ссылку на Google таблицу", "Ошибка");
                    return;
                }

                if (!File.Exists("service-account-credentials.json"))
                {
                    MessageBox.Show("Файл credentials.json не найден!\n\nПоместите OAuth credentials файл в папку с приложением.", "Ошибка");
                    return;
                }

                inputDialog.DialogResult = true;
                inputDialog.Close();

                await ExportDataToGoogleSheets(linkTextBox.Text, "");
            };

            cancelButton.Click += (s, e) =>
            {
                inputDialog.DialogResult = false;
                inputDialog.Close();
            };

            buttonStack.Children.Add(exportButton);
            buttonStack.Children.Add(cancelButton);

            stackPanel.Children.Add(linkLabel);
            stackPanel.Children.Add(linkTextBox);
            stackPanel.Children.Add(buttonStack);

            inputDialog.Content = stackPanel;

            if (inputDialog.ShowDialog() == true)
            {
                UpdateStatus("Данные экспортированы в Google Tables");
            }
        }

        private async Task ExportDataToGoogleSheets(string sheetLink, string apiKey)
        {
            try
            {
                string spreadsheetId = ExtractSheetIdFromLink(sheetLink);

                if (string.IsNullOrEmpty(spreadsheetId))
                { 
                    throw new Exception("Неверная ссылка на Google таблицу");
                }

                GoogleCredential credential;

                using (var stream = new FileStream("service-account-credentials.json", FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }

                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SLAU Solver"
                });

                var matrixA = GetMatrixA();
                var vectorB = GetVectorB();
                var vectorX = GetVectorX();

                if (matrixA == null || vectorB == null)
                { 
                    throw new Exception("Нет данных для экспорта");
                }


                var values = new List<IList<object>>();

                var headerRow = new List<object>();
                headerRow.Add("Матрица A");

                for (int i = 1; i < matrixSize; i++)
                {
                    headerRow.Add("");
                }
                headerRow.Add("Вектор B");

                if (vectorX != null)
                {
                    headerRow.Add("Вектор X");
                }

                values.Add(headerRow);

                for (int i = 0; i < matrixSize; i++)
                {
                    var dataRow = new List<object>();

                    for (int j = 0; j < matrixSize; j++)
                    {
                        dataRow.Add(matrixA[i, j]);
                    }

                    dataRow.Add(vectorB[i]);

                    if (vectorX != null && i < vectorX.Length)
                    {
                        dataRow.Add(Math.Round(vectorX[i], 6));
                    }

                    values.Add(dataRow);
                }

                int colCount = matrixSize + 1 + (vectorX != null ? 1 : 0);
                string range = $"A1:{GetColumnName(colCount - 1)}{values.Count}";

                var valueRange = new ValueRange();
                valueRange.Values = values;

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

                var response = await updateRequest.ExecuteAsync();

                MessageBox.Show($"Данные успешно экспортированы!\nОбновлено ячеек: {response.UpdatedCells}", "Экспорт завершен");
            }
            catch (Google.GoogleApiException ex)
            {
                throw new Exception($"Ошибка Google API: {ex.Error.Message}");
            }
        }

        private double[] GetVectorX()
        {
            try
            {
                var items = VectorXDataGrid.ItemsSource as List<List<double>>;
                if (items == null || items.Count == 0)
                { 
                    return null;
                }

                double[] vector = new double[matrixSize];
                for (int i = 0; i < matrixSize && i < items.Count; i++)
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

        private string GetColumnName(int columnIndex)
        {
            string columnName = "";

            while (columnIndex >= 0)
            {
                columnName = (char)('A' + (columnIndex % 26)) + columnName;
                columnIndex = (columnIndex / 26) - 1;
            }

            return columnName;
        }

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