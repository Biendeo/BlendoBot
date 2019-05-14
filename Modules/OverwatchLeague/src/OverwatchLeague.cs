using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using OverwatchLeague.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UserTimeZone;

namespace OverwatchLeague {
	public class OverwatchLeague : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?owl",
			Name = "Overwatch League",
			Description = "Tells you up-to-date stats about the Overwatch League.",
			Usage = $"Usage:\n{"?owl live".Code()} {"(stats about the match that is currently on)".Italics()}\n{"?owl next".Code()} {"(stats about the next match that will be played)".Italics()}\n{"?owl match [match id]".Code()} {"(stats about the specified match (match IDs are the 5-digit numbers in square brackets in the schedule commands))".Italics()}\n{"?owl standings".Code()} {"(the overall standings of the league)".Italics()}\n{"?owl standings [stage]".Code()} {"(the overall standings of the stage)".Italics()}\n{"?owl schedule".Code()} {"(shows times and scores for each match in the current or next week)".Italics()}\n{"?owl schedule [stage] [week]".Code()} {"(shows times and scores for each match in the given week)".Italics()}\n{"?owl schedule [stage] playoffs".Code()} {"(shows times and scores for each match in the given stage's playoffs)".Italics()}\n{"?owl schedule [abbreviated team name]".Code()} {"(shows times and scores for each match that a team plays)".Italics()}\nAll times are determined by the user's {"?usertimezone".Code()} setting.",
			Author = "Biendeo",
			Version = "1.1.0",
			Startup = Startup,
			OnMessage = OverwatchLeagueCommand
		};

		internal static Database Database;

		private static async Task<bool> Startup() {
			if (Database == null) {
				Database = new Database();
			}
			try {
				await Database.ReloadDatabase();
			} catch (Exception exc) {
				Console.Error.WriteLine(exc);
				return false;
			}

			return true;
		}

		private static string GetMatchDetails(Match match, TimeZoneInfo timeZone) {
			var sb = new StringBuilder();

			sb.Append("```");
			Team homeTeam = match.HomeTeam;
			Team awayTeam = match.AwayTeam;

			int currentHomeScore = match.HomeScore;
			int currentAwayScore = match.AwayScore;

			sb.AppendLine($"Match ID: {match.Id}");
			sb.AppendLine($"Planned time: {match.StartTime.Add(timeZone.BaseUtcOffset).ToString("d/MM/yyyy h:mm:ss tt")} UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(timeZone)} - {match.EndTime.Add(timeZone.BaseUtcOffset).ToString("d/MM/yyyy h:mm:ss tt")} UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(timeZone)}");
			if (match.ActualStartTime != null) {
				sb.Append("Real time: ");
				sb.Append(match.ActualStartTime?.Add(timeZone.BaseUtcOffset).ToString("d/MM/yyyy h:mm:ss tt") + $" UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(timeZone)}");
				sb.Append(" - ");
				if (match.ActualEndTime != null) {
					sb.AppendLine(match.ActualEndTime?.Add(timeZone.BaseUtcOffset).ToString("d/MM/yyyy h:mm:ss tt") + $" UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(timeZone)}");
				} else {
					sb.AppendLine("???");
				}
			}

			sb.AppendLine($"{homeTeam.Name} ({homeTeam.AbbreviatedName}) vs. {awayTeam.Name} ({awayTeam.AbbreviatedName})");
			sb.AppendLine($"{currentHomeScore} - {currentAwayScore}");

			foreach (MatchGame game in match.Games) {
				Map map = game.Map;
				sb.AppendLine();
				if (game != null) {
					if (map != null) {
						sb.AppendLine($"Map {game.MapNumber} on {game.Map.Name} ({game.Map.GameModes[0].Name}) - {game.Status}");
					} else {
						sb.AppendLine($"Map {game.MapNumber} on ??? (???) - {game.Status}");
					}
					sb.AppendLine($"{game.HomeScore} - {game.AwayScore}");
				} else {
					sb.AppendLine("Map ??? on ??? (???) - PENDING");
					sb.AppendLine("0 - 0");
				}
			}

			sb.Append("```");

			return sb.ToString();
		}

