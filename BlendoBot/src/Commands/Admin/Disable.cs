using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Disable {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?admin disable",
			Name = "Disable",
			Description = "Disables a command from regular usage on the server.\nUsage: ?admin disable [command]",
			Func = DisableCommand
		};

		public static async Task DisableCommand(MessageCreateEventArgs e) {
			if (e.Message.Content.Split(' ').Length <= 2) {
				var sb = new StringBuilder();
				sb.AppendLine("Please add a command to disable it!");
				sb.Append("Enabled commands are: ");
				//TODO: Add a bit if there are no enabled commands.
				foreach (var c in Command.AvailableCommands) {
					if (Program.Data.IsCommandEnabled(c.Key, e.Guild)) {
						sb.Append($"`{c.Key}`, ");
					}
				}
				sb.Length = sb.Length - 2;
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "AdminDisbleTooFewArguments"
				});
				return;
			}

			string command = e.Message.Content.Split(' ')[2];
			if (command[0] != '?') {
				command = $"?{command}";
			}

			if (!Command.AvailableCommands.ContainsKey(command)) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Command `{command}` does not exist!",
					Channel = e.Channel,
					LogMessage = "AdminDisableNotExist"
				});
				return;
			} else if (!Program.Data.IsCommandEnabled(command, e.Guild)) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Command `{command}` is already disabled!",
					Channel = e.Channel,
					LogMessage = "AdminDisableAlreadyDisabled"
				});
				return;
			} else {
				Program.Data.DisableCommand(command, e.Guild);
				Program.Data.Save();
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Command `{command}` is now disabled!",
					Channel = e.Channel,
					LogMessage = "AdminDisableSuccess"
				});
			}
		}
	}
}
