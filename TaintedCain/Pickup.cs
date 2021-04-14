using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TaintedCain
{
	public class Pickup : INotifyPropertyChanged
	{
		public static readonly string[] Names =
		{
			"Red Heart",	"Soul Heart",		"Black Heart",		"Eternal Heart",
			"Gold Heart",	"Bone Heart",		"Rotten Heart",		"Penny",
			"Nickel",		"Dime",				"Lucky Penny",		"Key",
			"Golden Key",	"Charged Key",		"Bomb",				"Golden Bomb",
			"Giga Bomb",	"Micro Battery",	"Battery",			"Mega Battery",
			"Card",			"Pill",				"Rune",				"Dice Shard",
			"Cracked Key"
		};

		private int id;
		private int amount;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		public string Icon => AppDomain.CurrentDomain.BaseDirectory + "Pickups\\" + Name + ".png";

		public string Name => Names[id - 1];

		public int Id
		{
			get => id;
			set
			{
				id = value;
				OnPropertyChanged("Id");
				OnPropertyChanged("Name");
			}
		}
		
		public int Amount
		{
			get => amount;
			set
			{
				amount = value;
				OnPropertyChanged("Amount");
			}
		}

		public Pickup(int id, int amount = 0)
		{
			Id = id;
			Amount = amount;
		}
		
		protected void OnPropertyChanged(string property_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property_name));
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