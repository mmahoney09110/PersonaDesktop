using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace PersonaDesk
{
    public partial class App : Application
    {
        public static EmbeddingServiceHost EmbeddingService { get; private set; }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        protected override void OnStartup(StartupEventArgs e)
        {
            AllocConsole();
            Console.WriteLine("Console allocated for debugging.");
            Console.WriteLine("Console is working.");

            base.OnStartup(e);

            try
            {
                string pythonExe = "py"; // or full path to python.exe
                string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services/embedding_service.py");

                Console.WriteLine($"Starting embedding service: {pythonExe} {scriptPath}");
                EmbeddingService = new EmbeddingServiceHost(pythonExe, scriptPath);
                EmbeddingService.Start();
                Console.WriteLine("Embedding service started successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to start embedding service: {ex}");
                // You might want to show a message box or log this somewhere more visible
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Console.WriteLine("Stopping embedding service...");
            EmbeddingService?.Dispose();
            Console.WriteLine("Embedding service stopped.");
            base.OnExit(e);
        }
    }
}
