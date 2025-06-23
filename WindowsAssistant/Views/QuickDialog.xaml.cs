using System.Windows;

namespace PersonaDesk.Views
{
    public partial class QuickDialog : Window
    {
        public QuickDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
