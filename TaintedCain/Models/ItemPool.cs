using System;
using System.Collections.Generic;
using System.Text;

namespace TaintedCain.Models
{
    public class ItemPool
    {
        public string Name { get; set; }
        public List<Tuple<Item, float>> Items { get; set; }

        public ItemPool(string name, List<Tuple<Item, float>> items)
        {
            Name = name;
            Items = items;
        }
    }
}
