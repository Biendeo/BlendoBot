using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Program {
		public static DiscordClient Discord;
		public static readonly Config Props = Config.FromJson("config.json");
		public static readonly Data Data = Data.Load();

		public static void Main(string[] args) {
			MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private static async Task MainAsync(string[] args) {
			Discord = new DiscordClient(new DiscordConfiguration {
				Token = Props.Token,
				TokenType = TokenType.Bot
			});

			Discord.Ready += Ready;
			Discord.MessageCreated += MessageCreated;
			Discord.GuildCreated += GuildCreated;

			await Discord.ConnectAsync();

			await Task.Delay(-1);
		}

		private static async Task Ready(ReadyEventArgs e) {
			await Discord.UpdateStatusAsync(new DiscordActivity(Props.ActivityName, Props.ActivityType), UserStatus.Online, DateTime.Now);
			Data.VerifyData();
			Log.LogMessage(LogType.Log, $"{Props.Name} ({Props.Version}) is up and ready!");
		}

		private static async Task MessageCreated(MessageCreateEventArgs e) {
			// The rule is: don't react to my own messages, and commands need to be triggered with
			// a ? character.
			if (!e.Author.IsCurrent && e.Message.Content.Length > 1 && e.Message.Content.StartsWith("?") && e.Message.Content[1].IsAlphabetical()) {
				await Commands.Command.ParseAndExecute(e);
			}
		}

		private static async Task GuildCreated(GuildCreateEventArgs e) {
			if (!Data.Servers.ContainsKey(e.Guild.Id)) {
				Data.Servers.Add(e.Guild.Id, new Data.ServerInfo());
				Data.Save();
			}
			Log.LogMessage(LogType.Log, $"Joined server {e.Guild.Name})");
			await Task.Delay(0);
		}

		public static async Task<DiscordMessage> SendMessage(string message, DiscordChannel channel, string logMessage = "a message") {
			Log.LogMessage(LogType.Log, $"Sending message {logMessage} to channel #{channel.Name} ({channel.Guild.Name})");
			return await channel.SendMessageAsync(message);
		}

		public static async Task<DiscordMessage> SendFile(string filePath, DiscordChannel channel, string logMessage = "a file") {
			Log.LogMessage(LogType.Log, $"Sending file {logMessage} to channel #{channel.Name} ({channel.Guild.Name})");
			return await channel.SendFileAsync(filePath);
		}

		public static async Task<DiscordMessage> SendException(Exception e, DiscordChannel channel, string logExceptionType = "exception") {
			Log.LogMessage(LogType.Error, $"{logExceptionType}\n{e}");
			return await channel.SendMessageAsync($"A {logExceptionType} occurred. Alert the authorities!\n```\n{e}\n```");
		}

		public static bool IsAlphabetical(this char c) {
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}
	}
}
