using System;
using System.Linq;

namespace TaintedCain.ViewModels
{
    public class SeedViewModel
    {
        public EventHandler OnRequestClose;
        public string Seed { get; set; } = "412";
        public bool DataSubmit { get; set; } = false;
        
        public RelayCommand Submit { get; set; }
        public RelayCommand Cancel { get; set; }
        
        public SeedViewModel()
        {
            Submit = new RelayCommand((sender) =>
            {
                DataSubmit = true;
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }, sender => Seed is {Length: 8} && Seed.All(char.IsLetterOrDigit));
            
            
            Cancel = new RelayCommand((sender) =>
            {
                DataSubmit = false;
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}