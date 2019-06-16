using BlendoBotLib;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MrPing.Data {
	[JsonObject(MemberSerialization.OptIn)]
	class Server {
		[JsonProperty(Required = Required.Always)]
		private List<Challenge> activeChallenges;
		[JsonProperty(Required = Required.Always)]
		private ServerStats stats;

		public Server() {
			activeChallenges = new List<Challenge>();
			stats = new ServerStats();
		}

		public async Task PingUser(DiscordUser target, DiscordUser author, DiscordChannel channel) {
			Challenge challenge = activeChallenges.Find(c => c.Target == target);
			if (challenge != null && !challenge.Completed && challenge.Channel == channel) {
				challenge.AddPing(author);
				stats.AddPing(target, author);
				if (challenge.Completed) {
					activeChallenges.Remove(challenge);
					stats.FinishChallenge(target, author);
					var sb = new StringBuilder();
					sb.AppendLine("Mr Ping challenge completed!");
					sb.AppendLine("```");
					sb.AppendLine("Biggest contributors:");
					foreach (var contributor in challenge.SeenPings) {
						sb.AppendLine($"{$"{contributor.Item1.Username}".PadLeft(20, ' ')} - {contributor.Item2} pings");
					}
					sb.AppendLine("```");
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = channel,
						LogMessage = "MrPingCompletedChallenge"
					});
				}
			}
		}

		public void NewChallenge(DiscordUser target, DiscordUser author, int pingCount, DiscordChannel channel) {
			activeChallenges.Add(new Challenge(DateTime.Now, channel, author, target, pingCount));
			stats.NewChallenge(target, author, pingCount);
		}

		public string GetStatsMessage() {
			return stats.GetStatsMessage();
		}

		public string GetActiveChallenges() {
			var sb = new StringBuilder();
			sb.AppendLine("Current Mr Ping challenges:");
			foreach (var challenge in activeChallenges) {
				sb.AppendLine($"[{challenge.TimeRemaining.ToString(@"hh\:mm\:ss")}] {challenge.Target.Username} #{challenge.Target.Discriminator} ({challenge.TotalPings}/{challenge.TargetPings})");
			}
			return sb.ToString();
		}
	}
}
