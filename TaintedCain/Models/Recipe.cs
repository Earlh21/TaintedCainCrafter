using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaintedCain
{
	public class Recipe
	{
		private Lazy<Pickup[]> discrete_pickups;
		private List<Pickup> pickups = new List<Pickup>();
		public ReadOnlyCollection<Pickup> DiscretePickups => Array.AsReadOnly(discrete_pickups.Value);
		public ReadOnlyCollection<Pickup> Pickups => pickups.AsReadOnly();
		public float AverageQuality { get; }

		Pickup[] InitDiscretePickups()
		{
			Pickup[] discrete_pickups = new Pickup[8];
			int i = 0;

			foreach (var pickup in pickups)
			{
				for (int j = 0; j < pickup.Amount; j++)
				{
					//Don't check bounds, if there's an error here then
					//something has gone wrong elsewhere
					discrete_pickups[i] = new Pickup(pickup.Id, 1);
					i++;
				}
			}

			return discrete_pickups;
		}

		public Recipe(IEnumerable<Pickup> pickups)
		{
			discrete_pickups = new Lazy<Pickup[]>(InitDiscretePickups);

			int size = 0;
			foreach (var pickup in pickups)
			{
				if (pickup.Amount < 1) continue;

				this.pickups.Add(pickup.Copy());
				size += pickup.Amount;

				if (size > 8)
				{
					throw new ArgumentException("Too many pickups.");
				}
			}

			if (size < 8)
			{
				throw new ArgumentException("Not enough pickups.");
			}
		}

		public bool CanCraft(ICollection<Pickup> available)
		{
			foreach (var required in pickups)
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