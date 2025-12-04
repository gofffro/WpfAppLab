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
        public List<double> SortedArray { get; set; }
        public List<ColorInfo> ColorInfo { get; set; }
    }

    public static class SortingAlgorithms
    {
        private static Random random = new Random();

        public static SortingResult BubbleSort(List<double> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int n = array.Count;
            var resultArray = new List<double>(array);
            var colors = CreateDefaultColors(n);

            for (int i = 0; i < n - 1 && iterations < maxIterations; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    UpdateColors(colors, j, j + 1, Brushes.Red);

                    bool shouldSwap = ascending ?
                        resultArray[j] > resultArray[j + 1] :
                        resultArray[j] < resultArray[j + 1];

                    if (shouldSwap)
                    {
                        double temp = resultArray[j];
                        resultArray[j] = resultArray[j + 1];
                        resultArray[j + 1] = temp;

                        UpdateColors(colors, j, j + 1, Brushes.Green);
                    }
                    else
                    {
                        UpdateColors(colors, j, j + 1, Brushes.Orange);
                    }
                }
                iterations++;
            }

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

        public static SortingResult InsertionSort(List<double> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            int n = array.Count;
            var resultArray = new List<double>(array);
            var colors = CreateDefaultColors(n);

            for (int i = 1; i < n && iterations < maxIterations; i++)
            {
                double key = resultArray[i];
                int j = i - 1;

                colors[i].Color = Brushes.Red;

                while (j >= 0 && (
                    (ascending && resultArray[j] > key) ||
                    (!ascending && resultArray[j] < key)) &&
                    iterations < maxIterations)
                {
                    resultArray[j + 1] = resultArray[j];

                    colors[j].Color = Brushes.Orange;
                    colors[j + 1].Color = Brushes.Green;

                    j--;
                    iterations++;
                }

                resultArray[j + 1] = key;

                for (int k = 0; k <= i; k++)
                {
                    colors[k].Color = Brushes.LightGreen;
                }

                iterations++;
            }

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

        public static SortingResult ShakerSort(List<double> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            var resultArray = new List<double>(array);
            var colors = CreateDefaultColors(resultArray.Count);

            int left = 0;
            int right = resultArray.Count - 1;
            bool swapped;

            // Главный цикл - do-while гарантирует минимум один проход
            do
            {
                swapped = false; // Начинаем каждый проход с предположения, что обменов не будет

                // ПРОХОД СЛЕВА НАПРАВО (как в пузырьковой сортировке)
                int lastSwapIndex = left; // Запоминаем место последнего обмена
                for (int i = left; i < right; i++)
                {
                    // Визуализация - подсвечиваем сравниваемые элементы
                    UpdateColors(colors, i, i + 1, Brushes.Red);

                    // Проверяем, нужно ли менять элементы местами
                    bool shouldSwap = ascending ?
                        resultArray[i] > resultArray[i + 1] :  // Для сортировки по возрастанию
                        resultArray[i] < resultArray[i + 1];   // Для сортировки по убыванию

                    if (shouldSwap)
                    {
                        // Меняем элементы местами
                        double temp = resultArray[i];
                        resultArray[i] = resultArray[i + 1];
                        resultArray[i + 1] = temp;

                        // Визуализация - подсвечиваем обмен
                        UpdateColors(colors, i, i + 1, Brushes.Green);

                        // Отмечаем, что был обмен и запоминаем его позицию
                        swapped = true;
                        lastSwapIndex = i;
                    }
                    else
                    {
                        // Визуализация - элементы в правильном порядке
                        UpdateColors(colors, i, i + 1, Brushes.Orange);
                    }
                }

                // Сужаем правую границу до места последнего обмена
                // (все элементы справа уже на своих местах)
                right = lastSwapIndex;

                // Увеличиваем счетчик итераций
                iterations++;

                // Проверяем, не достигнут ли лимит итераций
                if (iterations >= maxIterations) break;

                // Если НЕ БЫЛО обменов при проходе слева направо - массив отсортирован
                // Завершаем работу
                if (!swapped) break;

                // Сбрасываем флаг для прохода в обратном направлении
                swapped = false;

                // ПРОХОД СПРАВА НАЛЕВО (обратное направление)
                lastSwapIndex = right; // Снова запоминаем место последнего обмена
                for (int i = right; i > left; i--)
                {
                    // Визуализация - подсвечиваем сравниваемые элементы
                    UpdateColors(colors, i - 1, i, Brushes.Red);

                    // Проверяем, нужно ли менять элементы местами
                    bool shouldSwap = ascending ?
                        resultArray[i - 1] > resultArray[i] :  // Для сортировки по возрастанию
                        resultArray[i - 1] < resultArray[i];   // Для сортировки по убыванию

                    if (shouldSwap)
                    {
                        // Меняем элементы местами
                        double temp = resultArray[i];
                        resultArray[i] = resultArray[i - 1];
                        resultArray[i - 1] = temp;

                        // Визуализация - подсвечиваем обмен
                        UpdateColors(colors, i - 1, i, Brushes.Green);

                        // Отмечаем, что был обмен и запоминаем его позицию
                        swapped = true;
                        lastSwapIndex = i;
                    }
                    else
                    {
                        // Визуализация - элементы в правильном порядке
                        UpdateColors(colors, i - 1, i, Brushes.Orange);
                    }
                }

                // Сужаем левую границу до места последнего обмена
                // (все элементы слева уже на своих местах)
                left = lastSwapIndex;

                // Увеличиваем счетчик итераций
                iterations++;

            } while (left < right && swapped && iterations < maxIterations);

            // После завершения сортировки подсвечиваем все элементы зеленым
            SetAllColors(colors, Brushes.LightGreen);

            stopwatch.Stop();

            // Возвращаем результат сортировки
            return new SortingResult
            {
                Time = stopwatch.Elapsed,
                Iterations = iterations,
                SortedArray = resultArray,
                ColorInfo = colors,
                IsCompleted = iterations < maxIterations
            };
        }

        public static SortingResult QuickSort(List<double> array, bool ascending = true, int maxIterations = int.MaxValue)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            var resultArray = new List<double>(array);
            var colors = CreateDefaultColors(resultArray.Count);

            QuickSortRecursive(resultArray, 0, resultArray.Count - 1, ascending, colors, ref iterations, maxIterations);

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

        private static void QuickSortRecursive(List<double> array, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations, int maxIterations)
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

        private static int Partition(List<double> array, int low, int high, bool ascending, List<ColorInfo> colors, ref int iterations, int maxIterations)
        {
            double pivot = array[high];
            colors[high].Color = Brushes.Purple;

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

                    double temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;

                    colors[i].Color = Brushes.Green;
                    if (i != j) colors[j].Color = Brushes.Orange;
                }

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
                double temp1 = array[i + 1];
                array[i + 1] = array[high];
                array[high] = temp1;
            }

            colors[i + 1].Color = Brushes.LightGreen;
            colors[high].Color = Brushes.LightBlue;

            return i + 1;
        }

        public static SortingResult BogoSort(List<double> array, bool ascending = true, int maxIterations = 10000)
        {
            var stopwatch = Stopwatch.StartNew();
            int iterations = 0;
            var resultArray = new List<double>(array);
            var colors = CreateDefaultColors(resultArray.Count);

            while (!IsSorted(resultArray, ascending) && iterations < maxIterations)
            {
                iterations++;
                Shuffle(resultArray);

                SetAllColors(colors, Brushes.Orange);
            }

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

        private static bool IsSorted(List<double> array, bool ascending)
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

        private static void Shuffle(List<double> array)
        {
            int n = array.Count;
            for (int i = 0; i < n; i++)
            {
                int j = random.Next(i, n);
                double temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

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