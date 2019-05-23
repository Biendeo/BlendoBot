using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	/// <summary>
	/// The help command, which simply prints out the <see cref="CommandProps.Usage"/> property of a <see cref="ICommand"/>, or
	/// </summary>
	public class Help : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?help",
			Name = "Help",
			Description = "Posts what commands this bot can do, and additional help on how to use a command.",
			Usage = $"Use {"?help".Code()} to see a list of all commands on the server.\nUse {"?help [command]".Code()} to see help on a specific command, but you probably already know how to do that!",
			Author = "Biendeo",
			Version = "1.0.0",
			OnMessage = HelpCommand
		};

		public static async Task HelpCommand(MessageCreateEventArgs e) {
			// The help command definitely prints out a string. Which string will be determined by the arguments.
			var sb = new StringBuilder();
			if (e.Message.Content.Length == properties.Term.Length) {
				// This block runs if the ?help is run with no arguments (fortunately Discord trims whitespace).
				// All the commands are iterated through and their terms are printed out so the user knows which
				// commands are available.
				sb.AppendLine($"Use {($"{properties.Term} [command]").Code()} for specific help.");
				sb.AppendLine("List of available commands:");
				foreach (var command in Command.AvailableCommands) {
					if (Program.Data.IsCommandEnabled(command.Key, e.Guild)) {
						sb.AppendLine(command.Value.Term.Code());
					}
				}
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "HelpGeneric"
				});
			} else {
				// This block runs if the ?help is run with an argument. The relevant command is searched and its usage
				// is printed out, or an error message if that command doesn't exist.
				string specifiedCommand = e.Message.Content.Substring(properties.Term.Length + 1);
				if (!specifiedCommand.StartsWith('?')) {
					specifiedCommand = $"?{specifiedCommand}";
				}
				var command = Command.AvailableCommands.FirstOrDefault(x => x.Value.Term == specifiedCommand);
				if (command.Key == null) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"No command called {specifiedCommand.Code()}",
						Channel = e.Channel,
						LogMessage = "HelpErrorInvalidCommand"
					});
				} else {
					sb.AppendLine($"Help for {specifiedCommand.Code()}");
					if (command.Value.Usage != null && command.Value.Usage.Length != 0) {
						sb.AppendLine(command.Value.Usage);
					} else {
						sb.AppendLine("No help found for this command".Italics());
					}
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "HelpSpecific"
					});
				}
			}
		}
	}
}
