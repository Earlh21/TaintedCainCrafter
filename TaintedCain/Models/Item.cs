using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaintedCain
{
	public class Item : INotifyPropertyChanged
	{
		private int id;
		private string name;
		private BitmapImage image;
		private bool is_blacklisted;
		private string description;
		private ObservableCollection<List<Pickup>> recipes;
		private Color highlight_color;

		public event PropertyChangedEventHandler PropertyChanged;

		private string ImagePath => AppDomain.CurrentDomain.BaseDirectory + "Resources\\Items\\collectibles_" + Id.ToString().PadLeft(3, '0') + ".png";

		public int Id
		{
			get => id;
			set
			{
				id = value;
				Image = new BitmapImage(new Uri(ImagePath));
				
				NotifyPropertyChanged("Id");
			}
		}
		
		public BitmapImage Image
		{
			get => image;
			
			private set
			{
				image = value;
				NotifyPropertyChanged("Image");
			}
		}

		public string Name
		{
			get => name;
			set
			{
				name = value;
				NotifyPropertyChanged("Name");
			}
		}

		public string Description
		{
			get => description;
			set
			{
				description = value;
				NotifyPropertyChanged("Description");
			}
		}

		public Color HighlightColor
		{
			get => highlight_color;
			set
			{
				highlight_color = value;
				NotifyPropertyChanged("HighlightColor");
			}
		}

		public bool IsBlacklisted
		{
			get => is_blacklisted;
			set
			{
				is_blacklisted = value;
				NotifyPropertyChanged("IsBlacklisted");
			}
		}

		public bool HasRecipes => Recipes.Count > 0;

		public ObservableCollection<List<Pickup>> Recipes
		{
			get => recipes;
			set
			{
				recipes = value;
				NotifyPropertyChanged("Recipes");
			}
		}
		
		public Item(int id, string name, string description)
		{
			Id = id;
			Name = name;
			Description = description;
			Recipes = new ObservableCollection<List<Pickup>>();
			HighlightColor = Color.FromArgb(0, 0, 0, 0);
			
			Recipes.CollectionChanged += (sender, args) => { NotifyPropertyChanged("HasRecipes");};
		}

		protected void NotifyPropertyChanged(string property_name)
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