public class SettingsModel
{
    public string AssistantName { get; set; } = "Persona";
    public string PersonalityPrompt { get; set; } = "Friendly assistant.";
    public string Hotkey { get; set; } = "Ctrl+Shift+P";
    public bool SpeechEnabled { get; set; } = false;
    public string SpeechVoice { get; set; } = "Microsoft David";
}
