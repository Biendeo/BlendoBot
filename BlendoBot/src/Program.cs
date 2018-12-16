using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BlendoBot {
	public static class Program {
		public static DiscordClient Discord;
		public static readonly Config Props = Config.FromJson("config.json");
		public static readonly Data Data = Data.Load();
		public static string LogFile;

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

			Methods.SendMessage = Methods_MessageSent;
			Methods.SendFile = Methods_FileSent;
			Methods.SendException = Methods_ExceptionSent;
			Methods.Log = Methods_MessageLogged;

			LogFile = Path.Join("log", $"{DateTime.Now.ToString("yyyyMMddHHmmss")}.log");

			await Discord.ConnectAsync();

			ReloadModules();

			await Task.Delay(-1);
		}

		private static async Task<DiscordMessage> Methods_MessageSent(object sender, SendMessageEventArgs e) {
			Methods.Log(sender, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Sending message {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})"
			});
			return await e.Channel.SendMessageAsync(e.Message);
		}

		private static async Task<DiscordMessage> Methods_FileSent(object sender, SendFileEventArgs e) {
			Methods.Log(sender, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Sending file {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})"
			});
			return await e.Channel.SendFileAsync(e.FilePath);
		}

		private static async Task<DiscordMessage> Methods_ExceptionSent(object sender, SendExceptionEventArgs e) {
			Methods.Log(sender, new LogEventArgs {
				Type = LogType.Error,
				Message = $"{e.LogExceptionType}\n{e.Exception}"
			});
			return await e.Channel.SendMessageAsync($"A {e.LogExceptionType} occurred. Alert the authorities!\n```\n{e.Exception}\n```");
		}

		private static void Methods_MessageLogged(object sender, LogEventArgs e) {
			string typeString = Enum.GetName(typeof(LogType), e.Type);
			string logMessage = $"[{typeString}] ({DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}) | {e.Message}";
			Console.WriteLine(logMessage);
			if (!Directory.Exists("log")) Directory.CreateDirectory("log");
			File.AppendAllText(LogFile, logMessage + "\n");
		}

		private static async Task Ready(ReadyEventArgs e) {
			await Discord.UpdateStatusAsync(new DiscordActivity(Props.ActivityName, Props.ActivityType), UserStatus.Online, DateTime.Now);
			Data.VerifyData();
			Methods.Log(null, new LogEventArgs {
				Type = LogType.Log,
				Message = $"{Props.Name} ({Props.Version}) is up and ready!"
			});
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
			Methods.Log(null, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Joined server {e.Guild.Name})"
			});
			await Task.Delay(0);
		}

		public static void UnloadModules() {
			Commands.Command.AvailableCommands.Clear();
			var assembly = Assembly.GetEntryAssembly();
			var validTypes = assembly.DefinedTypes.ToList().FindAll(t => t.GetInterfaces().ToList().Contains(typeof(ICommand)));
			foreach (var validType in validTypes) {
				var t = Activator.CreateInstance(validType) as ICommand;
				Commands.Command.AvailableCommands.Add(t.Properties.Term, t.Properties);
				Methods.Log(null, new LogEventArgs {
					Type = LogType.Log,
					Message = $"Successfully loaded internal module {t.Properties.Name} ({t.Properties.Term})"
				});
			}
		}

		public static void ReloadModules() {
			UnloadModules();

			var dlls = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).ToList().FindAll(s => Path.GetExtension(s) == ".dll");
			dlls.RemoveAll(s => Path.GetFileName(s) == "BlendoBot.dll" || Path.GetFileName(s) == "BlendoBotLib.dll");

			foreach (string dll in dlls) {
				try {
					var assembly = Assembly.LoadFrom(dll);
					var types = assembly.ExportedTypes;
					var validTypes = assembly.ExportedTypes.ToList().FindAll(t => t.GetInterfaces().ToList().Contains(typeof(ICommand)));
					foreach (var validType in validTypes) {
						var t = Activator.CreateInstance(validType) as ICommand;
						Commands.Command.AvailableCommands.Add(t.Properties.Term, t.Properties);
						Methods.Log(null, new LogEventArgs {
							Type = LogType.Log,
							Message = $"Successfully loaded external module {t.Properties.Name} ({t.Properties.Term})"
						});
					}
				} catch (Exception) { }
			}
		}

		public static bool IsAlphabetical(this char c) {
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}
	}
}
