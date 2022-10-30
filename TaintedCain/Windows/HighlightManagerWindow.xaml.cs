using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TaintedCain.ViewModels;
using System.Collections.Generic;
using TaintedCain.Models;

namespace TaintedCain
{
	public partial class HighlightManagerWindow
	{
		public HighlightManagerWindow(ICollection<Item> items)
		{
			DataContext = new HighlighterViewModel(items);

			InitializeComponent();
		}
	}
}