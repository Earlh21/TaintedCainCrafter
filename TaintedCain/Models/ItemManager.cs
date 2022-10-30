using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using TaintedCain.Models;
using TaintedCain.Util;

namespace TaintedCain.Models
{
	public class ItemManager
	{
		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Resources\\Data\\";
		private Crafter crafter;
		private List<ItemPool> item_pools { get; }

		public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();
		public ObservableCollection<Pickup> Pickups { get; } = new ObservableCollection<Pickup>();

		public ItemManager()
		{
			var culture_format = new CultureInfo("en-US");

			var item_qualities =
				XElement.Load(DataFolder + "items_metadata.xml")
					.Elements("item")
					.ToDictionary(e => Convert.ToInt32(e.Attribute("id").Value),
						e => Convert.ToInt32(e.Attribute("quality").Value));

			var invalid_items = new int[] { 59, 656 };

			var items = XElement.Load(DataFolder + "items.xml")
				.Elements()
				.Where(x => x.Name == "passive" || x.Name == "active" || x.Name == "familiar")
				.Where(x => !invalid_items.Contains(Convert.ToInt32(x.Attribute("id").Value)))
				.Select(item_xml =>
				{
					string name = item_xml.Attribute("name").Value;
					string description = item_xml.Attribute("description").Value;
					int id = Convert.ToInt32(item_xml.Attribute("id").Value);

					string image_path = AppDomain.CurrentDomain.BaseDirectory + $"Resources\\Items\\{id}.png";

					int quality = item_qualities[id];

					return new Item(id, name, description, null, quality, image_path);
				});

			foreach(var item in items)
            {
				Items.Add(item);
            }

			item_pools =
				XElement.Load(DataFolder + "itempools.xml")
					.Elements()
					.Select(pool_xml =>
					{
						string pool_name = pool_xml.Attribute("Name").Value;
						var pool_items = pool_xml.Elements().Select(pool_item_xml =>
						{
							var item = Items.First(item => item.Id == Convert.ToInt32(pool_item_xml.Attribute("Id").Value));

							var weight = Convert.ToSingle(pool_item_xml.Attribute("Weight").Value, culture_format);
							return new Tuple<Item, float>(item, weight);
						});

						return new ItemPool(pool_name, pool_items.ToList());
					}).ToList();

			crafter = new Crafter("AAAAAAAA", item_pools, Items);

			for (int i = 1; i <= 25; i++)
			{
				Pickup pickup = new Pickup(i);
				pickup.PropertyChanged += PickupOnPropertyChanged;
				Pickups.Add(pickup);
			}
		}

		public void SetModdedItems(List<Mod> mod_items)
        {
			//Remove current modded items
			foreach (var item_pool in item_pools)
			{
				item_pool.Items = item_pool.Items.Where(entry => entry.Item1.Mod == null).ToList();
			}

			var vanilla_items = Items.Where(item => item.Mod == null).ToList();
			Items.Clear();

			foreach (var item in vanilla_items)
			{
				Items.Add(item);
			}

			//Add modded items
			int start_id = Items.Max(item => item.Id) + 1;

			foreach (var mod in mod_items)
			{
				foreach (var item in mod.Items)
				{
					item.Id += start_id - 1;
					Items.Add(item);
				}

				start_id = mod.Items.Max(item => item.Id) + 1;

				foreach (var item_pool in mod.ItemPools)
				{
					var existing_pool = item_pools.First(pool => pool.Name == item_pool.Name);
					existing_pool.Items.AddRange(item_pool.Items);
				}
			}

			//Recalculate recipes
			var pickups = Pickups.Select(p => p.Copy()).ToList();
			SetPickups(pickups);
		}

		public void SetSeed(uint seed)
		{
			crafter.Seed = seed;

			var pickups = Pickups.Select(p => p.Copy()).ToList();
			SetPickups(pickups);
		}

		public void SetSeed(string seed)
		{
			SetSeed(Crafter.StringToSeed(seed));
		}
		
		public void Clear()
		{
			foreach (Pickup pickup in Pickups)
			{
				pickup.PropertyChanged -= PickupOnPropertyChanged;
				pickup.Amount = 0;
				pickup.PropertyChanged += PickupOnPropertyChanged;
			}

			foreach (Item item in Items)
			{
				item.Recipes.Clear();
			}
		}

