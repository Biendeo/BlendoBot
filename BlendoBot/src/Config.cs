using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Since this is serialized, you'll get undefined warnings otherwise.
#pragma warning disable 0649

namespace BlendoBot {
	[JsonObject(MemberSerialization.OptOut)]
	internal class ConfigPrivate {
		internal string Token;
	}

	[JsonObject(MemberSerialization.OptOut)]
	internal class ConfigPublic {
		internal string Name;
		internal string Version;
		internal string Description;
		internal string Author;
		internal string ActivityName;
		[JsonIgnore]
		internal ActivityType ActivityType;
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class Config {
		[JsonProperty]
		internal ConfigPrivate Private;
		[JsonProperty]
		internal ConfigPublic Public;

		public string Name { get { return Public.Name; } }
		public string Version { get { return Public.Version; } }
		public string Description { get { return Public.Description; } }
		public string Author { get { return Public.Author; } }
		public string ActivityName { get { return Public.ActivityName; } }
		public ActivityType ActivityType { get { return Public.ActivityType; } }

		public static Config FromJson(string filePath) {
			if (!File.Exists(filePath)) {
				Console.Error.WriteLine($"Config.FromJson() can't find {filePath}! Aborting program...");
				Environment.Exit(1);
			}
			Config c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
			if (c.Name == null || c.Version == null || c.Description == null || c.Author == null || c.ActivityName == null || c.Private.Token == null) {
				Console.Error.WriteLine("Config.FromJson() read in an incomplete json. You need to add in all the fields!");
				Environment.Exit(1);
			}
			return c;
		}
	}
}

#pragma warning restore 0649