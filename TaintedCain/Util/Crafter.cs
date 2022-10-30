using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaintedCain.Models;

namespace TaintedCain.Util
{
    internal class Crafter
    { 
        public uint Seed { get; set; }
        public IList<Item> Items { get; set; }
        public IList<ItemPool> ItemPools { get; set; }

        public Crafter(string seed, IList<ItemPool> item_pools, IList<Item> items)
        {
            Seed = StringToSeed(seed);
            ItemPools = item_pools;
            Items = items;
        }

        public int CalculateCrafting(int[] idxs)
        {
            Dictionary<int, int> counts = idxs.GroupBy(i => i).Select(g => (g.Key, g.Count())).ToDictionary(g => g.Item1, g => g.Item2);
            foreach (var recipe in StaticRecipeCounts)
            {
                if (counts.Count == recipe.Key.Count && !counts.Except(recipe.Key).Any())
                {
                    return recipe.Value;
                }
            }

            var item_count = new int[CraftingShifts.Length];
            var item_score_sum = 0;

            foreach (var idx in idxs)
            {
                ++item_count[idx];
                item_score_sum += CraftingWeights[idx];
            }

            var weight_list = new List<(int idx, float weight)>
            {
                (0, 1f),
                (1, 2f),
                (2, 2f),
                (4, item_count[4] * 10f),
                (3, item_count[3] * 10f),
                (5, item_count[6] * 5f),
                (8, item_count[5] * 10f),
                (12, item_count[7] * 10f),
                (9, item_count[25] * 10f),
                (7, item_count[29] * 10f)
            };

            var combined = item_count[8] + item_count[1] + item_count[12] + item_count[15];
            if (combined == 0)
                weight_list.Add((26, item_count[23] * 10f));

            var rng = new Rng(Seed);

            for (int i = 0; i < item_count.Length; i++)
            {
                for (int j = 0; j < item_count[i]; j++)
                {
                    rng.Next(i);
                }
            }

            var total_weight = 0f;
            var item_weights = new float[Items.Max(item => item.Id) + 1];

            for (int weight_select_i = 0; weight_select_i < weight_list.Count; weight_select_i++)
            {
                if (weight_list[weight_select_i].Item2 <= 0)
                    continue;

                var score = item_score_sum;
                var id = weight_list[weight_select_i].idx;
                if (id == 4 || id == 3 || id == 5)
                {
                    score -= 5;
                }

                var quality_min = 0;
                var quality_max = 1;

                if (score > 34)
                {
                    quality_min = 4;
                    quality_max = 4;
                }
                else if (score > 30)
                {
                    quality_min = 3;
                    quality_max = 4;
                }
                else if (score > 26)
                {
                    quality_min = 3;
                    quality_max = 4;
                }
                else if (score > 22)
                {
                    quality_min = 2;
                    quality_max = 4;
                }
                else if (score > 18)
                {
                    quality_min = 2;
                    quality_max = 3;
                }
                else if (score > 14)
                {
                    quality_min = 1;
                    quality_max = 2;
                }
                else if (score > 8)
                {
                    quality_min = 0;
                    quality_max = 2;
                }

                var pool = ItemPools[weight_list[weight_select_i].idx];
                foreach (var entry in pool.Items)
                {
                    var item = entry.Item1;
                    var weight = entry.Item2;

                    if (item.Quality < quality_min)
                        continue;
                    if (item.Quality > quality_max)
                        continue;

                    var w = weight * weight_list[weight_select_i].weight;
                    item_weights[item.Id] += w;
                    total_weight += w;
                }
            }

            if (total_weight <= 0)
                return 25;

            var target = rng.NextFloat() * total_weight;
            for (int i = 0; i < item_weights.Length; i++)
            {
                if (target < item_weights[i])
                    return i;
                target -= item_weights[i];
            }

            return 25;
        }

