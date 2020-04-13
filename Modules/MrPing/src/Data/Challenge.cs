using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MrPing.Data {
	public class Challenge {
		public Challenge()
		{
			// parameterless ctor for System.Text.Json to use
		}

		public Challenge(DateTime startTime, DiscordChannel channel, DiscordUser author, DiscordUser target, int targetPings) {
			StartTime = startTime;
			ChannelId = channel.Id;
			AuthorId = author.Id;
			TargetId = target.Id;
			TargetPings = targetPings;
			seenPings = new Dictionary<string, int>();
		}

		[JsonPropertyName("startTime")]
		public DateTime StartTime { get; set; }

		[JsonIgnore]
		public DateTime EndTime {
			get {
				return StartTime.AddMinutes(10);
			}
		}

		[JsonIgnore]
		public TimeSpan TimeRemaining {
			get {
				return EndTime - DateTime.Now;
			}
		}


		[JsonPropertyName("channelId")]
		public ulong ChannelId { get; set; }

		[JsonPropertyName("authorId")]
		public ulong AuthorId { get; set; }

		[JsonPropertyName("targetId")]
		public ulong TargetId { get; set; }

		[JsonPropertyName("targetPings")]
		public int TargetPings { get; set; }

		[JsonPropertyName("seenPings")]
		public Dictionary<string, int> seenPings { get; set; }

		[JsonIgnore]
		public int TotalPings {
			get {
				return seenPings.Values.Sum();
			}
		}

		public List<Tuple<ulong, int>> SeenPings { get {
			var l = new List<Tuple<ulong, int>>();
			foreach (var kvp in this.seenPings)
			{
				l.Add(new Tuple<ulong, int>(ulong.Parse(kvp.Key), kvp.Value));
			}
			l.Sort((a, b) => a.Item2.CompareTo(a.Item1));
			return l;
		}}

		public bool Completed {
			get {
				return TotalPings >= TargetPings;
			}
		}

		public void AddPing(DiscordUser pingUser) {
			if (!seenPings.ContainsKey(pingUser.Id.ToString())) {
				seenPings.Add(pingUser.Id.ToString(), 0);
			}
			++seenPings[pingUser.Id.ToString()];
		}
	}
}
