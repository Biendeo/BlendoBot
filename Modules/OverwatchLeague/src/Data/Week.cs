using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OverwatchLeague.Data {
	public class Week {
		public int WeekNumber { get; private set; }
		public string Name { get; private set; }
		public Stage Stage { get; private set; }
		private List<Match> matches;
		public ReadOnlyCollection<Match> Matches { get { return matches.AsReadOnly(); } }

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
