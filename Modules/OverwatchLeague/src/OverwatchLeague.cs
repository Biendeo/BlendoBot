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

		public static Database Database;

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

		private static string GetMatchDetails(Match match) {
			var sb = new StringBuilder();

			sb.Append("```");
			Team homeTeam = match.HomeTeam;
			Team awayTeam = match.AwayTeam;

			int currentHomeScore = match.HomeScore;
			int currentAwayScore = match.AwayScore;

			sb.AppendLine($"Planned time: {match.StartTime.ToString("d/MM/yyyy h:mm:ss tt")} UTC - {match.EndTime.ToString("d/MM/yyyy h:mm:ss tt K")} UTC");
			if (match.ActualStartTime != null) {
				sb.Append("Real time: ");
				sb.Append(match.ActualStartTime?.ToString("d/MM/yyyy h:mm:ss tt") + " UTC");
				sb.Append(" - ");
				if (match.ActualEndTime != null) {
					sb.AppendLine(match.ActualEndTime?.ToString("d/MM/yyyy h:mm:ss tt") + " UTC");
				} else {
					sb.AppendLine("???");
				}
			}

			sb.AppendLine($"{homeTeam.Name} ({homeTeam.AbbreviatedName}) vs. {awayTeam.Name} ({awayTeam.AbbreviatedName})");
			sb.AppendLine($"{currentHomeScore} - {currentAwayScore}");

			foreach (MatchGame game in match.Games) {
				sb.AppendLine();
				sb.AppendLine($"Map {game.MapNumber} on {game.Map.Name} ({game.Map.GameModes[0].Name}) - {game.Status}");
				sb.AppendLine($"{game.HomeScore} - {game.AwayScore}");
			}

			sb.Append("```");

			return sb.ToString();
		}

		public static async Task OverwatchLeagueCommand(MessageCreateEventArgs e) {
			// Try and decipher the output.
			var splitMessage = e.Message.Content.Split(' ');

			using (var wc = new WebClient()) {
				if (splitMessage.Length > 1 && splitMessage[1] == "live") {
					Match match = (from c in Database.Matches where c.EndTime > DateTime.UtcNow select c).First();

					if (match == null) {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = "No match is currently live!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLiveNoMatch"
						});
					} else {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = GetMatchDetails(match),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLive"
						});
					}
				} else if (splitMessage.Length > 1 && splitMessage[1] == "next") {
					Match match = (from c in Database.Matches where c.StartTime > DateTime.UtcNow select c).First();

					if (match == null) {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = "There's no next match planned!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNextNoMatch"
						});
					} else {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = GetMatchDetails(match),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNext"
						});
					}
				} else if (splitMessage.Length > 1 && splitMessage[1] == "standings") {
					var sb = new StringBuilder();

					sb.Append("```");

					sb.AppendLine(" # |                         Name | W - L | Diff |   Map W-D-L");
					sb.AppendLine("---+------------------------------+-------+------+------------");

					foreach (Standing s in Database.GetStandings()) {
						sb.AppendLine($"{s.Position.ToString().PadLeft(2, ' ')} | {s.Team.Name.ToString().PadLeft(22, ' ')} ({s.Team.AbbreviatedName}) | {s.MatchWins.ToString().PadLeft(2, ' ')}-{s.MatchLosses.ToString().PadLeft(2, ' ')} | {$"{(s.MapDiff > 0 ? '+' : ' ')}{s.MapDiff}".PadLeft(4, ' ')} | {s.MapWins.ToString().PadLeft(3, ' ')}-{s.MapDraws.ToString().PadLeft(3, ' ')}-{s.MapLosses.ToString().PadLeft(3, ' ')}");
					}

					sb.Append("```");

					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueStandings"
					});
				} else if (splitMessage.Length > 2 && splitMessage[1] == "schedule") {
					if (splitMessage.Length > 3 && (int.TryParse(splitMessage[2], out int stage) && stage > 0 && stage <= 4) && ((int.TryParse(splitMessage[3], out int week) && week > 0 && week <= 5) || splitMessage[3] == "playoffs")) {
						var sb = new StringBuilder();

						sb.Append("```");

						Week relevantWeek;

						if (splitMessage[3] == "playoffs") {
							relevantWeek = Database.Stages[stage - 1].Playoffs;
						} else {
							relevantWeek = Database.Stages[stage - 1].Weeks[week - 1];
						}

						foreach (Match match in relevantWeek.Matches) {
							sb.Append($"{match.StartTime.ToString("d/MM hh:mm tt").PadLeft(15, ' ')} UTC - ");
							if (match.HomeTeam != null) {
								sb.Append($"{match.HomeTeam.AbbreviatedName} - ");
							} else {
								sb.Append("??? - ");
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
						Team team = (from c in Database.Teams where c.AbbreviatedName == teamName select c).First();

						if (team == null) {
							await Methods.SendMessage(null, new SendMessageEventArgs {
								Message = $"Invalid team code. Use {"?overwatchleague standings".Code()} to find your team's abbreviated name!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeamInvalid"
							});
						} else {
							var sb = new StringBuilder();

							sb.Append("```");

							foreach (var match in team.Matches) {
								if (match.HomeTeam == team) {
									sb.Append($"{match.StartTime.ToString("d/MM hh:mm tt").PadLeft(15, ' ')} UTC - {match.HomeTeam.AbbreviatedName} vs. {match.AwayTeam.AbbreviatedName}");
									if (match.Status != MatchStatus.Pending) {
										sb.Append($" ({match.HomeScore} - {match.AwayScore})");
									}
									if (match.Status == MatchStatus.Concluded) {
										sb.Append($" ({(match.HomeScore > match.AwayScore ? 'W' : 'L')})");
									}
								} else {
									sb.Append($"{match.StartTime.ToString("d/MM hh:mm tt").PadLeft(15, ' ')} UTC - {match.AwayTeam.AbbreviatedName} vs. {match.HomeTeam.AbbreviatedName}");
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
