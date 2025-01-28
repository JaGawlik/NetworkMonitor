using System;
using System.Windows;
using System.Windows.Controls;

namespace NetworkMonitor.Windows.Views
{
    public partial class AdminSettingsView : UserControl
    {
        public AdminSettingsView()
        {
            InitializeComponent();
            DataContext = new AdminSettingsViewModel();
        }

        private void AddRuleButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                string sid = SidTextBox.Text;
                string srcIp = SrcIpTextBox.Text;
                if (int.TryParse(LimitTextBox.Text, out int limit))
                {
                    viewModel.AddRule(sid, srcIp, limit);
                    RulesListBox.Items.Add($"SID: {sid}, IP: {srcIp}, Limit: {limit}s");
                }
                else
                {
                    MessageBox.Show("Limit musi być liczbą całkowitą.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminSettingsViewModel viewModel)
            {
                viewModel.SaveRules();
            }
        }
    }
}
