using PersonaDesk.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

public class EmbeddingServiceHost : IDisposable
{
    private Process _pythonProcess;
    private readonly string _scriptPath;
    private readonly string _venvDir;
    private readonly string _venvPython;
    private readonly string _setupScript;
    private readonly HttpClient _httpClient;

    public EmbeddingServiceHost(string scriptPath)
    {
        _scriptPath = scriptPath;
        var baseDir = Path.GetDirectoryName(scriptPath);
        _venvDir = Path.Combine(baseDir, "embedding_env");
        _setupScript = Path.Combine(baseDir, "setup_embedding_env.py");
        _venvPython = Path.Combine(_venvDir, "Scripts", "python.exe");
        _httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8000") };
    }

    public void Start()
    {
        Console.WriteLine("[Host] Ensuring virtual environment...");
        EnsureVirtualEnv();

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            Console.WriteLine("[Host] Python process already running.");
            return;
        }

        Console.WriteLine($"[Host] Starting embedding service using: {_venvPython}");

        var startInfo = new ProcessStartInfo
        {
            FileName = _venvPython,
            Arguments = $"\"{_scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _pythonProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _pythonProcess.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine("[Python STDOUT] " + e.Data);
        };
        _pythonProcess.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.Error.WriteLine("[Python STDERR] " + e.Data);
        };

        bool started = _pythonProcess.Start();
        Console.WriteLine($"[Host] Python script launched: {started}");

        _pythonProcess.BeginOutputReadLine();
        _pythonProcess.BeginErrorReadLine();
    }

    private void EnsureVirtualEnv()
    {
        Console.WriteLine($"[Host] Running setup script: {_setupScript}");
        var dialog = new QuickDialog("Checking and loading dependencies. This may take a few moments...");
        dialog.Owner = Application.Current.MainWindow; // Makes it center over your main app
        dialog.Show();

        var startInfo = new ProcessStartInfo
        {
            FileName = "py", // Or full path to python.exe
            Arguments = $"\"{Path.GetFileName(_setupScript)}\"",
            WorkingDirectory = Path.GetDirectoryName(_scriptPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var setupProcess = new Process { StartInfo = startInfo })
        {
            setupProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine("[Setup STDOUT] " + e.Data);
            };
            setupProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.Error.WriteLine("[Setup STDERR] " + e.Data);
            };

            setupProcess.Start();
            setupProcess.BeginOutputReadLine();
            setupProcess.BeginErrorReadLine();

            setupProcess.WaitForExit();

            Console.WriteLine($"[Host] Setup script exited with code: {setupProcess.ExitCode}");
            dialog.Close();

            if (setupProcess.ExitCode != 0)
                throw new Exception("Failed to set up Python environment. See logs for details.");
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string inputText)
    {
        Console.WriteLine("[Host] Sending request for embedding...");
        var payload = new { text = inputText };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/embed", content);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EmbeddingResponse>(raw);

        Console.WriteLine("[Host] Embedding response received.");
        return result?.embedding?.ToArray();
    }

    public void Dispose()
    {
        try
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                Console.WriteLine("[Host] Killing Python process.");
                _pythonProcess.Kill();
                _pythonProcess.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[Dispose Error] " + ex.Message);
        }

        _httpClient.Dispose();
    }
}

public class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] embedding { get; set; }
}
