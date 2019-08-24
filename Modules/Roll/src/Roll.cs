using BlendoBotLib;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roll {
	public class Roll : CommandBase {
		public Roll(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string Term => "?roll";
		public override string Name => "Roll";
		public override string Description => "Rolls a given dice a given number of times.";
		public override string Usage => $"Usage: {"?random [dice value] [optional: num rolls = 1]".Code()}\nIf you request 20 or fewer rolls, I'll print out all the individual dice numbers. Otherwise, I'll give you the five number summary.";
		public override string Author => "Biendeo";
		public override string Version => "0.2.0";

		public override async Task<bool> Startup() {
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			// We want to make sure that there's either two or three arguments.
			if (splitMessage.Length < 2) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Too few arguments specified to `?roll`",
					Channel = e.Channel,
					LogMessage = "RollErrorTooFewArgs"
				});
			} else if (splitMessage.Length > 3) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Too many arguments specified to `?roll`",
					Channel = e.Channel,
					LogMessage = "RollErrorTooManyArgs"
				});
			} else {
				// If two arguments are given, then we are just rolling a dice one time.
				int rollCount = 1;
				if (!int.TryParse(splitMessage[1], out int diceValue) || (diceValue <= 0)) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The dice value given is not a positive integer",
						Channel = e.Channel,
						LogMessage = "RollErrorFirstArgInvalid"
					});
					return;
				}
				if (splitMessage.Length == 3 && (!int.TryParse(splitMessage[2], out rollCount) || rollCount <= 0)) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The roll count is not a positive integer",
						Channel = e.Channel,
						LogMessage = "RollErrorSecondArgInvalid"
					});
					return;
				}
				if (rollCount > 1000000) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
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
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
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
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
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
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
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
