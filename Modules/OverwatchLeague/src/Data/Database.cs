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

		public Database() {
			teams = new List<Team>();
			divisions = new List<Division>();
			maps = new List<Map>();
			gameModes = new List<GameMode>();
		}

		public void Clear() {
			teams.Clear();
			divisions.Clear();
			maps.Clear();
			gameModes.Clear();
		}

		public async Task ReloadDatabase() {
			Clear();
			await LoadTeamsAndDivisions();
			await LoadMapsAndModes();
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
					// The division ID is a string for some reason.
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
	}
}