		public static async Task OverwatchLeagueCommand(MessageCreateEventArgs e) {
			// Try and decipher the output.
			var splitMessage = e.Message.Content.Split(' ');
			TimeZoneInfo userTimeZone = UserTimeZone.UserTimeZone.GetUserTimeZone(e.Author);

			using (var wc = new WebClient()) {
				if (splitMessage.Length > 1 && splitMessage[1] == "live") {
					Match match = (from c in Database.Matches where c.EndTime > DateTime.UtcNow && c.StartTime < DateTime.UtcNow select c).FirstOrDefault();

					if (match == null) {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = "No match is currently live!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLiveNoMatch"
						});
					} else {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = GetMatchDetails(match, userTimeZone),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLive"
						});
					}
				} else if (splitMessage.Length > 1 && splitMessage[1] == "next") {
					Match match = (from c in Database.Matches where c.StartTime > DateTime.UtcNow select c).FirstOrDefault();

					if (match == null) {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = "There's no next match planned!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNextNoMatch"
						});
					} else {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = GetMatchDetails(match, userTimeZone),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNext"
						});
					}
				} else if (splitMessage.Length > 2 && splitMessage[1] == "match") {
					if (int.TryParse(splitMessage[2], out int matchId)) {
						Match match = (from c in Database.Matches where c.Id == matchId select c).FirstOrDefault();
						if (match == null) {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = $"No match matches ID {matchId}!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueMatchNoMatch"
							});
						} else {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = GetMatchDetails(match, userTimeZone),
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueMatch"
							});
						}
					} else {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = "Match ID is invalid!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueMatchInvalidArgument"
						});
					}
				} else if (splitMessage.Length > 1 && splitMessage[1] == "standings") {
					int stageNum = 0;
					if (splitMessage.Length > 2) {
						if (int.TryParse(splitMessage[2], out stageNum)) {
							if (stageNum < 1 || stageNum > 4) {
								await Methods.SendMessage(null, new SendMessageEventArgs {
									Message = $"Invalid stage number; please make sure your stage number is between 1-4.",
									Channel = e.Channel,
									LogMessage = "OverwatchLeagueStandingsInvalidStage"
								});
								return;
							}
						} else {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = $"Invalid stage number; please make sure your stage number is between 1-4.",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueStandingsInvalidStage"
							});
							return;
						}
					}

					var sb = new StringBuilder();

					sb.Append("```");

					sb.AppendLine(" # |                         Name | W - L | Diff |   Map W-D-L");
					sb.AppendLine("---+------------------------------+-------+------+------------");

					foreach (Standing s in Database.GetStandings(stageNum)) {
						sb.AppendLine($"{s.Position.ToString().PadLeft(2, ' ')} | {s.Team.Name.ToString().PadLeft(22, ' ')} ({s.Team.AbbreviatedName}) | {s.MatchWins.ToString().PadLeft(2, ' ')}-{s.MatchLosses.ToString().PadLeft(2, ' ')} | {$"{(s.MapDiff > 0 ? '+' : ' ')}{s.MapDiff}".PadLeft(4, ' ')} | {s.MapWins.ToString().PadLeft(3, ' ')}-{s.MapDraws.ToString().PadLeft(3, ' ')}-{s.MapLosses.ToString().PadLeft(3, ' ')}");
					}

					sb.Append("```");

					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueStandings"
					});
				} else if (splitMessage.Length > 1 && splitMessage[1] == "schedule") {
					if (splitMessage.Length == 2) {
						Week relevantWeek = Database.GetCurrentWeek();
						var sb = new StringBuilder();

						sb.Append("```");

						foreach (Match match in relevantWeek.Matches) {
							sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {match.StartTime.Add(userTimeZone.BaseUtcOffset).ToString("d/MM hh:mm tt").PadLeft(14, ' ')} UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(userTimeZone)} - ");
							if (match.HomeTeam != null) {
								sb.Append($"{match.HomeTeam.AbbreviatedName} vs. ");
							} else {
								sb.Append("??? vs. ");
							}
							if (match.AwayTeam != null) {
								sb.Append($"{match.AwayTeam.AbbreviatedName}");
							} else {
								sb.Append("???");
							}
							if (match.Status != MatchStatus.Pending) {
								sb.Append($" ({match.HomeScore} - {match.AwayScore})");
							}
							if (match.Status == MatchStatus.InProgress) {
								sb.Append(" (LIVE)");
							}
							sb.AppendLine();
						}

						sb.Append("```");

						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = sb.ToString(),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleCurrent"
						});
					} else if (splitMessage.Length > 3 && (int.TryParse(splitMessage[2], out int stage) && stage > 0 && stage <= 4) && ((int.TryParse(splitMessage[3], out int week) && week > 0 && week <= 5) || splitMessage[3] == "playoffs")) {
						var sb = new StringBuilder();

						sb.Append("```");

						Week relevantWeek;

						if (splitMessage[3] == "playoffs") {
							relevantWeek = Database.Stages[stage - 1].Playoffs;
						} else {
							relevantWeek = Database.Stages[stage - 1].Weeks[week - 1];
						}

						foreach (Match match in relevantWeek.Matches) {
							sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {match.StartTime.Add(userTimeZone.BaseUtcOffset).ToString("d/MM hh:mm tt").PadLeft(14, ' ')} UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(userTimeZone)} - ");
							if (match.HomeTeam != null) {
								sb.Append($"{match.HomeTeam.AbbreviatedName} vs. ");
							} else {
								sb.Append("??? vs. ");
							}
							if (match.AwayTeam != null) {
								sb.Append($"{match.AwayTeam.AbbreviatedName}");
							} else {
								sb.Append("???");
							}
							if (match.Status != MatchStatus.Pending) {
								sb.Append($" ({match.HomeScore} - {match.AwayScore})");
							}
							if (match.Status == MatchStatus.InProgress) {
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
						Team team = (from c in Database.Teams where c.AbbreviatedName == teamName select c).FirstOrDefault();

						if (team == null) {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = $"Invalid team code. Use {"?owl standings".Code()} to find your team's abbreviated name!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeamInvalid"
							});
						} else {
							var sb = new StringBuilder();

							sb.Append("```");

							foreach (var match in team.Matches) {
								if (match.HomeTeam == team) {
									sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {match.StartTime.Add(userTimeZone.BaseUtcOffset).ToString("d/MM hh:mm tt").PadLeft(14, ' ')} UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(userTimeZone)} - {match.HomeTeam.AbbreviatedName} vs. {match.AwayTeam.AbbreviatedName}");
									if (match.Status != MatchStatus.Pending) {
										sb.Append($" ({match.HomeScore} - {match.AwayScore})");
									}
									if (match.Status == MatchStatus.Concluded) {
										sb.Append($" ({(match.HomeScore > match.AwayScore ? 'W' : 'L')})");
									}
								} else {
									sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {match.StartTime.Add(userTimeZone.BaseUtcOffset).ToString("d/MM hh:mm tt").PadLeft(14, ' ')} UTC{UserTimeZone.UserTimeZone.TimeZoneOffsetToString(userTimeZone)} - {match.AwayTeam.AbbreviatedName} vs. {match.HomeTeam.AbbreviatedName}");
									if (match.Status != MatchStatus.Pending) {
										sb.Append($" ({match.AwayScore} - {match.HomeScore})");
									}
									if (match.Status == MatchStatus.Concluded) {
										sb.Append($" ({(match.AwayScore > match.HomeScore ? 'W' : 'L')})");
									}
								}
								if (match.Status == MatchStatus.InProgress) {
									sb.Append(" (LIVE)");
								}
								sb.AppendLine();
							}

							sb.Append("```");

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
						return;
					}
				} else {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help owl".Code()}",
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueUnknownCommand"
					});
				}
			}
			await Task.Delay(0);
		}
	}
}
