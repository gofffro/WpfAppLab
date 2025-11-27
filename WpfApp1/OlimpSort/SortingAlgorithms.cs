using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;

namespace WpfApp1.OlimpSort
{
    public class SortingResult
    {
        public TimeSpan Time { get; set; }
        public int Iterations { get; set; }
        public bool IsCompleted { get; set; } = true;
        public List<int> SortedArray { get; set; }
        public List<ColorInfo> ColorInfo { get; set; }
    }

    public static class SortingAlgorithms
    {
        private static Random random = new Random();

        public static SortingResult BubbleSort(List<int> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int n = array.Count;
            var resultArray = new List<int>(array);
            var colors = CreateDefaultColors(n);

            for (int i = 0; i < n - 1 && iterations < maxIterations; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    // Обновляем цвета для визуализации
                    UpdateColors(colors, j, j + 1, Brushes.Red);

                    bool shouldSwap = ascending ?
                        resultArray[j] > resultArray[j + 1] :
                        resultArray[j] < resultArray[j + 1];

                    if (shouldSwap)
                    {
                        // Своп элементов
                        int temp = resultArray[j];
                        resultArray[j] = resultArray[j + 1];
                        resultArray[j + 1] = temp;

                        // Подсвечиваем swapped элементы
                        UpdateColors(colors, j, j + 1, Brushes.Green);
                    }
                    else
                    {
                        UpdateColors(colors, j, j + 1, Brushes.Orange);
                    }
                }
                iterations++; // Одна итерация = один полный проход
            }

            // Финальная раскраска - все отсортировано
            SetAllColors(colors, Brushes.LightGreen);

            stopwatch.Stop();
            return new SortingResult
            {
                Time = stopwatch.Elapsed,
                Iterations = iterations,
                SortedArray = resultArray,
                ColorInfo = colors,
                IsCompleted = iterations < maxIterations
            };
        }

        public static SortingResult InsertionSort(List<int> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int n = array.Count;
            var resultArray = new List<int>(array);
            var colors = CreateDefaultColors(n);

            for (int i = 1; i < n && iterations < maxIterations; i++)
            {
                int key = resultArray[i];
                int j = i - 1;

                // Подсвечиваем текущий элемент
                colors[i].Color = Brushes.Red;

                while (j >= 0 && (
                    (ascending && resultArray[j] > key) ||
                    (!ascending && resultArray[j] < key)) &&
                    iterations < maxIterations)
                {
                    resultArray[j + 1] = resultArray[j];

                    // Обновляем цвета при сдвиге
                    colors[j].Color = Brushes.Orange;
                    colors[j + 1].Color = Brushes.Green;

                    j--;
                    iterations++;
                }

                resultArray[j + 1] = key;

                // Обновляем цвета для отсортированной части
                for (int k = 0; k <= i; k++)
                {
                    colors[k].Color = Brushes.LightGreen;
                }

                iterations++; // Одна итерация = одна вставка элемента
            }

            // Финальная раскраска
            SetAllColors(colors, Brushes.LightGreen);

            stopwatch.Stop();
            return new SortingResult
            {
                Time = stopwatch.Elapsed,
                Iterations = iterations,
                SortedArray = resultArray,
                ColorInfo = colors,
                IsCompleted = iterations < maxIterations
            };
        }

        public static SortingResult ShakerSort(List<int> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            var resultArray = new List<int>(array);
            var colors = CreateDefaultColors(resultArray.Count);

            int left = 0;
            int right = resultArray.Count - 1;

            while (left <= right && iterations < maxIterations)
            {
                // Проход слева направо
                for (int i = left; i < right; i++)
                {
                    UpdateColors(colors, i, i + 1, Brushes.Red);

                    bool shouldSwap = ascending ?
                        resultArray[i] > resultArray[i + 1] :
                        resultArray[i] < resultArray[i + 1];

                    if (shouldSwap)
                    {
                        int temp = resultArray[i];
                        resultArray[i] = resultArray[i + 1];
                        resultArray[i + 1] = temp;

                        UpdateColors(colors, i, i + 1, Brushes.Green);
                    }
                }
                right--;

                // Проход справа налево
                for (int i = right; i > left; i--)
                {
                    UpdateColors(colors, i - 1, i, Brushes.Red);

                    bool shouldSwap = ascending ?
                        resultArray[i - 1] > resultArray[i] :
                        resultArray[i - 1] < resultArray[i];

                    if (shouldSwap)
                    {
                        int temp = resultArray[i];
                        resultArray[i] = resultArray[i - 1];
                        resultArray[i - 1] = temp;

                        UpdateColors(colors, i - 1, i, Brushes.Green);
                    }
                }
                left++;

                iterations++; // Одна итерация = один проход в обе стороны
            }

            // Финальная раскраска
            SetAllColors(colors, Brushes.LightGreen);

            stopwatch.Stop();
            return new SortingResult
            {
                Time = stopwatch.Elapsed,
                Iterations = iterations,
                SortedArray = resultArray,
                ColorInfo = colors,
                IsCompleted = iterations < maxIterations
            };
        }