		public void SetPickups(IEnumerable<Pickup> pickups)
		{
			Clear();
			AddPickups(pickups);
		}

		public void AddPickups(IEnumerable<Pickup> pickups)
		{
			foreach (Pickup pickup in pickups)
			{
				if (pickup.Amount == 0)
				{
					continue;
				}
				
				Pickup existing = Pickups.FirstOrDefault(p => p.Id == pickup.Id);
				if (existing == null)
				{
					continue;
				}

				existing.Amount += pickup.Amount;
			}
		}

		public void RemovePickups(IEnumerable<Pickup> pickups)
		{
			foreach (Pickup pickup in pickups)
			{
				if (pickup.Amount == 0)
				{
					continue;
				}
				
				Pickup existing = Pickups.FirstOrDefault(p => p.Id == pickup.Id);
				if (existing == null)
				{
					continue;
				}

				existing.Amount -= pickup.Amount;
			}
		}

		private void PickupOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!e.PropertyName.Equals("Amount"))
			{
				return;
			}
			
			Pickup changed_pickup = (Pickup) sender;
			var args = (PropertyChangedExtendedEventArgs<int>) e;
			
			if (args.NewValue < args.OldValue)
			{
				RemoveRecipes(changed_pickup);
			}
			else if(args.NewValue > args.OldValue)
			{
				AddRecipes(changed_pickup.Id, args.OldValue);
			}
		}

		private void AddRecipes(int changed_id, int old_amount)
		{
			if (old_amount >= 8)
			{
				 return;
			}
			
			void AddRecipesHelper(List<Pickup> current_recipe, int pickup_index, int prev_length)
			{
				Pickup current_pickup = current_recipe[pickup_index];
				
				current_pickup.Amount = Math.Min(8 - prev_length, Pickups[pickup_index].Amount);

				//Don't add duplicate recipes - only add recipes that include the changed pickup
				if (current_recipe[changed_id - 1].Amount > 0 && prev_length + current_pickup.Amount == 8)
				{
					AddCraft(current_recipe);

					current_pickup.Amount -= 1;
				}

				//Base case. Don't continue after the last pickup.
				if (pickup_index == current_recipe.Count - 1)
				{
					current_pickup.Amount = 0;
					return;
				}
				
				//Only consider recipes that include more than the old amount of the changed pickup
				int minimum = changed_id == pickup_index + 1 ? old_amount + 1 : 0;
				
				while (current_pickup.Amount >= minimum)
				{
					AddRecipesHelper(current_recipe, pickup_index + 1, prev_length + current_pickup.Amount);
					current_pickup.Amount -= 1;
				}

				current_pickup.Amount = 0;
			}

			List<Pickup> empty_recipe = new List<Pickup>();

			for (int i = 1; i <= 25; i++)
			{
				empty_recipe.Add(new Pickup(i, 0));
			}

			AddRecipesHelper(empty_recipe, 0, 0);
		}
		
		private void AddCraft(List<Pickup> pickups)
		{
			Recipe recipe = new Recipe(pickups);

			int[] ids = new int[8];
			
			int i = 0;
			foreach (var pickup in recipe.Pickups)
			{
				for (int j = 0; j < pickup.Amount; j++)
				{
					ids[i] = pickup.Id;
					i++;
				}
			}

			int item_id = crafter.CalculateCrafting(ids);

			Item existing_item = Items.FirstOrDefault(i => i.Id == item_id);
			existing_item?.Recipes.Add(recipe);
		}

		private void RemoveRecipes(Pickup changed_pickup)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				Item item = Items[i];

				RemoveItemRecipes(item, changed_pickup);
			}
		}
			
		private void RemoveItemRecipes(Item item, Pickup changed_pickup)
		{
			for (int j = 0; j < item.Recipes.Count; j++)
			{
				Pickup existing = item.Recipes[j].Pickups.FirstOrDefault(p => p.Id == changed_pickup.Id);

				if (existing == null)
				{
					continue;
				}

				if (existing.Amount > changed_pickup.Amount)
				{
					item.Recipes.RemoveAt(j);
					j--;
				}
			}
		}
	}
}