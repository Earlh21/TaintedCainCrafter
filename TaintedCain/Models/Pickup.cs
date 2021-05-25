using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace TaintedCain
{
	public class Pickup : INotifyPropertyChanged
	{
		private static readonly string[] Names =
		{
			"Red Heart",	"Soul Heart",		"Black Heart",		"Eternal Heart",
			"Gold Heart",	"Bone Heart",		"Rotten Heart",		"Penny",
			"Nickel",		"Dime",				"Lucky Penny",		"Key",
			"Golden Key",	"Charged Key",		"Bomb",				"Golden Bomb",
			"Giga Bomb",	"Micro Battery",	"Battery",			"Mega Battery",
			"Card",			"Pill",				"Rune",				"Dice Shard",
			"Cracked Key"
		};

		private static readonly BitmapImage[] Images = Names.Select(name => new BitmapImage(
			new Uri(AppDomain.CurrentDomain.BaseDirectory + "Resources\\Pickups\\" + name + ".png"))).ToArray();

		private int id;
		private BitmapImage image;
		private int amount;
		
		public event PropertyChangedEventHandler PropertyChanged;

		public string Name => Names[id - 1];

		public int Id
		{
			get => id;
			set
			{
				id = value;
				Image = Images[value - 1];
				
				NotifyPropertyChanged("Id");
				NotifyPropertyChanged("Name");
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
		
		public int Amount
		{
			get => amount;
			set
			{
				int old_value = amount;
				amount = value;
				NotifyPropertyChanged("Amount", old_value, value);
			}
		}

		public Pickup(int id, int amount = 0)
		{
			Id = id;
			Amount = amount;
		}

		protected void NotifyPropertyChanged(string property_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
		}
		
		protected void NotifyPropertyChanged<T>(string property_name, T old_value, T new_value)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedExtendedEventArgs<T>(property_name, old_value, new_value));
		}

		public static bool CanCraft(ICollection<Pickup> recipe, ICollection<Pickup> available)
		{
			foreach (var required in recipe)
			{
				int? available_amount = available.FirstOrDefault(p => p.Id.Equals(required.Id))?.Amount;

				if (available_amount == null || available_amount < required.Amount)
				{
					return false;
				}
			}

			return true;
		}
	}
}