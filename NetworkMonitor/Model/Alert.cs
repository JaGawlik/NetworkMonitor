using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetworkMonitor.Model
{
    public class Alert 
    {
        private string _status;
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string AlertMessage { get; set; }
        public string SourceIp { get; set; }
        public int? SourcePort { get; set; }
        public string DestinationIp { get; set; }
        public int? DestinationPort { get; set; }
        public string Protocol { get; set; }
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        public int SignatureId { get; set; }
        public string SnortInstance { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
