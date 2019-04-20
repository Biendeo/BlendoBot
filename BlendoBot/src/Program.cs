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
		public static DateTime StartTime;

		public static void Main(string[] args) {
			MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private static async Task MainAsync(string[] args) {
			if (Props == null) {
				Environment.Exit(1);
			}
			Discord = new DiscordClient(new DiscordConfiguration {
				Token = Props.Private.Token,
				TokenType = TokenType.Bot
			});

			Discord.Ready += Ready;
			Discord.MessageCreated += MessageCreated;
			Discord.GuildCreated += GuildCreated;

			Methods.SendMessage = Methods_MessageSent;
			Methods.SendFile = Methods_FileSent;
			Methods.SendException = Methods_ExceptionSent;
			Methods.Log = Methods_MessageLogged;

			StartTime = DateTime.Now;
			LogFile = Path.Join("log", $"{StartTime.ToString("yyyyMMddHHmmss")}.log");

			await Discord.ConnectAsync();

			await ReloadModulesAsync();

			await Task.Delay(-1);
		}

		private static async Task<DiscordMessage> Methods_MessageSent(object sender, SendMessageEventArgs e) {
			Methods.Log(sender, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Sending message {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})"
			});
			if (e.LogMessage.Length > 2000) {
				int oldLength = e.Message.Length;
				e.LogMessage = e.LogMessage.Substring(0, 2000);
				Methods.Log(sender, new LogEventArgs {
					Type = LogType.Warning,
					Message = $"Last message was {oldLength} characters long, truncated to 2000"
				});
			}
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
			string messageHeader = $"A {e.LogExceptionType} occurred. Alert the authorities!\n```\n";
			string messageFooter = "\n```";
			string exceptionString = e.Exception.ToString();
			if (exceptionString.Length + messageHeader.Length + messageFooter.Length > 2000) {
				int oldLength = exceptionString.Length;
				exceptionString = exceptionString.Substring(0, 2000 - messageHeader.Length - messageFooter.Length);
				Methods.Log(sender, new LogEventArgs {
					Type = LogType.Warning,
					Message = $"Last message was {oldLength} characters long, truncated to {exceptionString.Length}"
				});
			}
			return await e.Channel.SendMessageAsync(messageHeader + exceptionString + messageFooter);
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
				Message = $"{Props.Name} ({Props.Version}) is connected to Discord!"
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

		public static async Task ReloadModulesAsync() {
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
						if (await t.Properties.Startup()) {
							Commands.Command.AvailableCommands.Add(t.Properties.Term, t.Properties);
							Methods.Log(null, new LogEventArgs {
								Type = LogType.Log,
								Message = $"Successfully loaded external module {t.Properties.Name} ({t.Properties.Term})"
							});
						} else {
							Methods.Log(null, new LogEventArgs {
								Type = LogType.Error,
								Message = $"Could not load module {t.Properties.Name} ({t.Properties.Term})"
							});
						}
					}
				} catch (Exception) { }
			}
		}

		public static bool IsAlphabetical(this char c) {
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}
	}
}
