using PersonaDesk.Helpers;
using PersonaDesk.Services;
using PersonaDesk.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WindowsAssistant.Services;

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

        private bool loadingSTT = false;
        public ObservableCollection<string> OutputLog { get; set; }
        public ICommand SubmitCommand { get; set; }
        private CommandService _commandService;
        private SettingsModel _settings = SettingsService.LoadSettings();
        private List<MessageModel> _chatHistory;

        public MainViewModel()
        {
            OutputLog = new ObservableCollection<string>();
            SubmitCommand = new RelayCommand(ExecuteCommand);
            _commandService = new CommandService();
            _chatHistory = ChatHistoryService.LoadHistory();

            Task.Run(() =>
            {
                try
                {
                    App.InitializePythonServer(); // the method that pings /status
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => IsLoading = false);
                }
            });
        }

        public async Task Start()
        {
            string loadingMessage = _settings.AssistantName;
            Application.Current.Dispatcher.Invoke(() => OutputLog.Add(loadingMessage)); // Add on UI thread
            Console.WriteLine("Starting Persona...");
            var personaWelcome = await _commandService.PersonaResponse(
                "they just opened the program, give a greeting and let them know you are here to open a folder, browse the web, or just chat. They can do this by either typing or saying 'Persona'.",
                "Enter a command, or say Persona to get started. What can I help with today?"
            );

            Application.Current.Dispatcher.Invoke(() =>
            {
                OutputLog.Remove(loadingMessage); // Remove using exact instance
                OutputLog.Add(_settings.AssistantName+ ":\n" + personaWelcome);
            });

            string keywordPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Keywords",
                "Persona_en_windows_v3_0_0.ppn"
            );

            if (!File.Exists(keywordPath))
            {
                Console.Error.WriteLine($"Keyword file not found at: {keywordPath}");
                return;
            }

            var detector = new WakeWordDetector(
                keywordFilePath: keywordPath,
                accessKey: "yhsEk1mxHmS+FODacs/RRFELy9HpNPC5tWtY1sh0zAvwUBaRwY1sbA=="
            );


            detector.SpeechRecognized += text =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    loadingSTT = false;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        InputText = text;
                        ExecuteCommand();
                    }
                    else
                    {
                        InputText = string.Empty;
                        Console.WriteLine("No speech input — stopped listening.");
                    }
                });
            };

            detector.WakeWordDetected += (s, e) =>
            {
                Console.WriteLine("Wake word detected!");
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
                var audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "waitingSTT.wav");
                if (audioPath != null)
                {
                    var player = new System.Media.SoundPlayer(audioPath);
                    player.PlaySync();
                }
                detector.StartTTS();
                loadingSTT = true;
                WaitingForSTT();
            };

            detector.Start();

        }

        private async void WaitingForSTT()
        {
            InputText = "listening";
            while (loadingSTT)
            {
                if (InputText == "listening. . . " || InputText.Length>15)
                    InputText = "listening";
                InputText += ". ";
                await Task.Delay(1000);
            }
        }

        private void ExecuteCommand()
        {
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"Received command: {InputText}");
                    var audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "sent.wav");
                    if (audioPath != null)
                    {
                        var player = new System.Media.SoundPlayer(audioPath);
                        player.Play();
                    }
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

                    // Add messages to history
                    _chatHistory.Add(new MessageModel
                    {
                        Role = "user",
                        Content = input,
                        Timestamp = DateTime.Now
                    });
                    _chatHistory.Add(new MessageModel
                    {
                        Role = "assistant",
                        Content = result,
                        Timestamp = DateTime.Now
                    });
                    ChatHistoryService.SaveHistory(_chatHistory);
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
