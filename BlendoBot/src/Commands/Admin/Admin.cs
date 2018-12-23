using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public class Admin : ICommand {
		CommandProps ICommand.Properties => properties;

		public static readonly CommandProps properties = new CommandProps {
			Term = "?admin",
			Name = "Admin",
			Description = "Lets admins decide parts of the bot. Use `?admin help` to see more info.",
			Usage = $"Uh...",
			Author = "Biendeo",
			Version = "0.0.1",
			Func = ParseAndExecute
		};

		public static readonly Dictionary<string, CommandProps> AvailableCommands = new Dictionary<string, CommandProps> {
			{ Help.Properties.Term, Help.Properties },
			{ Disable.Properties.Term, Disable.Properties },
			{ Enable.Properties.Term, Enable.Properties },
			{ Allow.Properties.Term, Allow.Properties },
			{ Disallow.Properties.Term, Disallow.Properties },
			{ ReloadModules.Properties.Term, ReloadModules.Properties },
			{ UnloadModules.Properties.Term, UnloadModules.Properties }
		};

		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			string commandType = e.Message.Content.Split(' ').Length > 1 ? GetCommandType(e.Message.Content) : "invalid";
			if (AvailableCommands.ContainsKey(commandType) && (Program.Data.IsCommandEnabled(commandType, e.Guild) || Program.Data.IsUserVerified(e.Guild, e.Author))) {
				await AvailableCommands[commandType].Func(e);
			} else {
				await UnknownCommand(e);
			}
		}

		private static string GetCommandType(string message) {
			return message.Split(' ')[0].ToLower() + " " + message.Split(' ')[1].ToLower();
		}

		private static async Task UnknownCommand(MessageCreateEventArgs e) {
			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"I didn't know what you meant by that, {e.Author.Username}. Use `?admin help` to see what I can do!",
				Channel = e.Channel,
				LogMessage = "AdminUnknownMessage"
			});
		}
	}
}
