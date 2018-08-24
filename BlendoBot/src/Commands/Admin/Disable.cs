using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Disable {
		public static async Task DisableCommand(MessageCreateEventArgs e) {
			if (e.Message.Content.Split(' ').Length <= 2) {
				var sb = new StringBuilder();
				sb.AppendLine("Please add a command to disable it!");
				sb.Append("Enabled commands are: ");
				//TODO: Add a bit if there are no enabled commands.
				foreach (var c in Command.AvailableCommands) {
					if (Program.Data.IsCommandEnabled(c.Key, e.Guild) && c.Key != Command.DummyUnknownCommand) {
						sb.Append($"`{c.Key}`, ");
					}
				}
				sb.Length = sb.Length - 2;
				await Program.SendMessage(sb.ToString(), e.Channel, "AdminDisbleTooFewArguments");
				return;
			}

			string command = e.Message.Content.Split(' ')[2];
			if (command[0] != '?') {
				command = $"?{command}";
			}

			if (!Command.AvailableCommands.ContainsKey(command) || command == Command.DummyUnknownCommand) {
				await Program.SendMessage($"Command `{command}` does not exist!", e.Channel, "AdminDisableNotExist");
				return;
			} else if (!Program.Data.IsCommandEnabled(command, e.Guild)) {
				await Program.SendMessage($"Command `{command}` is already disabled!", e.Channel, "AdminDisableAlreadyDisabled");
				return;
			} else {
				Program.Data.DisableCommand(command, e.Guild);
				Program.Data.Save();
				await Program.SendMessage($"Command `{command}` is now disabled!", e.Channel, "AdminDisableSuccess");
			}
		}
	}
}
