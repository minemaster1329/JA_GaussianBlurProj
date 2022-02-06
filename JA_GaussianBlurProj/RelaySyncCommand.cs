using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace JA_GaussianBlurProj
{
    public delegate void ExecuteMethod(object parameter);
    public delegate bool CanExecuteMethod(object parameter);
    public class RelaySyncCommand : ICommand
    {
        private readonly ExecuteMethod _execute;
        private readonly CanExecuteMethod _canExecute;

        public RelaySyncCommand(ExecuteMethod execute, CanExecuteMethod canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute is null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke(parameter);
        }

        private RelaySyncCommand(){}

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
