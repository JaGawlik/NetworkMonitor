//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NetworkMonitor.Snort
//{
//    public class SnortConfigUpdater
//    {
//        /// <summary>
//        /// Aktualizuje plik snort.conf na podstawie wybranego interfejsu, adresu IP i ścieżki logów.
//        /// </summary>
//        /// <param name="snortConfPath">Ścieżka do pliku snort.conf</param>
//        /// <param name="homeNet">Zakres sieci HOME_NET (np. "192.168.0.0/24")</param>
//        /// <param name="logPath">Ścieżka do folderu logów Snorta</param>
//        /// <param name="rulePath">Ścieżka do folderu reguł Snorta</param>
//        public static void UpdateSnortConfig(string snortConfPath, string homeNet, string logPath, string rulePath)
//        {
//            if (!File.Exists(snortConfPath))
//            {
//                throw new FileNotFoundException($"Nie znaleziono pliku konfiguracyjnego Snorta: {snortConfPath}");
//            }

//            try
//            {
//                string[] lines = File.ReadAllLines(snortConfPath);

//                for (int i = 0; i < lines.Length; i++)
//                {
//                    // Aktualizacja HOME_NET
//                    if (lines[i].StartsWith("ipvar HOME_NET"))
//                    {
//                        lines[i] = $"ipvar HOME_NET {homeNet}";
//                    }

//                    // Aktualizacja logdir
//                    if (lines[i].StartsWith("config logdir:"))
//                    {
//                        lines[i] = $"config logdir: {logPath}";
//                    }

//                    // Aktualizacja RULE_PATH
//                    if (lines[i].StartsWith("var RULE_PATH"))
//                    {
//                        lines[i] = $"var RULE_PATH {rulePath}";
//                    }
//                }

//                // Zapis zaktualizowanego pliku
//                File.WriteAllLines(snortConfPath, lines);

//                Console.WriteLine("Plik snort.conf został zaktualizowany pomyślnie.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Błąd podczas aktualizacji pliku snort.conf: {ex.Message}");
//            }
//        }
//    }
//}