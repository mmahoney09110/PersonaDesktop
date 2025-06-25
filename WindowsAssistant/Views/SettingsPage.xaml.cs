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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PersonaDesk.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            var modifiers = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore modifier-only keys
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
                return;

            var hotkey = $"{(modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "")}" +
                         $"{(modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "")}" +
                         $"{(modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "")}" +
                         $"{key}";

            if (DataContext is SettingsViewModel vm)
                vm.Hotkey = hotkey;
        }
    }
}
