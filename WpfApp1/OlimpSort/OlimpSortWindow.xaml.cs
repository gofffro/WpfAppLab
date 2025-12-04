using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp1.OlimpSort
{
    public partial class OlimpSortWindow : Window
    {
        private int arraySize = 10;
        private Random random = new Random();
        private DispatcherTimer animationTimer;
        private List<SortAlgorithm> activeAlgorithms = new List<SortAlgorithm>();
        private Dictionary<string, TimeSpan> algorithmTimes = new Dictionary<string, TimeSpan>();
        private Dictionary<string, int> algorithmIterations = new Dictionary<string, int>();
        private List<double> originalArray = new List<double>();
        private List<VisualizationElement> visualizationElements = new List<VisualizationElement>();
        private bool isVisualizationRunning = false;

        private int maxIterations = 10000;
        private Dictionary<string, int> algorithmMaxIterations = new Dictionary<string, int>();

        public OlimpSortWindow()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Sorting Visualizer App");
            InitializeComponent();
            InitializeDataGrid();
            InitializeVisualization();
            UpdateStatus("Готов к работе");
        }

        private void InitializeDataGrid()
        {
            InputDataGrid.Columns.Clear();
            InputDataGrid.ItemsSource = null;

            // Создаем список для хранения данных
            var data = new List<InputDataItem>();
            for (int i = 0; i < arraySize; i++)
            {
                data.Add(new InputDataItem { Value = "0.0" });
            }
            InputDataGrid.ItemsSource = data;
        }

        private void InputDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var textBox = e.EditingElement as TextBox;
                if (textBox != null)
                {
                    string input = textBox.Text;

                    // Заменяем запятую на точку для универсального парсинга
                    string normalizedInput = input.Replace(',', '.');

                    // Проверяем, является ли ввод валидным числом
                    if (!double.TryParse(normalizedInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                    {
                        // Если не валидно, отменяем изменение и показываем сообщение
                        e.Cancel = true;

                        // Восстанавливаем старое значение
                        var dataItem = e.Row.Item as InputDataItem;
                        if (dataItem != null)
                        {
                            textBox.Text = dataItem.Value;
                        }

                        MessageBox.Show($"Неверный формат числа: {input}\nИспользуйте формат 1.1 или 1,1", "Ошибка ввода");
                    }
                    else
                    {
                        // Обновляем значение с нормализованным форматом
                        var dataItem = e.Row.Item as InputDataItem;
                        if (dataItem != null)
                        {
                            dataItem.Value = normalizedInput;
                        }
                    }
                }
            }
        }

        private void InitializeVisualization()
        {
            visualizationElements.Clear();
            VisualizationContainer.ItemsSource = null;
        }

        private void UpdateVisualization(List<double> data, List<ColorInfo> colorInfo = null)
        {
            visualizationElements.Clear();

            if (data == null || data.Count == 0) return;

            double maxValue = data.Max();
            double minValue = data.Min();
            double range = Math.Max(1, maxValue - minValue);

            for (int i = 0; i < data.Count; i++)
            {
                double normalizedHeight = (data[i] - minValue) / range * 100 + 20;
                var element = new VisualizationElement
                {
                    Value = data[i],
                    Height = normalizedHeight,
                    Color = Brushes.LightBlue
                };

                if (colorInfo != null && i < colorInfo.Count)
                {
                    element.Color = colorInfo[i].Color;
                }

                visualizationElements.Add(element);
            }

            VisualizationContainer.ItemsSource = visualizationElements.ToList();
        }

        private List<double> GetInputData()
        {
            try
            {
                var items = InputDataGrid.ItemsSource as IEnumerable<InputDataItem>;
                if (items == null)
                {
                    MessageBox.Show("Данные не загружены", "Ошибка");
                    return null;
                }

                var data = new List<double>();
                int index = 0;

                foreach (var item in items)
                {
                    index++;

                    if (string.IsNullOrWhiteSpace(item.Value))
                    {
                        MessageBox.Show($"Не заполнена строка {index}", "Ошибка");
                        return null;
                    }

                    // Нормализуем и парсим число
                    string normalizedValue = item.Value.Replace(',', '.');
                    if (double.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                    {
                        data.Add(value);
                    }
                    else
                    {
                        MessageBox.Show($"Неверный формат числа в строке {index}: {item.Value}\nИспользуйте формат 1.1 или 1,1", "Ошибка");
                        return null;
                    }
                }

                if (data.Count != arraySize)
                {
                    MessageBox.Show($"Неверное количество элементов. Ожидается: {arraySize}, получено: {data.Count}", "Ошибка");
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка чтения данных: {ex.Message}", "Ошибка");
                return null;
            }
        }


        private void SetInputData(List<double> data)
        {
            var dataList = new List<InputDataItem>();
            foreach (var value in data)
            {
                dataList.Add(new InputDataItem { Value = value.ToString(CultureInfo.InvariantCulture) });
            }
            InputDataGrid.ItemsSource = dataList;
        }

        private void SetResultData(List<double> data)
        {
            var dataList = new List<List<double>>();
            foreach (var value in data)
            {
                dataList.Add(new List<double> { value });
            }
            ResultDataGrid.ItemsSource = dataList;
        }

        private void StartSorting_Click(object sender, RoutedEventArgs e)
        {
            var inputData = GetInputData();
            if (inputData == null || inputData.Count == 0)
            {
                MessageBox.Show("Нет данных для сортировки", "Ошибка");
                return;
            }

            if (inputData.Count != arraySize)
            {
                MessageBox.Show($"Размер массива должен быть {arraySize}. Текущий размер: {inputData.Count}", "Ошибка");
                return;
            }

            originalArray = inputData.ToList();
            algorithmTimes.Clear();
            algorithmIterations.Clear();
            activeAlgorithms.Clear();

            bool isAscending = AscendingRadio.IsChecked == true;

            if (BubbleSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Пузырьковая",
                    SortFunction = SortingAlgorithms.BubbleSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (InsertionSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Вставками",
                    SortFunction = SortingAlgorithms.InsertionSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (ShakerSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Шейкерная",
                    SortFunction = SortingAlgorithms.ShakerSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (QuickSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Быстрая",
                    SortFunction = SortingAlgorithms.QuickSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (BogoSortCheckBox.IsChecked == true)
            {
                if (inputData.Count > 20)
                {
                    MessageBox.Show("BOGO сортировка работает только для массивов размером до 20 элементов", "Предупреждение");
                }
                else
                {
                    activeAlgorithms.Add(new SortAlgorithm
                    {
                        Name = "BOGO",
                        SortFunction = SortingAlgorithms.BogoSort,
                        CurrentData = inputData.ToList(),
                        IsAscending = isAscending
                    });
                }
            }

            if (activeAlgorithms.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один алгоритм сортировки", "Ошибка");
                return;
            }

            StartVisualization();
        }

        private void StartVisualization()
        {
            var timingResults = new System.Text.StringBuilder();
            timingResults.AppendLine("Время выполнения алгоритмов:\n");

            foreach (var algorithm in activeAlgorithms)
            {
                try
                {
                    int algoMaxIterations = 0;
                    string algoKey = algorithm.Name switch
                    {
                        "Пузырьковая" => "BubbleSort",
                        "Вставками" => "InsertionSort",
                        "Шейкерная" => "ShakerSort",
                        "Быстрая" => "QuickSort",
                        "BOGO" => "BogoSort",
                        _ => algorithm.Name
                    };

                    if (algorithmMaxIterations.ContainsKey(algoKey) && algorithmMaxIterations[algoKey] > 0)
                    {
                        algoMaxIterations = algorithmMaxIterations[algoKey];
                    }
                    else
                    {
                        algoMaxIterations = maxIterations;
                    }

                    var result = algorithm.SortFunction(algorithm.CurrentData, algorithm.IsAscending, algoMaxIterations);

                    algorithmTimes[algorithm.Name] = result.Time;
                    algorithmIterations[algorithm.Name] = result.Iterations;

                    timingResults.AppendLine($"{algorithm.Name}:");
                    timingResults.AppendLine($"  Время: {result.Time.TotalMilliseconds:F4} мс");
                    timingResults.AppendLine($"  Итераций: {result.Iterations}");

                    if (!result.IsCompleted)
                    {
                        timingResults.AppendLine($"  ПРЕРВАНО - достигнут лимит итераций ({algoMaxIterations})");
                    }
                    timingResults.AppendLine();

                    if (algorithm == activeAlgorithms.First())
                    {
                        SetResultData(result.SortedArray);
                        UpdateVisualization(result.SortedArray, result.ColorInfo);
                    }
                }
                catch (Exception ex)
                {
                    timingResults.AppendLine($"{algorithm.Name}: ОШИБКА - {ex.Message}");
                    timingResults.AppendLine();
                }
            }

            TimingResultsTextBox.Text = timingResults.ToString();

            if (algorithmTimes.Any())
            {
                var fastest = algorithmTimes.OrderBy(kvp => kvp.Value.TotalMilliseconds).First();
                var fastestIterations = algorithmIterations[fastest.Key];
                timingResults.AppendLine($"Самый быстрый алгоритм: {fastest.Key}");
                timingResults.AppendLine($"Время: {fastest.Value.TotalMilliseconds:F4} мс");
                timingResults.AppendLine($"Итераций: {fastestIterations}");
                TimingResultsTextBox.Text = timingResults.ToString();
            }

            UpdateStatus("Сортировка завершена");
        }

        private void ConfigureIterations_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Настройки итераций",
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20, 20, 20, 20) };

            var generalLabel = new TextBlock
            {
                Text = "Общее максимальное количество итераций:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            var generalTextBox = new TextBox
            {
                Text = maxIterations.ToString(),
                Height = 25,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var algorithmsLabel = new TextBlock
            {
                Text = "Настройки для алгоритмов (0 = без лимита):",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 10)
            };

            var algorithmsStack = new StackPanel();

            var algorithms = new Dictionary<string, string>
            {
                { "Пузырьковая", "BubbleSort" },
                { "Вставками", "InsertionSort" },
                { "Шейкерная", "ShakerSort" },
                { "Быстрая", "QuickSort" },
                { "BOGO", "BogoSort" }
            };

            foreach (var algo in algorithms)
            {
                var algoStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                var label = new TextBlock
                {
                    Text = algo.Key + ":",
                    Width = 100,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var textBox = new TextBox
                {
                    Text = algorithmMaxIterations.ContainsKey(algo.Value) ? algorithmMaxIterations[algo.Value].ToString() : "0",
                    Width = 80,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = algo.Value
                };

                algoStack.Children.Add(label);
                algoStack.Children.Add(textBox);
                algorithmsStack.Children.Add(algoStack);
            }

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 15, 0, 0)
            };
            var okButton = new Button
            {
                Content = "Применить",
                Width = 100,
                Margin = new Thickness(10, 0, 10, 0)
            };
            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Margin = new Thickness(10, 0, 10, 0)
            };

            okButton.Click += (s, e2) =>
            {
                if (int.TryParse(generalTextBox.Text, out int generalMax) && generalMax >= 0)
                {
                    maxIterations = generalMax;
                }

                foreach (var child in algorithmsStack.Children)
                {
                    if (child is StackPanel algoStack && algoStack.Children.Count == 2)
                    {
                        var textBox = algoStack.Children[1] as TextBox;
                        if (textBox != null && int.TryParse(textBox.Text, out int algoMax) && algoMax >= 0)
                        {
                            algorithmMaxIterations[textBox.Tag.ToString()] = algoMax;
                        }
                    }
                }

                dialog.DialogResult = true;
                dialog.Close();
                UpdateStatus("Настройки итераций применены");
            };

            cancelButton.Click += (s, e2) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            buttonStack.Children.Add(okButton);
            buttonStack.Children.Add(cancelButton);

            stackPanel.Children.Add(generalLabel);
            stackPanel.Children.Add(generalTextBox);
            stackPanel.Children.Add(algorithmsLabel);
            stackPanel.Children.Add(algorithmsStack);
            stackPanel.Children.Add(buttonStack);

            dialog.Content = stackPanel;
            dialog.ShowDialog();
        }

        private void ResetColors_Click(object sender, RoutedEventArgs e)
        {
            var data = GetInputData();
            if (data != null)
            {
                UpdateVisualization(data);
            }
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

                var data = new List<double>();
                for (int i = 0; i < arraySize; i++)
                {
                    double value = min + (random.NextDouble() * (max - min));
                    data.Add(Math.Round(value, 2));
                }

                SetInputData(data);
                UpdateVisualization(data);
                UpdateStatus("Данные сгенерированы");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка");
            }
        }

        private void ApplySize_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ArraySizeTextBox.Text, out int size) && size >= 2 && size <= 100)
            {
                arraySize = size;
                InitializeDataGrid();
                UpdateStatus($"Размер массива изменен на {size}");
            }
            else
            {
                MessageBox.Show("Размер массива должен быть от 2 до 100", "Ошибка");
                ArraySizeTextBox.Text = arraySize.ToString();
            }
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            InitializeDataGrid();
            InitializeVisualization();
            ResultDataGrid.ItemsSource = null;
            TimingResultsTextBox.Text = "";
            UpdateStatus("Данные очищены");
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            BubbleSortCheckBox.IsChecked = true;
            InsertionSortCheckBox.IsChecked = true;
            ShakerSortCheckBox.IsChecked = true;
            QuickSortCheckBox.IsChecked = true;
            BogoSortCheckBox.IsChecked = true;
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            BubbleSortCheckBox.IsChecked = false;
            InsertionSortCheckBox.IsChecked = false;
            ShakerSortCheckBox.IsChecked = false;
            QuickSortCheckBox.IsChecked = false;
            BogoSortCheckBox.IsChecked = false;
        }

        private void LoadFromExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*";
                openFileDialog.Title = "Выберите файл Excel";

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadDataFromExcel(openFileDialog.FileName);
                    UpdateStatus($"Данные загружены из {Path.GetFileName(openFileDialog.FileName)}");
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
                var dataList = new List<InputDataItem>();

                for (int i = 1; i <= 100; i++)
                {
                    if (worksheet.Cells[i, 1].Value == null) break;

                    double value;
                    if (double.TryParse(worksheet.Cells[i, 1].Value.ToString(), out value))
                    {
                        dataList.Add(new InputDataItem { Value = value.ToString(CultureInfo.InvariantCulture) });
                    }
                    else
                    {
                        // Пробуем заменить запятую на точку
                        string normalized = worksheet.Cells[i, 1].Value.ToString().Replace(',', '.');
                        if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                        {
                            dataList.Add(new InputDataItem { Value = normalized });
                        }
                    }
                }

                if (dataList.Count > 0)
                {
                    arraySize = dataList.Count;
                    ArraySizeTextBox.Text = arraySize.ToString();
                    InputDataGrid.ItemsSource = dataList;

                    // Обновляем визуализацию
                    var numericData = dataList.Select(item =>
                        double.Parse(item.Value.Replace(',', '.'), CultureInfo.InvariantCulture)).ToList();
                    UpdateVisualization(numericData);
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

            var stackPanel = new StackPanel { Margin = new Thickness(20, 20, 20, 20) };

            var linkLabel = new TextBlock { Text = "Ссылка на Google таблицу:" };
            var linkTextBox = new TextBox { Height = 25, Margin = new Thickness(0, 5, 0, 15) };

            var apiKeyLabel = new TextBlock { Text = "API ключ:" };
            var apiKeyTextBox = new TextBox { Height = 25, Margin = new Thickness(0, 5, 0, 15) };

            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "Загрузить", Width = 100, Margin = new Thickness(10, 0, 10, 0) };
            var cancelButton = new Button { Content = "Отмена", Width = 100, Margin = new Thickness(10, 0, 10, 0) };

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
                    ApplicationName = "Sorting Visualizer"
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
            var data = new List<double>();

            for (int i = 0; i < values.Count && i < 100; i++)
            {
                if (values[i].Count > 0 && values[i][0] != null)
                {
                    if (double.TryParse(values[i][0].ToString(), out double value))
                    {
                        data.Add(value);
                    }
                }
            }

            if (data.Count > 0)
            {
                arraySize = data.Count;
                ArraySizeTextBox.Text = arraySize.ToString();
                SetInputData(data);
                UpdateVisualization(data);
            }
            else
            {
                throw new Exception("Не найдены числовые данные в таблице");
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ArraySizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(ArraySizeTextBox.Text, out int size) && size >= 2 && size <= 100)
            {
                arraySize = size;
            }
        }
    }
}