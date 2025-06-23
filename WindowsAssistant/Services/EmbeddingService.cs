using System.Diagnostics;
using System.Text.Json;

public static class EmbeddingService
{
    public static async Task<float[]> GetEmbeddingFromPython(string inputText)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "py",
            Arguments = $"Services\\embed.py \"{inputText}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);
        string output = await process.StandardOutput.ReadToEndAsync();
        string errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Python error: {errors}");
        }

        float[]? floats = JsonSerializer.Deserialize<float[]>(output);

        if (floats == null)
        {
            throw new Exception("Deserialization returned null.");
        }

        return floats;
    }
}