        public static uint StringToSeed(string seed)
        {
            var dict = new uint[256];

            for (int i = 0; i < 256; i++)
            {
                dict[i] = 0xFF;
            }

            for (uint i = 0; i < 32; i++)
            {
                dict["ABCDEFGHJKLMNPQRSTWXYZ01234V6789"[(int)i]] = i;
            }

            var bytes = new List<byte>();

            foreach (char c in seed)
            {
                bytes.Add((byte)dict[c]);
            }

            uint v8 = 0;
            uint v10;
            for (uint i = (uint)((bytes[6] >> 3) | (4 * (bytes[5] |
                                                         (32 * (bytes[4] | (32 * (bytes[3] |
                                                             (32 * (bytes[2] |
                                                                    (32 * (bytes[1] | (32 * bytes[0])))))))))))) ^
                          0xFEF7FFD;
                i != 0;
                v8 = ((v10 >> 7) + 2 * v10) & 0xFF)
            {
                v10 = ((i & 0xFF) + v8) & 0xFF;
                i >>= 5;
            }

            return (uint)((bytes[6] >> 3) | (4 * (bytes[5] |
                                                  (32 * (bytes[4] | (32 * (bytes[3] |
                                                                           (32 * (bytes[2] |
                                                                               (32 * (bytes[1] |
                                                                                   (32 * bytes[0])))))))))))) ^
                   0xFEF7FFD;
        }

        public class Rng
        {
            public uint Seed;

            public uint Next(int offset_id)
            {
                var num = Seed;
                num ^= num >> CraftingShifts[offset_id].Item1 & 0xFFFFFFFF;
                num ^= num << CraftingShifts[offset_id].Item2 & 0xFFFFFFFF;
                num ^= num >> CraftingShifts[offset_id].Item3 & 0xFFFFFFFF;
                Seed = num >> 0;
                return num;
            }

            public Rng(uint seed)
            {
                Seed = seed;
            }

            public unsafe float NextFloat()
            {
                uint multi = 0x2F7FFFFE;
                return Next(6) * (*(float*)&multi);
            }
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

        static int[] CraftingWeights =
        {
            0x00000000, 0x00000001, 0x00000004, 0x00000005, 0x00000005, 0x00000005, 0x00000005,
            0x00000001, 0x00000001, 0x00000003, 0x00000005, 0x00000008, 0x00000002, 0x00000007,
            0x00000005, 0x00000002, 0x00000007, 0x0000000A, 0x00000002, 0x00000004, 0x00000008,
            0x00000002, 0x00000002, 0x00000004, 0x00000004, 0x00000002, 0x00000007, 0x00000007,
            0x00000007, 0x00000000, 0x00000001
        };

        private static Dictionary<int[], int> StaticRecipes = new Dictionary<int[], int>()
        {
            {new [] {8, 8, 8, 8, 8, 8, 8, 8}, 177},
            {new [] {1, 1, 1, 1, 1, 1, 1, 1}, 45},
            {new [] {2, 2, 2, 2, 2, 2, 2, 2}, 686},
            {new [] {3, 3, 3, 3, 3, 3, 3, 3}, 118},
            {new [] {12, 12, 12, 12, 12, 12, 12, 12}, 343},
            {new [] {15, 15, 15, 15, 15, 15, 15, 15}, 37},
            {new [] {21, 21, 21, 21, 21, 21, 21, 21}, 85},
            {new [] {1, 2, 5, 4, 4, 4, 4, 4}, 331},
            {new [] {4, 4, 4, 4, 4, 4, 4, 4}, 182},
            {new [] {22, 22, 22, 22, 22, 22, 22, 22}, 75},
            {new [] {3, 22, 22, 22, 22, 22, 22, 22}, 654},
            {new [] {7, 7, 1, 1, 1, 1, 1, 1}, 639},
            {new [] {13, 12, 12, 12, 12, 12, 12, 12}, 175},
            {new [] {17, 17, 17, 17, 17, 17, 17, 17}, 483},
            {new [] {16, 16, 15, 15, 15, 15, 15, 15}, 483},
            {new [] {6, 6, 6, 6, 6, 6, 6, 6}, 628},
            {new [] {24, 24, 24, 24, 24, 24, 24, 24}, 489},
            {new [] {25, 25, 25, 25, 25, 25, 25, 25}, 580}
        };

        private static Dictionary<Dictionary<int, int>, int> StaticRecipeCounts = StaticRecipes.Select(
            (kv) =>
            {
                var counts = new Dictionary<int, int>();
                foreach (var item in kv.Key)
                {
                    if (counts.ContainsKey(item))
                    {
                        counts[item]++;
                    }
                    else
                    {
                        counts.Add(item, 1);
                    }
                }
                return new KeyValuePair<Dictionary<int, int>, int>(counts, kv.Value);
            }).ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
