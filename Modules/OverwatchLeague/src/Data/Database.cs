using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchLeague.Data {
	public class Database {
		private List<Team> teams;
		private List<Division> divisions;
		private List<Map> maps;
		private List<GameMode> gameModes;
		private List<Match> matches;
		private List<Stage> stages;

		public Database() {
			teams = new List<Team>();
			divisions = new List<Division>();
			maps = new List<Map>();
			gameModes = new List<GameMode>();
			matches = new List<Match>();
			stages = new List<Stage>();
		}

		public void Clear() {
			teams.Clear();
			divisions.Clear();
			maps.Clear();
			gameModes.Clear();
			matches.Clear();
			stages.Clear();
		}

		public async Task ReloadDatabase() {
			Clear();
			await LoadTeamsAndDivisions();
			await LoadMapsAndModes();
			await LoadMatches();
			await LoadSchedule();
		}

		private async Task LoadTeamsAndDivisions() {
			// Both teams and divisions are available through one API call.
			using (var wc = new WebClient()) {
				string teamsJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/teams");
				dynamic teamsJson = JsonConvert.DeserializeObject(teamsJsonString);

				// First note the divisions.
				foreach (var division in teamsJson.owl_divisions) {
					// The division ID is a string for some reason.
					int id = int.Parse(division.id.Value);
					string name = division.name;
					string abbreviatedName = division.abbrev;
					divisions.Add(new Division(id, name, abbreviatedName));
				}

				// Then note the teams, and then we'll link back to the divisions.
				foreach (var competitor in teamsJson.competitors) {
					var details = competitor.competitor; // The JSON has an additional hurdle for some reason.

					// We need to handle the colors first.
					Color primaryColor = Color.FromArgb(Convert.ToInt32(details.primaryColor.Value, 16));
					Color secondaryColor = Color.FromArgb(Convert.ToInt32(details.secondaryColor.Value, 16));
					int id = details.id;
					string name = details.name;
					string abbreviatedName = details.abbreviatedName;
					Team newTeam = new Team(id, name, abbreviatedName, primaryColor, secondaryColor);
					teams.Add(newTeam);

					// Also do some linking here.
					int divisionId = details.owl_division;
					Division division = divisions.Find(d => d.Id == divisionId);
					division.AddTeam(newTeam);
					newTeam.SetDivision(division);
				}
			}
		}

		private async Task LoadMapsAndModes() {
			using (var wc = new WebClient()) {
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
		}

		private async Task LoadMatches() {
			using (var wc = new WebClient()) {
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

					Match m = new Match(id, homeTeam, awayTeam, homeScore, awayScore, status, startTime, endTime, actualStartTime, actualEndTime);
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
		}

		private async Task LoadSchedule() {
			using (var wc = new WebClient()) {
				string scheduleJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/schedule");
				dynamic scheduleJson = JsonConvert.DeserializeObject(scheduleJsonString);

				foreach (var stage in scheduleJson.data.stages) {
					int id = stage.id;
					string name = stage.name;

					Stage s = new Stage(id, name);

					stages.Add(s);

					// Associate several weeks for the stage.
					foreach (var week in stage.weeks) {
						int weekId = week.id;
						string weekName = week.name;

						Week w = new Week(weekId, weekName);

						s.AddWeek(w);
						w.SetStage(s);

						// Associate all the matches for this week.
						foreach (var match in week.matches) {
							int matchId = match.id;
							Match m = matches.Find(x => x.Id == matchId);
							m.SetWeek(w);
							w.AddMatch(m);
						}
					}

					// Every stage also has a playoff week.
					Week playoffWeek = new Week(-1, "PLAYOFFS");
					s.SetPlayoffs(playoffWeek);
					playoffWeek.SetStage(s);

					// And then we find what matches haven't been allocated yet and set them.
					foreach (var match in stage.matches) {
						int matchId = match.id;
						Match m = matches.Find(x => x.Id == matchId);
						if (m == null) {
							// The match didn't exist so we need to make it.
							Team homeTeam = null;
							Team awayTeam = null;
							int homeScore = match.scores[0].value;
							int awayScore = match.scores[1].value;
							string status = match.status;
							DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDateTS);
							DateTime endTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.endDateTS);
							DateTime? actualStartTime = null; // These are always null in schedule.
							DateTime? actualEndTime = null;

							m = new Match(matchId, homeTeam, awayTeam, homeScore, awayScore, status, startTime, endTime, actualStartTime, actualEndTime);
							matches.Add(m);
						}
						if (m.Week == null) {
							m.SetWeek(playoffWeek);
							playoffWeek.AddMatch(m);
						}
					}
				}
			}
		}
	}
}
