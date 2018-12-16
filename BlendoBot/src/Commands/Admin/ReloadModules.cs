using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class ReloadModules {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?admin reload",
			Name = "Reload Modules",
			Description = "Reloads any DLL modules back into the bot.",
			Func = ReloadCommand
		};

		public static async Task ReloadCommand(MessageCreateEventArgs e) {
			Program.ReloadModules();

			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"Loaded {Commands.Command.AvailableCommands.Count} modules.",
				Channel = e.Channel,
				LogMessage = "AdminReloadModulesSuccess"
			});

			await Task.Delay(0);
		}
	}
}
