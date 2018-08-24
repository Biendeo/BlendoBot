using DSharpPlus.EventArgs;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Help {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?admin help",
			Name = "Help",
			Description = "Posts what commands the admin panel can do.",
			Func = HelpCommand
		};

		public static async Task HelpCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			foreach (var command in Admin.AvailableCommands) {
				sb.AppendLine($"**{command.Value.Name}** - `{command.Value.Term}`");
				sb.AppendLine($"{command.Value.Description}");
				sb.AppendLine();
			}
			await Program.SendMessage(sb.ToString(), e.Channel, "Help");
		}
	}
}
