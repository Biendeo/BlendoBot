using DSharpPlus.Entities;
using MrPing.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MrPing {
	internal class Database {
		public Database() {
			servers = new Dictionary<ulong, Server>();
		}

		private Dictionary<ulong, Server> servers;

		public async Task PingUser(DiscordUser target, DiscordUser author, DiscordGuild server, DiscordChannel channel) {
			if (servers.ContainsKey(server.Id)) {
				await servers[server.Id].PingUser(target, author, channel);
			}
		}

		public void NewChallenge(DiscordUser target, DiscordUser author, int pingCount, DiscordGuild server, DiscordChannel channel) {
			if (!servers.ContainsKey(server.Id)) {
				servers.Add(server.Id, new Server());
			}
			servers[server.Id].NewChallenge(target, author, pingCount, channel);
		}
	}
}
