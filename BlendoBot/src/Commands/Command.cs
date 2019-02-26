using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public static class Command {
		public static Dictionary<string, CommandProps> AvailableCommands = new Dictionary<string, CommandProps>();

		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			string commandType = GetCommandType(e.Message.Content);
			if (AvailableCommands.ContainsKey(commandType) && (Program.Data.IsCommandEnabled(commandType, e.Guild) || Program.Data.IsUserVerified(e.Guild, e.Author))) {
				try {
					await AvailableCommands[commandType].OnMessage(e);
				} catch (Exception exc) {
					await Methods.SendException(null, new SendExceptionEventArgs {
						Exception = exc,
						Channel = e.Channel,
						LogExceptionType = "GenericExceptionNotCaught"
					});
				}
			} else {
				await UnknownCommand(e);
			}
		}

		private static string GetCommandType(string message) {
			return message.Split(' ')[0].ToLower();
		}

		private static async Task UnknownCommand(MessageCreateEventArgs e) {
			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"I didn't know what you meant by that, {e.Author.Username}. Use `?help` to see what I can do!",
				Channel = e.Channel,
				LogMessage = "UnknownMessage"
			});
		}
	}
}
