using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Linq;
using System.Xml.XPath;
using System.IO;
using System.Globalization;

namespace TaintedCain.Models
{
    public class Mod
    {
        public string Name { get; set; }
        public bool IsValid { get; set; }
        public List<Item> Items { get; set; }
        public List<ItemPool> ItemPools { get; set; }

        public static Mod? FromDirectory(string mod_path)
        {
            string metadata_path = mod_path + "/metadata.xml";

            if (!File.Exists(metadata_path))
            {
                //This directory isn't even a mod
                return null;
            }

            string mod_name = "unknown";

            try
            {
                var culture_format = new CultureInfo("en-US");

                mod_name = XElement.Load(metadata_path)
                    .Element("name").Value;

                string items_path = mod_path + "/content/items.xml";
                string itempools_path = mod_path + "/content/itempools.xml";

                if (!File.Exists(items_path) || !File.Exists(itempools_path))
                {
                    return new Mod
                    {
                        Name = mod_name,
                        Items = new List<Item>(),
                        IsValid = true
                    };
                }

                var item_images_path = mod_path + "resources/" +
                    XElement.Load(items_path)
                    .Attribute("gfxroot").Value
                    + "/collectibles/";

                var items = XElement.Load(items_path)
                    .Elements()
                    .Where(x => x.Name == "passive" || x.Name == "active" || x.Name == "familiar")
                    .Select((x, i) =>
                    {
                        string name = x.Attribute("name").Value;
                        string description = x.Attribute("description")?.Value ?? "";

                        string image_path = null;

                        if (x.Attribute("gfx") != null)
                        {
                            image_path = item_images_path + x.Attribute("gfx").Value;
                        }
                        
                        int quality = Convert.ToInt32(x.Attribute("quality")?.Value ?? "0");
                        int id = i + 1;
                        return new Item(id, name, description, mod_name, quality, image_path);


                    }).ToList();

                var item_pools =
                    XElement.Load(itempools_path)
                        .Elements()
                        .Select(pool_xml =>
                        {
                            string pool_name = pool_xml.Attribute("Name").Value;
                            var pool_items = pool_xml.Elements().Select(pool_item_xml =>
                            {
                                var item = items.First(item => item.Name == pool_item_xml.Attribute("Name").Value);
                                var weight = Convert.ToSingle(pool_item_xml.Attribute("Weight").Value, culture_format);

                                var tuple = new Tuple<Item, float>(item, weight);

                                return new Tuple<Item, float>(item, weight);
                            }).ToList();

                            return new ItemPool(pool_name, pool_items);
                        }).ToList();

                //Remove any items that aren't in an item pool
                items = items.Where(item => item_pools.Any(pool => pool.Items.Any(items => items.Item1 == item))).ToList();

                return new Mod
                {
                    Name = mod_name,
                    Items = items,
                    ItemPools = item_pools,
                    IsValid = true
                };
            }
            catch (Exception)
            {
                return new Mod
                {
                    Name = mod_name,
                    IsValid = false
                };
            }
        }
    }
}
