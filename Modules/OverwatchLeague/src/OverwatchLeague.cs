using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
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
			Usage = $"Usage:\n{"?overwatchleague live".Code()} {"(stats about the match that is currently on)".Italics()}\n{"?overwatchleague next".Code()} {"(stats about the next match that will be played)".Italics()}\nAll times listed are in UTC.",
			Author = "Biendeo",
			Version = "0.1.0",
			Startup = async () => { await Task.Delay(0); return true; },
			OnMessage = OverwatchLeagueCommand
		};

		private static string GetMatchDetails(dynamic match) {
			var sb = new StringBuilder();

			sb.Append("```");
			var homeTeam = match.competitors[0];
			var awayTeam = match.competitors[1];

			var currentHomeScore = match.scores[0].value;
			var currentAwayScore = match.scores[1].value;

			sb.AppendLine($"Planned time: {new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.startDateTS).ToString("d/MM/yyyy h:mm:ss tt K")} - {new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.endDateTS).ToString("d/MM/yyyy h:mm:ss tt K")}");
			sb.Append("Real time: ");

			try {
				sb.Append(new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.actualStartDate).ToString("d/MM/yyyy h:mm:ss tt K"));
				sb.Append(" - ");
			} catch (RuntimeBinderException) {
				sb.Append("??? - ");
			}

			try {
				sb.Append(new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds((double)match.actualEndDate).ToString("d/MM/yyyy h:mm:ss tt K"));
			} catch (RuntimeBinderException) {
				sb.Append("???");
			}
			sb.AppendLine();

			sb.AppendLine($"{homeTeam.name} vs. {awayTeam.name}");
			sb.AppendLine($"{currentHomeScore} - {currentAwayScore}");

			string[] mapTypes = { "Control", "Hybrid", "Assault", "Payload", "Control" };

			foreach (var game in match.games) {
				sb.AppendLine();
				sb.AppendLine($"Map {game.number} on {(game.attributes.map != null ? game.attributes.map : "???")} ({mapTypes[game.number - 1]}) - {game.status}");
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
