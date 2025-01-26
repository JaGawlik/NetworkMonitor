using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Input;
using System;
using System.Web;

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

        public ICommand SaveCommand { get; }
        public ICommand BrowseLogFileCommand { get; }
        public ICommand BrowseSnortFolderCommand { get; }
        public ICommand SaveConnectionStringCommand { get; }
        public ConfigurationViewModel()
        {           
            LogFilePath = ConfigurationManager.GetSetting("LogFilePath");
            ApiAddress = ConfigurationManager.GetSetting("ApiAddress");
            SnortInstallationPath = ConfigurationManager.GetSetting("SnortInstallationPath");
        }
        public void SaveSettings()
        {
            ConfigurationManager.SetSetting("LogFilePath", LogFilePath);
            ConfigurationManager.SetSetting("ApiAddress", ApiAddress);
            ConfigurationManager.SetSetting("SnortInstallationPath", SnortInstallationPath);
            ConfigurationManager.SaveSettings();
        }

        private void UpdateConnectionString()
        {
            ConnectionString = $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