        public static SortingResult QuickSort(List<int> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            var resultArray = new List<int>(array);
            var colors = CreateDefaultColors(resultArray.Count);

            QuickSortRecursive(resultArray, 0, resultArray.Count - 1, ascending, colors, ref iterations, maxIterations);

            // Финальная раскраска
            SetAllColors(colors, Brushes.LightGreen);

            stopwatch.Stop();
            return new SortingResult
            {
                Time = stopwatch.Elapsed,
                Iterations = iterations,
                SortedArray = resultArray,
                ColorInfo = colors,
                IsCompleted = iterations < maxIterations
            };
        }

        private static void QuickSortRecursive(List<int> array, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations, int maxIterations)
        {
            if (low < high && iterations < maxIterations)
            {
                int pi = Partition(array, low, high, ascending, colors, ref iterations, maxIterations);
                iterations++;

                if (iterations < maxIterations)
                    QuickSortRecursive(array, low, pi - 1, ascending, colors, ref iterations, maxIterations);

                if (iterations < maxIterations)
                    QuickSortRecursive(array, pi + 1, high, ascending, colors, ref iterations, maxIterations);
            }
        }

        private static int Partition(List<int> array, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations, int maxIterations)
        {
            int pivot = array[high];
            colors[high].Color = Brushes.Purple; // Опорный элемент

            int i = low - 1;

            for (int j = low; j < high && iterations < maxIterations; j++)
            {
                colors[j].Color = Brushes.Red;

                bool condition = ascending ?
                    array[j] <= pivot :
                    array[j] >= pivot;

                if (condition)
                {
                    i++;

                    // Своп элементов
                    int temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;

                    colors[i].Color = Brushes.Green;
                    if (i != j) colors[j].Color = Brushes.Orange;
                }

                // Сбрасываем цвета неактивных элементов
                for (int k = low; k <= high; k++)
                {
                    if (k != j && k != i && k != high && colors[k].Color != Brushes.Green)
                    {
                        colors[k].Color = Brushes.LightBlue;
                    }
                }
            }

            if (iterations < maxIterations)
            {
                int temp1 = array[i + 1];
                array[i + 1] = array[high];
                array[high] = temp1;
            }

            colors[i + 1].Color = Brushes.LightGreen;
            colors[high].Color = Brushes.LightBlue;

            return i + 1;
        }

        public static SortingResult BogoSort(List<int> array, bool ascending = true, int maxIterations = 10000)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            var resultArray = new List<int>(array);
            var colors = CreateDefaultColors(resultArray.Count);

            while (!IsSorted(resultArray, ascending) && iterations < maxIterations)
            {
                iterations++;
                Shuffle(resultArray);

                // Раскрашиваем при перемешивании
                SetAllColors(colors, Brushes.Orange);
            }

            // Финальная раскраска
            SetAllColors(colors, IsSorted(resultArray, ascending) ? Brushes.LightGreen : Brushes.Red);

            stopwatch.Stop();
            return new SortingResult
            {
                Time = stopwatch.Elapsed,
                Iterations = iterations,
                SortedArray = resultArray,
                ColorInfo = colors,
                IsCompleted = iterations < maxIterations && IsSorted(resultArray, ascending)
            };
        }

        private static bool IsSorted(List<int> array, bool ascending)
        {
            for (int i = 0; i < array.Count - 1; i++)
            {
                if (ascending && array[i] > array[i + 1])
                    return false;
                if (!ascending && array[i] < array[i + 1])
                    return false;
            }
            return true;
        }

        private static void Shuffle(List<int> array)
        {
            int n = array.Count;
            for (int i = 0; i < n; i++)
            {
                int j = random.Next(i, n);
                int temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        // Вспомогательные методы для работы с цветами
        private static List<ColorInfo> CreateDefaultColors(int count)
        {
            var colors = new List<ColorInfo>();
            for (int i = 0; i < count; i++)
            {
                colors.Add(new ColorInfo
                {
                    Color = Brushes.LightBlue,
                    Index = i
                });
            }
            return colors;
        }

        private static void UpdateColors(List<ColorInfo> colors, int index1, int index2, Brush color)
        {
            if (index1 < colors.Count) colors[index1].Color = color;
            if (index2 < colors.Count) colors[index2].Color = color;
        }

        private static void SetAllColors(List<ColorInfo> colors, Brush color)
        {
            foreach (var colorInfo in colors)
            {
                colorInfo.Color = color;
            }
        }
    }
}