using System;
using System.IO;
using Newtonsoft.Json;

namespace TaintedCain
{
	[Serializable]
	public class UserSettings
	{
		public string UiTheme { get; set; } = "Light";

		public void Save(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this));
		}

		public static UserSettings Load(string path)
		{
			return JsonConvert.DeserializeObject<UserSettings>(File.ReadAllText(path));
		}
	}
}