using System.Windows;

namespace PersonaDesk.Views
{
    /// <summary>
    /// Gives a quick dialog with a message and an OK button.
    /// clicking OK will close the dialog.
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
