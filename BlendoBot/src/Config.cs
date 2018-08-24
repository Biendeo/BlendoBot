using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendoBot {
	public class Config {
		public string Token;
		public string Name;
		public string Version;
		public string Description;
		public string Author;
		public string ActivityName;
		public ActivityType ActivityType;

		public static Config FromJson(string filePath) {
			dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(filePath));

			return new Config {
				Token = json.Private.Token,
				Name = json.Public.Name,
				Version = json.Public.Version,
				Description = json.Public.Description,
				Author = json.Public.Author,
				ActivityName = json.Public.ActivityName,
				ActivityType = (ActivityType)Enum.Parse(typeof(ActivityType), json.Public.ActivityType.Value),
			};
		}

		[Obsolete]
		public bool IsUserAuthorised(DiscordUser user) {
			return false; //return AuthorisedUsers.Exists((User u) => { return u.Name == user.Username && u.Discriminator == user.Discriminator; });
		}
	}
}
