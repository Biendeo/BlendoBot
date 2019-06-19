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
					sb.AppendLine($"Mr Ping challenge completed! {target.Username} has been successfully pinged {challenge.TargetPings} times.");
					sb.AppendLine($"{author.Username} got the last ping!");
					sb.AppendLine("```");
					sb.AppendLine("Biggest contributors:");
					foreach (var contributor in challenge.SeenPings) {
						// Safety check to not print too much.
						if (sb.Length > 1950) {
							break;
						}
						sb.AppendLine($"{$"{contributor.Item1.Username}"} - {contributor.Item2} pings");
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

		public string GetActiveChallenges(DiscordChannel channel) {
			PurgeFinishedChallenges();
			var releventChallenges = activeChallenges.FindAll(c => c.Channel == channel);
			if (releventChallenges.Count > 0) {
				var sb = new StringBuilder();
				sb.AppendLine("Current Mr Ping challenges:");
				int countedChallenges = 0;
				foreach (var challenge in releventChallenges) {
						// Safety check to not print too much.
					if (sb.Length > 1900) {
						break;
					}
					sb.AppendLine($"[{challenge.TimeRemaining.ToString(@"mm\:ss")}] {challenge.Target.Username} #{challenge.Target.Discriminator} ({challenge.TotalPings}/{challenge.TargetPings})");
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

		public void PurgeFinishedChallenges() {
			while (activeChallenges.Count > 0 && (activeChallenges[0].Completed || activeChallenges[0].EndTime < DateTime.Now)) {
				activeChallenges.RemoveAt(0);
			}
		}
	}
}
