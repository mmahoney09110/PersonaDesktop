using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;

namespace WindowsAssistant.Services
{
    public class TtsService
    {
        private readonly string _scriptPath;
        private readonly string _venvDir;
        private readonly string _venvPython;
        SettingsModel _setting;

        public TtsService(string scriptRelativePath = "Services/generate_tts.py")
        {
            var baseDir = Path.GetDirectoryName(scriptRelativePath);
            _venvDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "embedding_env");
            _venvPython = Path.Combine(_venvDir, "Scripts", "python.exe");
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptRelativePath);
        }

        public async Task GenerateSpeechAsync(string text, string outputFile = "output.wav")
        {
            var synth = new SpeechSynthesizer();
            _setting = SettingsService.LoadSettings();
            Console.WriteLine("Available voices:");
            foreach (var voice in SpeechSynthesizer.AllVoices)
            {
                Console.WriteLine($"Name: {voice.DisplayName}, Gender: {voice.Gender}, Language: {voice.Language}");
            }

            // Choose a specific voice
            var selectedVoice = SpeechSynthesizer.AllVoices
                .FirstOrDefault(v => v.DisplayName.Contains(_setting.SpeechVoice));
            if (selectedVoice != null)
            {
                synth.Voice = selectedVoice;
            }

            var stream = await synth.SynthesizeTextToStreamAsync(text);

            using (var fileStream = File.Create("output.wav"))
            {
                await stream.AsStream().CopyToAsync(fileStream);
            }

            Console.WriteLine("Saved to output.wav!");
        }
    }
}
