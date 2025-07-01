using System.Windows;
using System.Windows.Input;
using System.Collections.Specialized;
using PersonaDesk.ViewModels;
using WindowsAssistant.Services;
using System.IO;

namespace PersonaDesk.Views
{
    public partial class MainView : Window
    {
        MainViewModel _viewModel;
        private bool loadingSTT = false;

        public MainView()
        {
            InitializeComponent();
            var vm = new ViewModels.MainViewModel();
            DataContext = vm;
            vm.Start();
            // Auto-scroll when OutputLog changes
            vm.OutputLog.CollectionChanged += OutputLog_CollectionChanged;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

        private void OutputLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Only scroll when items are added
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Dispatcher to ensure we’re on UI thread and after the item is rendered
                Dispatcher.BeginInvoke(new Action(ScrollToEnd), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ((ViewModels.MainViewModel)DataContext).SubmitCommand.Execute(null);
            }
            this.Topmost = true; // Ensure the window stays on top when input is focused
        }

        private void ScrollToEnd()
        {
            if (MessageList.Items.Count > 0)
            {
                MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
            }
        }

        private void MessageList_Loaded(object sender, RoutedEventArgs e) => ScrollToEnd();

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                App.TrayIcon.Visibility = Visibility.Visible;
            }
        }

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
                                //((ViewModels.MainViewModel)DataContext).SubmitCommand.Execute(null);
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
