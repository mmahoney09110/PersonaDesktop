using PersonaDesk.ViewModels;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WindowsAssistant.Services;

namespace PersonaDesk.Views
{
    public partial class MainView : Window
    {
        MainViewModel _viewModel;
        private bool loadingSTT = false;

        /// <summary>
        /// Initializes the main chat window.
        /// </summary>
        public MainView()
        {
            InitializeComponent();
            var vm = new ViewModels.MainViewModel();
            DataContext = vm;
            // sends welcome message
            vm.Start();
            // Subscribe to auto-scroll on new messages
            vm.OutputLog.CollectionChanged += OutputLog_CollectionChanged;
        }

        /// <summary>
        /// Position window at bottom-right corner of the screen on init.
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

        /// <summary>
        /// Scroll chat list to end when new items are added.
        /// </summary>
        private void OutputLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke(new Action(ScrollToEnd), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Handle Enter key to submit message.
        /// </summary>
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ((ViewModels.MainViewModel)DataContext).SubmitCommand.Execute(null);
            }
            this.Topmost = true;
        }

        /// <summary>
        /// Helper to scroll to last message in list.
        /// </summary>
        private void ScrollToEnd()
        {
            if (MessageList.Items.Count > 0)
            {
                MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
            }
        }

        private void MessageList_Loaded(object sender, RoutedEventArgs e) => ScrollToEnd();

        /// <summary>
        /// Hide window when minimized; show tray icon instead.
        /// </summary>
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                App.TrayIcon.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Set up hotkey listener and speech-to-text activation.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = SettingsService.LoadSettings();
            HotkeyService.RegisterHotkey(this, settings.Hotkey);
            HotkeyService.AttachHotkeyListener(this, () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var vm = DataContext as MainViewModel;
                    vm?.ShowMainWindowCommand.Execute(null);

                    var detector = new WakeWordDetector();
                    detector.SpeechRecognized += text =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            loadingSTT = false;

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                InputBox.Text = text;
                            }
                            else
                            {
                                InputBox.Text = string.Empty;
                                Console.WriteLine("No speech input — stopped listening.");
                            }
                        });
                    };
                    var audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "waitingSTT.wav");
                    if (audioPath != null)
                    {
                        var player = new System.Media.SoundPlayer(audioPath);
                        player.Play();
                    }
                    detector.StartTTS();
                    loadingSTT = true;
                    WaitingForSTT();
                });
            });
        }

        /// <summary>
        /// Animate "listening" text while waiting for speech-to-text input.
        /// </summary>
        private async void WaitingForSTT()
        {
            InputBox.Text = "listening";
            while (loadingSTT)
            {
                if (InputBox.Text == "listening. . . " || InputBox.Text.Length > 15)
                    InputBox.Text = "listening";
                InputBox.Text += ". ";
                await Task.Delay(1000);
            }
        }
    }
}
