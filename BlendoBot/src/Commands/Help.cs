using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public class Help : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?help",
			Name = "Help",
			Description = "Posts what commands this bot can do. You probably know how to access this already.",
			Func = HelpCommand
		};

	public static async Task HelpCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			foreach (var command in Command.AvailableCommands) {
				if (Program.Data.IsCommandEnabled(command.Key, e.Guild)) {
					sb.AppendLine($"**{command.Value.Name}** - `{command.Value.Term}`");
					sb.AppendLine($"{command.Value.Description}");
					sb.AppendLine();
				}
			}
			await Program.SendMessage(sb.ToString(), e.Channel, "Help");
		}
	}
}
