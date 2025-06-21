using System.Windows;
using System.Windows.Input;

namespace PersonaDesk.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainViewModel();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ((ViewModels.MainViewModel)DataContext).SubmitCommand.Execute(null);
            }
        }
    }
}
