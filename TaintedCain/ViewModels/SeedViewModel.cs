using System;
using System.Linq;

namespace TaintedCain.ViewModels
{
    public class SeedViewModel
    {
        public Action CloseAction { get; }
        public string Seed { get; set; } = "412";
        public bool DataSubmit { get; set; } = false;
        
        public RelayCommand Submit { get; set; }
        public RelayCommand Cancel { get; set; }
        
        public SeedViewModel(Action close_action)
        {
            CloseAction = close_action;

            Submit = new RelayCommand(() =>
            {
                DataSubmit = true;
                CloseAction();
            }, () => Seed.Length == 8 && Seed.All(char.IsLetterOrDigit));
            
            
            Cancel = new RelayCommand(() =>
            {
                DataSubmit = false;
                CloseAction();
            });
        }
    }
}