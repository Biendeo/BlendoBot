using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OverwatchLeague.Data {
	public class Stage {
		public int StageNumber { get; private set; }
		public string Name { get; private set; }
		private List<Week> weeks;
		public ReadOnlyCollection<Week> Weeks { get { return weeks.AsReadOnly(); } }
		public Week Playoffs { get; private set; }

		public Stage(int stageNumber, string name) {
			StageNumber = stageNumber;
			Name = name;
			weeks = new List<Week>();
			Playoffs = null;
		}

		public void AddWeek(Week week) {
			weeks.Add(week);
		}

		public void SetPlayoffs(Week playoffWeek) {
			Playoffs = playoffWeek;
		}
	}
}
