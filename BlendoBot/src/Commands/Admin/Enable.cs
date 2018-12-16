using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Enable {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?admin enable",
			Name = "Enable",
			Description = "Enables a previously disabled command from regular usage on the server.\nUsage: ?admin enable [command]",
			Func = EnableCommand
		};

		public static async Task EnableCommand(MessageCreateEventArgs e) {
			if (e.Message.Content.Split(' ').Length <= 2) {
				var sb = new StringBuilder();
				sb.AppendLine("Please add a command to enable it!");
				sb.Append("Disabled commands are: ");
				//TODO: Add a bit if there are no disabled commands.
				foreach (var c in Command.AvailableCommands) {
					if (!Program.Data.IsCommandEnabled(c.Key, e.Guild)) {
						sb.Append($"`{c.Key}`, ");
					}
				}
				sb.Length = sb.Length - 2;
				await Program.SendMessage(sb.ToString(), e.Channel, "AdminEnableTooFewArguments");
				return;
			}

			string command = e.Message.Content.Split(' ')[2];
			if (command[0] != '?') {
				command = $"?{command}";
			}

			if (!Command.AvailableCommands.ContainsKey(command)) {
				await Program.SendMessage($"Command `{command}` does not exist!", e.Channel, "AdminEnableNotExist");
				return;
			} else if (Program.Data.IsCommandEnabled(command, e.Guild)) {
				await Program.SendMessage($"Command `{command}` is already enabled!", e.Channel, "AdminEnableAlreadyEnabled");
				return;
			} else {
				Program.Data.EnableCommand(command, e.Guild);
				Program.Data.Save();
				await Program.SendMessage($"Command `{command}` is now enabled!", e.Channel, "AdminEnableSuccess");
			}
		}
	}
}
