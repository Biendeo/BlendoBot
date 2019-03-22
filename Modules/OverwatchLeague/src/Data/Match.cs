using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;

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
		private Timer updateTimer;


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

			updateTimer = new Timer();
			updateTimer.Elapsed += UpdateTimer_Elapsed;
			
			// No point attempting to update the map if it won't start within 24 hours, since the whole
			// DB is being updated anyways. This gets around a weird bug where the time start is more
			// than int32Max ticks away.
			if ((StartTime - DateTime.UtcNow) < TimeSpan.FromDays(1)) {
				if (DateTime.UtcNow < StartTime) {
					updateTimer.Interval = (StartTime - DateTime.UtcNow).TotalMilliseconds;
					updateTimer.Enabled = true;
				} else if (DateTime.UtcNow < EndTime) {
					updateTimer.Interval = 1000 * 60;
					updateTimer.Enabled = true;
				}
			} else {
				updateTimer.Enabled = false;
			}
		}

		private async void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e) {
			if (DateTime.UtcNow < StartTime) {
				updateTimer.Interval = (StartTime - DateTime.UtcNow).TotalMilliseconds;
				return;
			} else if (DateTime.UtcNow < EndTime) {
				updateTimer.Interval = 1000 * 60;

				using (var wc = new WebClient()) {
					string matchJsonString = await wc.DownloadStringTaskAsync($"https://api.overwatchleague.com/matches/{Id}");
					dynamic matchJson = JsonConvert.DeserializeObject(matchJsonString);

					if (HomeTeam == null && matchJson.competitors[0] != null) {
						int teamId = matchJson.competitors[0].id;
						HomeTeam = (from c in OverwatchLeague.Database.Teams where c.Id == teamId select c).First();
					}

					if (AwayTeam == null && matchJson.competitors[1] != null) {
						int teamId = matchJson.competitors[1].id;
						AwayTeam = (from c in OverwatchLeague.Database.Teams where c.Id == teamId select c).First();
					}

					if (matchJson["scores"] != null) {
						HomeScore = matchJson.scores[0].value;
						AwayScore = matchJson.scores[1].value;
					}

					status = matchJson.status;
					StartTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)matchJson.startDate);
					EndTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)matchJson.endDate);
					if (matchJson["actualStartTime"] != null) {
						ActualStartTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)matchJson.actualStartTime);
					}
					if (matchJson["actualEndTime"] != null) {
						ActualEndTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)matchJson.actualEndTime);
					}

					foreach (var game in matchJson.games) {
						int gameId = game.id;
						MatchGame g = games.Find(x => x.Id == gameId);

						if (g == null) {
							int gameNumber = game.number;
							int gameHomePoints = 0;
							int gameAwayPoints = 0;
							if (game["points"] != null) {
								gameHomePoints = game.points[0];
								gameAwayPoints = game.points[1];
							}
							ulong mapGuid = 0;
							if (game["attributes"]["mapGuid"] != null) {
								mapGuid = Convert.ToUInt64(game.attributes.mapGuid.Value, 16);
							}
							string gameStatus = game.status;
							Map map = (from c in OverwatchLeague.Database.Maps where c.Guid == mapGuid select c).First();

							g = new MatchGame(gameId, gameNumber, gameHomePoints, gameAwayPoints, this, map, gameStatus);

							if (map != null) {
								map.AddGame(g);
							}
							AddGame(g);
						} else {
							if (game["points"] != null) {
								g.SetHomeScore(game.points[0]);
								g.SetAwayScore(game.points[1]);
							}
							if (g.Map == null) {
								ulong mapGuid = 0;
								if (game["attributes"]["mapGuid"] != null) {
									mapGuid = Convert.ToUInt64(game.attributes.mapGuid.Value, 16);
								}
								Map map = (from c in OverwatchLeague.Database.Maps where c.Guid == mapGuid select c).First();
								g.SetMap(map);
								if (map != null) {
									map.AddGame(g);
								}
							}
						}
					}
				}
			} else {
				updateTimer.Enabled = false;
			}
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
