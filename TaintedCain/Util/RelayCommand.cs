using System;
using System.Windows.Input;

namespace TaintedCain
{
    public class RelayCommand : ICommand    
    {    
        private Action<object> execute;    
        private Func<object, bool> can_execute;    
     
        public event EventHandler CanExecuteChanged    
        {    
            add { CommandManager.RequerySuggested += value; }    
            remove { CommandManager.RequerySuggested -= value; }    
        }    
     
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)    
        {    
            this.execute = execute;    
            this.can_execute = canExecute;    
        }    
     
        public bool CanExecute(object parameter)    
        {    
            return this.can_execute == null || this.can_execute(parameter);    
        }    
     
        public void Execute(object parameter)    
        {    
            this.execute(parameter);    
        }    
    }  
}