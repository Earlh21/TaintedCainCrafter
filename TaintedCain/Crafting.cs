using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TaintedCain
{
	public static class Crafting
	{
		private static readonly string DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
		private static (string name, (int id, float weight)[] items)[] ItemPools { get; }
		private static Dictionary<int, int> ItemQualities { get; }

		static Crafting()
		{
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
		}
		
		/**
		 * Taken from bladecoding on Github
		 */
		
		public static int CalculateCrafting(int[] idxs)
		{
			var rng = new Rng(0x77777770, 0, 0, 0);
			var shiftCounts = new int[CraftingShifts.Length];
			var weight = 0;

			if (idxs.Length != 8)
				throw new ArgumentException();

			foreach (var idx in idxs)
			{
				++shiftCounts[idx];
				weight += CraftingWeights[idx];
			}

			for (int i = 0; i < shiftCounts.Length; i++)
			{
				for (int j = 0; j < shiftCounts[i]; j++)
				{
					rng.Shift1 = CraftingShifts[i].Item1;
					rng.Shift2 = CraftingShifts[i].Item2;
					rng.Shift3 = CraftingShifts[i].Item3;
					rng.Next();
				}
			}

			rng.Shift1 = 1;
			rng.Shift2 = 21;
			rng.Shift3 = 20;

			var poolChances = new List<(int idx, float weight)>
			{
				(0, 1f),
				(1, 2f),
				(2, 2f),
				(4, shiftCounts[4] * 10f),
				(3, shiftCounts[3] * 10f),
				(5, shiftCounts[6] * 5f),
				(8, shiftCounts[5] * 10f),
				(12, shiftCounts[7] * 10f),
				(9, shiftCounts[25] * 10f),
			};

			var combined = shiftCounts[8] + shiftCounts[1] + shiftCounts[12] + shiftCounts[15];
			if (combined == 0)
				poolChances.Add((26, shiftCounts[23] * 10f));


			var totalWeight = 0f;
			var itemWeights = new float[730];


			for (int i = 0; i < poolChances.Count; i++)
			{
				var val = poolChances[i].Item1;
				if (poolChances[i].Item2 <= 0)
					continue;

				var qualityMin = 0;
				var qualityMax = 1;
				var n = weight;
				if (val >= 3 && val <= 5)
					n -= 5;

				if (n > 34)
				{
					qualityMin = 4;
					qualityMax = 4;
				}
				else if (n > 30)
				{
					qualityMin = 3;
					qualityMax = 4;
				}
				else if (n > 26)
				{
					qualityMin = 2;
					qualityMax = 4;
				}
				else if (n > 22)
				{
					qualityMin = 1;
					qualityMax = 4;
				}
				else if (n > 18)
				{
					qualityMin = 1;
					qualityMax = 3;
				}
				else if (n > 14)
				{
					qualityMin = 1;
					qualityMax = 2;
				}
				else if (n > 8)
				{
					qualityMin = 0;
					qualityMax = 2;
				}

				var pool = ItemPools[poolChances[i].idx];
				foreach (var item in pool.items)
				{
					var quality = ItemQualities[item.id];
					if (quality < qualityMin)
						continue;
					if (quality > qualityMax)
						continue;

					var w = item.weight * poolChances[i].weight;
					itemWeights[item.id] += w;
					totalWeight += w;
				}
			}

			if (totalWeight <= 0)
				return 25;

			var target = rng.NextFloat() * totalWeight;
			for (int i = 0; i < itemWeights.Length; i++)
			{
				if (target < itemWeights[i])
					return i;
				target -= itemWeights[i];
			}

			return 25;
		}

		static (int, int, int)[] CraftingShifts = new (int, int, int)[]
		{
			(0x00000001, 0x00000005, 0x00000010),
			(0x00000001, 0x00000005, 0x00000013),
			(0x00000001, 0x00000009, 0x0000001D),
			(0x00000001, 0x0000000B, 0x00000006),
			(0x00000001, 0x0000000B, 0x00000010),
			(0x00000001, 0x00000013, 0x00000003),
			(0x00000001, 0x00000015, 0x00000014),
			(0x00000001, 0x0000001B, 0x0000001B),
			(0x00000002, 0x00000005, 0x0000000F),
			(0x00000002, 0x00000005, 0x00000015),
			(0x00000002, 0x00000007, 0x00000007),
			(0x00000002, 0x00000007, 0x00000009),
			(0x00000002, 0x00000007, 0x00000019),
			(0x00000002, 0x00000009, 0x0000000F),
			(0x00000002, 0x0000000F, 0x00000011),
			(0x00000002, 0x0000000F, 0x00000019),
			(0x00000002, 0x00000015, 0x00000009),
			(0x00000003, 0x00000001, 0x0000000E),
			(0x00000003, 0x00000003, 0x0000001A),
			(0x00000003, 0x00000003, 0x0000001C),
			(0x00000003, 0x00000003, 0x0000001D),
			(0x00000003, 0x00000005, 0x00000014),
			(0x00000003, 0x00000005, 0x00000016),
			(0x00000003, 0x00000005, 0x00000019),
			(0x00000003, 0x00000007, 0x0000001D),
			(0x00000003, 0x0000000D, 0x00000007),
			(0x00000003, 0x00000017, 0x00000019),
			(0x00000003, 0x00000019, 0x00000018),
			(0x00000003, 0x0000001B, 0x0000000B),
			(0x00000004, 0x00000003, 0x00000011),
			(0x00000004, 0x00000003, 0x0000001B),
			(0x00000004, 0x00000005, 0x0000000F),
			(0x00000005, 0x00000003, 0x00000015),
			(0x00000005, 0x00000007, 0x00000016),
			(0x00000005, 0x00000009, 0x00000007),
			(0x00000005, 0x00000009, 0x0000001C),
			(0x00000005, 0x00000009, 0x0000001F),
			(0x00000005, 0x0000000D, 0x00000006),
			(0x00000005, 0x0000000F, 0x00000011),
			(0x00000005, 0x00000011, 0x0000000D),
			(0x00000005, 0x00000015, 0x0000000C),
			(0x00000005, 0x0000001B, 0x00000008),
			(0x00000005, 0x0000001B, 0x00000015),
			(0x00000005, 0x0000001B, 0x00000019),
			(0x00000005, 0x0000001B, 0x0000001C),
			(0x00000006, 0x00000001, 0x0000000B),
			(0x00000006, 0x00000003, 0x00000011),
			(0x00000006, 0x00000011, 0x00000009),
			(0x00000006, 0x00000015, 0x00000007),
			(0x00000006, 0x00000015, 0x0000000D),
			(0x00000007, 0x00000001, 0x00000009),
			(0x00000007, 0x00000001, 0x00000012),
			(0x00000007, 0x00000001, 0x00000019),
			(0x00000007, 0x0000000D, 0x00000019),
			(0x00000007, 0x00000011, 0x00000015),
			(0x00000007, 0x00000019, 0x0000000C),
			(0x00000007, 0x00000019, 0x00000014),
			(0x00000008, 0x00000007, 0x00000017),
			(0x00000008, 0x00000009, 0x00000017),
			(0x00000009, 0x00000005, 0x0000000E),
			(0x00000009, 0x00000005, 0x00000019),
			(0x00000009, 0x0000000B, 0x00000013),
			(0x00000009, 0x00000015, 0x00000010),
			(0x0000000A, 0x00000009, 0x00000015),
			(0x0000000A, 0x00000009, 0x00000019),
			(0x0000000B, 0x00000007, 0x0000000C),
			(0x0000000B, 0x00000007, 0x00000010),
			(0x0000000B, 0x00000011, 0x0000000D),
			(0x0000000B, 0x00000015, 0x0000000D),
			(0x0000000C, 0x00000009, 0x00000017),
			(0x0000000D, 0x00000003, 0x00000011),
			(0x0000000D, 0x00000003, 0x0000001B),
			(0x0000000D, 0x00000005, 0x00000013),
			(0x0000000D, 0x00000011, 0x0000000F),
			(0x0000000E, 0x00000001, 0x0000000F),
			(0x0000000E, 0x0000000D, 0x0000000F),
			(0x0000000F, 0x00000001, 0x0000001D),
			(0x00000011, 0x0000000F, 0x00000014),
			(0x00000011, 0x0000000F, 0x00000017),
			(0x00000011, 0x0000000F, 0x0000001A)
		};

		static int[] CraftingWeights =
		{
			0x00000000,
			0x00000001,
			0x00000004,
			0x00000005,
			0x00000005,
			0x00000005,
			0x00000005,
			0x00000001,
			0x00000001,
			0x00000003,
			0x00000005,
			0x00000008,
			0x00000002,
			0x00000005,
			0x00000005,
			0x00000002,
			0x00000006,
			0x0000000A,
			0x00000002,
			0x00000004,
			0x00000008,
			0x00000002,
			0x00000002,
			0x00000004,
			0x00000004,
			0x00000002,
			0x00000001
		};

		public class Rng
		{
			public uint Seed;
			public int Shift1;
			public int Shift2;
			public int Shift3;

			public uint Next()
			{
				var num = Seed;
				num ^= num >> Shift1;
				num ^= num << Shift2;
				num ^= num >> Shift3;
				Seed = num;
				return num;
			}

			public Rng(uint seed, int s1, int s2, int s3)
			{
				this.Seed = seed;
				Shift1 = s1;
				Shift2 = s2;
				Shift3 = s3;
			}

			public unsafe float NextFloat()
			{
				uint multi = 0x2F7FFFFE;
				return Next() * (*(float*) &multi);
			}
		};
	}
}