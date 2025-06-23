using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class EmbeddingServiceHost : IDisposable
{
    private Process _pythonProcess;
    private readonly string _pythonExePath;
    private readonly string _scriptPath;
    private readonly HttpClient _httpClient;

    public EmbeddingServiceHost(string pythonExePath, string scriptPath)
    {
        _pythonExePath = pythonExePath;
        _scriptPath = scriptPath;
        _httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8000") };
    }

    public void Start()
    {
        if (_pythonProcess != null && !_pythonProcess.HasExited)
            return;

        var startInfo = new ProcessStartInfo
        {
            FileName = _pythonExePath,
            Arguments = _scriptPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _pythonProcess = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        // Subscribe to output events
        _pythonProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine($"[Python STDOUT] {e.Data}");
        };

        _pythonProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.Error.WriteLine($"[Python STDERR] {e.Data}");
        };

        _pythonProcess.Start();

        // Begin async read of output streams
        _pythonProcess.BeginOutputReadLine();
        _pythonProcess.BeginErrorReadLine();
    }

    public async Task<float[]> GetEmbeddingAsync(string inputText)
    {
        var payload = new { text = inputText };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/embed", content);

        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<EmbeddingResponse>(raw);
        return result?.embedding?.ToArray();
    }

    public void Dispose()
    {
        try
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                _pythonProcess.Kill();
                _pythonProcess.Dispose();
            }
        }
        catch { }
        _httpClient.Dispose();
    }
}

public class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] embedding { get; set; }
}