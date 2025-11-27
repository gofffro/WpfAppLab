using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;

namespace WpfApp1.OlimpSort
{
    public class SortAlgorithm
    {
        public string Name { get; set; }
        public Func<List<int>, bool, SortingAlgorithms.SortParameters, (List<int>, List<ColorInfo>, int)> SortFunction { get; set; }
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