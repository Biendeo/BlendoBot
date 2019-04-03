using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
	[JsonObject(MemberSerialization.OptIn)]
	internal class ConfigPrivate {
		[JsonProperty(Required = Required.Always)]
		public string Token;
	}

	/// <summary>
	/// Contains the public properties of the config.
	/// </summary>
	[JsonObject(MemberSerialization.OptOut)]
	internal class ConfigPublic {
		[JsonProperty(Required = Required.Always)]
		public string Name;
		[JsonProperty(Required = Required.Always)]
		public string Version;
		[JsonProperty(Required = Required.Always)]
		public string Description;
		[JsonProperty(Required = Required.Always)]
		public string Author;
		[JsonProperty(Required = Required.Always)]
		public string ActivityName;
		[JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))]
		public ActivityType ActivityType;
	}

	/// <summary>
	/// The main config object. This exposes several properties that exist within the <see cref="ConfigPublic"/>, and
	/// can be deserialized from JSON.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public class Config {
		[JsonProperty(Required = Required.Always)]
		internal ConfigPrivate Private;
		[JsonProperty(Required = Required.Always)]
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
			try {
				Config c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
				return c;
			} catch (JsonSerializationException exc) {
				Console.Error.WriteLine("Config.FromJson() read in an incomplete json. You need to add in all the fields!");
				Console.Error.WriteLine(exc);
				return null;
			}
		}
	}
}

#pragma warning restore 0649