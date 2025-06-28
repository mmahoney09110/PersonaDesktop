using System;
using System.IO;
using System.Threading;
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

        public event EventHandler? WakeWordDetected;

        public WakeWordDetector()
        {
            if (!File.Exists("Persona_en_windows_v3_0_0.ppn"))
                throw new FileNotFoundException("Keyword file not found");

            Porcupine porcupine = Porcupine.FromKeywordPaths(
                "yhsEk1mxHmS+FODacs/RRFELy9HpNPC5tWtY1sh0zAvwUBaRwY1sbA==",
                new List<string> { "Persona_en_windows_v3_0_0.ppn" });
            _sampleRate = _porcupine.SampleRate;
            _frameLength = _porcupine.FrameLength;

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(_sampleRate, 16, 1),
                BufferMilliseconds = (int)((float)_frameLength / _sampleRate * 1000.0f)
            };

            _waveIn.DataAvailable += WaveIn_DataAvailable;
        }

        public void Start()
        {
            _waveIn.StartRecording();
        }

        public void Stop()
        {
            _waveIn.StopRecording();
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // Porcupine expects 16-bit PCM audio samples as short[]
            int samplesRequired = _frameLength;
            if (e.BytesRecorded < samplesRequired * 2) return; // Not enough data yet

            // Convert byte[] to short[] (PCM 16-bit)
            short[] pcm = new short[samplesRequired];
            for (int i = 0; i < samplesRequired; i++)
            {
                pcm[i] = BitConverter.ToInt16(e.Buffer, i * 2);
            }

            try
            {
                int keywordIndex = _porcupine.Process(pcm);
                if (keywordIndex == 0)
                {
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
    }
}
