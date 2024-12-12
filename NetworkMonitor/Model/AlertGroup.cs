using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Model
{
    public class AlertGroup
    {
        public string DestinationIp { get; set; }
        public List<Alert> Alerts { get; set; }
    }
}
