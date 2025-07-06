using System.Windows.Controls;
using System.Windows.Input;

namespace PersonaDesk.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// This code-behind handles the hotkey input logic and initializes the settings view model.
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();

            // Set DataContext to the settings view model
            DataContext = new SettingsViewModel();
        }

        /// <summary>
        /// Intercepts key presses in the hotkey textbox and builds a formatted hotkey string.
        /// </summary>
        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent default handling so text box doesn't show plain characters
            e.Handled = true;

            var modifiers = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore pure modifier keys (Ctrl, Shift, Alt, Win)
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
                return;

            // Build hotkey string (e.g., "Ctrl+Shift+P")
            var hotkey = $"{(modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "")}" +
                         $"{(modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "")}" +
                         $"{(modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "")}" +
                         $"{key}";

            // Update the view model with the new hotkey string
            if (DataContext is SettingsViewModel vm)
                vm.Hotkey = hotkey;
        }
    }
}
