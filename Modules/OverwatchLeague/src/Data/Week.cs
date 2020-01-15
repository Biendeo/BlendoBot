using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OverwatchLeague.Data {
	public class Week {
		public int WeekNumber { get; private set; }
		public string Name { get; private set; }
		public Stage Stage { get; private set; }
		private readonly List<Match> matches;
		public ReadOnlyCollection<Match> Matches { get { return matches.AsReadOnly(); } }

		public bool IsCurrent { get {
			return matches.Exists(m => m.StartTime < DateTime.Now) && matches.Exists(m => m.EndTime > DateTime.Now);
		} }

		public DateTime FirstStartTime { get {
			return matches.Min(m => m.StartTime);
		} }

		public DateTime LastEndTime { get {
			return matches.Max(m => m.EndTime);
		} }

		public Week(int weekNumber, string name) {
			WeekNumber = weekNumber;
			Name = name;
			Stage = null;
			matches = new List<Match>();
		}

		public void AddMatch(Match match) {
			matches.Add(match);
		}

		public void SetStage(Stage stage) {
			Stage = stage;
		}
	}
}
