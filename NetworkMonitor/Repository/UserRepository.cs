using NetworkMonitor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Repository
{    public static class UserRepository
    {
        public static List<User> Users = new List<User>
    {
        new User { Username = "admin", Password = "admin123", Role = "admin" },
        new User { Username = "user1", Password = "user123", Role = "user", AssignedIp = "192.168.1.100" }
    };

        public static User Authenticate(string username, string password)
        {
            return Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }
    }
}
