using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TaintedCain.ViewModels;

namespace TaintedCain.Windows
{
    /// <summary>
    /// Interaction logic for ModsPathWindow.xaml
    /// </summary>
    public partial class ModsPathWindow : Window
    {
        public ModsPathWindow(string? current_path)
        {
            DataContext = new ModsPathViewModel(current_path ?? "", Close);

            InitializeComponent();
        }
    }
}
