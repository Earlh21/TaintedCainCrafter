using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace TaintedCain
{
	public partial class ItemViewWindow
	{
		public Item Item { get; set; }
		public List<List<Pickup>> DiscreteRecipes { get; set; } = new List<List<Pickup>>();
		public Tuple<Item, List<Pickup>> Result { get; private set; }
		
		public ItemViewWindow(Item item)
		{
			Item = item;

			//Display the recipe as 8 icons instead of using counters
			foreach (var recipe in item.Recipes)
			{
				var discrete_recipe = new List<Pickup>();

				foreach (var pickup in recipe)
				{
					for (int i = 0; i < pickup.Amount; i++)
					{
						discrete_recipe.Add(new Pickup(pickup.Id, 1));
					}
				}
				
				DiscreteRecipes.Add(discrete_recipe);
			}
			
			InitializeComponent();
		}
		
		public void CraftItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			List<Pickup> recipe = (List<Pickup>)e.Parameter;

			MainWindow.ItemManager.RemovePickups(recipe);

			Close();
		}

		public void PlanItem_OnExecute(object sender, ExecutedRoutedEventArgs e)
		{
			List<Pickup> recipe = (List<Pickup>)e.Parameter;
			
			MainWindow.ItemManager.RemovePickups(recipe);

			Result = new Tuple<Item, List<Pickup>>(Item, recipe);
			Close();
		}
	}
}