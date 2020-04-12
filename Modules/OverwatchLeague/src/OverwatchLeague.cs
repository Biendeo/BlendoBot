using BlendoBotLib;
using BlendoBotLib.Interfaces;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using OverwatchLeague.Data;
using UserTimeZone;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OverwatchLeague {
	[CommandDefaults(defaultTerm: "owl")]
	public class OverwatchLeague : ICommand {
		public OverwatchLeague(
			IDiscordClient discordClient,
			ILogger<OverwatchLeague> logger,
			ILoggerFactory loggerFactory,
			IUserTimeZoneProvider timeZoneProvider)
		{
            this.discordClient = discordClient;
            this.logger = logger;
			this.timeZoneProvider = timeZoneProvider;
            this.db = new Lazy<Database>(
				() =>
				{
					var db = new Database(loggerFactory.CreateLogger<Database>(), loggerFactory);
					db.ReloadDatabase().Wait();
					return db;
				},
				LazyThreadSafetyMode.ExecutionAndPublication
			);

			// Start background task to start lazy loading the database
			Task.Run(() => this.Database.ToString());
        }

		public string Name => "Overwatch League";
		public string Description => "Tells you up-to-date stats about the Overwatch League.";
		public string GetUsage(string term) => $"Usage:\n{$"{term} live".Code()} {"(stats about the match that is currently on)".Italics()}\n{$"{term} next".Code()} {"(stats about the next match that will be played)".Italics()}\n{$"{term} match [match id]".Code()} {"(stats about the specified match (match IDs are the 5-digit numbers in square brackets in the schedule commands))".Italics()}\n{$"{term} standings".Code()} {"(the overall standings of the league)".Italics()}\n{$"{term} schedule".Code()} {"(shows times and scores for each match in the current or next week)".Italics()}\n{$"{term} schedule [week]".Code()} {"(shows times and scores for each match in the given week)".Italics()}\n{$"{term} schedule [abbreviated team name]".Code()} {"(shows times and scores for each match that a team plays)".Italics()}\nAll times are determined by the user's timezone setting.";
		public string Author => "Biendeo";
		public string Version => "1.5.0";

		private Lazy<Database> db;
		private Database Database => this.db.Value;
		private const string TimeFormatStringLong = "d/MM/yyyy h:mm:ss tt";
		private const string TimeFormatStringShort = "d/MM hh:mm tt";
        private readonly IDiscordClient discordClient;
        private readonly ILogger<OverwatchLeague> logger;
		private readonly IUserTimeZoneProvider timeZoneProvider;

        private string GetMatchDetails(Match match, TimeZoneInfo timeZone) {
			var sb = new StringBuilder();

			sb.Append("```");
			Team homeTeam = match.HomeTeam;
			Team awayTeam = match.AwayTeam;

			int currentHomeScore = match.HomeScore;
			int currentAwayScore = match.AwayScore;

			sb.AppendLine($"Match ID: {match.Id}");
			sb.AppendLine($"Planned time: {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, timeZone).ToString(TimeFormatStringLong)} {timeZone.ToShortString()} - {TimeZoneInfo.ConvertTimeFromUtc(match.EndTime, timeZone).ToString(TimeFormatStringLong)} {timeZone.ToShortString()}");

			if (match.ActualStartTime != null) {
				sb.Append("Real time: ");
				sb.Append(TimeZoneInfo.ConvertTimeFromUtc(match.ActualStartTime ?? default, timeZone).ToString(TimeFormatStringLong));
				sb.Append(" ");
				sb.Append(timeZone.ToShortString());
				sb.Append(" - ");
				if (match.ActualEndTime != null) {
					sb.AppendLine(TimeZoneInfo.ConvertTimeFromUtc(match.ActualEndTime ?? default, timeZone).ToString(TimeFormatStringLong));
					sb.Append(" ");
					sb.Append(timeZone.ToShortString());
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

		public async Task OnMessage(MessageCreateEventArgs e) {
			if (!this.db.IsValueCreated)
			{
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = "Overwatch League is still loading, please try again later!",
					Channel = e.Channel,
					LogMessage = "OverwatchLeagueNotReady"
				});
				return;
			}

			// Try and decipher the output.
			string[] splitMessage = e.Message.Content.Split(' ');
			TimeZoneInfo userTimeZone = await this.timeZoneProvider.GetTimeZone(e.Guild.Id, e.Author.Id);

			using (var wc = new WebClient()) {
				if (splitMessage.Length > 1 && splitMessage[1] == "live") {
					Match match = (from c in Database.Matches where c.EndTime > DateTime.UtcNow && c.StartTime < DateTime.UtcNow select c).FirstOrDefault();
					if (match == null) {
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = "No match is currently live!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLiveNoMatch"
						});
					} else {
						if (match.Games.Count == 0) {
							this.logger.LogInformation("Updating match {} on demand", match.Id);
							await match.UpdateMatch();
						}
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = GetMatchDetails(match, userTimeZone),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueLive"
						});
					}
				} else if (splitMessage.Length > 1 && splitMessage[1] == "next") {
					Match match = (from c in Database.Matches where c.StartTime > DateTime.UtcNow select c).FirstOrDefault();

					if (match == null) {
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = "There's no next match planned!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNextNoMatch"
						});
					} else {
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = GetMatchDetails(match, userTimeZone),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueNext"
						});
					}
				} else if (splitMessage.Length > 2 && splitMessage[1] == "match") {
					if (int.TryParse(splitMessage[2], out int matchId)) {
						Match match = (from c in Database.Matches where c.Id == matchId select c).FirstOrDefault();
						if (match == null) {
							await this.discordClient.SendMessage(this, new SendMessageEventArgs {
								Message = $"No match matches ID {matchId}!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueMatchNoMatch"
							});
						} else {
							if (match.Games.Count == 0) {
								this.logger.LogInformation("Updating match {} on demand", match.Id);
								await match.UpdateMatch();
							}
							await this.discordClient.SendMessage(this, new SendMessageEventArgs {
								Message = GetMatchDetails(match, userTimeZone),
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueMatch"
							});
						}
					} else {
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
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

					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
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
								sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {userTimeZone.ToShortString()} - ");
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


						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
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
								sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {userTimeZone.ToShortString()} - ");
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

						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = sb.ToString(),
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleWeek"
						});
					} else if (splitMessage.Length > 2 && splitMessage[2].Length == 3) {
						string teamName = splitMessage[2].ToUpper();
						Team team = (from c in Database.Teams where c.AbbreviatedName == teamName select c).FirstOrDefault();

						if (team == null) {
							await this.discordClient.SendMessage(this, new SendMessageEventArgs {
								// TODO term
								Message = $"Invalid team code. Use {$"?owl standings".Code()} to find your team's abbreviated name!",
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeamInvalid"
							});
						} else {
							var sb = new StringBuilder();

							sb.Append("```");

							foreach (var match in team.Matches) {
								if (match.HomeTeam == team) {
									sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {userTimeZone.ToShortString()} - {match.HomeTeam.AbbreviatedName} vs. {match.AwayTeam.AbbreviatedName}");
									if (match.Status != MatchStatus.Pending) {
										sb.Append($" ({match.HomeScore} - {match.AwayScore})");
									}
									if (match.Status == MatchStatus.Concluded) {
										sb.Append($" ({(match.HomeScore > match.AwayScore ? 'W' : 'L')})");
									}
								} else {
									sb.Append($"[{match.Id.ToString().PadLeft(5, ' ')}] {TimeZoneInfo.ConvertTimeFromUtc(match.StartTime, userTimeZone).ToString(TimeFormatStringShort).PadLeft(15, ' ')} {userTimeZone.ToShortString()} - {match.AwayTeam.AbbreviatedName} vs. {match.HomeTeam.AbbreviatedName}");
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

							await this.discordClient.SendMessage(this, new SendMessageEventArgs {
								Message = sb.ToString(),
								Channel = e.Channel,
								LogMessage = "OverwatchLeagueScheduleTeam"
							});
						}
					} else {
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = $"Invalid usage of the schedule command!",
							Channel = e.Channel,
							LogMessage = "OverwatchLeagueScheduleInvalid"
						});
						return;
					}
				} else {
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help owl".Code()}",
						Channel = e.Channel,
						LogMessage = "OverwatchLeagueUnknownCommand"
					});
				}
			}
		}
	}
}
