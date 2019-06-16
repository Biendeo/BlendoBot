using BlendoBotLib;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MrPing.Data {
	public class ServerStats {
		public ServerStats() {
			numPingsSent = new Dictionary<DiscordUser, int>();
			numChallengesReceived = new Dictionary<DiscordUser, int>();
			numChallengesSent = new Dictionary<DiscordUser, int>();
			numPingsPrescribed = new Dictionary<DiscordUser, int>();
			numPingsReceived = new Dictionary<DiscordUser, int>();
			numChallengesCompleted = new Dictionary<DiscordUser, int>();
			numChallengesSelfFinished = new Dictionary<DiscordUser, int>();
		}

		private Dictionary<DiscordUser, int> numPingsSent;
		private Dictionary<DiscordUser, int> numChallengesReceived;
		private Dictionary<DiscordUser, int> numChallengesSent;
		private Dictionary<DiscordUser, int> numPingsPrescribed;
		private Dictionary<DiscordUser, int> numPingsReceived;
		private Dictionary<DiscordUser, int> numChallengesCompleted;
		private Dictionary<DiscordUser, int> numChallengesSelfFinished;
		private Dictionary<DiscordUser, double> percentageSuccessfulPings {
			get {
				var d = new Dictionary<DiscordUser, double>();
				foreach (var user in numPingsReceived.Keys) {
					d.Add(user, numPingsReceived[user] * 1.0 / numPingsPrescribed[user]);
				}
				return d;
			}
		}

		public string GetStatsMessage() {
			var sb = new StringBuilder();
			sb.AppendLine("Mr Ping Challenge Stats:");
			if (numPingsSent.Count > 0) {
				var mostActiveUser = numPingsSent.First(u => u.Value == numPingsSent.Values.Max());
				sb.AppendLine($"{"Most active fella".Bold()} - {mostActiveUser.Key.Username} #{mostActiveUser.Key.Discriminator} ({$"{mostActiveUser.Value} pings sent".Italics()})");
			}
			if (numPingsReceived.Count > 0) {
				var mostPopularUser = numPingsReceived.First(u => u.Value == numPingsReceived.Values.Max());
				sb.AppendLine($"{"Most popular prize".Bold()} - {mostPopularUser.Key.Username} #{mostPopularUser.Key.Discriminator} ({$"{mostPopularUser.Value} pings received".Italics()})");
			}
			if (numChallengesReceived.Count > 0) {
				var unluckiestUser = numChallengesReceived.FirstOrDefault(u => u.Value == numChallengesReceived.Values.Max());
				sb.AppendLine($"{"Unluckiest person".Bold()} - {unluckiestUser.Key.Username} #{unluckiestUser.Key.Discriminator} ({$"{unluckiestUser.Value} challenges received".Italics()})");
			}
			if (numChallengesSent.Count > 0) {
				var cruelistUser = numChallengesSent.First(u => u.Value == numChallengesSent.Values.Max());
				sb.AppendLine($"{"Cruelist crew member".Bold()} - {cruelistUser.Key.Username} #{cruelistUser.Key.Discriminator} ({$"{cruelistUser.Value} challenges issued".Italics()})");
			}
			if (numChallengesCompleted.Count > 0) {
				var successfulUser = numChallengesCompleted.First(u => u.Value == numChallengesCompleted.Values.Max());
				sb.AppendLine($"{"Most successful dude".Bold()} - {successfulUser.Key.Username} #{successfulUser.Key.Discriminator} ({$"{successfulUser.Value} successful challenges".Italics()})");
			}
			if (numChallengesSelfFinished.Count > 0) {
				var stealerUser = numChallengesSelfFinished.First(u => u.Value == numChallengesSelfFinished.Values.Max());
				sb.AppendLine($"{"Ping stealer".Bold()} - {stealerUser.Key.Username} #{stealerUser.Key.Discriminator} ({$"{stealerUser.Value} challenges personally finished".Italics()})");
			}
			if (percentageSuccessfulPings.Count > 0) {
				var reliableTarget = percentageSuccessfulPings.First(u => u.Value == percentageSuccessfulPings.Values.Max());
				sb.AppendLine($"{"Easy target".Bold()} - {reliableTarget.Key.Username} #{reliableTarget.Key.Discriminator} ({$"{(reliableTarget.Value * 100.0).ToString("0.0000")}% ping success rate".Italics()})");
			}
			return sb.ToString();
		}

		public void NewChallenge(DiscordUser target, DiscordUser author, int pingCount) {
			if (!numChallengesReceived.ContainsKey(target)) {
				numChallengesReceived.Add(target, 1);
			} else {
				++numChallengesReceived[target];
			}
			if (!numChallengesSent.ContainsKey(author)) {
				numChallengesSent.Add(author, 1);
			} else {
				++numChallengesSent[author];
			}
			if (!numPingsPrescribed.ContainsKey(target)) {
				numPingsPrescribed.Add(target, pingCount);
			} else {
				numPingsPrescribed[target] += pingCount;
			}
		}

		public void AddPing(DiscordUser target, DiscordUser author) {
			if (!numPingsSent.ContainsKey(author)) {
				numPingsSent.Add(author, 1);
			} else {
				++numPingsSent[author];
			}
			if (!numPingsReceived.ContainsKey(target)) {
				numPingsReceived.Add(target, 1);
			} else {
				++numPingsReceived[target];
			}
		}

		public void FinishChallenge(DiscordUser target, DiscordUser author) {
			if (!numChallengesSelfFinished.ContainsKey(author)) {
				numChallengesSelfFinished.Add(author, 1);
			} else {
				++numChallengesSelfFinished[author];
			}
			if (!numChallengesCompleted.ContainsKey(target)) {
				numChallengesCompleted.Add(target, 1);
			} else {
				++numChallengesCompleted[target];
			}
		}
	}
}