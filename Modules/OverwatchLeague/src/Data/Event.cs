using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OverwatchLeague.Data {
	public class Event {
		public string Title { get; private set; }
		public Week Week { get; private set; }
		private readonly List<Match> matches;
		public ReadOnlyCollection<Match> Matches => matches.AsReadOnly();

		public bool IsCurrent {
			get {
				return matches.Exists(m => m.StartTime < DateTime.Now) && matches.Exists(m => m.EndTime > DateTime.Now);
			}
		}

		public DateTime FirstStartTime {
			get {
				return matches.Min(m => m.StartTime);
			}
		}

		public DateTime LastEndTime {
			get {
				return matches.Max(m => m.EndTime);
			}
		}

		public Event(string title) {
			Title = title;
			Week = null;
			matches = new List<Match>();
		}

		public void AddMatch(Match match) {
			matches.Add(match);
		}

		public void SetWeek(Week week) {
			Week = week;
		}
	}
}
