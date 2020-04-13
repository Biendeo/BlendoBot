using BlendoBotLib;
using BlendoBotLib.Interfaces;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MrPing.Data {
	public class ServerStats {
		public ServerStats() {
			numPingsSent = new Dictionary<string, int>();
			numChallengesReceived = new Dictionary<string, int>();
			numChallengesSent = new Dictionary<string, int>();
			numPingsPrescribed = new Dictionary<string, int>();
			numPingsReceived = new Dictionary<string, int>();
			numChallengesCompleted = new Dictionary<string, int>();
			numChallengesSelfFinished = new Dictionary<string, int>();
		}

		[JsonPropertyName("numPingsSent")]
		public Dictionary<string, int> numPingsSent { get; set; }

		[JsonPropertyName("numChallengesReceived")]
		public Dictionary<string, int> numChallengesReceived { get; set; }

		[JsonPropertyName("numChallengesSent")]
		public Dictionary<string, int> numChallengesSent { get; set; }

		[JsonPropertyName("numPingsPrescribed")]
		public Dictionary<string, int> numPingsPrescribed { get; set; }

		[JsonPropertyName("numPingsReceived")]
		public Dictionary<string, int> numPingsReceived { get; set; }

		[JsonPropertyName("numChallengesCompleted")]
		public Dictionary<string, int> numChallengesCompleted { get; set; }

		[JsonPropertyName("numChallengesSelfFinished")]
		public Dictionary<string, int> numChallengesSelfFinished { get; set; }

		[JsonIgnore]
		private Dictionary<ulong, double> PercentageSuccessfulPings {
			get {
				var d = new Dictionary<ulong, double>();
				foreach (var user in numPingsReceived.Keys) {
					d.Add(ulong.Parse(user), numPingsReceived[user] * 1.0 / (numPingsPrescribed[user] > 0 ? numPingsPrescribed[user] : 1));
				}
				return d;
			}
		}

		public async Task<string> GetStatsMessage(IDiscordClient discordClient) {
			var sb = new StringBuilder();
			sb.AppendLine("Mr Ping Challenge Stats:");
			if (numPingsSent.Count > 0) {
				var mostActiveUser = numPingsSent.First(u => u.Value == numPingsSent.Values.Max());
				var user = await discordClient.GetUser(ulong.Parse(mostActiveUser.Key));
				sb.AppendLine($"{"Most active fella".Bold()} - {user.Username} #{user.Discriminator} ({$"{mostActiveUser.Value} pings sent".Italics()})");
			}
			if (numPingsReceived.Count > 0) {
				var mostPopularUser = numPingsReceived.First(u => u.Value == numPingsReceived.Values.Max());
				var user = await discordClient.GetUser(ulong.Parse(mostPopularUser.Key));
				sb.AppendLine($"{"Most popular prize".Bold()} - {user.Username} #{user.Discriminator} ({$"{mostPopularUser.Value} pings received".Italics()})");
			}
			if (numChallengesReceived.Count > 0) {
				var unluckiestUser = numChallengesReceived.FirstOrDefault(u => u.Value == numChallengesReceived.Values.Max());
				var user = await discordClient.GetUser(ulong.Parse(unluckiestUser.Key));
				sb.AppendLine($"{"Unluckiest person".Bold()} - {user.Username} #{user.Discriminator} ({$"{unluckiestUser.Value} challenges received".Italics()})");
			}
			if (numChallengesSent.Count > 0) {
				var cruelistUser = numChallengesSent.First(u => u.Value == numChallengesSent.Values.Max());
				var user = await discordClient.GetUser(ulong.Parse(cruelistUser.Key));
				sb.AppendLine($"{"Cruelist crew member".Bold()} - {user.Username} #{user.Discriminator} ({$"{cruelistUser.Value} challenges issued".Italics()})");
			}
			if (numChallengesCompleted.Count > 0) {
				var successfulUser = numChallengesCompleted.First(u => u.Value == numChallengesCompleted.Values.Max());
				var user = await discordClient.GetUser(ulong.Parse(successfulUser.Key));
				sb.AppendLine($"{"Most successful dude".Bold()} - {user.Username} #{user.Discriminator} ({$"{successfulUser.Value} successful challenges".Italics()})");
			}
			if (numChallengesSelfFinished.Count > 0) {
				var stealerUser = numChallengesSelfFinished.First(u => u.Value == numChallengesSelfFinished.Values.Max());
				var user = await discordClient.GetUser(ulong.Parse(stealerUser.Key));
				sb.AppendLine($"{"Ping stealer".Bold()} - {user.Username} #{user.Discriminator} ({$"{stealerUser.Value} challenges personally finished".Italics()})");
			}
			if (PercentageSuccessfulPings.Count > 0) {
				var reliableTarget = PercentageSuccessfulPings.First(u => u.Value == PercentageSuccessfulPings.Values.Max());
				var user = await discordClient.GetUser(reliableTarget.Key);
				sb.AppendLine($"{"Easy target".Bold()} - {user.Username} #{user.Discriminator} ({$"{(reliableTarget.Value * 100.0).ToString("0.0000")}% ping success rate".Italics()})");
			}
			return sb.ToString();
		}

		public void NewChallenge(DiscordUser target, DiscordUser author, int pingCount) {
			ConfirmUserIsInTable(target);
			ConfirmUserIsInTable(author);
			++numChallengesReceived[target.Id.ToString()];
			++numChallengesSent[author.Id.ToString()];
			numPingsPrescribed[target.Id.ToString()] += pingCount;
		}

		public void AddPing(DiscordUser target, DiscordUser author) {
			ConfirmUserIsInTable(target);
			ConfirmUserIsInTable(author);
			++numPingsSent[author.Id.ToString()];
			++numPingsReceived[target.Id.ToString()];
		}

		public void FinishChallenge(DiscordUser target, DiscordUser author) {
			ConfirmUserIsInTable(target);
			ConfirmUserIsInTable(author);
			++numChallengesSelfFinished[author.Id.ToString()];
			++numChallengesCompleted[target.Id.ToString()];
		}

		private void ConfirmUserIsInTable(DiscordUser user) {
			string userIdStr = user.Id.ToString();
			if (!numPingsSent.ContainsKey(userIdStr)) {
				numPingsSent.Add(userIdStr, 0);
				numChallengesReceived.Add(userIdStr, 0);
				numChallengesSent.Add(userIdStr, 0);
				numPingsPrescribed.Add(userIdStr, 0);
				numPingsReceived.Add(userIdStr, 0);
				numChallengesCompleted.Add(userIdStr, 0);
				numChallengesSelfFinished.Add(userIdStr, 0);
			}
		}
	}
}