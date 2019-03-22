using System;
using System.Collections.Generic;
using System.Text;

namespace OverwatchLeague.Data {
	public class MatchGame {
		public int Id { get; private set; }
		public int MapNumber { get; private set; }
		public int HomeScore { get; private set; }
		public int AwayScore { get; private set; }
		public Match Match { get; private set; }
		public Map Map { get; private set; }
		public string status { get; private set; }
		public MatchStatus Status { get { return MatchStatusExtensions.FromString(status); } }

		public MatchGame(int id, int mapNumber, int homeScore, int awayScore, Match match, Map map, string status) {
			Id = id;
			MapNumber = mapNumber;
			HomeScore = homeScore;
			AwayScore = awayScore;
			Match = match;
			Map = map;
			this.status = status;
		}

		public void SetHomeScore(int score) {
			HomeScore = score;
		}

		public void SetAwayScore(int score) {
			AwayScore = score;
		}

		public void SetMap(Map map) {
			Map = map;
		}

		public void SetStatus(string status) {
			this.status = status;
		}
	}
}
