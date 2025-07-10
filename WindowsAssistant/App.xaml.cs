using Hardcodet.Wpf.TaskbarNotification; // For system tray icon support
using PersonaDesk.ViewModels;
using PersonaDesk.Views;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;

namespace PersonaDesk
{
    /// <summary>
    /// Main application entry point (App.xaml.cs).
    /// Manages startup logic, global services, and graceful shutdown.
    /// </summary>
    public partial class App : Application
    {
        // Embedding service instance for local embeddings
        public static EmbeddingServiceHost EmbeddingService { get; private set; }

        // Import Win32 API to allocate a console window (for logs)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        // Tray icon reference to allow control during app lifecycle
        public static TaskbarIcon TrayIcon { get; private set; }

        private readonly SettingsModel _settings = SettingsService.LoadSettings();

        /// <summary>
        /// Main startup logic for WPF app.
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Create console window for debugging/logging. This is optional and can be removed in production.
            //AllocConsole();

            base.OnStartup(e);

            // Create and show main window manually
            var mainWindow = new MainView();
            mainWindow.Show();

            // Start Python-based embedding service
            try
            {
                string pythonExe = "py"; // Could be explicit path if needed

                Console.WriteLine($"Starting embedding service");
                EmbeddingService = new EmbeddingServiceHost();
                EmbeddingService.Start();
                Console.WriteLine("Embedding service started successfully...");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to start embedding service: {ex}");
                MessageBox.Show($"Failed to start Assistant service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // Initialize and bind tray icon
            TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
            TrayIcon.DataContext = new MainViewModel();
        }

        /// <summary>
        /// Utility function to wait until local Python service is ready.
        /// </summary>
        public static async Task InitializePythonServer()
        {
            using var httpClient = new HttpClient();
            const string statusUrl = "http://localhost:8000/status";
            const int maxRetries = 60;
            const int delayMs = 500;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = httpClient.GetAsync(statusUrl).Result;
                    if (response.IsSuccessStatusCode)
                        return; // Server ready
                }
                catch
                {
                    // Ignore errors while warming up
                }

                Thread.Sleep(delayMs);
            }

            throw new Exception("Python server failed to start in time.");
        }

        /// <summary>
        /// Cleanup logic when application exits.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("Stopping embedding service...");
            TrayIcon.Dispose();
            EmbeddingService?.Dispose();
            HotkeyService.UnregisterHotkey(Application.Current.MainWindow);
            Console.WriteLine("Embedding service stopped.");

            base.OnExit(e);
        }
    }
}
