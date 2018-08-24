using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Admin {
		public static readonly string DummyUnknownCommand = "?admin unknown_command";

		public static readonly Dictionary<string, Command.CommandProps> AvailableCommands = new Dictionary<string, Command.CommandProps> {
			{ DummyUnknownCommand, new Command.CommandProps {
				Term = DummyUnknownCommand,
				Name = "Unknown Command",
				Description = "If you're reading this, then...whoops!",
				Func = UnknownCommand,
			}}, { "help", new Command.CommandProps {
				Term = "?admin help",
				Name = "Help",
				Description = "Posts what commands the admin panel can do.",
				Func = Help.HelpCommand,
			}}, { "disable", new Command.CommandProps {
				Term = "?admin disable",
				Name = "Disable",
				Description = "Disables a command from regular usage on the server.\nUsage: ?admin disable [command]",
				Func = Disable.DisableCommand,
			}}, { "enable", new Command.CommandProps {
				Term = "?admin enable",
				Name = "Enable",
				Description = "Enables a previously disabled command from regular usage on the server.\nUsage: ?admin enable [command]",
				Func = Enable.EnableCommand,
			}}, { "allow", new Command.CommandProps {
				Term = "?admin allow",
				Name = "Allow",
				Description = "Allows a specified user to interact with disabled commands.\nUsage: ?admin allow [@users ...]",
				Func = Allow.AllowCommand,
			}}, { "disallow", new Command.CommandProps {
				Term = "?admin disallow",
				Name = "Disallow",
				Description = "Disallows a specified user to interact with disabled commands.\nUsage: ?admin disallow [@users ...]",
				Func = Disallow.DisallowCommand,
			}}
		};

		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			string commandType = e.Message.Content.Split(' ').Length > 1 ? GetCommandType(e.Message.Content) : DummyUnknownCommand;
			if (AvailableCommands.ContainsKey(commandType) && (Program.Data.IsCommandEnabled(commandType, e.Guild) || Program.Data.IsUserVerified(e.Guild, e.Author))) {
				await AvailableCommands[commandType].Func(e);
			} else {
				await UnknownCommand(e);
			}
		}

		private static string GetCommandType(string message) {
			return message.Split(' ')[1].ToLower();
		}

		private static async Task UnknownCommand(MessageCreateEventArgs e) {
			await Program.SendMessage($"I didn't know what you meant by that, {e.Author.Username}. Use `?admin help` to see what I can do!", e.Channel, "AdminUnknownMessage");
		}
	}
}
