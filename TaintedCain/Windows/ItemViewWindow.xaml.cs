using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using TaintedCain.Models;
using TaintedCain.ViewModels;

namespace TaintedCain
{
	public partial class ItemViewWindow
	{
		public ItemViewWindow(Item item, ItemManager item_manager)
		{
			DataContext = new ItemViewModel(item, item_manager, Close);

			InitializeComponent();
		}
	}
}