using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Database
{
    public class PostgreSQLManager
    {
        public void InitializeNewInstance(string dataDirectory, string postgresBinPath)
        {
            var initdbPath = System.IO.Path.Combine(postgresBinPath, "initdb.exe");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = initdbPath,
                Arguments = $"-D \"{dataDirectory}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Nowa instancja PostgreSQL została zainicjalizowana w katalogu: {dataDirectory}");
                }
                else
                {
                    Console.WriteLine($"Wystąpił błąd podczas inicjalizacji: {process.StandardError.ReadToEnd()}");
                }
            }
        }
    }
}
