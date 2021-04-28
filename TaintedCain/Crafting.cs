using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TaintedCain
{
	public static class Crafting
	{
		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
		private static Dictionary<ulong, int> RecipeTable { get; }

		static Crafting()
		{
			RecipeTable = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(File.ReadAllText("C:\\Users\\Idrialite\\Desktop\\table.json"));
		}

		/// <summary>
		/// Gets the id of the item crafted from the given recipe.
		/// </summary>
		/// <param name="recipe">Array of 8 pickup IDs (indexed from 0). Must be sorted in ascending order.</param>
		/// <returns>The crafted item.</returns>
		/// <exception cref="ArgumentException">Recipe length not 8, or recipe not found.</exception>
		public static int Craft(uint[] recipe)
		{
			if (recipe.Length != 8)
			{
				throw new ArgumentException("Recipe length must be 8.");
			}
			
			ulong encoding = 0;

			for (int i = 0; i < 8; i++)
			{
				encoding |= recipe[i];

				if (i != 7)
				{
					encoding <<= 5;
				}
			}

			if (!RecipeTable.ContainsKey(encoding))
			{
				throw new ArgumentException("Recipe not found in table.");
			}
			
			return RecipeTable[encoding];
		}
	}
}