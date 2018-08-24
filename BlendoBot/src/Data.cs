using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendoBot {
	public class Data {
		public class ServerInfo {
			public HashSet<string> DisabledCommands;
			public HashSet<ulong> VerifiedUsers;

			public ServerInfo() {
				DisabledCommands = new HashSet<string>();
				VerifiedUsers = new HashSet<ulong>();
			}
		}

		public Dictionary<ulong, ServerInfo> Servers;

		public static readonly string DataPath = "data.json";

		private Data() {
			Servers = new Dictionary<ulong, ServerInfo>();
		}

		public static Data Load() {
			if (File.Exists(DataPath)) {
				return JsonConvert.DeserializeObject<Data>(File.ReadAllText(DataPath));
			} else {
				Data d = new Data();
				d.Save();
				return d;
			}
		}

		public void Save() {
			File.WriteAllText(DataPath, JsonConvert.SerializeObject(this));
		}

		public void VerifyData() {
			// We need to check that all of the servers that the bot is on are in our list.
			foreach (var server in Program.Discord.Guilds) {
				if (!Servers.ContainsKey(server.Key)) {
					Servers.Add(server.Key, new ServerInfo());
				}
			}
			Save();
		}

		public bool IsUserVerified(DiscordGuild guild, DiscordUser user) {
			// A user is verified if either they are an admin, or they are on the verified users list.
			if (Servers[guild.Id].VerifiedUsers.Contains(user.Id)) {
				return true;
			} else {
				return IsUserAdmin(guild, user);
			}
		}

		public bool IsUserAdmin(DiscordGuild guild, DiscordUser user) {
			// Run through their roles and see if any are admin.
			// Awkwardly, the message returns a user, not a member, so we need to find them.
			DiscordMember member = new List<DiscordMember>(guild.Members).Find(a => a.Username == user.Username && a.Discriminator == user.Discriminator);
			foreach (var role in member.Roles) {
				if (role.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed) {
					return true;
				}
			}
			return guild.EveryoneRole.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed;
		}

		public bool DisallowUser(DiscordGuild guild, DiscordUser user) {
			if (!Servers[guild.Id].VerifiedUsers.Contains(user.Id)) {
				return false;
			} else {
				Servers[guild.Id].VerifiedUsers.Remove(user.Id);
				return true;
			}
		}

		public bool AllowUser(DiscordGuild guild, DiscordUser user) {
			if (Servers[guild.Id].VerifiedUsers.Contains(user.Id)) {
				return false;
			} else {
				Servers[guild.Id].VerifiedUsers.Add(user.Id);
				return true;
			}
		}

		public bool IsCommandEnabled(string commandType, DiscordGuild guild) {
			return !Servers[guild.Id].DisabledCommands.Contains(commandType);
		}

		/// <summary>
		/// Disables the command for the given server. Returns true if it sets, false if it already exists.
		/// The sanity check for whether the command exists should be elsewhere.
		/// </summary>
		/// <param name="commandType"></param>
		/// <param name="guild"></param>
		/// <returns></returns>
		public bool DisableCommand(string commandType, DiscordGuild guild) {
			if (Servers[guild.Id].DisabledCommands.Contains(commandType)) {
				return false;
			} else {
				Servers[guild.Id].DisabledCommands.Add(commandType);
				return true;
			}
		}

		/// <summary>
		/// Enables the command for the given server. Returns true if it sets, false if it already exists.
		/// The sanity check for whether the command exists should be elsewhere.
		/// </summary>
		/// <param name="commandType"></param>
		/// <param name="guild"></param>
		/// <returns></returns>
		public bool EnableCommand(string commandType, DiscordGuild guild) {
			if (!Servers[guild.Id].DisabledCommands.Contains(commandType)) {
				return false;
			} else {
				Servers[guild.Id].DisabledCommands.Remove(commandType);
				return true;
			}
		}
	}
}
