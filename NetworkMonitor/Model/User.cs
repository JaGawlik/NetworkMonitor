using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Używaj hashowania w produkcji
        public string Role { get; set; } 
        public string AssignedIp { get; set; }
    }
}
