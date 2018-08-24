using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public static class Command {

		//TODO: Could this be automatically generated?
		public static readonly Dictionary<string, CommandProps> AvailableCommands = new Dictionary<string, CommandProps> {
			{ Help.Properties.Term, Help.Properties },
			{ About.Properties.Term, About.Properties },
			{ Admin.Admin.Properties.Term, Admin.Admin.Properties },
			{ Roll.Properties.Term, Roll.Properties },
			{ MrPing.Properties.Term, MrPing.Properties },
			{ Regional.Properties.Term, Regional.Properties }
		};

		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			string commandType = GetCommandType(e.Message.Content);
			if (AvailableCommands.ContainsKey(commandType) && (Program.Data.IsCommandEnabled(commandType, e.Guild) || Program.Data.IsUserVerified(e.Guild, e.Author))) {
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
			return message.Split(' ')[0].ToLower();
		}

		private static async Task UnknownCommand(MessageCreateEventArgs e) {
			await Program.SendMessage($"I didn't know what you meant by that, {e.Author.Username}. Use `?help` to see what I can do!", e.Channel, "UnknownMessage");
		}
	}
}
