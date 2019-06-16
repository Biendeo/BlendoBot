using DSharpPlus.Entities;
using MrPing.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MrPing {
	internal class Database {
		public Database() {
			servers = new Dictionary<ulong, Server>();
			if (Directory.Exists("mrping")) {
				foreach (var file in new DirectoryInfo("mrping").GetFiles("*.json")) {
					servers.Add(ulong.Parse(file.Name.Substring(0, file.Name.Length - 5)), JsonConvert.DeserializeObject<Server>(File.ReadAllText(file.FullName)));
				}
			} else {
				Directory.CreateDirectory("mrping");
			}
		}

		private Dictionary<ulong, Server> servers;

		public async Task PingUser(DiscordUser target, DiscordUser author, DiscordGuild server, DiscordChannel channel) {
			if (servers.ContainsKey(server.Id)) {
				servers[server.Id].PurgeFinishedChallenges();
				await servers[server.Id].PingUser(target, author, channel);
				File.WriteAllText(Path.Combine("mrping", $"{server.Id}.json"), JsonConvert.SerializeObject(servers[server.Id]));
			}
		}

		public void NewChallenge(DiscordUser target, DiscordUser author, int pingCount, DiscordGuild server, DiscordChannel channel) {
			if (!servers.ContainsKey(server.Id)) {
				servers.Add(server.Id, new Server());
			} else {
				servers[server.Id].PurgeFinishedChallenges();
			}
			servers[server.Id].NewChallenge(target, author, pingCount, channel);
			File.WriteAllText(Path.Combine("mrping", $"{server.Id}.json"), JsonConvert.SerializeObject(servers[server.Id]));
		}

		public string GetStatsMessage(DiscordGuild server) {
			return servers.ContainsKey(server.Id) ? servers[server.Id].GetStatsMessage() : "No challenges have been made on this server!";
		}

		public string GetActiveChallenges(DiscordGuild server) {
			return servers.ContainsKey(server.Id) ? servers[server.Id].GetActiveChallenges() : "No challenges have been made on this server!";
		}
	}
}
