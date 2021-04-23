using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;

namespace TaintedCain
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public static ItemManager ItemManager { get; } = new ItemManager();

		public static ObservableCollection<Tuple<Item, List<Pickup>>> PlannedRecipes { get; set; } =
			new ObservableCollection<Tuple<Item, List<Pickup>>>();
		public static ObservableCollection<Item> BlacklistedItems { get; } = new ObservableCollection<Item>();

		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
		private static readonly string BlacklistPath = DataFolder + "blacklist.json";

		public MainWindow()
		{


			if (File.Exists(BlacklistPath))
			{
				List<int> blacklisted_ids = JsonConvert.DeserializeObject<List<int>>(File.ReadAllText(BlacklistPath));

				foreach (int id in blacklisted_ids)
				{
					BlacklistedItems.Add(new Item(id, ItemManager.ItemNames[id], ItemManager.ItemDescriptions[id]));
				}
			}

			GetDefaultView(ItemManager.Items).Filter = ItemsFilter;
			GetDefaultView(ItemManager.Items).SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
			
			InitializeComponent();
		}

		private bool ItemsFilter(object obj)
		{
			Item item = (Item) obj;
			
			if (BlacklistedItems.Any(i => i.Id == item.Id))
			{
				return false;
			}
			
			if(!item.Name.ToLower().Contains(NameSearchBox.Text.Trim().ToLower()))
			{
				return false;
			}

			if (!item.Description.ToLower().Contains(DescriptionSearchBox.Text.Trim().ToLower()))
			{
				return false;
			}

			return true;
		}

		private CollectionView GetDefaultView(object collection)
		{
			return (CollectionView) CollectionViewSource.GetDefaultView(collection);
		}

		public void OnSearchChanged(object sender, TextChangedEventArgs e)
		{
			GetDefaultView(ItemManager.Items).Refresh();
		}

		public void ViewItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;
			var window = new ItemViewWindow(item);
			window.ShowDialog();

			if (window.Result != null)
			{
				PlannedRecipes.Add(window.Result);
			}
		}

		public void ClearPickups_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			ItemManager.Clear();
		}

		public void BlacklistItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;
			if (BlacklistedItems.All(i => i.Id != item.Id))
			{
				BlacklistedItems.Add(new Item(item.Id, item.Name, item.Description));
			}
			
			GetDefaultView(ItemManager.Items).Refresh();
		}

		public void ViewBlacklist_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var window = new BlacklistManagerWindow();
			window.ShowDialog();
			
			GetDefaultView(ItemManager.Items).Refresh();
		}

		public void ReleaseItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var planned = (Tuple<Item, List<Pickup>>) e.Parameter;
			List<Pickup> condensed_recipe = new List<Pickup>();

			//This recipe is discretized, so condense it to reduce crafting recalculation
			foreach (Pickup pickup in planned.Item2)
			{
				Pickup existing = condensed_recipe.FirstOrDefault(p => p.Id == pickup.Id);
				
				if (existing == null)
				{
					condensed_recipe.Add(new Pickup(pickup.Id, pickup.Amount));
				}
				else
				{
					existing.Amount += pickup.Amount;
				}
			}
			
			ItemManager.AddPickups(condensed_recipe);
			PlannedRecipes.Remove(planned);
		}

		public void ClearPlan_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			PlannedRecipes.Clear();
		}

		public void ClearPlan_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = PlannedRecipes.Count > 0;
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			List<int> blacklisted_ids = BlacklistedItems.Select(i => i.Id).ToList();
			File.WriteAllText(BlacklistPath, JsonConvert.SerializeObject(blacklisted_ids));
		}
		
		//Select all text when a pickup textbox is clicked
		private void ValueText_GotFocus(object sender, RoutedEventArgs e)
		{
			TextBox tb = (TextBox)e.OriginalSource;
			tb.Dispatcher.BeginInvoke(
				new Action(delegate
				{
					tb.SelectAll();
				}), System.Windows.Threading.DispatcherPriority.Input);
		}

		public void IncrementPickup_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Pickup pickup = (Pickup) e.Parameter;
			pickup.Amount++;
		}

		public void DecrementPickup_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Pickup pickup = (Pickup) e.Parameter;
			pickup.Amount--;
		}
	}

	public static class Commands
	{
		public static RoutedCommand ViewItem = new RoutedCommand("View Item", typeof(Commands));
		public static RoutedCommand CraftItem = new RoutedCommand("Craft Item", typeof(Commands));
		public static RoutedCommand ClearPickups = new RoutedCommand("Clear Pickups", typeof(Commands));
		public static RoutedCommand BlacklistItem = new RoutedCommand("Blacklist Item", typeof(Commands));
		public static RoutedCommand UnblacklistItem = new RoutedCommand("Unblacklist Item", typeof(Commands));
		public static RoutedCommand ViewBlacklist = new RoutedCommand("View Blacklist", typeof(Commands));
		public static RoutedCommand PlanItem = new RoutedCommand("Plan Item", typeof(Commands));
		public static RoutedCommand ReleaseItem = new RoutedCommand("Release Item", typeof(Commands));
		public static RoutedCommand ClearPlan = new RoutedCommand("Clear Plan", typeof(Commands));
		public static RoutedCommand IncrementPickup = new RoutedCommand("Increment Pickup", typeof(Commands));
		public static RoutedCommand DecrementPickup = new RoutedCommand("Decrement Pickup", typeof(Commands));
	}
}