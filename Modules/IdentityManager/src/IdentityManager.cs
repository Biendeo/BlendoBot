using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IdentityManager {
	public class IdentityManager : ICommand {
		CommandProps ICommand.Properties => properties;

		public static readonly CommandProps properties = new CommandProps {
			Term = "?remind",
			Name = "Remind Me",
			Description = "Lets you store certain identities",
			Usage = $"Usage: None yet, I don't do anything.",
			Author = "Biendeo",
			Version = "0.1.0",
			Startup = Startup,
			OnMessage = IdentityManagerCommand
		};

		private static async Task<bool> Startup() {
			await Task.Delay(0);
			return false;
		}

		public static async Task IdentityManagerCommand(MessageCreateEventArgs e) {
			await Task.Delay(0);
		}
	}
}
