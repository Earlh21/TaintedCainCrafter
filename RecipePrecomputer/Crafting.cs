using System;
using System.Collections.Generic;

namespace TaintedCain
{
	public static class Crafting
	{
		/**
		 * Taken from bladecoding on Github
		 */
		
		public static int CalculateCrafting(int[] idxs, (string name, (int id, float weight)[] items)[] itempools,
			Dictionary<int, int> itemsquality)
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

				var pool = itempools[poolChances[i].idx];
				foreach (var item in pool.items)
				{
					var quality = itemsquality[item.id];
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

		static T[] ToArray<T>((T, T) t)
		{
			return new T[] {t.Item1, t.Item2};
		}

		static int[] PickupWhitelist = new int[]
		{
			10,
			20,
			30,
			40,
			42,
			70,
			90,
			300,
		};

		static Dictionary<int, (int, int)[]> PickupIndexes = new Dictionary<int, (int, int)[]>
		{
			{
				10, new[]
				{
					(1, 0),
					(1, 0),
					(2, 0),
					(4, 0),
					(1, 1),
					(3, 0),
					(5, 0),
					(2, 0),
					(1, 0),
					(2, 1),
					(6, 0),
					(7, 0),
				}
			},
			{
				20, new[]
				{
					(8, 0),
					(9, 0),
					(10, 0),
					(8, 8),
					(11, 0),
				}
			},
			{
				30, new[]
				{
					(12, 0),
					(13, 0),
					(12, 12),
					(14, 0),
				}
			},
			{
				40, new[]
				{
					(15, 0),
					(15, 15),
					(0, 0),
					(16, 0),
					(0, 0),
					(0, 0),
					(17, 0),
				}
			},
		};

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

		static int[] CraftingWeights = new int[]
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

		static string SeedToString(uint num)
		{
			const string chars = "ABCDEFGHJKLMNPQRSTWXYZ01234V6789";
			byte x = 0;
			var tnum = num;
			while (tnum != 0)
			{
				x += ((byte) tnum);
				x += (byte) (x + (x >> 7));
				tnum >>= 5;
			}

			num ^= 0x0FEF7FFD;
			tnum = (num) << 8 | x;

			var ret = new char[8];
			for (int i = 0; i < 6; i++)
			{
				ret[i] = chars[(int) (num >> (27 - (i * 5)) & 0x1F)];
			}

			ret[6] = chars[(int) (tnum >> 5 & 0x1F)];
			ret[7] = chars[(int) (tnum & 0x1F)];

			return new string(ret);
		}

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