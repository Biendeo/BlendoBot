using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Program {
		public static DiscordClient Discord;

		public const string BotName = "BlendoBot";
		public const string Author = "Biendeo";
		public const string BotVersion = "0.0.0";
		public const string BotVersionTitle = "Too early";

		public static void Main(string[] args) {
			MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		static async Task MainAsync(string[] args) {
			Discord = new DiscordClient(new DiscordConfiguration {
				Token = ReadToken("token.txt"),
				TokenType = TokenType.Bot
			});

			Discord.Ready += Ready;
			Discord.MessageCreated += MessageCreated;

			await Discord.ConnectAsync();
			await Task.Delay(-1);
		}

		private static string ReadToken(string filePath) {
			string token = null;
			try {
				token = File.ReadAllText(filePath);
				if (token.Length != 59) {
					throw new Exception("Token in token.txt is incorrect!");
				}
			} catch (IOException) {
				throw new Exception("token.txt does not exist!");
			}
			return token;
		}

		private static async Task Ready(ReadyEventArgs e) {
			foreach (var g in Discord.Guilds) {
				Console.WriteLine($"{g.Key}: {g.Value.MemberCount}");
			}
			await Task.Delay(0);
		}

		private static async Task MessageCreated(MessageCreateEventArgs e) {
			// The rule is: don't react to my own messages, and commands need to be triggered with
			// a ? character. Additional functionality should be added here.
			if (!e.Author.IsCurrent && e.Message.Content.StartsWith("?")) {
				await Command.ParseAndExecute(e);
			}
		}

		public static async Task SendMessage(string message, DiscordChannel channel, string logMessage = "a message") {
			Log.LogMessage(LogType.Log, $"Sending {logMessage} to channel #{channel.Name} ({channel.Guild.Name})");
			await channel.SendMessageAsync(message);
		}

	}
}
