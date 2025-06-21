using System.Collections.ObjectModel;
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
            var result = _commandService.HandleCommand(InputText);
            OutputLog.Add($"> {InputText}");
            OutputLog.Add(result);
            InputText = string.Empty;
        }
    }
}
