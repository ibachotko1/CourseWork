using System.ComponentModel;
using System.Windows.Media;

namespace SmartGreenhouse.App
{
    public class OperationResultViewModel : INotifyPropertyChanged
    {
        private bool _preConditionMet;
        private bool _postConditionMet;
        private bool _canExecute;

        public string OperationName { get; set; }
        public string PreConditionsDescription { get; set; }
        public string PostConditionsDescription { get; set; }

        public bool PreConditionMet
        {
            get => _preConditionMet;
            set
            {
                _preConditionMet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreConditionColor));
            }
        }

        public bool PostConditionMet
        {
            get => _postConditionMet;
            set
            {
                _postConditionMet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PostConditionColor));
            }
        }

        public bool CanExecute
        {
            get => _canExecute;
            set
            {
                _canExecute = value;
                OnPropertyChanged();
            }
        }

        public Brush PreConditionColor => PreConditionMet ? Brushes.Green : Brushes.Red;
        public Brush PostConditionColor => PostConditionMet ? Brushes.Green : Brushes.Red;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

