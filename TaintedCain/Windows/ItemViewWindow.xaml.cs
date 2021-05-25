using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace TaintedCain
{
	public partial class ItemViewWindow
	{
		public Item Item { get; set; }
		public Tuple<Item, List<Pickup>> Result { get; private set; }

		private List<Pickup> blacklisted_pickups = new List<Pickup>();
		
		public ItemViewWindow(Item item)
		{
			Item = item;

			InitializeComponent();
		}

		private void RecipeFilter(object sender, FilterEventArgs e)
		{
			Recipe recipe = (Recipe) e.Item;

			foreach (var pickup in blacklisted_pickups)
			{
				if (recipe.Pickups.Any(p => p.Id == pickup.Id))
				{
					e.Accepted = false;
					return;
				}
			}

			e.Accepted = true;
		}
		
		public void CraftItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var recipe = (Recipe)e.Parameter;

			MainWindow.ItemManager.RemovePickups(recipe.Pickups);

			Close();
		}

		public void PlanItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			List<Pickup> recipe = (List<Pickup>)e.Parameter;
			
			MainWindow.ItemManager.RemovePickups(recipe);

			Result = new Tuple<Item, List<Pickup>>(Item, recipe);
			Close();
		}

		public void BlacklistPickup_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			Pickup pickup = (Pickup) e.Parameter;
			blacklisted_pickups.Add(pickup.Copy());
			
			((CollectionViewSource) Resources["RecipesView"]).View.Refresh();
		}
	}
}