using BlendoBotLib;
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
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "AdminEnableTooFewArguments"
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
					LogMessage = "AdminEnableNotExist"
				});
				return;
			} else if (Program.Data.IsCommandEnabled(command, e.Guild)) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Command `{command}` is already enabled!",
					Channel = e.Channel,
					LogMessage = "AdminEnableAlreadyEnabled"
				});
				return;
			} else {
				Program.Data.EnableCommand(command, e.Guild);
				Program.Data.Save();
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Command `{command}` is now enabled!",
					Channel = e.Channel,
					LogMessage = "AdminEnableSuccess"
				});
			}
		}
	}
}
