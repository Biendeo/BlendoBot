using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Program {
		public static DiscordClient discord;

		public static void Main(string[] args) {
			MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		static async Task MainAsync(string[] args) {
			discord = new DiscordClient(new DiscordConfiguration {
				Token = ReadToken("token.txt"),
				TokenType = TokenType.Bot
			});

			discord.Ready += Ready;
			discord.MessageCreated += MessageCreated;

			await discord.ConnectAsync();
			await Task.Delay(-1);
		}

		private static string ReadToken(string filePath) {
			string token = null;
			try {
				token = File.ReadAllText(filePath);
				if (token.Length != 59) {
					throw new Exception("Token in token.txt is incorrect!");
				}
			} catch (IOException exception) {
				throw new Exception("token.txt does not exist!");
			}
			return token;
		}

		private static async Task Ready(ReadyEventArgs e) {
			foreach (var g in discord.Guilds) {
				Console.WriteLine($"{g.Key}: {g.Value.MemberCount}");
			}
			await Task.Delay(0);
		}

		private static async Task MessageCreated(MessageCreateEventArgs e) {
			// The rule is: don't react to my own messages, and commands need to be triggered with
			// a ? character. Additional functionality should be added here.
			if (e.Author != discord.CurrentUser && e.Message.Content.StartsWith("?")) {
				await Command.ParseAndExecute(e);
			}
		}


	}
}
