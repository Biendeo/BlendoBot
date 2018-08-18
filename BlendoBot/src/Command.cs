using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Command {
		/// <summary>
		/// A structure which lets you determine properties for a command. These should be stored in
		/// availableCommands and only referred to otherwise.
		/// </summary>
		public struct CommandProps {
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

		public static readonly Dictionary<string, CommandProps> AvailableCommands = new Dictionary<string, CommandProps> {
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
				Func = Commands.Help.HelpCommand,
				Enabled = true,
				AppearsInHelp = true
			}}, { "?about", new CommandProps {
				Term = "?about",
				Name = "About",
				Description = "Posts information about this version of the bot.",
				Func = Commands.About.AboutCommand,
				Enabled = true,
				AppearsInHelp = true
			}}, { "?roll", new CommandProps {
				Term = "?roll",
				Name = "Roll",
				Description = "Rolls a given dice a given number of times.\nUsage: ?random [dice value] [optional: num rolls = 1]\n20 or fewer rolls returns all the roll results, any more and a five-number summary is used.",
				Func = Commands.Roll.RollCommand,
				Enabled = true,
				AppearsInHelp = true
			}}, { "?mrping", new CommandProps {
				Term = "?mrping",
				Name = "Mr. Ping Challenge",
				Description = "Subjects someone to the Mr. Ping Challenge!",
				Func = Commands.MrPing.MrPingCommand,
				Enabled = true,
				AppearsInHelp = true
			}}
		};

		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			string commandType = GetCommandType(e.Message.Content);
			if (AvailableCommands.ContainsKey(commandType) && AvailableCommands[commandType].Enabled) {
				try {
					await AvailableCommands[commandType].Func(e);
				} catch (Exception exc) {
					await Program.SendException(exc, e.Channel, "GenericExceptionNotCaught");
				}
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
	}
}
