using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Input;
using System;
using System.Web;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using NetworkMonitor.Snort;

namespace NetworkMonitor.AppConfiguration
{
    public class ConfigurationViewModel : INotifyPropertyChanged
    {
        private string _logFilePath;
        private string _apiAddress;
        private string _snortInstallationPath;
        private string _host;
        private string _port;
        private string _database;
        private string _username;
        private string _password;
        private string _connectionString;
        private string _postgresInstallationPath;

        public ObservableCollection<NetworkDevice> DeviceList { get; set; } = new ObservableCollection<NetworkDevice>();

        public string LogFilePath
        {
            get => _logFilePath;
            set
            {
                _logFilePath = value;
                OnPropertyChanged(nameof(LogFilePath));
            }
        }

        public string ApiAddress
        {
            get => _apiAddress;
            set
            {
                _apiAddress = value;
                OnPropertyChanged(nameof(ApiAddress));
            }
        }
        public string SnortInstallationPath
        {
            get => _snortInstallationPath;
            set
            {
                _snortInstallationPath = value;
                OnPropertyChanged(nameof(SnortInstallationPath));
            }
        }
        public string Host
        {
            get => _host;
            set
            {
                _host = value;
                OnPropertyChanged(nameof(Host));
                UpdateConnectionString();
            }
        }
        public string Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
                UpdateConnectionString();
            }
        }
        public string Database
        {
            get => _database;
            set
            {
                _database = value;
                OnPropertyChanged(nameof(Database));
                UpdateConnectionString();
            }
        }
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
                UpdateConnectionString();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                UpdateConnectionString();
            }
        }
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                _connectionString = value;
                OnPropertyChanged(nameof(ConnectionString));
            }
        }
        public string PostgresInstallationPath
        {
            get => _postgresInstallationPath;
            set
            {
                if (_postgresInstallationPath != value)
                {
                    _postgresInstallationPath = value;
                    OnPropertyChanged(nameof(PostgresInstallationPath));
                }
            }
        }

        private NetworkDevice _selectedDevice;
        public NetworkDevice SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand BrowseLogFileCommand { get; }
        public ICommand BrowseSnortFolderCommand { get; }
        public ICommand SaveConnectionStringCommand { get; }
        public ConfigurationViewModel()
        {
            LogFilePath = ConfigurationManager.GetSetting("LogFilePath");
            ApiAddress = ConfigurationManager.GetSetting("ApiAddress");
            SnortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");

            if (ConfigurationManager.Settings.SelectedDevice != null)
            {
                LoadDevices();
                int savedIndex = ConfigurationManager.Settings.SelectedDevice.Index;
                SelectedDevice = DeviceList.FirstOrDefault(device => device.Index == savedIndex);
            }

        }
        public void SaveSettings()
        {
            ConfigurationManager.SetSetting("LogFilePath", LogFilePath);
            ConfigurationManager.SetSetting("ApiAddress", ApiAddress);
            ConfigurationManager.SetSetting("SnortInstallationPath", SnortInstallationPath);
            if (SelectedDevice != null)
            {
                ConfigurationManager.Settings.SelectedDevice = new SelectedDeviceDetails
                {
                    Index = SelectedDevice.Index,
                    DeviceName = SelectedDevice.Description,
                    IpAddress = SelectedDevice.IpAddress
                };
            }
            ConfigurationManager.SaveSettings();
        }

        private void UpdateConnectionString()
        {
            ConnectionString = $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
        }

        public void LoadDevices()
        {
            if (string.IsNullOrWhiteSpace(SnortInstallationPath) || !Directory.Exists(SnortInstallationPath))
            {
                MessageBox.Show("Ścieżka instalacyjna Snort jest nieprawidłowa lub nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string snortBinPath = Path.Combine(SnortInstallationPath, "bin");
            string snortExecutable = Path.Combine(snortBinPath, "snort.exe");

            if (!File.Exists(snortExecutable))
            {
                MessageBox.Show("Nie znaleziono pliku snort.exe w folderze bin. Upewnij się, że ścieżka jest poprawna.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = snortExecutable,
                        Arguments = "-W",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = snortBinPath
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                ParseSnortOutput(output);

                // Po wczytaniu listy urządzeń ustaw wybrany interfejs
                var savedDevice = ConfigurationManager.Settings.SelectedDevice;
                if (savedDevice != null)
                {
                    SelectedDevice = DeviceList.FirstOrDefault(d => d.Index == savedDevice.Index);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wykonywania snort -W: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ParseSnortOutput(string output)
        {
            DeviceList.Clear();

            // Podziel dane na linie na podstawie nowej linii
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            bool headerPassed = false;

            foreach (var line in lines)
            {
                // Ignoruj linie przed tabelą
                if (!headerPassed)
                {
                    if (line.StartsWith("Index"))
                    {
                        headerPassed = true; // Znaleziono nagłówek tabeli
                    }
                    continue;
                }

                if (line.StartsWith("-----"))
                    continue; // Ignoruj linie z separatorami

                // Rozdziel dane w wierszu na kolumny za pomocą tabulatorów (\t)
                var columns = line.Split('\t');

                if (columns.Length < 5)
                    continue; // Jeśli linia ma mniej niż 5 kolumn, pomiń ją

                try
                {
                    // Odczytaj dane z kolumn
                    int index = int.Parse(columns[0].Trim());
                    string physicalAddress = columns[1].Trim();
                    string ipAddress = columns[2].Trim();
                    string deviceName = columns[3].Trim();
                    string description = columns[4].Trim();

                    // Zamień "disabled" na bardziej przyjazną wartość
                    if (ipAddress == "disabled")
                    {
                        ipAddress = "Brak";
                    }

                    // Dodaj urządzenie do listy
                    DeviceList.Add(new NetworkDevice
                    {
                        Index = index,
                        PhysicalAddress = physicalAddress,
                        IpAddress = ipAddress,
                        DeviceName = deviceName,
                        Description = description
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas parsowania linii: {line} - {ex.Message}");
                }
            }
        }




        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class NetworkDevice
        {
            public int Index { get; set; }
            public string PhysicalAddress { get; set; }
            public string IpAddress { get; set; }
            public string DeviceName { get; set; }
            public string Description { get; set; }
        }
    }
}