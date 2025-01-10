using System.Windows;

namespace NetworkMonitor.Windows
{
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiUrl = ApiUrlTextBox.Text;
            string logPath = LogPathTextBox.Text;

            // Zapisz konfigurację (np. do pliku lub bazy danych)
            SaveConfiguration(apiUrl, logPath);

            MessageBox.Show("Konfiguracja została zapisana.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close(); // Zamknij okno
        }

        private void SaveConfiguration(string apiUrl, string logPath)
        {
            // Przykładowy zapis konfiguracji do pliku
            System.IO.File.WriteAllText("config.txt", $"ApiUrl={apiUrl}\nLogPath={logPath}");
        }
    }
}
