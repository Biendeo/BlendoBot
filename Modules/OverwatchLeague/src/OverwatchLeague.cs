using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchLeague {
	public class OverwatchLeague : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?overwatchleague",
			Name = "Overwatch League",
			Description = "Tells you up-to-date stats about the Overwatch League.",
			Usage = $"Usage:\n{"?overwatchleague live".Code()} {"(stats about the match that is currently on)".Italics()}\n{"?overwatchleague next".Code()} {"(stats about the next match that will be played)".Italics()}\n{"?overwatchleague standings".Code()} {"(the overall standings of the league)".Italics()}\n{"?overwatchleague schedule [stage] [week]".Code()} {"(shows times and scores for each match in the given week)".Italics()}\n{"?overwatchleague schedule [stage] playoffs".Code()} {"(shows times and scores for each match in the given stage's playoffs)".Italics()}\n{"?overwatchleague schedule [abbreviated team name]".Code()} {"(shows times and scores for each match that a team plays)".Italics()}\nAll times listed are in UTC.",
			Author = "Biendeo",
			Version = "0.4.0",
			Startup = Startup,
			OnMessage = OverwatchLeagueCommand
		};

		private static Dictionary<string, string> MapNames;
		private static Dictionary<string, string> MapModes;

		private static async Task<bool> Startup() {
			MapNames = new Dictionary<string, string>();
			MapModes = new Dictionary<string, string>();

			try {
				using (var wc = new WebClient()) {
					string mapJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/maps");
					dynamic mapJson = JsonConvert.DeserializeObject(mapJsonString);
					foreach (var map in mapJson) {
						try {
							MapNames.Add((string)map.guid, (string)map.name.en_US);
							MapModes.Add((string)map.guid, (string)map.gameModes[0].Name);
						} catch (RuntimeBinderException) { }
					}
				}
			} catch (RuntimeBinderException) {
				return false;
			}
			return true;
		}

		private static string GetMatchDetails(dynamic match) {
			var sb = new StringBuilder();

			sb.Append("```");
			var homeTeam = match.competitors[0];
			var awayTeam = match.competitors[1];

			var currentHomeScore = match.scores[0].value;
			var currentAwayScore = match.scores[1].value;

			sb.AppendLine($"Planned time: {new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDateTS).ToString("d/MM/yyyy h:mm:ss tt")} UTC - {new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.endDateTS).ToString("d/MM/yyyy h:mm:ss tt K")} UTC");
			sb.Append("Real time: ");

			try {
				sb.Append(new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.actualStartDate).ToString("d/MM/yyyy h:mm:ss tt") + " UTC");
				sb.Append(" - ");
			} catch (RuntimeBinderException) {
				sb.Append("??? - ");
			}

			try {
				sb.Append(new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.actualEndDate).ToString("d/MM/yyyy h:mm:ss tt") + " UTC");
			} catch (RuntimeBinderException) {
				sb.Append("???");
			}
			sb.AppendLine();

			sb.AppendLine($"{homeTeam.name} vs. {awayTeam.name}");
			sb.AppendLine($"{currentHomeScore} - {currentAwayScore}");

			foreach (var game in match.games) {
				sb.AppendLine();
				sb.AppendLine($"Map {game.number} on {MapNames[(string)game.attributes.mapGuid]} ({MapModes[(string)game.attributes.mapGuid]}) - {game.status}");
				try {
					sb.AppendLine($"{game.points[0]} - {game.points[1]}");
				} catch (RuntimeBinderException) { }
			}

			sb.Append("```");

			return sb.ToString();
		}

		public static async Task OverwatchLeagueCommand(MessageCreateEventArgs e) {
			// Try and decipher the output.
			var splitMessage = e.Message.Content.Split(' ');

			using (var wc = new WebClient()) {
				if (splitMessage.Length > 1 && splitMessage[1] == "live") {
					string liveMatchJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/live-match");
					dynamic liveMatchJson = JsonConvert.DeserializeObject(liveMatchJsonString);
					var liveMatch = liveMatchJson.data.liveMatch;

					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = GetMatchDetails(liveMatch),
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueLive"
					});
				} else if (splitMessage.Length > 1 && splitMessage[1] == "next") {
					string liveMatchJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/live-match");
					dynamic liveMatchJson = JsonConvert.DeserializeObject(liveMatchJsonString);
					var nextMatch = liveMatchJson.data.nextMatch;

					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = GetMatchDetails(nextMatch),
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueNext"
					});
				} else if (splitMessage.Length > 1 && splitMessage[1] == "standings") {
					string standingsJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/standings");
					dynamic standingsJson = JsonConvert.DeserializeObject(standingsJsonString);

					var sb = new StringBuilder();

					sb.Append("```");

					sb.AppendLine(" # |                         Name | W - L | Diff |   Map W-D-L");
					sb.AppendLine("---+------------------------------+-------+------+------------");

					var rankingIndicies = Enumerable.Range(0, 20).ToList().ConvertAll(delegate (int i) { return i.ToString(); });

					foreach (var index in rankingIndicies) {
						var team = standingsJson.ranks.content[index];
						var mapDiff = team.records[0].gameWin - team.records[0].gameLoss;
						sb.AppendLine($"{team.placement.ToString().PadLeft(2, ' ')} | {team.competitor.name.ToString().PadLeft(22, ' ')} ({team.competitor.abbreviatedName}) | {team.records[0].matchWin.ToString().PadLeft(2, ' ')}-{team.records[0].matchLoss.ToString().PadLeft(2, ' ')} | {$"{(mapDiff > 0 ? '+' : ' ')}{mapDiff}".PadLeft(4, ' ')} | {team.records[0].gameWin.ToString().PadLeft(3, ' ')}-{team.records[0].gameTie.ToString().PadLeft(3, ' ')}-{team.records[0].gameLoss.ToString().PadLeft(3, ' ')}");
					}

					sb.Append("```");

					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueStandings"
					});
				} else if (splitMessage.Length > 2 && splitMessage[1] == "schedule") {
					if (splitMessage.Length > 3 && (int.TryParse(splitMessage[2], out int stage) && stage > 0 && stage <= 4) && ((int.TryParse(splitMessage[3], out int week) && week > 0 && week <= 5) || splitMessage[3] == "playoffs")) {
						string scheduleJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/schedule");
						dynamic scheduleJson = JsonConvert.DeserializeObject(scheduleJsonString);

						var sb = new StringBuilder();

						sb.Append("```");

						dynamic relevantMatches;

						if (splitMessage[3] == "playoffs") {
							Newtonsoft.Json.Linq.JArray a = scheduleJson.data.stages[stage - 1].matches;
							relevantMatches = a.Skip(70).ToArray();
						} else {
							relevantMatches = scheduleJson.data.stages[stage - 1].weeks[week - 1].matches;
						}

						foreach (var match in relevantMatches) {
							dynamic home = match.competitors[0];
							dynamic away = match.competitors[1];

							string homeTeam = home != null ? home.abbreviatedName : "???";
							string awayTeam = away != null ? away.abbreviatedName : "???";

							int homeScore = match.scores[0].value;
							int awayScore = match.scores[1].value;

							var startTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDateTS);

							sb.Append($"{startTime.ToString("d/MM hh:mm tt").PadLeft(15, ' ')} UTC - {homeTeam} vs. {awayTeam}");
							if (match.status != "PENDING") {
								sb.Append($" ({homeScore} - {awayScore})");
							}
							if (match.status == "IN_PROGRESS") {
								sb.Append(" (LIVE)");
							}
							sb.AppendLine();
						}

						sb.Append("```");

						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = sb.ToString(),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleWeek"
						});
					} else if (splitMessage.Length > 2 && splitMessage[2].Length == 3) {
						string teamName = splitMessage[2].ToUpper();
						string matchesJsonString = await wc.DownloadStringTaskAsync("https://api.overwatchleague.com/matches");
						dynamic matchesJson = JsonConvert.DeserializeObject(matchesJsonString);
						int countedMatches = 0;
						var sb = new StringBuilder();

						sb.Append("```");

						foreach (var match in matchesJson.content) {
							if (match.competitors[0].abbreviatedName == teamName || match.competitors[1].abbreviatedName == teamName) {
								++countedMatches;

								string homeTeam = match.competitors[0].abbreviatedName;
								string awayTeam = match.competitors[1].abbreviatedName;

								int homeScore = match.scores[0].value;
								int awayScore = match.scores[1].value;

								var startTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDate);

								if (homeTeam == teamName) {
									sb.Append($"{startTime.ToString("d/MM hh:mm tt").PadLeft(15, ' ')} UTC - {homeTeam} vs. {awayTeam}");
									if (match.status != "PENDING") {
										sb.Append($" ({homeScore} - {awayScore})");
									}
								} else {
									sb.Append($"{startTime.ToString("d/MM hh:mm tt").PadLeft(15, ' ')} UTC - {awayTeam} vs. {homeTeam}");
									if (match.status != "PENDING") {
										sb.Append($" ({awayScore} - {homeScore})");
									}
								}
								if (match.status == "IN_PROGRESS") {
									sb.Append(" (LIVE)");
								} else if (match.status == "CONCLUDED") {
									if (match.winner.abbreviatedName == teamName) {
										sb.Append(" (W)");
									} else {
										sb.Append(" (L)");
									}
								}
								sb.AppendLine();
							}
						}

						sb.Append("```");

						if (countedMatches == 0) {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = $"Invalid team code. Use {"?overwatchleague standings".Code()} to find your team's abbreviated name!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeamInvalid"
							});
						} else {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = sb.ToString(),
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeam"
							});
						}
					} else {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = $"Invalid usage of the schedule command!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleInvalid"
						});
					}
				} else {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help overwatchleague".Code()}",
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueUnknownCommand"
					});
				}
			}
			await Task.Delay(0);
		}
	}
}
