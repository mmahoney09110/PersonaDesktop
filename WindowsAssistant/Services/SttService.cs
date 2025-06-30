using System.IO;
using System.Speech.Recognition;
using Pv;
using NAudio.Wave;

namespace WindowsAssistant.Services
{
    public class WakeWordDetector : IDisposable
    {
        private readonly Porcupine _porcupine;
        private readonly WaveInEvent _waveIn;
        private readonly int _sampleRate;
        private readonly int _frameLength;
        private System.Timers.Timer? _timeoutTimer;


        public event EventHandler? WakeWordDetected;

        private SpeechRecognitionEngine _recognizer;

        public event Action<string>? SpeechRecognized;

        public WakeWordDetector(string keywordFilePath, string accessKey)
        {
            if (!File.Exists(keywordFilePath))
                throw new FileNotFoundException("Keyword file not found", keywordFilePath);

            // assign to the field, not a local variable
            _porcupine = Porcupine.FromKeywordPaths(
                accessKey,
                new[] { keywordFilePath }
            );

            _sampleRate = _porcupine.SampleRate;
            _frameLength = _porcupine.FrameLength;

            // List available audio devices for debugging
            Console.WriteLine("Available input devices:");
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                Console.WriteLine($"  [{i}] {caps.ProductName}");
            }

            // You can change DeviceNumber if it’s not the default
            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(_sampleRate, 16, 1),
                BufferMilliseconds = (int)(_frameLength / (double)_sampleRate * 1000)
            };

            _waveIn.DataAvailable += WaveIn_DataAvailable;

            // Use the default system recognizer for en-US
            _recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));

            // Load a default dictation grammar (so it recognizes free speech)
            _recognizer.LoadGrammar(new DictationGrammar());

            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
        }

        public void Start()
        {
            Console.WriteLine("Starting audio capture...");
            _waveIn.StartRecording();
        }

        public void Stop()
        {
            Console.WriteLine("Stopping audio capture...");
            _waveIn.StopRecording();
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // ensure we have exactly frameLength samples
            if (e.BytesRecorded < _frameLength * 2) return;

            // convert bytes to shorts
            var pcm = new short[_frameLength];
            for (int i = 0; i < _frameLength; i++)
                pcm[i] = BitConverter.ToInt16(e.Buffer, i * 2);

            try
            {
                int result = _porcupine.Process(pcm);
                if (result >= 0)
                {
                    Console.WriteLine("Porcupine detected keyword index: " + result);
                    WakeWordDetected?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Porcupine error: " + ex);
            }
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
            _porcupine?.Dispose();
        }

        public void StartTTS()
        {
            Console.WriteLine("Listening for your speech...");

            // Always remove previous handlers to avoid duplicate firing
            _recognizer.RecognizeCompleted -= Recognizer_RecognizeCompleted;
            _recognizer.SpeechRecognized -= Recognizer_SpeechRecognized;

            _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
            _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;

            _timeoutTimer = new System.Timers.Timer(5000); // 5 seconds
            _timeoutTimer.Elapsed += TimeoutElapsed;
            _timeoutTimer.AutoReset = false;
            _timeoutTimer.Start();

            _recognizer.RecognizeAsync(RecognizeMode.Single);
        }

        private void TimeoutElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("No speech detected after 5 seconds. Stopping recognition.");
            _recognizer.RecognizeAsyncCancel();
            _timeoutTimer?.Stop();

            // Fire empty recognized event to stop dots
            SpeechRecognized?.Invoke(string.Empty);
        }


        private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Speech recognized: " + e.Result.Text);
            _timeoutTimer?.Stop();

            SpeechRecognized?.Invoke(e.Result.Text);
        }

        private void Recognizer_RecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            Console.WriteLine("Recognition completed.");

            // Always stop timer on completion
            _timeoutTimer?.Stop();
        }
    }
}
