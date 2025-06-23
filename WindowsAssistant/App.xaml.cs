using Hardcodet.Wpf.TaskbarNotification;
using PersonaDesk.ViewModels;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;

namespace PersonaDesk
{
    public partial class App : Application
    {
        public static EmbeddingServiceHost EmbeddingService { get; private set; }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        public static TaskbarIcon TrayIcon { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            AllocConsole();

            base.OnStartup(e);

            TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
            TrayIcon.DataContext = new MainViewModel();

            try
            {
                string pythonExe = "py"; // or full path to python.exe
                string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services/embedding_service.py");

                Console.WriteLine($"Starting embedding service: {pythonExe} {scriptPath}");
                EmbeddingService = new EmbeddingServiceHost(pythonExe, scriptPath);
                EmbeddingService.Start();
                Console.WriteLine("Embedding service startinf successfully...");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to start embedding service: {ex}");
                MessageBox.Show($"Failed to start Assistant service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        public static void InitializePythonServer()
        {
            // Wait until the /status endpoint returns 200
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
                        return; // Ready!
                }
                catch
                {
                    // Ignore connection errors during warmup
                }

                Thread.Sleep(delayMs);
            }

            throw new Exception("Python server failed to start in time.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("Stopping embedding service...");
            TrayIcon.Dispose();
            EmbeddingService?.Dispose();
            Console.WriteLine("Embedding service stopped.");
            base.OnExit(e);
        }
    }
}
