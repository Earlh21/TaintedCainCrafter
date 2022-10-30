using System;
using System.Windows.Input;

namespace TaintedCain
{
    public class RelayCommand : ICommand
    {
        private Action execute;
        private Func<bool> can_execute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute;
            this.can_execute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            return this.can_execute == null || this.can_execute();
        }

        public void Execute(object parameter)
        {
            this.execute();
        }
    }

    public class RelayCommand<TParam> : ICommand
    {
        private Action<TParam> execute;
        private Func<TParam, bool> can_execute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<TParam> execute, Func<TParam, bool> canExecute = null)
        {
            this.execute = execute;
            this.can_execute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            return this.can_execute == null || this.can_execute((TParam)parameter);
        }

        public void Execute(object parameter)
        {
            this.execute((TParam)parameter);
        }
    }
}