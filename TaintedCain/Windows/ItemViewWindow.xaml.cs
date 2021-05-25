using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace TaintedCain
{
	public partial class ItemViewWindow
	{
		public Item Item { get; set; }

		public Tuple<Item, List<Pickup>> Result { get; private set; }
		
		public ItemViewWindow(Item item)
		{
			Item = item;

			InitializeComponent();
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
	}
}