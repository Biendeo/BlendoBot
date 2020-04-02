using BlendoBotLib;
using DSharpPlus.EventArgs;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	/// <summary>
	/// The about command, which simply prints out the <see cref="CommandProps.Description"/> property of a <see cref="ICommand"/>, or on its own details about the bot.
	/// </summary>
	public class About : CommandBase {
		public About(ulong guildId, Program program) : base(guildId, program) {
			this.program = program;
		}

		public override string DefaultTerm => "?about";
		public override string Name => "About";
		public override string Description => "Posts information about this version of the bot, or of any loaded module. You probably already know how to use this command by now.";
		public override string Usage => $"Use {Term.Code()} to see the information about the bot.\nUse {$"{Term} [command]".Code()} to see information about another command.";
		public override string Author => "Biendeo";
		public override string Version => "1.0.0";

		private readonly Program program;

		public override async Task<bool> Startup() {
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			// The about command definitely prints out a string. Which string will be determined by the arguments.
			var sb = new StringBuilder();

			if (e.Message.Content.Length == Term.Length) {
				// This block runs if the ?about is run with no arguments (fortunately Discord trims whitespace). Simply
				// print out a message.
				sb.AppendLine($"{program.Config.Name} {program.Config.Version} ({program.Config.Description}) by {program.Config.Author}\nBeen running for {(DateTime.Now - program.StartTime).Days} days, {(DateTime.Now - program.StartTime).Hours} hours, {(DateTime.Now - program.StartTime).Minutes} minutes, and {(DateTime.Now - program.StartTime).Seconds} seconds.");
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "About"
				});
			} else {
				// This block runs if the ?about is run with an argument. Take the remaining length of the string and
				// figure out which command uses that. Then print their name, version, author, and description.
				string specifiedCommand = e.Message.Content.Substring(Term.Length + 1);
				if (!specifiedCommand.StartsWith('?')) {
					specifiedCommand = $"?{specifiedCommand}";
				}
				var command = program.GetCommand(this, GuildId, specifiedCommand);
				if (command == null) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"No command called {specifiedCommand.Code()}",
						Channel = e.Channel,
						LogMessage = "AboutErrorInvalidCommand"
					});
				} else {
					sb.AppendLine($"{command.Name.Bold()} ({command.Version?.Italics()}) by {command.Author?.Italics()}");
					sb.AppendLine(command.Description);
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "AboutSpecific"
					});
				}
			}
		}
	}
}
