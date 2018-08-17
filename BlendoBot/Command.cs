using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Command {
		/// <summary>
		/// A structure which lets you determine properties for a command. These should be stored in
		/// availableCommands and only referred to otherwise.
		/// </summary>
		private struct CommandProps {
			/// <summary>
			/// The command that users will need to type in order to access this command.
			/// </summary>
			public string Term { get; set; }
			/// <summary>
			/// The user-friendly name for this command. Appears in help.
			/// </summary>
			public string Name { get; set; }
			/// <summary>
			/// A description of this command. Appears in help.
			/// </summary>
			public string Description { get; set; }
			/// <summary>
			/// The function that handles this command. Since all commands are made by creating an
			/// event, all command handles must forward the MessageCreateEventArgs from when the
			/// message was received. They're also async, so they'll need to return Task.
			/// </summary>
			public Func<MessageCreateEventArgs, Task> Func { get; set; }
			/// <summary>
			/// Whether this command can be called or not by regular users. Authorised users may
			/// override this one (you can create your own permissions within these functions).
			/// </summary>
			public bool Enabled { get; set; }
			/// <summary>
			/// Whether this command appears on the help menu. Useful for allowing hidden commands.
			/// </summary>
			public bool AppearsInHelp { get; set; }
		}

		private static string dummyUnknownCommand = "unknown_command";

		private static Dictionary<string, CommandProps> availableCommands = new Dictionary<string, CommandProps> {
			{ dummyUnknownCommand, new CommandProps {
				Term = dummyUnknownCommand,
				Name = "Unknown Command",
				Description = "If you're reading this, then...whoops!",
				Func = UnknownCommand,
				Enabled = false,
				AppearsInHelp = false
			}}, { "?help", new CommandProps {
				Term = "?help",
				Name = "Help",
				Description = "Posts what commands this bot can do. You probably know how to access this already.",
				Func = HelpCommand,
				Enabled = true,
				AppearsInHelp = true
			}}, { "?about", new CommandProps {
				Term = "?about",
				Name = "About",
				Description = "Posts information about this version of the bot.",
				Func = AboutCommand,
				Enabled = true,
				AppearsInHelp = true
			}}, { "?roll", new CommandProps {
				Term = "?roll",
				Name = "Roll",
				Description = "Rolls a given dice a given number of times.\nUsage: ?random [dice value] [optional: num rolls = 1]",
				Func = RollCommand,
				Enabled = true,
				AppearsInHelp = true
			}}
		};

		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			//await Program.SendMessage($"{e.Author.Username} said `{e.Message}`", e.Channel);
			string commandType = GetCommandType(e.Message.Content);
			if (availableCommands.ContainsKey(commandType) && availableCommands[commandType].Enabled) {
				await availableCommands[commandType].Func(e);
			} else {
				await UnknownCommand(e);
			}
		}

		private static string GetCommandType(string message) {
			return message.Split(' ')[0];
		}

		private static async Task UnknownCommand(MessageCreateEventArgs e) {
			await Program.SendMessage($"I didn't know what you meant by that {e.Author.Username}. Use `?help` to see what I can do!", e.Channel, "UnknownMessage");
		}

		private static async Task HelpCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			foreach (var command in availableCommands) {
				if (command.Value.AppearsInHelp) {
					sb.AppendLine($"**{command.Value.Name}** - `{command.Value.Term}`");
					sb.AppendLine($"{command.Value.Description}");
					sb.AppendLine();
				}
			}
			await Program.SendMessage(sb.ToString(), e.Channel, "Help");
		}

		private static async Task AboutCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			sb.AppendLine($"`{Program.BotName} {Program.BotVersion} ({Program.BotVersionTitle}) by {Program.Author}`");
			await Program.SendMessage(sb.ToString(), e.Channel, "About");
		}

		private static async Task RollCommand(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			// We want to make sure that there's either two or three arguments.
			if (splitMessage.Length < 2) {
				await Program.SendMessage($"Too few arguments specified to `?random`", e.Channel, "RollErrorTooFewArgs");
			} else if (splitMessage.Length > 3) {
				await Program.SendMessage($"Too many arguments specified to `?random`", e.Channel, "RollErrorTooManyArgs");
			} else {
				// If two arguments are given, then we are just rolling a dice one time.
				int rollCount = 1;
				int diceValue = 2;
				if (!int.TryParse(splitMessage[1], out diceValue) || (diceValue <= 0)) {
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
