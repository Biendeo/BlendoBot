using BlendoBotLib;
using BlendoBotLib.Interfaces;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MrPing.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MrPing {
	internal class Database {
		public Database(
			ulong guildId,
			IDataStore<MrPing, List<Challenge>> challengeStore,
			IDataStore<MrPing, ServerStats> statsStore,
			IDiscordClient discordClient,
			ILogger<Database> logger)
		{
			activeChallenges = new List<Challenge>();
			stats = new ServerStats();
            this.guildId = guildId;
            this.challengeStore = challengeStore;
            this.statsStore = statsStore;
            this.discordClient = discordClient;
            this.logger = logger;

            LoadDatabase().Wait();
        }

		private List<Challenge> activeChallenges;

		private ServerStats stats;

        private readonly ulong guildId;
        private readonly IDataStore<MrPing, List<Challenge>> challengeStore;
        private readonly IDataStore<MrPing, ServerStats> statsStore;
        private readonly IDiscordClient discordClient;
        private readonly ILogger<Database> logger;

        public async Task PingUser(DiscordUser target, DiscordUser author, DiscordChannel channel) {
			PurgeFinishedChallenges();
			Challenge challenge = activeChallenges.Find(c => c.TargetId == target.Id);
			if (challenge != null && !challenge.Completed && challenge.ChannelId == channel.Id) {
				challenge.AddPing(author);
				stats.AddPing(target, author);
				if (challenge.Completed) {
					activeChallenges.Remove(challenge);
					stats.FinishChallenge(target, author);
					var sb = new StringBuilder();
					sb.AppendLine($"Mr Ping challenge completed! {target.Username} has been successfully pinged {challenge.TargetPings} times.");
					sb.AppendLine($"{author.Username} got the last ping!");
					sb.AppendLine("```");
					sb.AppendLine("Biggest contributors:");
					foreach (var tuple in challenge.SeenPings) {
						// Safety check to not print too much.
						if (sb.Length > 1950) {
							break;
						}
						sb.AppendLine($"{$"{(await this.discordClient.GetUser(tuple.Item1)).Username}"} - {tuple.Item2} pings");
					}
					sb.AppendLine("```");
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = channel,
						LogMessage = "MrPingCompletedChallenge"
					});
				}
			}
			await WriteDatabase();
		}

		public async Task NewChallenge(DiscordUser target, DiscordUser author, int pingCount, DiscordChannel channel) {
			PurgeFinishedChallenges();
			activeChallenges.Add(new Challenge(DateTime.Now, channel, author, target, pingCount));
			stats.NewChallenge(target, author, pingCount);
			await WriteDatabase();
		}

		public Task<string> GetStatsMessage() {
			return stats.GetStatsMessage(this.discordClient);
		}

		public async Task<string> GetActiveChallenges(DiscordChannel channel) {
			PurgeFinishedChallenges();
			var releventChallenges = activeChallenges.FindAll(c => c.ChannelId == channel.Id);
			if (releventChallenges.Count > 0) {
				var sb = new StringBuilder();
				sb.AppendLine("Current Mr Ping challenges:");
				int countedChallenges = 0;
				foreach (var challenge in releventChallenges) {
					// Safety check to not print too much.
					if (sb.Length > 1900) {
						break;
					}
					var targetUser = await this.discordClient.GetUser(challenge.TargetId);
					sb.AppendLine($"[{challenge.TimeRemaining.ToString(@"mm\:ss")}] {targetUser.Username} #{targetUser.Discriminator} ({challenge.TotalPings}/{challenge.TargetPings})");
					++countedChallenges;
				}
				if (countedChallenges != releventChallenges.Count) {
					sb.AppendLine($"And {releventChallenges.Count - countedChallenges} more...");
				}
				return sb.ToString();
			} else {
				return $"No active Mr Ping challenges! You should type {"?mrping".Code()}!";
			}
		}
		private void PurgeFinishedChallenges() {
			while (activeChallenges.Count > 0 && (activeChallenges[0].Completed || activeChallenges[0].EndTime < DateTime.Now)) {
				activeChallenges.RemoveAt(0);
			}
		}

		private async Task LoadDatabase() {
			try
			{
				activeChallenges = await this.challengeStore.ReadAsync(this.challengesStorePath);
			}
			catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
			{
				this.logger.LogInformation("MrPing challenges not found from data store, creating new");
				activeChallenges = new List<Challenge>();
				await this.challengeStore.WriteAsync(this.challengesStorePath, activeChallenges);
			}

			try
			{
				stats = await this.statsStore.ReadAsync(this.statsStorePath);
			}
			catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
			{
				this.logger.LogInformation("MrPing stats not found from data store, creating new");
				stats = new ServerStats();
				await this.statsStore.WriteAsync(this.statsStorePath, stats);
			}
		}

		private async Task WriteDatabase() {
			await this.challengeStore.WriteAsync(this.challengesStorePath, activeChallenges);
			await this.statsStore.WriteAsync(this.statsStorePath, stats);
		}

		private string challengesStorePath => Path.Join(this.guildId.ToString(), "challenges");
		private string statsStorePath => Path.Join(this.guildId.ToString(), "stats");
	}
}
