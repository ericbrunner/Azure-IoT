using System;
using System.Windows.Input;

namespace Zone.IoT.App.ViewModels.Commands
{
    internal class DevkitCommand : ICommand
    {
        private readonly Action<object> _action;

        public DevkitCommand(Action<object> action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}