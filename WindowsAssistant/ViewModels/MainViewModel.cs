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

        public ObservableCollection<string> OutputLog { get; set; }
        public ICommand SubmitCommand { get; set; }
        private CommandService _commandService;

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

        private void Start()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OutputLog.Add($"Persona:\nHow can I help you today?");

            });
        }

        private void ExecuteCommand()
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"Received command: {InputText}");

                    string loadingMessage = "Persona: ...";

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLog.Add($"You:\n{InputText}");
                        OutputLog.Add(loadingMessage); // Temporary spinner message
                    });


                    var result = await _commandService.HandleCommand(InputText).ConfigureAwait(false);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OutputLog.Remove(loadingMessage);
                        OutputLog.Add($"Persona:\n{result}");
                        InputText = string.Empty;
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

    }

}
