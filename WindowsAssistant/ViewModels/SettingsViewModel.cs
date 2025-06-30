using PersonaDesk.Helpers;
using PersonaDesk.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Windows.Media.SpeechSynthesis;


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

    public bool SpeechEnabled
    {
        get => _settings.SpeechEnabled;
        set { _settings.SpeechEnabled = value; OnPropertyChanged(); }
    }

    public string SpeechVoice
    {
        get => _settings.SpeechVoice;
        set { _settings.SpeechVoice = value; OnPropertyChanged(); }
    }

    public List<string> AvailableVoices { get; } = new();

    public ICommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _settings = SettingsService.LoadSettings();
        SaveCommand = new RelayCommand(SaveSettings);

        // Populate voices
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            AvailableVoices.Add(voice.DisplayName);
        }
        // Set default if current saved one doesn't exist
        if (!AvailableVoices.Contains(SpeechVoice))
        {
            SpeechVoice = AvailableVoices.FirstOrDefault() ?? "Microsoft David";
        }
    }

    private void SaveSettings()
    {
        try
        {
            SettingsService.SaveSettings(_settings);
            var dialog = new QuickDialog("Settings saved!");

            var mainWindow = Application.Current.MainWindow;
            HotkeyService.RegisterHotkey(mainWindow, _settings.Hotkey);

            dialog.Owner = mainWindow;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            var dialog = new QuickDialog($"Failed to save settings: {ex.Message}");
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
