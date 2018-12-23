using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roll {
	public class Roll : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?roll",
			Name = "Roll",
			Description = "Rolls a given dice a given number of times.",
			Usage = $"Usage: {"?random [dice value] [optional: num rolls = 1]".Code()}\nIf you request 20 or fewer rolls, I'll print out all the individual dice numbers. Otherwise, I'll give you the five number summary.",
			Author = "Biendeo",
			Version = "0.2.0",
			Func = RollCommand
		};

		public static async Task RollCommand(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			// We want to make sure that there's either two or three arguments.
			if (splitMessage.Length < 2) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Too few arguments specified to `?roll`",
					Channel = e.Channel,
					LogMessage = "RollErrorTooFewArgs"
				});
			} else if (splitMessage.Length > 3) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Too many arguments specified to `?roll`",
					Channel = e.Channel,
					LogMessage = "RollErrorTooManyArgs"
				});
			} else {
				// If two arguments are given, then we are just rolling a dice one time.
				int rollCount = 1;
				if (!int.TryParse(splitMessage[1], out int diceValue) || (diceValue <= 0)) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"The dice value given is not a positive integer",
						Channel = e.Channel,
						LogMessage = "RollErrorFirstArgInvalid"
					});
					return;
				}
				if (splitMessage.Length == 3 && (!int.TryParse(splitMessage[2], out rollCount) || rollCount <= 0)) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"The roll count is not a positive integer",
						Channel = e.Channel,
						LogMessage = "RollErrorSecondArgInvalid"
					});
					return;
				}
				if (rollCount > 1000000) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"The roll count is above 1000000, and I don't want to do that",
						Channel = e.Channel,
						LogMessage = "RollErrorSecondArgTooLarge"
					});
					return;
				}
				var results = new List<int>(rollCount);
				var random = new Random();
				for (int count = 0; count < rollCount; ++count) {
					// The int cast should truncate the double.
					int currentRoll = (int)(random.NextDouble() * diceValue + 1);
					results.Add(currentRoll);
				}
				double average = results.Average();
				if (rollCount == 1) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"**{results[0]}**",
						Channel = e.Channel,
						LogMessage = "RollErrorTooManyArgs"
					});
				} else if (rollCount <= 20) {
					var sb = new StringBuilder();
					sb.AppendLine($"Average: **{average}**");
					sb.Append("`[");
					for (int i = 0; i < rollCount; ++i) {
						sb.Append(results[i]);
						if (i != rollCount - 1) {
							sb.Append(", ");
						}
					}
					sb.Append("]`");
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "RollSuccessLowRoll"
					});
				} else {
					// If more than ten rolls occurred, a 5-number summary is a better way of showing the results.
					results.Sort();
					var sb = new StringBuilder();
					sb.AppendLine($"Average: **{average}**");
					int halfSize = rollCount / 2;
					double median = rollCount % 2 == 0 ? (results[halfSize] + results[halfSize - 1]) / 2.0 : results[halfSize];
					double firstQuart = halfSize % 2 == 0 ? (results[halfSize / 2] + results[halfSize / 2 - 1]) / 2.0 : results[halfSize / 2];
					double thirdQuart = halfSize % 2 == 0 ? (results[rollCount - halfSize / 2] + results[rollCount - halfSize / 2 - 1]) / 2.0 : results[rollCount - halfSize / 2];
					sb.Append($"`[{results.First()}, {firstQuart}, {median}, {thirdQuart}, {results.Last()}]`");
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "RollSuccessHighRoll"
					});
				}
			}

			await Task.Delay(0);
		}
	}
}
