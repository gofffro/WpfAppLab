using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace WpfApp1.OlimpSort
{
    public static class SortingAlgorithms
    {
        private static Random random = new Random();

        // Структура для хранения параметров сортировки
        public struct SortParameters
        {
            public int MaxIterations { get; set; }
            public bool CountSwapsAsIterations { get; set; }
        }

        // Исправленные алгоритмы сортировки с улучшенным счетчиком итераций
        public static (List<int>, List<ColorInfo>, int) BubbleSort(List<int> data, bool ascending, SortParameters parameters = default)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            bool swapped;
            int iterations = 0;
            int maxIterations = parameters.MaxIterations > 0 ? parameters.MaxIterations : int.MaxValue;

            for (int i = 0; i < arr.Count - 1 && iterations < maxIterations; i++)
            {
                swapped = false;
                for (int j = 0; j < arr.Count - i - 1 && iterations < maxIterations; j++)
                {
                    iterations++;

                    // Проверка превышения лимита итераций
                    if (iterations >= maxIterations)
                        break;

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

        public static (List<int>, List<ColorInfo>, int) InsertionSort(List<int> data, bool ascending, SortParameters parameters = default)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            int iterations = 0;
            int maxIterations = parameters.MaxIterations > 0 ? parameters.MaxIterations : int.MaxValue;

            for (int i = 1; i < arr.Count && iterations < maxIterations; i++)
            {
                int key = arr[i];
                int j = i - 1;

                colors[i].Color = Brushes.Red;

                while (j >= 0 && (ascending ? arr[j] > key : arr[j] < key) && iterations < maxIterations)
                {
                    iterations++;

                    if (iterations >= maxIterations)
                        break;

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

                if (iterations < maxIterations)
                {
                    iterations++; // Count the last comparison that breaks the loop
                    arr[j + 1] = key;
                }

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

        public static (List<int>, List<ColorInfo>, int) ShakerSort(List<int> data, bool ascending, SortParameters parameters = default)
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
            int maxIterations = parameters.MaxIterations > 0 ? parameters.MaxIterations : int.MaxValue;

            while (swapped && iterations < maxIterations)
            {
                swapped = false;

                // Forward pass
                for (int i = start; i < end && iterations < maxIterations; i++)
                {
                    iterations++;

                    if (iterations >= maxIterations)
                        break;

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

                if (!swapped || iterations >= maxIterations) break;

                end--;

                // Backward pass
                for (int i = end - 1; i >= start && iterations < maxIterations; i--)
                {
                    iterations++;

                    if (iterations >= maxIterations)
                        break;

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

        public static (List<int>, List<ColorInfo>, int) QuickSort(List<int> data, bool ascending, SortParameters parameters = default)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            int iterations = 0;
            int maxIterations = parameters.MaxIterations > 0 ? parameters.MaxIterations : int.MaxValue;
            QuickSortRecursive(arr, 0, arr.Count - 1, ascending, colors, ref iterations, maxIterations);

            // Final state
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i].Color = Brushes.LightGreen;
            }
            return (arr, colors, iterations);
        }

        private static void QuickSortRecursive(List<int> arr, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations, int maxIterations)
        {
            if (low < high && iterations < maxIterations)
            {
                int pi = Partition(arr, low, high, ascending, colors, ref iterations, maxIterations);

                if (iterations < maxIterations)
                    QuickSortRecursive(arr, low, pi - 1, ascending, colors, ref iterations, maxIterations);

                if (iterations < maxIterations)
                    QuickSortRecursive(arr, pi + 1, high, ascending, colors, ref iterations, maxIterations);
            }
        }

        private static int Partition(List<int> arr, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations, int maxIterations)
        {
            int pivot = arr[high];

            // Color pivot
            colors[high].Color = Brushes.Purple;

            int i = low - 1;

            for (int j = low; j < high && iterations < maxIterations; j++)
            {
                iterations++;

                if (iterations >= maxIterations)
                    break;

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

            if (iterations < maxIterations)
            {
                (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
            }

            // Color the final pivot position
            colors[i + 1].Color = Brushes.LightGreen;
            colors[high].Color = Brushes.LightBlue;

            return i + 1;
        }

        public static (List<int>, List<ColorInfo>, int) BogoSort(List<int> data, bool ascending, SortParameters parameters = default)
        {
            var arr = data.ToList();
            var colors = Enumerable.Range(0, arr.Count).Select(i => new ColorInfo
            {
                Color = Brushes.LightBlue,
                Index = i
            }).ToList();

            int attempts = 0;
            int maxAttempts = parameters.MaxIterations > 0 ? parameters.MaxIterations : 10000; // Настраиваемый лимит
            int iterations = 0;

            while (!IsSorted(arr, ascending) && attempts < maxAttempts && iterations < maxAttempts)
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

        private static bool IsSorted(List<int> arr, bool ascending)
        {
            for (int i = 0; i < arr.Count - 1; i++)
            {
                if (ascending && arr[i] > arr[i + 1]) return false;
                if (!ascending && arr[i] < arr[i + 1]) return false;
            }
            return true;
        }

        private static void Shuffle(List<int> arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                int j = random.Next(i, arr.Count);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}