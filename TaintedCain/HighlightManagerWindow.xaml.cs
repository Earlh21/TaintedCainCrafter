using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace TaintedCain
{
	public partial class HighlightManagerWindow
	{
		private string filter_name = "";
		private string filter_description = "";
		
		public ObservableCollection<Item> Items { get; } = MainWindow.ItemManager.Items;

		public static Color[] AvailableColors { get; } =
		{
			Color.FromArgb(0, 0, 0, 0),
			Color.FromArgb(100, 255, 0, 0),
			Color.FromArgb(100, 0, 255, 0),
			Color.FromArgb(100, 0, 0, 255),
		};

		private static Color Highlighter { get; set; }
		
		public string FilterName
		{
			get => filter_name;
			set
			{
				filter_name = value;
				((CollectionViewSource)Resources["ItemsView"]).View.Refresh();
			}
		}
		
		public string FilterDescription
		{
			get => filter_description;
			set
			{
				filter_description = value;
				((CollectionViewSource)Resources["ItemsView"]).View.Refresh();
			}
		}

		public HighlightManagerWindow()
		{
			InitializeComponent();
		}

		public void SetItemHighlight_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;

			item.HighlightColor = Highlighter;
		}

		public void SetHighlighter_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Highlighter = (Color) e.Parameter;
		}

		private void ItemFilter(object sender, FilterEventArgs e)
		{
			Item item = (Item) e.Item;

			if (!item.Name.ToLower().Contains(FilterName.Trim().ToLower()))
			{
				e.Accepted = false;
				return;
			}

			if (!item.Description.ToLower().Contains(FilterDescription.Trim().ToLower()))
			{
				e.Accepted = false;
				return;
			}

			e.Accepted = true;
		}
	}
}