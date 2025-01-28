using NetworkMonitor.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NetworkMonitor.Windows
{
    public class AlertGroupViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string DestinationIp { get; set; }
        public ObservableCollection<Alert> Alerts { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

}
