using BlendoBotLib;
using DSharpPlus.EventArgs;
using OverwatchLeague.Data;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchLeague {
	public class OverwatchLeague : CommandBase {
		public OverwatchLeague(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string DefaultTerm => "?owl";
		public override string Name => "Overwatch League";
		public override string Description => "Tells you up-to-date stats about the Overwatch League.";
		public override string Usage => $"Usage:\n{$"{Term} live".Code()} {"(stats about the match that is currently on)".Italics()}\n{$"{Term} next".Code()} {"(stats about the next match that will be played)".Italics()}\n{$"{Term} match [match id]".Code()} {"(stats about the specified match (match IDs are the 5-digit numbers in square brackets in the schedule commands))".Italics()}\n{$"{Term} standings".Code()} {"(the overall standings of the league)".Italics()}\n{$"{Term} schedule".Code()} {"(shows times and scores for each match in the current or next week)".Italics()}\n{$"{Term} schedule [week]".Code()} {"(shows times and scores for each match in the given week)".Italics()}\n{$"{Term} schedule [abbreviated team name]".Code()} {"(shows times and scores for each match that a team plays)".Italics()}\nAll times are determined by the user's {BotMethods.GetCommand<UserTimeZone.UserTimeZone>(this, GuildId).Term.Code()} setting.";
		public override string Author => "Biendeo";
		public override string Version => "1.1.0";

		static internal Database Database;
		private const string TimeFormatStringLong = "d/MM/yyyy h:mm:ss tt";
		private const string TimeFormatStringShort = "d/MM hh:mm tt";

		public override async Task<bool> Startup() {
			//TODO: Double check that this doesn't return true immediately for all but one guild. Maybe await?
			if (Database == null) {
				//? Because the database is static, BotMethods here only is from the bot instance that first loaded
				//? this. If any more bots exist in one program, this may not be great. It also means that if it were
				//? to call anything other than logging, it would panic if the server didn't exist.
				Database = new Database(BotMethods);
				try {
					await Database.ReloadDatabase();
				} catch (Exception exc) {
					Console.Error.WriteLine(exc);
					Database = null;
					return false;
				}
			}

			return true;
		}

		private string GetMatchDetails(Match match, TimeZoneInfo timeZone) {
			var sb = new StringBuilder();

			sb.Append("```");
			Team homeTeam = match.HomeTeam;
			Team awayTeam = match.AwayTeam;

			int currentHomeScore = match.HomeScore;
			int currentAwayScore = match.AwayScore;

			sb.AppendLine($"Match ID: {match.Id}");
			sb.AppendLine($"Planned time: {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, timeZone).ToString(TimeFormatStringLong)} {UserTimeZone.UserTimeZone.ToShortString(timeZone)} - {TimeZoneInfo.ConvertTimeFromUtc(match.EndTime, timeZone).ToString(TimeFormatStringLong)} {UserTimeZone.UserTimeZone.ToShortString(timeZone)}");

			if (match.ActualStartTime != null) {
				sb.Append("Real time: ");
				sb.Append(TimeZoneInfo.ConvertTimeFromUtc(match.ActualStartTime ?? default, timeZone).ToString(TimeFormatStringLong));
				sb.Append(" ");
				sb.Append(UserTimeZone.UserTimeZone.ToShortString(timeZone));
				sb.Append(" - ");
				if (match.ActualEndTime != null) {
					sb.AppendLine(TimeZoneInfo.ConvertTimeFromUtc(match.ActualEndTime ?? default, timeZone).ToString(TimeFormatStringLong));
					sb.Append(" ");
					sb.Append(UserTimeZone.UserTimeZone.ToShortString(timeZone));
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

		public override async Task OnMessage(MessageCreateEventArgs e) {
			// Try and decipher the output.
			string[] splitMessage = e.Message.Content.Split(' ');
			TimeZoneInfo userTimeZone = UserTimeZone.UserTimeZone.GetUserTimeZone(this, e.Author);

			using (var wc = new WebClient()) {
				if (splitMessage.Length > 1 && splitMessage[1] == "live") {
					Match match = (from c in Database.Matches where c.EndTime > DateTime.UtcNow && c.StartTime < DateTime.UtcNow select c).FirstOrDefault();
					if (match == null) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = "No match is currently live!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLiveNoMatch"
						});
					} else {
						if (match.Games.Count == 0) {
							BotMethods.Log(this, new LogEventArgs {
								Message = $"Updating match {match.Id} on demand",
								Type = LogType.Log,
							});
							await match.UpdateMatch();
						}
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = GetMatchDetails(match, userTimeZone),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLive"
						});
					}
				} else if (splitMessage.Length > 1 && splitMessage[1] == "next") {
					Match match = (from c in Database.Matches where c.StartTime > DateTime.UtcNow select c).FirstOrDefault();

					if (match == null) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = "There's no next match planned!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNextNoMatch"
						});
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = GetMatchDetails(match, userTimeZone),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNext"
						});
					}
				} else if (splitMessage.Length > 2 && splitMessage[1] == "match") {
					if (int.TryParse(splitMessage[2], out int matchId)) {
						Match match = (from c in Database.Matches where c.Id == matchId select c).FirstOrDefault();
						if (match == null) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"No match matches ID {matchId}!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueMatchNoMatch"
							});
						} else {
							if (match.Games.Count == 0) {
								BotMethods.Log(this, new LogEventArgs {
									Message = $"Updating match {match.Id} on demand",
									Type = LogType.Log,
								});
								await match.UpdateMatch();
							}
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = GetMatchDetails(match, userTimeZone),
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueMatch"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = "Match ID is invalid!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueMatchInvalidArgument"
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

					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueStandings"
					});
				} else if (splitMessage.Length > 1 && splitMessage[1] == "schedule") {
					if (splitMessage.Length == 2) {
						Week relevantWeek = Database.GetCurrentWeek();
						var sb = new StringBuilder();

						foreach (var weekEvent in relevantWeek.Events) {
							sb.AppendLine(weekEvent.Title.Bold());
							sb.Append("```");

							foreach (Match match in weekEvent.Matches) {
								sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)} - ");
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

							sb.AppendLine("```");

						}


						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = sb.ToString(),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleCurrent"
						});
					//TODO: Hard-coded value as 27.
					} else if (splitMessage.Length > 2 && int.TryParse(splitMessage[2], out int week) && week > 0 && week <= 27) {
						var sb = new StringBuilder();

						Week relevantWeek = Database.Weeks.First(w => w.WeekNumber == week);

						foreach (var weekEvent in relevantWeek.Events) {
							sb.AppendLine(weekEvent.Title.Bold());
							sb.Append("```");

							foreach (Match match in weekEvent.Matches) {
								sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)} - ");
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

							sb.AppendLine("```");

						}

						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = sb.ToString(),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleWeek"
						});
					} else if (splitMessage.Length > 2 && splitMessage[2].Length == 3) {
						string teamName = splitMessage[2].ToUpper();
						Team team = (from c in Database.Teams where c.AbbreviatedName == teamName select c).FirstOrDefault();

						if (team == null) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Invalid team code. Use {$"{Term} standings".Code()} to find your team's abbreviated name!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeamInvalid"
							});
						} else {
							var sb = new StringBuilder();

							sb.Append("```");

							foreach (var match in team.Matches) {
								if (match.HomeTeam == team) {
									sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)} - {match.HomeTeam.AbbreviatedName} vs. {match.AwayTeam.AbbreviatedName}");
									if (match.Status != MatchStatus.Pending) {
										sb.Append($" ({match.HomeScore} - {match.AwayScore})");
									}
									if (match.Status == MatchStatus.Concluded) {
										sb.Append($" ({(match.HomeScore > match.AwayScore ? 'W' : 'L')})");
									}
								} else {
									sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)} - {match.AwayTeam.AbbreviatedName} vs. {match.HomeTeam.AbbreviatedName}");
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

							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = sb.ToString(),
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeam"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Invalid usage of the schedule command!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleInvalid"
						});
						return;
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"I couldn't determine what you wanted. Make sure your command is handled by {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} owl".Code()}",
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueUnknownCommand"
					});
				}
			}
			await Task.Delay(0);
		}
	}
}
