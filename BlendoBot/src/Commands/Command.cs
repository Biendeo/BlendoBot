using BlendoBotLib;
using BlendoBotLib.Commands;
using BlendoBotLib.MessageListeners;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	/// <summary>
	/// Handles all the command based logic for BlendoBot.
	/// </summary>
	public static class Command {
		/// <summary>
		/// A dictionary of all the commands, with the keys being the strings for easy lookup.
		/// </summary>
		public static Dictionary<string, CommandProps> AvailableCommands = new Dictionary<string, CommandProps>();
		public static List<MessageListenerProps> MessageListeners = new List<MessageListenerProps>();

		/// <summary>
		/// Handles commands given a <see cref="MessageCreateEventArgs"/>. This should parse the command and execute the
		/// relevant command, or <see cref="UnknownCommand(MessageCreateEventArgs)"/> otherwise.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			string commandType = GetCommandType(e.Message.Content);
			if (AvailableCommands.ContainsKey(commandType) && (Program.Data.IsCommandEnabled(commandType, e.Guild) || Program.Data.IsUserVerified(e.Guild, e.Author))) {
				try {
					await AvailableCommands[commandType].OnMessage(e);
				} catch (Exception exc) {
					// This should hopefully make it such that the bot never crashes (although it hasn't stopped it).
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

		/// <summary>
		/// This grabs the command type (i.e. the question mark part of the command) and automatically makes it
		/// lowercase.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private static string GetCommandType(string message) {
			//TODO: Does CommandType indicate the actual directive?
			return message.Split(' ')[0].ToLower();
		}

		/// <summary>
		/// This sends a default message if a command wasn't found.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static async Task UnknownCommand(MessageCreateEventArgs e) {
			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"I didn't know what you meant by that, {e.Author.Username}. Use {"?help".Code()} to see what I can do!",
				Channel = e.Channel,
				LogMessage = "UnknownMessage"
			});
		}
	}
}
