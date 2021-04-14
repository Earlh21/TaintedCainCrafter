using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TaintedCain
{
	public class Item : INotifyPropertyChanged
	{
		private int id;
		private string name;
		private string text;
		private ObservableCollection<List<Pickup>> recipes;

		public event PropertyChangedEventHandler PropertyChanged;

		public string Icon => AppDomain.CurrentDomain.BaseDirectory + "Items\\collectibles_" + Id.ToString().PadLeft(3, '0') + ".png";

		public int Id
		{
			get => id;
			set
			{
				id = value;
				OnPropertyChanged("Id");
			}
		}

		public string Name
		{
			get => name;
			set
			{
				name = value;
				OnPropertyChanged("Name");
			}
		}

		public string Text
		{
			get => text;
			set
			{
				text = value;
				OnPropertyChanged("Description");
			}
		}

		public ObservableCollection<List<Pickup>> Recipes
		{
			get => recipes;
			set
			{
				recipes = value;
				OnPropertyChanged("Recipes");
			}
		}
		
		public Item(int id, string name, string text)
		{
			Id = id;
			Name = name;
			Text = text;
			Recipes = new ObservableCollection<List<Pickup>>();
		}

		protected void OnPropertyChanged(string property_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
		}

		public bool CanCraft(Collection<Pickup> available_pickups)
		{
			foreach (var recipe in recipes)
			{
				if (Pickup.CanCraft(recipe, available_pickups))
				{
					return true;
				}
			}

			return false;
		}
	}
}