using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class UnloadModules {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?admin unload",
			Name = "Unload Modules",
			Description = "Unloads any additional DLL modules.",
			Func = UnloadCommand
		};

		public static async Task UnloadCommand(MessageCreateEventArgs e) {
			Program.UnloadModules();

			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"Unloaded all modules, {Command.AvailableCommands.Count} defaulted modules exist.",
				Channel = e.Channel,
				LogMessage = "AdminUnloadModulesSuccess"
			});
			await Task.Delay(0);
		}
	}
}
