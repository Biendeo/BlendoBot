using DSharpPlus.EventArgs;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Help {
		public static async Task HelpCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			foreach (var command in Admin.AvailableCommands) {
				if (command.Key != Admin.DummyUnknownCommand) {
					sb.AppendLine($"**{command.Value.Name}** - `{command.Value.Term}`");
					sb.AppendLine($"{command.Value.Description}");
					sb.AppendLine();
				}
			}
			await Program.SendMessage(sb.ToString(), e.Channel, "Help");
		}
	}
}
