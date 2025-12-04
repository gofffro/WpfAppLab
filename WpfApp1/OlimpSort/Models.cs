using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

namespace WpfApp1.OlimpSort
{
    public class SortAlgorithm
    {
        public string Name { get; set; }
        public Func<List<double>, bool, int, SortingResult> SortFunction { get; set; }
        public List<double> CurrentData { get; set; }
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
        public double Value { get; set; }
        public double Height { get; set; }
        public Brush Color { get; set; }
    }

    public class InputDataItem : INotifyPropertyChanged
    {
        private string _value;

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}