using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Command {
		public static async Task ParseAndExecute(MessageCreateEventArgs e) {
			await e.Message.RespondAsync($"{e.Author.Username} said `{e.Message}`");
			Console.WriteLine($"{e.Author.Username} said `{e.Message}`");
		}
	}
}
