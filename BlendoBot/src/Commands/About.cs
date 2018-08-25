using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public static class About {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?about",
			Name = "About",
			Description = "Posts information about this version of the bot.",
			Func = AboutCommand
		};

		public static async Task AboutCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			sb.AppendLine($"`{Program.Props.Name} {Program.Props.Version} ({Program.Props.Description}) by {Program.Props.Author}`");
			await Program.SendMessage(sb.ToString(), e.Channel, "About");
		}
	}
}
