using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TaintedCain.ViewModels
{
    internal class ModsPathViewModel : ViewModelBase
    {
        private string mods_path = "";

        public Action CloseAction;
        public string ModsPath
        {
            get => mods_path;
            set
            {
                mods_path = value;
                NotifyPropertyChanged("ModsPath");
            }
        }
        public bool DataSubmit { get; set; } = false;

        public RelayCommand Browse { get; set; }
        public RelayCommand Submit { get; set; }
        public RelayCommand Cancel { get; set; }

        public ModsPathViewModel(string current_path, Action close_action)
        {
            ModsPath = current_path;
            CloseAction = close_action;

            Browse = new RelayCommand(() =>
            {
                var dialog = new FolderBrowserDialog();

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ModsPath = dialog.SelectedPath;
                }
            });

            Submit = new RelayCommand(() =>
            {
                if (ModsPath == "")
                {
                    ModsPath = null;
                }

                DataSubmit = true;
                CloseAction();
            }, () => Directory.Exists(ModsPath) || ModsPath == String.Empty);

            Cancel = new RelayCommand(() =>
            {
                DataSubmit = false;
                CloseAction();
            });
        }
    }
}
