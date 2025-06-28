using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WindowsAssistant.Services
{
    public class TtsService
    {
        private readonly string _scriptPath;
        private readonly string _venvDir;
        private readonly string _venvPython;
        

        public TtsService(string scriptRelativePath = "Services/generate_tts.py")
        {
            var baseDir = Path.GetDirectoryName(scriptRelativePath);
            _venvDir = Path.Combine(baseDir, "embedding_env");
            _venvPython = Path.Combine(_venvDir, "Scripts", "python.exe");
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptRelativePath);
        }

        public async Task<string?> GenerateSpeechAsync(string text, string outputFile = "output.wav")
        {
            if (!File.Exists(_scriptPath))
            {
                Console.Error.WriteLine("TTS script not found: " + _scriptPath);
                return null;
            }

            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, outputFile);
            string quotedText = $"\"{text}\"";
            string quotedOutput = $"\"{outputPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _venvPython,
                Arguments = $"{_scriptPath} {quotedText} {quotedOutput}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                    throw new Exception("Failed to start TTS process.");

                string stdOut = await process.StandardOutput.ReadToEndAsync();
                string stdErr = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                Console.WriteLine("[TTS STDOUT]: " + stdOut);
                if (!string.IsNullOrWhiteSpace(stdErr))
                    Console.WriteLine("[TTS STDERR]: " + stdErr);

                return File.Exists(outputPath) ? outputPath : null;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("TTS Error: " + ex);
                return null;
            }
        }
    }
}
