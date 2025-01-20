using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace NetworkMonitor.Windows
{
    /// <summary>
    /// Interaction logic for DatabaseConfigWindow.xaml
    /// </summary>
    public partial class DatabaseConfigWindow : Window
    {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public ObservableCollection<string> SQLDatabaseNames { get; set; } = new ObservableCollection<string>();
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string UserDatabaseName { get; private set; }
        public DatabaseConfigWindow()
        {
            InitializeComponent();
            //SQLDatabaseNameComboBox.ItemsSource = SQLDatabaseNames;
        }

        private void SaveAndInitialize_Click(object sender, RoutedEventArgs e)
        {
            Host = HostTextBox.Text;
            Port = int.TryParse(PortTextBox.Text, out int port) ? port : 5432;
            Username = UsernameTextBox.Text;
            Password = PasswordBox.Password;            

            //string selectedDatabase = SQLDatabaseNameComboBox.SelectedItem?.ToString();
            UserDatabaseName = DatabaseNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) /*|| string.IsNullOrEmpty(selectedDatabase)*/ || string.IsNullOrEmpty(UserDatabaseName))
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            DialogResult = true;
            Close();
        }

        private void RefreshDatabaseList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pobierz listę baz danych
                var connectionString = $"Host={HostTextBox.Text};Port={PortTextBox.Text};Username={UsernameTextBox.Text};Password={PasswordBox.Password};Database=postgres";
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var databases = new ObservableCollection<string>();
                using (var cmd = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datistemplate = false", conn))
                {
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        databases.Add(reader.GetString(0));
                    }
                }

                // Aktualizacja ComboBox
                SQLDatabaseNames.Clear();
                foreach (var db in databases)
                {
                    SQLDatabaseNames.Add(db);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas pobierania listy baz danych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
