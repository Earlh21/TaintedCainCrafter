using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace TaintedCain
{
	public partial class BlacklistManagerWindow : Window
	{
		public ObservableCollection<Item> Items { get; } = MainWindow.ItemManager.Items;

		public BlacklistManagerWindow()
		{
			InitializeComponent();
			
			
		}

		public void UnblacklistItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;

			item.IsBlacklisted = false;
		}

		private void ItemsFilter(object sender, FilterEventArgs e)
		{
			Item item = (Item) e.Item;
			e.Accepted = item.IsBlacklisted;
		}
	}
}