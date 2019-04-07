using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OverwatchLeague.Data {
	public class Stage {
		public int StageNumber { get; private set; }
		public string Name { get; private set; }
		private List<Week> weeks;
		public ReadOnlyCollection<Week> Weeks { get { return weeks.AsReadOnly(); } }
		public Week Playoffs { get; private set; }
		public bool IsCurrent { get {
			return weeks.Exists(w => w.IsCurrent);
		} }

		public Week CurrentWeek { get {
			return weeks.Find(w => w.LastEndTime > DateTime.Now);
		} }

		public DateTime FirstStartTime { get {
			return weeks.Min(w => w.FirstStartTime);
		} }

		public DateTime LastEndTime { get {
			return weeks.Max(w => w.LastEndTime);
		} }


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
