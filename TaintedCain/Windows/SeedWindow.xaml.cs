using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaintedCain.ViewModels;

namespace TaintedCain.Windows
{
    public partial class SeedWindow : Window
    {
        public SeedWindow()
        {
            DataContext = new SeedViewModel(Close);

            InitializeComponent();
        }

        private void Filter_TextChanged(object sender, EventArgs e)
        {
            var textboxSender = (TextBox)sender;
            var cursorPosition = textboxSender.SelectionStart;
            textboxSender.Text = Regex.Replace(textboxSender.Text, "[^0-9a-zA-Z ]", "");
            textboxSender.SelectionStart = cursorPosition;
        }
    }
}