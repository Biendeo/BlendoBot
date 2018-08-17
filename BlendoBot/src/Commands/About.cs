using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public static class About {
		public static async Task AboutCommand(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			sb.AppendLine($"`{Program.BotName} {Program.BotVersion} ({Program.BotVersionTitle}) by {Program.Author}`");
			await Program.SendMessage(sb.ToString(), e.Channel, "About");
		}
	}
}
