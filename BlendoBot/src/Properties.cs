using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendoBot {
	public struct Properties {
		private struct User {
			public string Name;
			public string Discriminator;
		}
		public string Token;
		public string Name;
		public string Version;
		public string Description;
		public string Author;
		public string ActivityName;
		public ActivityType ActivityType;
		private List<User> AuthorisedUsers;

		public static Properties FromJson(string filePath) {
			dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(filePath));
			// Is there a smoother way of doing this?
			var authorised = new List<User>(json.Private.AuthorisedClients.Count);
			foreach (dynamic u in json.Private.AuthorisedClients) {
				authorised.Add(new User { Name = u.Name, Discriminator = u.Discriminator });
			}

			return new Properties {
				Token = json.Private.Token,
				Name = json.Public.Name,
				Version = json.Public.Version,
				Description = json.Public.Description,
				Author = json.Public.Author,
				ActivityName = json.Public.ActivityName,
				ActivityType = (ActivityType)Enum.Parse(typeof(ActivityType), json.Public.ActivityType.Value),
				AuthorisedUsers = authorised
			};
		}

		public bool IsUserAuthorised(DiscordUser user) {
			return AuthorisedUsers.Exists((User u) => { return u.Name == user.Username && u.Discriminator == user.Discriminator; });
		}
	}
}
