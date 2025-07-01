using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

public class PersonaResponse
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _clientIp;

    public PersonaResponse()
    {
        _serverUrl = "https://openai-proxy-server-vo9f.onrender.com/persona/response";
        _clientIp = GetLocalIPAddress();
        Console.WriteLine($"Using client IP: {_clientIp}");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Forwarded-For", _clientIp);
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            // Pick the first IPv4 address that is not a loopback
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                return ip.ToString();
        }

        return "127.0.0.1"; // Fallback if no suitable address found
    }

    public async Task<string> SendAsync(string systemPrompt, string message)
    {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("system", systemPrompt),
                new KeyValuePair<string, string>("message", message)
            });

            var response = await _httpClient.PostAsync(_serverUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
    }
}
