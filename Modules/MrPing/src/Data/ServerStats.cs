using BlendoBotLib;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MrPing.Data {
	[JsonObject(MemberSerialization.OptIn)]
	public class ServerStats {
		public ServerStats() {
			users = new Dictionary<ulong, DiscordUser>();
			numPingsSent = new Dictionary<ulong, int>();
			numChallengesReceived = new Dictionary<ulong, int>();
			numChallengesSent = new Dictionary<ulong, int>();
			numPingsPrescribed = new Dictionary<ulong, int>();
			numPingsReceived = new Dictionary<ulong, int>();
			numChallengesCompleted = new Dictionary<ulong, int>();
			numChallengesSelfFinished = new Dictionary<ulong, int>();
		}

		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, DiscordUser> users;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numPingsSent;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numChallengesReceived;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numChallengesSent;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numPingsPrescribed;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numPingsReceived;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numChallengesCompleted;
		[JsonProperty(Required = Required.Always)]
		private readonly Dictionary<ulong, int> numChallengesSelfFinished;
		private Dictionary<ulong, double> PercentageSuccessfulPings {
			get {
				var d = new Dictionary<ulong, double>();
				foreach (ulong user in numPingsReceived.Keys) {
					d.Add(user, numPingsReceived[user] * 1.0 / (numPingsPrescribed[user] > 0 ? numPingsPrescribed[user] : 1));
				}
				return d;
			}
		}

		public string GetStatsMessage() {
			var sb = new StringBuilder();
			sb.AppendLine("Mr Ping Challenge Stats:");
			if (numPingsSent.Count > 0) {
				var mostActiveUser = numPingsSent.First(u => u.Value == numPingsSent.Values.Max());
				sb.AppendLine($"{"Most active fella".Bold()} - {users[mostActiveUser.Key].Username} #{users[mostActiveUser.Key].Discriminator} ({$"{mostActiveUser.Value} pings sent".Italics()})");
			}
			if (numPingsReceived.Count > 0) {
				var mostPopularUser = numPingsReceived.First(u => u.Value == numPingsReceived.Values.Max());
				sb.AppendLine($"{"Most popular prize".Bold()} - {users[mostPopularUser.Key].Username} #{users[mostPopularUser.Key].Discriminator} ({$"{mostPopularUser.Value} pings received".Italics()})");
			}
			if (numChallengesReceived.Count > 0) {
				var unluckiestUser = numChallengesReceived.FirstOrDefault(u => u.Value == numChallengesReceived.Values.Max());
				sb.AppendLine($"{"Unluckiest person".Bold()} - {users[unluckiestUser.Key].Username} #{users[unluckiestUser.Key].Discriminator} ({$"{unluckiestUser.Value} challenges received".Italics()})");
			}
			if (numChallengesSent.Count > 0) {
				var cruelistUser = numChallengesSent.First(u => u.Value == numChallengesSent.Values.Max());
				sb.AppendLine($"{"Cruelist crew member".Bold()} - {users[cruelistUser.Key].Username} #{users[cruelistUser.Key].Discriminator} ({$"{cruelistUser.Value} challenges issued".Italics()})");
			}
			if (numChallengesCompleted.Count > 0) {
				var successfulUser = numChallengesCompleted.First(u => u.Value == numChallengesCompleted.Values.Max());
				sb.AppendLine($"{"Most successful dude".Bold()} - {users[successfulUser.Key].Username} #{users[successfulUser.Key].Discriminator} ({$"{successfulUser.Value} successful challenges".Italics()})");
			}
			if (numChallengesSelfFinished.Count > 0) {
				var stealerUser = numChallengesSelfFinished.First(u => u.Value == numChallengesSelfFinished.Values.Max());
				sb.AppendLine($"{"Ping stealer".Bold()} - {users[stealerUser.Key].Username} #{users[stealerUser.Key].Discriminator} ({$"{stealerUser.Value} challenges personally finished".Italics()})");
			}
			if (PercentageSuccessfulPings.Count > 0) {
				var reliableTarget = PercentageSuccessfulPings.First(u => u.Value == PercentageSuccessfulPings.Values.Max());
				sb.AppendLine($"{"Easy target".Bold()} - {users[reliableTarget.Key].Username} #{users[reliableTarget.Key].Discriminator} ({$"{(reliableTarget.Value * 100.0).ToString("0.0000")}% ping success rate".Italics()})");
			}
			return sb.ToString();
		}

		public void NewChallenge(DiscordUser target, DiscordUser author, int pingCount) {
			ConfirmUserIsInTable(target);
			ConfirmUserIsInTable(author);
			++numChallengesReceived[target.Id];
			++numChallengesSent[author.Id];
			numPingsPrescribed[target.Id] += pingCount;
		}

		public void AddPing(DiscordUser target, DiscordUser author) {
			ConfirmUserIsInTable(target);
			ConfirmUserIsInTable(author);
			++numPingsSent[author.Id];
			++numPingsReceived[target.Id];
		}

		public void FinishChallenge(DiscordUser target, DiscordUser author) {
			ConfirmUserIsInTable(target);
			ConfirmUserIsInTable(author);
			++numChallengesSelfFinished[author.Id];
			++numChallengesCompleted[target.Id];
		}

		private void ConfirmUserIsInTable(DiscordUser user) {
			if (!users.ContainsKey(user.Id)) {
				users.Add(user.Id, user);
				numPingsSent.Add(user.Id, 0);
				numChallengesReceived.Add(user.Id, 0);
				numChallengesSent.Add(user.Id, 0);
				numPingsPrescribed.Add(user.Id, 0);
				numPingsReceived.Add(user.Id, 0);
				numChallengesCompleted.Add(user.Id, 0);
				numChallengesSelfFinished.Add(user.Id, 0);
			} else {
				users[user.Id] = user;
			}
		}
	}
}