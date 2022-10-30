using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using TaintedCain.Models;
using TaintedCain.ViewModels;

namespace TaintedCain
{
	public partial class BlacklistManagerWindow
	{
		public BlacklistManagerWindow(ICollection<Item> items)
		{
			DataContext = new BlacklistViewModel(items);

			InitializeComponent();
		}
	}
}