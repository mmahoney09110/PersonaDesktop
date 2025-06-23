using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PersonaDesk.Helpers;
using PersonaDesk.Services;

namespace PersonaDesk.ViewModels
{
    public class MainViewModel
    {
        public string InputText { get; set; }
        public ObservableCollection<string> OutputLog { get; set; }
        public ICommand SubmitCommand { get; set; }

        private CommandService _commandService;

        public MainViewModel()
        {
            OutputLog = new ObservableCollection<string>();
            SubmitCommand = new RelayCommand(ExecuteCommand);
            _commandService = new CommandService();
        }

        private void ExecuteCommand()
        {
            Task.Run(async () =>
            {
                var result = await _commandService.HandleCommand(InputText)
                                                   .ConfigureAwait(false);
                // Since you need to update OutputLog on the UI thread:
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OutputLog.Add($"You: {InputText}");
                    OutputLog.Add($"Persona: {result}");
                    InputText = string.Empty;
                });
                Console.WriteLine($"Command executed:");
            });
        }
    }
}
