using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkMonitor.AppConfiguration;

namespace NetworkMonitor.Utilities
{
    public static class SnortConfigManager
    {
        private static readonly string SnortConfigFilePath = Path.Combine(AppConfiguration.ConfigurationManager.GetSetting("SnortInstallationPath"), "etc", "snort.conf");

        private static string snortPath = ConfigurationManager.GetSetting("SnortInstallationPath");

        public static Dictionary<string, string> LoadConfig()
        {
            var config = new Dictionary<string, string>();

            if (!File.Exists(SnortConfigFilePath))
            {
                Console.WriteLine($"Plik {SnortConfigFilePath} nie istnieje.");
                return config;
            }

            var lines = File.ReadAllLines(SnortConfigFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && (parts[0].StartsWith("var") || parts[0].StartsWith("ipvar") || parts[0].StartsWith("portvar")))
                {
                    config[parts[0]] = parts[1];
                }
            }

            return config;
        }

        public static void ModifyConfigValue(string key, string oldValue, string newValue)
        {
            var lines = File.ReadAllLines(SnortConfigFilePath).ToList();
            bool modified = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.Contains(key) && line.Contains(oldValue))
                {
                    var cleanLine = line.Trim().TrimEnd('\\').Trim();
                   
                    if (cleanLine.StartsWith($"{key} {oldValue}".Trim()))
                    {
                        lines[i] = $"{key} {newValue}".TrimEnd();
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                File.WriteAllLines(SnortConfigFilePath, lines);
            }
        }

        public static void CommentOutConfigValue(string key)
        {
            if (!File.Exists(SnortConfigFilePath))
            {
                Console.WriteLine("Plik konfiguracyjny nie istnieje.");
                return;
            }

            var lines = File.ReadAllLines(SnortConfigFilePath).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().StartsWith(key))
                {
                    lines[i] = "#" + lines[i];
                }
            }

            File.WriteAllLines(SnortConfigFilePath, lines);
            Console.WriteLine($"Zakomentowano wszystkie wystąpienia {key}");
        }

        public static void ConfigureSnort()
        {
            ModifyConfigValue("ipvar HOME_NET", "any", "192.168.0.0/24");
            ModifyConfigValue("ipvar EXTERNAL_NET", "any", "!$HOME_NET");

            ModifyConfigValue("var RULE_PATH", "../rules", snortPath + "\\rules");

            ModifyConfigValue("var SO_RULE_PATH", "../so_rules", "# var SO_RULE_PATH ../so_rules");            

            ModifyConfigValue("var WHITE_LIST_PATH", "../rules", snortPath + "\\rules");
            ModifyConfigValue("var BLACK_LIST_PATH", "../rules", snortPath + "\\rules");
            ModifyConfigValue("config logdir:", "", "config logdir: " + snortPath + "\\log");
            ModifyConfigValue("dynamicpreprocessor directory", "/usr/local/lib/snort_dynamicpreprocessor/", snortPath +  "\\lib\\snort_dynamicpreprocessor");
            ModifyConfigValue("dynamicengine", "/usr/local/lib/snort_dynamicengine/libsf_engine.so", snortPath + "\\lib\\snort_dynamicengine\\sf_engine.dll");

            ModifyConfigValue("dynamicdetection directory", "/usr/local/lib/snort_dynamicrules", "#dynamicdetection directory /usr/local/lib/snort_dynamicrules");
            ModifyConfigValue("preprocessor sfportscan: proto  { all } memcap { 10000000 } sense_level { low }",
                              "# preprocessor sfportscan: proto  { all } memcap { 10000000 } sense_level { low }",
                              "preprocessor sfportscan: proto  { all } memcap { 10000000 } sense_level { low }");     
            ModifyConfigValue("include", "$RULE_PATH/local.rules", "$RULE_PATH\\local.rules");
            ModifyConfigValue("include", "$PREPROC_RULE_PATH/preprocessor.rules", "$PREPROC_RULE_PATH\\preprocessor.rules");
            ModifyConfigValue("include", "$PREPROC_RULE_PATH/decoder.rules", "$PREPROC_RULE_PATH\\decoder.rules");
            ModifyConfigValue("include", "$PREPROC_RULE_PATH/sensitive-data.rules", "$PREPROC_RULE_PATH\\sensitive-data.rules");
            ModifyConfigValue("whitelist", "$WHITE_LIST_PATH/white_list.rules,", "$WHITE_LIST_PATH\\white_list.rules, \\");
            ModifyConfigValue("blacklist", "$BLACK_LIST_PATH/black_list.rules", "$BLACK_LIST_PATH\\blacklist.rules");
            CommentOutConfigValue("var SO_RULE_PATH");
            CommentOutConfigValue("dynamicdetection directory");
            CommentOutConfigValue("preprocessor bo");
        }


        public static void AddConfigValue(string key, string value)
        {
            if (!File.Exists(SnortConfigFilePath))
            {
                Console.WriteLine("Plik konfiguracyjny nie istnieje.");
                return;
            }

            using (StreamWriter sw = File.AppendText(SnortConfigFilePath))
            {
                sw.WriteLine(key + " " + value);
            }
            Console.WriteLine($"Dodano nową wartość: {key} {value}");
        }
    }
}

//105 #var SO_RULE_PATH # var SO_RULE_PATH ../so_rules 
//253 #dynamicdetection directory #dynamicdetection directory /usr/local/lib/snort_dynamicrules
//511 whitelist $WHITE_LIST_PATH\whitelist.rules, \