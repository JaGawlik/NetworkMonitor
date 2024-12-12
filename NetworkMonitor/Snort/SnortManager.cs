using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkMonitor.Snort
{
    internal class SnortManager
    {
        public static Process StartSnort(string snortPath, string arguments)
        {
            if (!File.Exists(snortPath))
            {
                MessageBox.Show("Nie znaleziono pliku snort.exe w podanej lokalizacji. Aplikacja zostanie zamknięta.");
                Application.Current.Shutdown();
                return null;
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = snortPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process snortProcess = new Process();
            snortProcess.StartInfo = startInfo;

            bool started = snortProcess.Start();
            if (started)
            {
                Console.WriteLine("Snort został uruchomiony.");
                return snortProcess;
            }
            else
            {
                Console.WriteLine("Nie udało się uruchomić Snorta.");
                return snortProcess;
            }
        }
    
    }
}
