using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OverwatchLeague.Data {
	public class Match : IComparable<Match> {
		public int Id { get; private set; }
		public Team HomeTeam { get; private set; }
		public Team AwayTeam { get; private set; }
		public int HomeScore { get; private set; }
		public int AwayScore { get; private set; }
		public int DrawMaps { get { return Games.Where(x => x.HomeScore == x.AwayScore).Count(); } }
		private string status;
		public MatchStatus Status { get { return MatchStatusExtensions.FromString(status); } }
		public DateTime StartTime { get; private set; }
		public DateTime EndTime { get; private set; }
		public DateTime? ActualStartTime { get; private set; }
		public DateTime? ActualEndTime { get; private set; }
		private List<MatchGame> games;
		public ReadOnlyCollection<MatchGame> Games { get { return games.AsReadOnly(); } }
		public Week Week { get; private set; }
		public Stage Stage { get { return Week.Stage; } }


		public Match(int id, Team homeTeam, Team awayTeam, int homeScore, int awayScore, string status, DateTime startTime, DateTime endTime, DateTime? actualStartTime, DateTime? actualEndTime) {
			Id = id;
			HomeTeam = homeTeam;
			AwayTeam = awayTeam;
			HomeScore = homeScore;
			AwayScore = awayScore;
			this.status = status;
			StartTime = startTime;
			EndTime = endTime;
			ActualStartTime = actualStartTime;
			ActualEndTime = actualEndTime;

			games = new List<MatchGame>();
		}

		public void AddGame(MatchGame game) {
			games.Add(game);
		}

		public int TeamScore(Team team) {
			if (team == HomeTeam) {
				return HomeScore;
			} else if (team == AwayTeam) {
				return AwayScore;
			} else {
				return -1;
			}
		}

		public void SetWeek(Week week) {
			Week = week;
		}
		public int CompareTo(Match other) {
			return StartTime.CompareTo(other.StartTime);
		}
	}
}
