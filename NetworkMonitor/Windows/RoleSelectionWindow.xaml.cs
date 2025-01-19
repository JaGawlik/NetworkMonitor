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

namespace NetworkMonitor.Windows
{
    /// <summary>
    /// Interaction logic for RoleSelectionWindow.xaml
    /// </summary>
    public partial class RoleSelectionWindow : Window
    {
        public string SelectedRole { get; private set; }
        public RoleSelectionWindow()
        {
            InitializeComponent();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRole = "Administrator";
            DialogResult = true;
            Close();
        }

        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRole = "User";
            DialogResult = true;
            Close();
        }
    }
}
