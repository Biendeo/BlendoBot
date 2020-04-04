using BlendoBotLib;
using DSharpPlus.EventArgs;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	/// <summary>
	/// The help command, which simply prints out the <see cref="CommandProps.Usage"/> property of a <see cref="ICommand"/>, or
	/// </summary>
	public class Help : CommandBase {
		public Help(ulong guildId, Program program) : base(guildId, program) {
			this.program = program;
		}

		public override string DefaultTerm => "?help";
		public override string Name => "Help";
		public override string Description => "Posts what commands this bot can do, and additional help on how to use a command.";
		public override string Usage => $"Use {Term.Code()} to see a list of all commands on the server.\nUse {$"{Term} [command]".Code()} to see help on a specific command, but you probably already know how to do that!";
		public override string Author => "Biendeo";
		public override string Version => "1.0.0";

		private readonly Program program;

		public override async Task<bool> Startup() {
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			// The help command definitely prints out a string. Which string will be determined by the arguments.
			var sb = new StringBuilder();
			if (e.Message.Content.Length == Term.Length) {
				// This block runs if the ?help is run with no arguments (fortunately Discord trims whitespace).
				// All the commands are iterated through and their terms are printed out so the user knows which
				// commands are available.
				sb.AppendLine($"Use {$"{Term} [command]".Code()} for specific help.");
				sb.AppendLine("List of available commands:");
				foreach (var command in program.GetCommands(this, GuildId)) {
						sb.AppendLine(command.Term.Code());
				}
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "HelpGeneric"
				});
			} else {
				// This block runs if the ?help is run with an argument. The relevant command is searched and its usage
				// is printed out, or an error message if that command doesn't exist.
				string specifiedCommand = e.Message.Content.Substring(Term.Length + 1);
				var command = program.GetCommand(this, GuildId, specifiedCommand);
				if (command == null) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"No command called {specifiedCommand.Code()}",
						Channel = e.Channel,
						LogMessage = "HelpErrorInvalidCommand"
					});
				} else {
					sb.AppendLine($"Help for {command.Term.Code()}:");
					if (command.Usage != null && command.Usage.Length != 0) {
						sb.AppendLine(command.Usage);
					} else {
						sb.AppendLine("No help found for this command".Italics());
					}
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "HelpSpecific"
					});
				}
			}
		}
	}
}
