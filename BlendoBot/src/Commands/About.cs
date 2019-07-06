using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	/// <summary>
	/// The about command, which simply prints out the <see cref="CommandProps.Description"/> property of a <see cref="ICommand"/>, or on its own details about the bot.
	/// </summary>
	public class About : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?about",
			Name = "About",
			Description = "Posts information about this version of the bot, or of any loaded module. You probably already know how to use this command by now.",
			Usage = $"Use {"?about".Code()} to see the information about the bot.\nUse {"?about [command]".Code()} to see information about another command.",
			Author = "Biendeo",
			Version = "1.0.0",
			OnMessage = AboutCommand
		};

		public static async Task AboutCommand(MessageCreateEventArgs e) {
			// The about command definitely prints out a string. Which string will be determined by the arguments.
			var sb = new StringBuilder();

			if (e.Message.Content.Length == properties.Term.Length) {
				// This block runs if the ?about is run with no arguments (fortunately Discord trims whitespace). Simply
				// print out a message.
				sb.AppendLine($"{Program.Config.Name} {Program.Config.Version} ({Program.Config.Description}) by {Program.Config.Author}\nBeen running for {(DateTime.Now - Program.StartTime).Days} days, {(DateTime.Now - Program.StartTime).Hours} hours, {(DateTime.Now - Program.StartTime).Minutes} minutes, and {(DateTime.Now - Program.StartTime).Seconds} seconds.");
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "About"
				});
			} else {
				// This block runs if the ?about is run with an argument. Take the remaining length of the string and
				// figure out which command uses that. Then print their name, version, author, and description.
				string specifiedCommand = e.Message.Content.Substring(properties.Term.Length + 1);
				if (!specifiedCommand.StartsWith('?')) {
					specifiedCommand = $"?{specifiedCommand}";
				}
				var command = Command.AvailableCommands.FirstOrDefault(x => x.Value.Term == specifiedCommand);
				if (command.Key == null) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"No command called {specifiedCommand.Code()}",
						Channel = e.Channel,
						LogMessage = "AboutErrorInvalidCommand"
					});
				} else {
					sb.AppendLine($"{command.Value.Name.Bold()} ({command.Value.Version?.Italics()}) by {command.Value.Author?.Italics()}");
					sb.AppendLine(command.Value.Description);
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "AboutSpecific"
					});
				}
			}
		}
	}
}
