using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using OfficeOpenXml;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;

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
        private List<int> originalArray = new List<int>();
        private List<VisualizationElement> visualizationElements = new List<VisualizationElement>();
        private bool isVisualizationRunning = false;

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

            InputDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Значение",
                Binding = new System.Windows.Data.Binding("[0]")
            });

            var data = new List<List<int>>();
            for (int i = 0; i < arraySize; i++)
            {
                data.Add(new List<int> { 0 });
            }
            InputDataGrid.ItemsSource = data;

            // Initialize result data grid
            ResultDataGrid.Columns.Clear();
            ResultDataGrid.ItemsSource = null;
            ResultDataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Значение",
                Binding = new System.Windows.Data.Binding("[0]")
            });
        }

        private void InitializeVisualization()
        {
            visualizationElements.Clear();
            VisualizationContainer.ItemsSource = null;
        }

        private void UpdateVisualization(List<int> data, List<ColorInfo> colorInfo = null)
        {
            visualizationElements.Clear();

            if (data == null || data.Count == 0) return;

            int maxValue = data.Max();
            int minValue = data.Min();
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

                // Apply color if provided
                if (colorInfo != null && i < colorInfo.Count)
                {
                    element.Color = colorInfo[i].Color;
                }

                visualizationElements.Add(element);
            }

            VisualizationContainer.ItemsSource = visualizationElements.ToList();
        }

        // Data management methods - ОДИН метод GetInputData
        private List<int> GetInputData()
        {
            try
            {
                var items = InputDataGrid.ItemsSource as List<List<int>>;
                if (items == null)
                {
                    MessageBox.Show("Данные не загружены", "Ошибка");
                    return null;
                }

                var data = new List<int>();
                for (int i = 0; i < arraySize && i < items.Count; i++)
                {
                    if (items[i] == null || items[i].Count == 0)
                    {
                        MessageBox.Show($"Не заполнена строка {i + 1}", "Ошибка");
                        return null;
                    }
                    data.Add(items[i][0]);
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

        private void SetInputData(List<int> data)
        {
            var dataList = new List<List<int>>();
            foreach (var value in data)
            {
                dataList.Add(new List<int> { value });
            }
            InputDataGrid.ItemsSource = dataList;
        }

        private void SetResultData(List<int> data)
        {
            var dataList = new List<List<int>>();
            foreach (var value in data)
            {
                dataList.Add(new List<int> { value });
            }
            ResultDataGrid.ItemsSource = dataList;
        }

        // Sorting algorithms implementation
        public class SortAlgorithm
        {
            public string Name { get; set; }
            public Func<List<int>, bool, (List<int>, List<ColorInfo>, int)> SortFunction { get; set; }
            public List<int> CurrentData { get; set; }
            public bool IsAscending { get; set; }
            public Stopwatch Timer { get; set; } = new Stopwatch();
        }

        public class ColorInfo
        {
            public Brush Color { get; set; }
            public int Index { get; set; }
        }

        public class VisualizationElement
        {
            public int Value { get; set; }
            public double Height { get; set; }
            public Brush Color { get; set; }
        }

        // Исправленные алгоритмы сортировки с счетчиком итераций
        private (List<int>, List<ColorInfo>, int) BubbleSort(List<int> data, bool ascending)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            bool swapped;
            int iterations = 0;

            for (int i = 0; i < arr.Count - 1; i++)
            {
                swapped = false;
                for (int j = 0; j < arr.Count - i - 1; j++)
                {
                    iterations++;

                    // Highlight compared elements
                    colors[j].Color = Brushes.Red;
                    colors[j + 1].Color = Brushes.Red;

                    bool shouldSwap = ascending ? arr[j] > arr[j + 1] : arr[j] < arr[j + 1];

                    if (shouldSwap)
                    {
                        (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                        swapped = true;

                        // Highlight swapped elements
                        colors[j].Color = Brushes.Green;
                        colors[j + 1].Color = Brushes.Green;
                    }
                    else
                    {
                        colors[j].Color = Brushes.Orange;
                        colors[j + 1].Color = Brushes.Orange;
                    }

                    // Reset other colors
                    for (int k = 0; k < colors.Count; k++)
                    {
                        if (k != j && k != j + 1)
                        {
                            colors[k].Color = k < arr.Count - i ? Brushes.LightBlue : Brushes.LightGreen;
                        }
                    }
                }

                if (!swapped) break;
            }

            // Final state - all sorted
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i].Color = Brushes.LightGreen;
            }
            return (arr, colors, iterations);
        }

        private (List<int>, List<ColorInfo>, int) InsertionSort(List<int> data, bool ascending)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            int iterations = 0;

            for (int i = 1; i < arr.Count; i++)
            {
                int key = arr[i];
                int j = i - 1;

                colors[i].Color = Brushes.Red;

                while (j >= 0 && (ascending ? arr[j] > key : arr[j] < key))
                {
                    iterations++;
                    colors[j].Color = Brushes.Orange;
                    arr[j + 1] = arr[j];
                    j--;

                    // Update colors during shift
                    for (int k = 0; k <= i; k++)
                    {
                        if (k == j + 1) colors[k].Color = Brushes.Green;
                        else if (k <= i) colors[k].Color = Brushes.LightBlue;
                    }
                }
                iterations++; // Count the last comparison that breaks the loop
                arr[j + 1] = key;

                // Update colors for sorted portion
                for (int k = 0; k <= i; k++)
                {
                    colors[k].Color = Brushes.LightGreen;
                }
            }

            // Final state
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i].Color = Brushes.LightGreen;
            }
            return (arr, colors, iterations);
        }

        private (List<int>, List<ColorInfo>, int) ShakerSort(List<int> data, bool ascending)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            bool swapped = true;
            int start = 0;
            int end = arr.Count - 1;
            int iterations = 0;

            while (swapped)
            {
                swapped = false;

                // Forward pass
                for (int i = start; i < end; i++)
                {
                    iterations++;
                    colors[i].Color = Brushes.Red;
                    colors[i + 1].Color = Brushes.Red;

                    bool shouldSwap = ascending ? arr[i] > arr[i + 1] : arr[i] < arr[i + 1];

                    if (shouldSwap)
                    {
                        (arr[i], arr[i + 1]) = (arr[i + 1], arr[i]);
                        swapped = true;

                        colors[i].Color = Brushes.Green;
                        colors[i + 1].Color = Brushes.Green;
                    }

                    // Reset colors for non-active elements
                    for (int k = 0; k < colors.Count; k++)
                    {
                        if (k != i && k != i + 1)
                        {
                            if (k < start || k > end)
                                colors[k].Color = Brushes.LightGreen;
                            else
                                colors[k].Color = Brushes.LightBlue;
                        }
                    }
                }

                if (!swapped) break;

                end--;

                // Backward pass
                for (int i = end - 1; i >= start; i--)
                {
                    iterations++;
                    colors[i].Color = Brushes.Red;
                    colors[i + 1].Color = Brushes.Red;

                    bool shouldSwap = ascending ? arr[i] > arr[i + 1] : arr[i] < arr[i + 1];

                    if (shouldSwap)
                    {
                        (arr[i], arr[i + 1]) = (arr[i + 1], arr[i]);
                        swapped = true;

                        colors[i].Color = Brushes.Green;
                        colors[i + 1].Color = Brushes.Green;
                    }

                    // Reset colors for non-active elements
                    for (int k = 0; k < colors.Count; k++)
                    {
                        if (k != i && k != i + 1)
                        {
                            if (k < start || k > end)
                                colors[k].Color = Brushes.LightGreen;
                            else
                                colors[k].Color = Brushes.LightBlue;
                        }
                    }
                }

                start++;
            }

            // Final state
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i].Color = Brushes.LightGreen;
            }
            return (arr, colors, iterations);
        }

        private (List<int>, List<ColorInfo>, int) QuickSort(List<int> data, bool ascending)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            int iterations = 0;
            QuickSortRecursive(arr, 0, arr.Count - 1, ascending, colors, ref iterations);

            // Final state
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i].Color = Brushes.LightGreen;
            }
            return (arr, colors, iterations);
        }

        private void QuickSortRecursive(List<int> arr, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations)
        {
            if (low < high)
            {
                int pi = Partition(arr, low, high, ascending, colors, ref iterations);
                QuickSortRecursive(arr, low, pi - 1, ascending, colors, ref iterations);
                QuickSortRecursive(arr, pi + 1, high, ascending, colors, ref iterations);
            }
        }

        private int Partition(List<int> arr, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations)
        {
            int pivot = arr[high];

            // Color pivot
            colors[high].Color = Brushes.Purple;

            int i = low - 1;

            for (int j = low; j < high; j++)
            {
                iterations++;
                colors[j].Color = Brushes.Red;

                bool shouldSwap = ascending ? arr[j] <= pivot : arr[j] >= pivot;

                if (shouldSwap)
                {
                    i++;
                    (arr[i], arr[j]) = (arr[j], arr[i]);

                    // Color swapped elements
                    colors[i].Color = Brushes.Green;
                    if (i != j) colors[j].Color = Brushes.Orange;
                }

                // Reset colors for non-active elements
                for (int k = low; k <= high; k++)
                {
                    if (k != j && k != i && k != high && colors[k].Color != Brushes.Green)
                    {
                        colors[k].Color = Brushes.LightBlue;
                    }
                }
            }

            (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);

            // Color the final pivot position
            colors[i + 1].Color = Brushes.LightGreen;
            colors[high].Color = Brushes.LightBlue;

            return i + 1;
        }

        private (List<int>, List<ColorInfo>, int) BogoSort(List<int> data, bool ascending)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            int attempts = 0;
            int maxAttempts = 10000; // Safety limit
            int iterations = 0;

            while (!IsSorted(arr, ascending) && attempts < maxAttempts)
            {
                iterations++;
                Shuffle(arr);
                attempts++;

                // Color based on sorted status
                for (int i = 0; i < colors.Count; i++)
                {
                    colors[i].Color = Brushes.Orange; // Shuffling color
                }
            }

            // Final coloring
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i].Color = IsSorted(arr, ascending) ? Brushes.LightGreen : Brushes.Red;
            }

            return (arr, colors, iterations);
        }

        private bool IsSorted(List<int> arr, bool ascending)
        {
            for (int i = 0; i < arr.Count - 1; i++)
            {
                if (ascending && arr[i] > arr[i + 1]) return false;
                if (!ascending && arr[i] < arr[i + 1]) return false;
            }
            return true;
        }

        private void Shuffle(List<int> arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                int j = random.Next(i, arr.Count);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        // Исправленный метод запуска сортировки
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

            // Create algorithms based on selection
            if (BubbleSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Пузырьковая",
                    SortFunction = BubbleSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (InsertionSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Вставками",
                    SortFunction = InsertionSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (ShakerSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Шейкерная",
                    SortFunction = ShakerSort,
                    CurrentData = inputData.ToList(),
                    IsAscending = isAscending
                });
            }

            if (QuickSortCheckBox.IsChecked == true)
            {
                activeAlgorithms.Add(new SortAlgorithm
                {
                    Name = "Быстрая",
                    SortFunction = QuickSort,
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
                        SortFunction = BogoSort,
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

            // Run each algorithm and measure time
            foreach (var algorithm in activeAlgorithms)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var result = algorithm.SortFunction(algorithm.CurrentData, algorithm.IsAscending);
                    stopwatch.Stop();

                    algorithmTimes[algorithm.Name] = stopwatch.Elapsed;
                    algorithmIterations[algorithm.Name] = result.Item3;

                    timingResults.AppendLine($"{algorithm.Name}:");
                    timingResults.AppendLine($"  Время: {stopwatch.Elapsed.TotalMilliseconds:F4} мс");
                    timingResults.AppendLine($"  Итераций: {result.Item3}");
                    timingResults.AppendLine();

                    // Display first algorithm's visualization
                    if (algorithm == activeAlgorithms.First())
                    {
                        SetResultData(result.Item1);
                        UpdateVisualization(result.Item1, result.Item2);
                    }
                }
                catch (Exception ex)
                {
                    timingResults.AppendLine($"{algorithm.Name}: ОШИБКА - {ex.Message}");
                    timingResults.AppendLine();
                }
            }

            TimingResultsTextBox.Text = timingResults.ToString();

            // Determine fastest algorithm
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

        private void ResetColors_Click(object sender, RoutedEventArgs e)
        {
            var data = GetInputData();
            if (data != null)
            {
                UpdateVisualization(data);
            }
        }

        // Data generation and management
        private void GenerateRandom_Click(object sender, RoutedEventArgs e)
        {
            GenerateWithRange_Click(sender, e);
        }

        private void GenerateWithRange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int min = int.Parse(MinValueTextBox.Text);
                int max = int.Parse(MaxValueTextBox.Text);

                var data = new List<int>();
                for (int i = 0; i < arraySize; i++)
                {
                    data.Add(random.Next(min, max + 1));
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

        // Excel and Google Sheets methods
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
                var data = new List<int>();

                for (int i = 1; i <= 100; i++)
                {
                    if (worksheet.Cells[i, 1].Value == null) break;
                    if (int.TryParse(worksheet.Cells[i, 1].Value.ToString(), out int value))
                    {
                        data.Add(value);
                    }
                }

                if (data.Count > 0)
                {
                    arraySize = data.Count;
                    ArraySizeTextBox.Text = arraySize.ToString();
                    SetInputData(data);
                    UpdateVisualization(data);
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
            var data = new List<int>();

            // Читаем данные из первого столбца
            for (int i = 0; i < values.Count && i < 100; i++) // ограничение на 100 элементов
            {
                if (values[i].Count > 0 && values[i][0] != null)
                {
                    if (int.TryParse(values[i][0].ToString(), out int value))
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

    // Вспомогательные классы вынесены за пределы основного класса
    public class SortAlgorithm
    {
        public string Name { get; set; }
        public Func<List<int>, bool, (List<int>, List<ColorInfo>, int)> SortFunction { get; set; }
        public List<int> CurrentData { get; set; }
        public bool IsAscending { get; set; }
        public Stopwatch Timer { get; set; } = new Stopwatch();
    }

    public class ColorInfo
    {
        public Brush Color { get; set; }
        public int Index { get; set; }
    }

    public class VisualizationElement
    {
        public int Value { get; set; }
        public double Height { get; set; }
        public Brush Color { get; set; }
    }
}
