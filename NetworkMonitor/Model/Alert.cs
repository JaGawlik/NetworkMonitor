using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetworkMonitor.Model
{
    public class Alert
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string AlertMessage { get; set; }
        public string SourceIp { get; set; }
        public int? SourcePort { get; set; }
        public string DestinationIp { get; set; }
        public int? DestinationPort { get; set; }
        public string Protocol { get; set; }

        public int SignatureId { get; set; }
        public string Status { get; set; }
        public string SnortInstance { get; set; }

    }
}
