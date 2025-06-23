using System.IO;
using System.Text.Json;

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PersonaDesk",
        "settings.json");

    public static SettingsModel LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
            }
        }
        catch { }
        return new SettingsModel();
    }

    public static void SaveSettings(SettingsModel settings)
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
