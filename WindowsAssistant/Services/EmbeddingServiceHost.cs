using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class EmbeddingServiceHost : IDisposable
{
    private Process _process;
    private readonly string _exePath;
    private readonly HttpClient _httpClient;

    public EmbeddingServiceHost(string exeName = "embedding_service.exe")
    {
        // assume exe is in your app's base directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _exePath = Path.Combine(baseDir, exeName);
        _httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8000") };
    }

    public void Start()
    {
        if (_process != null && !_process.HasExited)
            return;

        if (!File.Exists(_exePath))
            throw new FileNotFoundException("Embedding service executable not found", _exePath);

        var startInfo = new ProcessStartInfo
        {
            FileName = _exePath,
            Arguments = "",               
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(_exePath)
        };

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("[EmbedSvc] " + e.Data); };
        _process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.Error.WriteLine("[EmbedSvc ERR] " + e.Data); };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
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
        return result?.embedding;
    }

    public void Dispose()
    {
        try
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill(true);
                _process.WaitForExit();
            }
                
        }
        catch { /* ignore */ }
        _httpClient.Dispose();
    }
}

public class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] embedding { get; set; }
}
