using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MrPing.Data {
	class Challenge {
		public Challenge(DateTime startTime, DiscordChannel channel, DiscordUser author, DiscordUser target, int targetPings) {
			StartTime = startTime;
			Channel = channel;
			Author = author;
			Target = target;
			TargetPings = targetPings;
			seenPings = new Dictionary<DiscordUser, int>();
		}

		public DateTime StartTime { get; }
		public DateTime EndTime {
			get {
				return StartTime.AddDays(1);
			}
		}
		public TimeSpan TimeRemaining {
			get {
				return EndTime - DateTime.Now;
			}
		}


		public DiscordChannel Channel { get; }

		public DiscordUser Author { get; }
		public DiscordUser Target { get; }
		public int TargetPings { get; }
		private Dictionary<DiscordUser, int> seenPings;
		public int TotalPings {
			get {
				return seenPings.Values.Sum();
			}
		}
		public List<Tuple<DiscordUser, int>> SeenPings {
			get {
				var l = new List<Tuple<DiscordUser, int>>();
				foreach (var x in seenPings) {
					l.Add(new Tuple<DiscordUser, int>(x.Key, x.Value));
				}
				l.Sort((a, b) => a.Item2.CompareTo(a.Item1));
				return l;
			}
		}

		public bool Completed {
			get {
				return TotalPings >= TargetPings;
			}
		}

		public void AddPing(DiscordUser pingUser) {
			if (seenPings.ContainsKey(pingUser)) {
				++seenPings[pingUser];
			} else {
				seenPings.Add(pingUser, 1);
			}
		}
	}
}
