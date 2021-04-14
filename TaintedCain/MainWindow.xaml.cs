using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TaintedCain
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private (string name, (int id, float weight)[] items)[] ItemPools { get; set; }
		private Dictionary<int, int> ItemQualities { get; set; }
		
		private Dictionary<int, string> ItemNames { get; set; }
		private Dictionary<int, string> ItemDescriptions { get; set; }

		public static ObservableCollection<Pickup> PickupPool { get; set; } = new ObservableCollection<Pickup>();
		public static ObservableCollection<Item> Items { get; set; } = new ObservableCollection<Item>();

		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data\\";

		public MainWindow()
		{
			ItemPools =
				XElement.Load(DataFolder + "itempools.xml")
					.XPathSelectElements("Pool")
					.Select(e => (
						e.Attribute("Name").Value,
						e.Elements("Item").Select(x => (Convert.ToInt32(x.Attribute("Id").Value),
							Convert.ToSingle(x.Attribute("Weight").Value))).ToArray()))
					.ToArray();

			ItemQualities =
				XElement.Load(DataFolder + "items_metadata.xml")
					.XPathSelectElements("item")
					.ToDictionary(e => Convert.ToInt32(e.Attribute("id").Value),
						e => Convert.ToInt32(e.Attribute("quality").Value));

			ItemNames =
				XElement.Load(DataFolder + "items.xml")
					.Elements()
					.ToDictionary(e => Convert.ToInt32(e.Attribute("id").Value),
						e => e.Attribute("name").Value);
			
			ItemDescriptions =
				XElement.Load(DataFolder + "items.xml")
					.Elements()
					.ToDictionary(e => Convert.ToInt32(e.Attribute("id").Value),
						e => e.Attribute("description").Value);

			InitializeComponent();

			for (int i = 1; i <= Pickup.Names.Length; i++)
			{
				PickupPool.Add(new Pickup(i));
			}
			
			GetDefaultView(Items).SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
		}

		private void RecraftItems()
		{
			Items.Clear();

			List<Pickup> empty_recipe = new List<Pickup>();

			for (int i = 1; i <= Pickup.Names.Length; i++)
			{
				empty_recipe.Add(new Pickup(i));
			}

			RecraftItemsHelper(empty_recipe, 0, 0);
		}

		private void RecraftItemsHelper(List<Pickup> current_recipe, int pickup_index, int prev_length)
		{
			Pickup current_pickup = current_recipe[pickup_index];
			current_pickup.Amount = Math.Min(8 - prev_length, PickupPool[pickup_index].Amount);

			if (prev_length + current_pickup.Amount == 8)
			{
				AddCraft(current_recipe);

				current_pickup.Amount -= 1;
			}

			if (pickup_index == current_recipe.Count - 1)
			{
				return;
			}

			while (current_pickup.Amount >= 0)
			{
				RecraftItemsHelper(current_recipe, pickup_index + 1, prev_length + current_pickup.Amount);
				current_pickup.Amount -= 1;
			}

			current_pickup.Amount = 0;
		}

		private void AddCraft(List<Pickup> recipe)
		{
			List<Pickup> recipe_copy = new List<Pickup>();
			List<int> crafting_array = new List<int>();

			foreach (Pickup p in recipe)
			{
				if (p.Amount == 0) continue;

				recipe_copy.Add(new Pickup(p.Id, p.Amount));

				for (int i = 0; i < p.Amount; i++)
				{
					crafting_array.Add(p.Id);
				}
			}

			int item_id = Crafting.CalculateCrafting(crafting_array.ToArray(), ItemPools, ItemQualities);

			Item existing_item = Items.FirstOrDefault(i => i.Id == item_id);
			if (existing_item == null)
			{
				Item item = new Item(item_id, 
					ItemNames.GetValueOrDefault(item_id), 
					ItemDescriptions.GetValueOrDefault(item_id));
				item.Recipes.Add(recipe_copy);
				Items.Add(item);
			}
			else
			{
				existing_item.Recipes.Add(recipe_copy);
			}
		}

		private CollectionView GetDefaultView(object collection)
		{
			return (CollectionView) CollectionViewSource.GetDefaultView(collection);
		}

		private bool RecipeFilter(object item)
		{
			return ((Item) item).CanCraft(PickupPool);
		}

		public void OnPickupsChanged(object sender, DataTransferEventArgs e)
		{
			RecraftItems();
		}

		public void ViewItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Item item = (Item) e.Parameter;
			var window = new ItemViewWindow(item);
			window.ShowDialog();
			RecraftItems();
		}

		public void ClearPickups_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (Pickup p in PickupPool)
			{
				p.Amount = 0;
			}
			
			RecraftItems();
		}
	}

	public static class Commands
	{
		public static RoutedCommand ViewItem = new RoutedCommand("View Item", typeof(Commands));
		public static RoutedCommand CraftItem = new RoutedCommand("Craft Item", typeof(Commands));
		public static RoutedCommand ClearPickups = new RoutedCommand("Clear Pickups", typeof(Commands));
	}
}