using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartGreenhouse.App
{
    public class LoopDataRow : INotifyPropertyChanged
    {
        private int _index;
        private double _value;
        private bool _isCurrent;
        private bool _isModified;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(); }
        }

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(); }
        }

        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

