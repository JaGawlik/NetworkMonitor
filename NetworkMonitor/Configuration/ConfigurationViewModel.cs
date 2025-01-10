using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Input;
using System;

namespace NetworkMonitor.Configuration
{
    public class ConfigurationViewModel : INotifyPropertyChanged
    {
        private string _logFilePath;
        private string _apiAddress;
        private string _snortInstallationPath;

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

        public ICommand SaveCommand { get; }
        public ICommand BrowseLogFileCommand { get; }
        public ICommand BrowseSnortFolderCommand { get; }
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
