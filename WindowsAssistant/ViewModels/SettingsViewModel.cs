using PersonaDesk.Helpers;
using PersonaDesk.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

public class SettingsViewModel : INotifyPropertyChanged
{
    private SettingsModel _settings;

    public string AssistantName
    {
        get => _settings.AssistantName;
        set { _settings.AssistantName = value; OnPropertyChanged(); }
    }

    public string PersonalityPrompt
    {
        get => _settings.PersonalityPrompt;
        set { _settings.PersonalityPrompt = value; OnPropertyChanged(); }
    }

    public string Hotkey
    {
        get => _settings.Hotkey;
        set { _settings.Hotkey = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _settings = SettingsService.LoadSettings();
        SaveCommand = new RelayCommand(SaveSettings);
    }

    private void SaveSettings()
    {
        try
        {
            SettingsService.SaveSettings(_settings);
            var dialog = new QuickDialog("Settings saved!");
            dialog.Owner = Application.Current.MainWindow; // Makes it center over your main app
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            var dialog = new QuickDialog($"Failed to save settings: {ex.Message}");
            dialog.Owner = Application.Current.MainWindow; // Makes it center over your main app
            dialog.ShowDialog();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
