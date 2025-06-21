namespace PersonaDesk.Services
{
    public class CommandService
    {
        public string HandleCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "No command entered.";

            if (input.ToLower().Contains("hello"))
                return "Hi there! How can I assist you?";

            return $"Unknown command: {input}";
        }
    }
}
