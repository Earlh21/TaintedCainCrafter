using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace TaintedCain
{
	public partial class BlacklistManagerWindow : Window
	{
		public ObservableCollection<Item> BlacklistedItems { get; }
		
		public BlacklistManagerWindow()
		{
			BlacklistedItems = MainWindow.BlacklistedItems;
			
			InitializeComponent();
		}

		public void UnblacklistItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;

			BlacklistedItems.Remove(item);
		}
	}
}