using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public static class Roll {
		public static async Task RollCommand(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			// We want to make sure that there's either two or three arguments.
			if (splitMessage.Length < 2) {
				await Program.SendMessage($"Too few arguments specified to `?roll`", e.Channel, "RollErrorTooFewArgs");
			} else if (splitMessage.Length > 3) {
				await Program.SendMessage($"Too many arguments specified to `?roll`", e.Channel, "RollErrorTooManyArgs");
			} else {
				// If two arguments are given, then we are just rolling a dice one time.
				int rollCount = 1;
				if (!int.TryParse(splitMessage[1], out int diceValue) || (diceValue <= 0)) {
					await Program.SendMessage($"The dice value given is not a positive integer", e.Channel, "RollErrorFirstArgInvalid");
					return;
				}
				if (splitMessage.Length == 3 && (!int.TryParse(splitMessage[2], out rollCount) || rollCount <= 0)) {
					await Program.SendMessage($"The roll count is not a positive integer", e.Channel, "RollErrorSecondArgInvalid");
					return;
				}
				if (rollCount > 1000000) {
					await Program.SendMessage($"The roll count is above 1000000, and I don't want to do that", e.Channel, "RollErrorSecondArgTooLarge");
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
					await Program.SendMessage($"**{results[0]}**", e.Channel, "RollSuccessOneRoll");
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
					await Program.SendMessage(sb.ToString(), e.Channel, "RollSuccessLowRoll");
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
					await Program.SendMessage(sb.ToString(), e.Channel, "RollSuccessHighRoll");
				}
			}
		}
	}
}
