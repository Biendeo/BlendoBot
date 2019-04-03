using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Since this is serialized, you'll get undefined warnings otherwise.
#pragma warning disable 0649

namespace BlendoBot {
	/// <summary>
	/// Contains the private properties of the config.
	/// </summary>
	[JsonObject(MemberSerialization.OptOut)]
	internal class ConfigPrivate {
		public string Token;
	}

	/// <summary>
	/// Contains the public properties of the config.
	/// </summary>
	[JsonObject(MemberSerialization.OptOut)]
	internal class ConfigPublic {
		public string Name;
		public string Version;
		public string Description;
		public string Author;
		public string ActivityName;
		[JsonIgnore]
		public ActivityType ActivityType;
	}

	/// <summary>
	/// The main config object. This exposes several properties that exist within the <see cref="ConfigPublic"/>, and
	/// can be deserialized from JSON.
	/// </summary>
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

		/// <summary>
		/// Creates a JSON object from a file path. Returns null if the file doesn't exist or
		/// if the object doesn't contain every element.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static Config FromJson(string filePath) {
			if (!File.Exists(filePath)) {
				Console.Error.WriteLine($"Config.FromJson() can't find {filePath}! Aborting program...");
				return null;
			}
			Config c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
			if (c.Name == null || c.Version == null || c.Description == null || c.Author == null || c.ActivityName == null || c.Private.Token == null) {
				Console.Error.WriteLine("Config.FromJson() read in an incomplete json. You need to add in all the fields!");
				return null;
			}
			return c;
		}
	}
}

#pragma warning restore 0649