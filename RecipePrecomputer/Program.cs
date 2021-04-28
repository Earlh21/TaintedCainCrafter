using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using TaintedCain;

namespace RecipePrecomputer
{
	class Program
	{
		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
		private static (string name, (int id, float weight)[] items)[] ItemPools;
		private static Dictionary<int, int> ItemQualities;
		
		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("Usage: precompute <output_location>");
				return;
			}
			
			var culture_format = new CultureInfo("en-US");
			
			ItemPools =
				XElement.Load(DataFolder + "itempools.xml")
					.XPathSelectElements("Pool")
					.Select(e => (
						e.Attribute("Name").Value,
						e.Elements("Item").Select(x => (Convert.ToInt32(x.Attribute("Id").Value),
							Convert.ToSingle(x.Attribute("Weight").Value, culture_format))).ToArray()))
					.ToArray();

			ItemQualities =
				XElement.Load(DataFolder + "items_metadata.xml")
					.XPathSelectElements("item")
					.ToDictionary(e => Convert.ToInt32(e.Attribute("id").Value),
						e => Convert.ToInt32(e.Attribute("quality").Value));

			var table = new Dictionary<ulong, short>();
			
			ComputeRecipes(table);

			File.WriteAllText(args[0], JsonConvert.SerializeObject(table));
		}

		private static void WriteBinaryFile(Dictionary<ulong, short> table, string filepath)
		{
			using (var writer = new BinaryWriter(File.Open(filepath, FileMode.Create)))
			{
				foreach (var pair in table)
				{
					var bytes = BitConverter.GetBytes(pair.Key);
					for (int i = 0; i < 5; i++)
					{
						writer.Write(BitConverter.IsLittleEndian ? bytes[i] : bytes[7 - i]);
					}
					
					writer.Write(pair.Value);
				}
			}
		}
		
		private static void ComputeRecipes(Dictionary<ulong, short> table)
		{
			void AddRecipesHelper(int[] current_recipe, int pickup_index, int prev_length)
			{
				current_recipe[pickup_index] = 8 - prev_length;
				
				if (prev_length + current_recipe[pickup_index] == 8)
				{
					AddCraft(current_recipe, table);

					current_recipe[pickup_index] -= 1;
				}

				//Base case. Don't continue after the last pickup.
				if (pickup_index == current_recipe.Length - 1)
				{
					current_recipe[pickup_index] = 0;
					return;
				}

				while (current_recipe[pickup_index] >= 0)
				{
					if (pickup_index == 0)
					{
						Console.WriteLine(current_recipe[0]);
					}
					
					AddRecipesHelper(current_recipe, pickup_index + 1, prev_length + current_recipe[pickup_index]);
					current_recipe[pickup_index] -= 1;
				}

				current_recipe[pickup_index] = 0;
			}

			var empty_recipe = new int[25];

			AddRecipesHelper(empty_recipe, 0, 0);
		}
		
		private static void AddCraft(int[] recipe, Dictionary<ulong, short> table)
		{
			int[] discrete_recipe = new int[8];
			int i = 0;

			ulong coded_recipe = 0;
			for (ulong j = 0; (int)j < recipe.Length; j++)
			{
				for (int k = 0; k < recipe[j]; k++)
				{
					discrete_recipe[i] = (int)j;
					i++;

					coded_recipe |= j;

					if (i != 8)
					{
						coded_recipe <<= 5;
					}
				}
			}

			if (table.ContainsKey(coded_recipe))
			{
				throw new Exception("Duplicated encoding");
			}
			
			table[coded_recipe] = (short)Crafting.CalculateCrafting(discrete_recipe, ItemPools, ItemQualities);
		}
	}
}