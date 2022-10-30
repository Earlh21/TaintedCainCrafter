using System;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Collections.Generic;

namespace TaintedCain
{
	[Serializable]
	public class UserSettings
	{
		public string UiTheme { get; set; } = "Light";
		public string ModsPath { get; set; } = string.Empty;
		public bool UseModdedItems { get; set; } = false;

		public Dictionary<string, Color> Highlights = new Dictionary<string, Color>();
		public HashSet<string> Blacklist = new HashSet<string>();

		public void Save(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public static UserSettings Load(string path)
		{
			return JsonConvert.DeserializeObject<UserSettings>(File.ReadAllText(path));
		}
	}
}