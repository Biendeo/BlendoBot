using BlendoBotLib;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OverwatchLeague.Data {
	public class Database {
		private readonly List<Team> teams;
		public ReadOnlyCollection<Team> Teams => teams.AsReadOnly();
		private readonly List<Map> maps;
		public ReadOnlyCollection<Map> Maps => maps.AsReadOnly();
		private readonly List<GameMode> gameModes;
		public ReadOnlyCollection<GameMode> GameModes => gameModes.AsReadOnly();
		private readonly List<Match> matches;
		public ReadOnlyCollection<Match> Matches => matches.AsReadOnly();
		private readonly List<Week> weeks;
		public ReadOnlyCollection<Week> Weeks => weeks.AsReadOnly();
		private readonly Timer fullUpdateTimer;

		private readonly IBotMethods botMethods;

		public Database(IBotMethods botMethods) {
			teams = new List<Team>();
			maps = new List<Map>();
			gameModes = new List<GameMode>();
			matches = new List<Match>();
			weeks = new List<Week>();

			this.botMethods = botMethods;

			// The next time to update should always
			fullUpdateTimer = new Timer((NextFullUpdate() - DateTime.UtcNow).TotalMilliseconds);
			fullUpdateTimer.Elapsed += FullUpdateTimer_Elapsed;
			fullUpdateTimer.Enabled = true;
		}

		private async void FullUpdateTimer_Elapsed(object sender, ElapsedEventArgs e) {
			try {
				botMethods.Log(this, new LogEventArgs {
					Type = LogType.Log,
					Message = $"OverwatchLeague is performing a full update."
				});
				await ReloadDatabase();
				botMethods.Log(this, new LogEventArgs {
					Type = LogType.Log,
					Message = $"OverwatchLeague will fully update again at {NextFullUpdate().ToString("yyyy-MM-dd HH:mm:ss")}"
				});
			} catch (WebException exc) {
				botMethods.Log(this, new LogEventArgs {
					Type = LogType.Error,
					Message = $"OverwatchLeague failed to update, trying again at {NextFullUpdate().ToString("yyyy-MM-dd HH:mm:ss")}\n{exc}"
				});
			}
			fullUpdateTimer.Interval = (NextFullUpdate() - DateTime.UtcNow).TotalMilliseconds;
		}

		public void Clear() {
			teams.Clear();
			maps.Clear();
			gameModes.Clear();
			matches.Clear();
			weeks.Clear();
		}

		public async Task ReloadDatabase() {
			Clear();
			await LoadTeams();
			await LoadMapsAndModes();
			//await LoadMatches();
			await LoadSchedule();
		}

		private async Task LoadTeams() {
			botMethods.Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"OverwatchLeague is requesting teams..."
			});

			//? BEGIN TESTING
			using var httpClient = new HttpClient();
			var uri = new Uri("https://wzavfvwgfk.execute-api.us-east-2.amazonaws.com/production/owl/paginator/schedule?stage=regular_season&season=2020&locale=en-us");
			string referer = "https://overwatchleague.com/en-us/schedule?stage=regular_season&week=2";
			using var getMessage = new HttpRequestMessage {
				Method = HttpMethod.Get,
				RequestUri = uri
			};
			getMessage.Headers.Add("Referer", referer);
			var getResponse = await httpClient.SendAsync(getMessage);
			var getResponseString = await getResponse.Content.ReadAsStringAsync();
			//? END TESTING

			using var wc = new WebClient();

			string teamsJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/v2/teams");
			dynamic teamsJson = JsonConvert.DeserializeObject(teamsJsonString);

			// Then note the teams, and then we'll link back to the divisions.
			foreach (var team in teamsJson.data) {
				Color primaryColor = Color.FromArgb(Convert.ToInt32(team.colors.primary.Value, 16));
				Color secondaryColor = Color.FromArgb(Convert.ToInt32(team.colors.secondary.Value, 16));
				Color tertiaryColor = Color.FromArgb(Convert.ToInt32(team.colors.tertiary.Value, 16));
				int id = team.id;
				string name = team.name;
				string abbreviatedName = team.abbreviatedName;
				Team newTeam = new Team(id, name, abbreviatedName, primaryColor, secondaryColor, tertiaryColor);
				teams.Add(newTeam);
			}
		}

		private async Task LoadMapsAndModes() {
			botMethods.Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"OverwatchLeague is requesting maps..."
			});
			using var wc = new WebClient();
			string mapsJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/maps");
			dynamic mapsJson = JsonConvert.DeserializeObject(mapsJsonString);

			// Just store the maps straight.
			foreach (var map in mapsJson) {
				ulong guid = Convert.ToUInt64(map.guid.Value, 16);
				string name = map.name.en_US;

				Map newMap = new Map(guid, name);
				maps.Add(newMap);


				if (map["gameModes"] != null) {
					foreach (var gameMode in map.gameModes) {
						ulong gameModeId = Convert.ToUInt64(gameMode.Id.Value, 16);
						string modeName = gameMode.Name;

						GameMode mode = gameModes.Find(m => m.Id == gameModeId);
						if (mode == null) {
							mode = new GameMode(gameModeId, modeName);
							gameModes.Add(mode);
						}
						mode.AddMap(newMap);
						newMap.AddGameMode(mode);
					}
				}
			}
		}

		private async Task LoadMatches() {
			//TODO: This may be irrelevent if the new endpoint delivers all the appropriate information.
			botMethods.Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"OverwatchLeague is requesting matches..."
			});
			using var wc = new WebClient();
			string matchesJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/matches");
			dynamic matchesJson = JsonConvert.DeserializeObject(matchesJsonString);

			foreach (var match in matchesJson.content) {
				int id = match.id;
				Team homeTeam = teams.Find(t => t.Id == match.competitors[0].id.Value);
				Team awayTeam = teams.Find(t => t.Id == match.competitors[1].id.Value);
				int homeScore = match.scores[0].value;
				int awayScore = match.scores[1].value;
				string status = match.status;
				DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDate);
				DateTime endTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.endDate);
				DateTime? actualStartTime = null;
				if (match["actualStartTime"] != null) {
					actualStartTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.actualStartTime);
				}
				DateTime? actualEndTime = null;
				if (match["actualEndTime"] != null) {
					actualEndTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.actualEndTime);
				}

				Match m = new Match(botMethods, id, homeTeam, awayTeam, homeScore, awayScore, status, startTime, endTime, actualStartTime, actualEndTime);
				matches.Add(m);
				if (homeTeam != null) {
					homeTeam.AddMatch(m);
				}
				if (awayTeam != null) {
					awayTeam.AddMatch(m);
				}

				// Now the individual games of the matches.
				foreach (var game in match.games) {
					int gameId = game.id;
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
					Map map = maps.Find(x => x.Guid == mapGuid);
					string gameStatus = game.status;

					MatchGame g = new MatchGame(gameId, gameNumber, gameHomePoints, gameAwayPoints, m, map, gameStatus);

					m.AddGame(g);
					if (map != null) {
						map.AddGame(g);
					}
				}
			}
		}

		private async Task LoadSchedule() {
			using var httpClient = new HttpClient();
			//TODO: Hard-coded week count, if there was a non-paginated version that'd be nice.
			foreach (int page in Enumerable.Range(1, 27)) {
				botMethods.Log(this, new LogEventArgs {
					Type = LogType.Log,
					Message = $"OverwatchLeague is requesting the schedule for week {page}"
				});
				var uri = new Uri($"https://wzavfvwgfk.execute-api.us-east-2.amazonaws.com/production/owl/paginator/schedule?stage=regular_season&page={page}&season=2020&locale=en-us");
				string referer = "https://overwatchleague.com/en-us/schedule";
				using var getMessage = new HttpRequestMessage {
					Method = HttpMethod.Get,
					RequestUri = uri
				};
				getMessage.Headers.Add("Referer", referer);
				var getResponse = await httpClient.SendAsync(getMessage);
				var getResponseString = await getResponse.Content.ReadAsStringAsync();
				dynamic getResponseJson = JsonConvert.DeserializeObject(getResponseString);

				int weekNumber = (int)getResponseJson.content.tableData.weekNumber.Value;
				string weekName = getResponseJson.content.tableData.name;

				var week = new Week(weekNumber, weekName);
				foreach (var weekEvent in getResponseJson.content.tableData.events) {
					string eventTitle = "Venue to be decided"; //TODO: Week 10 is the first event where this is the case; the website seems to not list a venue either.
					try {
						eventTitle = weekEvent.eventBanner.title;
					} catch (RuntimeBinderException) { }
					var e = new Event(eventTitle);
					e.SetWeek(week);
					week.AddEvent(e);

					foreach (var match in weekEvent.matches) {
						int matchId = (int)match.id;
						var homeTeam = teams.Single(t => t.Id == (int)match.competitors[0].id.Value);
						var awayTeam = teams.Single(t => t.Id == (int)match.competitors[1].id.Value);
						int homeScore = 0;
						int awayScore = 0;
						if (match.scores.Count > 0) {
							homeScore = (int)match.scores[0];
							awayScore = (int)match.scores[1];
						}
						string status = match.status;
						DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDate);
						DateTime endTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.endDate);
						//TODO: Get the actual start and end times (do they not exist anymore?).
						// Maps are added only when requested since they must be gotten on a per-match basis.
						var m = new Match(botMethods, matchId, homeTeam, awayTeam, homeScore, awayScore, status, startTime, endTime, null, null);
						m.SetEvent(e);
						matches.Add(m);
						homeTeam.AddMatch(m);
						awayTeam.AddMatch(m);
						e.AddMatch(m);
					}
				}

				weeks.Add(week);
			}
		}

		public List<Standing> GetStandings() {
			var standings = new List<Standing>();
			foreach (Team team in teams) {
				var s = new Standing { Team = team };
				foreach (Match match in matches) {
					if (match.Status == MatchStatus.Concluded) {
						if (match.HomeTeam == team) {
							s.MapWins += match.HomeScore;
							s.MapDraws += match.DrawMaps;
							s.MapLosses += match.AwayScore;
							if (match.HomeScore > match.AwayScore) {
								++s.MatchWins;
							} else {
								++s.MatchLosses;
							}
						} else if (match.AwayTeam == team) {
							s.MapWins += match.AwayScore;
							s.MapDraws += match.DrawMaps;
							s.MapLosses += match.HomeScore;
							if (match.AwayScore > match.HomeScore) {
								++s.MatchWins;
							} else {
								++s.MatchLosses;
							}
						}
					}
				}
				standings.Add(s);
			}

			standings.Sort((a, b) => {
				if ((a.MatchWins - a.MatchLosses) == (b.MatchWins - b.MatchLosses)) {
					return b.MapDiff.CompareTo(a.MapDiff);
				} else {
					return (b.MatchWins - b.MatchLosses).CompareTo(a.MatchWins - a.MatchLosses);
				}
			});

			int lastPosition = 1;
			for (int i = 1; i <= teams.Count; ++i) {
				Standing newStanding = standings[i - 1];
				if (i == 1 || ((standings[i - 2].MatchWins - standings[i - 2].MatchLosses) == (standings[i - 1].MatchWins - standings[i - 1].MatchLosses) && standings[i - 2].MapDiff == standings[i - 1].MapDiff)) {
					newStanding.Position = lastPosition;
					standings[i - 1] = newStanding;
				} else {
					newStanding.Position = i;
					standings[i - 1] = newStanding;
					lastPosition = i;
				}
			}

			return standings;
		}

		public Week GetCurrentWeek() {
			return weeks.Find(w => w.LastEndTime > DateTime.Now);
		}

		private static DateTime NextFullUpdate() {
			// The next full update should be at 3AM PST which is 11AM UTC.
			return DateTime.UtcNow.AddDays(1).AddHours(-DateTime.UtcNow.Hour + 11).AddMinutes(-DateTime.UtcNow.Minute).AddSeconds(-DateTime.UtcNow.Second);
		}
	}
}
