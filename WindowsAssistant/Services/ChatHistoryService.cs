using System.IO;
using System.Text.Json;

internal class ChatHistoryService
{
    private static readonly string HistoryFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PersonaDesk", "chat_history.json");

    public static List<MessageModel> LoadHistory()
    {
        if (!File.Exists(HistoryFilePath))
            return new List<MessageModel>();

        var json = File.ReadAllText(HistoryFilePath);
        return JsonSerializer.Deserialize<List<MessageModel>>(json) ?? new List<MessageModel>();
    }

    public static void SaveHistory(List<MessageModel> messages)
    {
        var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(HistoryFilePath, json);
    }
}
