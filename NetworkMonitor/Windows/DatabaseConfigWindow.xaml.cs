using System;
using System.Collections.Generic;
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
        public string SQLDatabaseName { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string DatabaseName { get; private set; }
        public DatabaseConfigWindow()
        {
            InitializeComponent();
        }

        private void SaveAndInitialize_Click(object sender, RoutedEventArgs e)
        {
            Host = HostTextBox.Text;
            Port = int.TryParse(PortTextBox.Text, out int port) ? port : 5432;
            SQLDatabaseName = SQLDatabaseNameTextBox.Text;
            Username = UsernameTextBox.Text;
            Password = PasswordBox.Password;
            DatabaseName = DatabaseNameTextBox.Text;


            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(SQLDatabaseName) || string.IsNullOrEmpty(DatabaseName))
            {
                MessageBox.Show("Proszę wypełnić wszystkie pola.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

    }
}
