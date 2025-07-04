﻿using Hardcodet.Wpf.TaskbarNotification;
using PersonaDesk.ViewModels;
using PersonaDesk.Views;
using System;
using System.Net.Http;
using System.Runtime;
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

        private readonly SettingsModel _settings = SettingsService.LoadSettings();

        protected override async void OnStartup(StartupEventArgs e)
        {
            AllocConsole();

            base.OnStartup(e);

            var mainWindow = new MainView();
            mainWindow.Show();

            try
            {
                string pythonExe = "py"; // or full path to python.exe

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
            TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
            TrayIcon.DataContext = new MainViewModel();
        }

        public static async Task InitializePythonServer()
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
            HotkeyService.UnregisterHotkey(Application.Current.MainWindow);
            Console.WriteLine("Embedding service stopped.");
            base.OnExit(e);
        }

        private void TaskbarIcon_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }
    }
}
