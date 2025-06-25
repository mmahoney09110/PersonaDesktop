using PersonaDesk.Helpers;
using PersonaDesk.Services;
using PersonaDesk.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PersonaDesk.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged(nameof(InputText));
                }
            }
        }

        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private string _loadingStatusText = "Loading Persona...";
        public string LoadingStatusText
        {
            get => _loadingStatusText;
            set
            {
                if (_loadingStatusText != value)
                {
                    _loadingStatusText = value;
                    OnPropertyChanged(nameof(LoadingStatusText));
                }
            }
        }
        public ObservableCollection<string> OutputLog { get; set; }
        public ICommand SubmitCommand { get; set; }
        private CommandService _commandService;
        private SettingsModel _settings = SettingsService.LoadSettings();
        public MainViewModel()
        {
            OutputLog = new ObservableCollection<string>();
            SubmitCommand = new RelayCommand(ExecuteCommand);
            _commandService = new CommandService();

            Task.Run(() =>
            {
                try
                {
                    App.InitializePythonServer(); // the method that pings /status
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => IsLoading = false);
                    Start();
                }
            });
        }

        private async Task Start()
        {
            string loadingMessage = _settings.AssistantName;
            Application.Current.Dispatcher.Invoke(() => OutputLog.Add(loadingMessage)); // Add on UI thread

            var personaWelcome = await _commandService.PersonaResponse(
                "they just opened the program, give a greeting and let them know you are here to open a folder, browse the web, or just chat.",
                "What can I help with today?"
            );

            Application.Current.Dispatcher.Invoke(() =>
            {
                OutputLog.Remove(loadingMessage); // Remove using exact instance
                OutputLog.Add(_settings.AssistantName+ ":\n" + personaWelcome);
            });
        }

        private void ExecuteCommand()
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"Received command: {InputText}");

                    string loadingMessage = _settings.AssistantName;
                    var input = InputText.Trim();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLog.Add($"You:\n{InputText}");
                        OutputLog.Add(loadingMessage); // Temporary spinner message
                        InputText = string.Empty; // Clear input after submission
                    });

                    
                    var result = await _commandService.HandleCommand(input).ConfigureAwait(false);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLog.Remove(loadingMessage);
                        OutputLog.Add($"{_settings.AssistantName}:\n{result}");
                    });

                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLog.Add($"Error: {ex.Message}");
                    });
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ICommand ShowMainWindowCommand => new RelayCommand(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.Windows.OfType<MainView>().FirstOrDefault();
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                    App.TrayIcon.Visibility = Visibility.Collapsed;
                }
            });
        });

        public ICommand ExitApplicationCommand => new RelayCommand(() =>
        {
            Application.Current.Shutdown();
        });
    }

}
