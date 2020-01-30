using BlendoBot.Commands.Admin;
using BlendoBotLib;
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
	public class Program : IBotMethods {
		private DiscordClient DiscordClient { get; set; }
		private string ConfigPath { get; }
		public Config Config { get; private set; }
		private string LogFile { get; set; }
		public DateTime StartTime { get; private set; }

		private Dictionary<string, Type> LoadedCommands { get; set; }
		private Dictionary<string, Type> SystemCommands { get; set; }
		private Dictionary<ulong, Dictionary<string, CommandBase>> GuildCommands { get; set; }
		private Dictionary<ulong, List<IMessageListener>> GuildMessageListeners { get; set; }

		public static void Main(string[] args) {
			var program = new Program("config.cfg");
			program.Start(args).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public Program(string configPath) {
			ConfigPath = configPath;
			LoadedCommands = new Dictionary<string, Type>();
			SystemCommands = new Dictionary<string, Type>();
			GuildCommands = new Dictionary<ulong, Dictionary<string, CommandBase>>();
			GuildMessageListeners = new Dictionary<ulong, List<IMessageListener>>();
			// The rest of the fields will be initialised during the Start operation.
		}

		public async Task Start(string[] _) {
			if (!Config.FromFile(ConfigPath, out Config readInConfig)) {
				Config = readInConfig;
				Console.Error.WriteLine($"Could not find {ConfigPath}! A default one will be created. Please modify the appropriate fields!");
				CreateDefaultConfig();
				Environment.Exit(1);
			} else {
				Config = readInConfig;
				Console.WriteLine($"Successfully read config file: bot name is {Config.Name}");

				if (Config.ActivityType.HasValue ^ Config.ActivityName != null) {
					Console.WriteLine("The config's ActivityType and ActivityName must both be present to work. Defaulting to no current activity.");
				}
			}

			//! This is very unsafe because other modules can attempt to read the bot API token, and worse, try and
			//! change it.
			DiscordClient = new DiscordClient(new DiscordConfiguration {
				Token = Config.ReadString(this, "BlendoBot", "Token"),
				TokenType = TokenType.Bot
			});

			StartTime = DateTime.Now;
			LogFile = Path.Join("log", $"{StartTime.ToString("yyyyMMddHHmmss")}.log");

			DiscordClient.Ready += DiscordReady;
			DiscordClient.MessageCreated += DiscordMessageCreated;
			DiscordClient.GuildCreated += DiscordGuildCreated;
			DiscordClient.GuildAvailable += DiscordGuildAvailable;

			//? These are for debugging in the short-term.
			DiscordClient.ClientErrored += DiscordClientErrored;
			DiscordClient.SocketClosed += DiscordSocketClosed;
			DiscordClient.SocketErrored += DiscordSocketErrored;

			LoadCommands();

			await DiscordClient.ConnectAsync();

			await Task.Delay(-1);
		}

		/// <summary>
		/// Returns and instance of a command given the guild ID that it belongs to and the term used to invoke it.
		/// Returns null if the command could not be found.
		/// </summary>
		/// <param name="guildId"></param>
		/// <param name="commandTerm"></param>
		/// <returns></returns>
		public CommandBase GetCommand(object _, ulong guildId, string commandTerm) {
			if (GuildCommands.ContainsKey(guildId)) {
				if (GuildCommands[guildId].ContainsKey(commandTerm)) {
					return GuildCommands[guildId][commandTerm];
				}
			}
			return null;
		}

		public List<CommandBase> GetCommands(object _, ulong guildId) {
			if (GuildCommands.ContainsKey(guildId)) {
				return GuildCommands[guildId].Values.ToList();
			} else {
				return new List<CommandBase>();
			}
		}

		private void CreateDefaultConfig() {
			Config.WriteString(this, "BlendoBot", "Name", "YOUR BLENDOBOT NAME HERE");
			Config.WriteString(this, "BlendoBot", "Version", "YOUR BLENDOBOT VERSION HERE");
			Config.WriteString(this, "BlendoBot", "Description", "YOUR BLENDOBOT DESCRIPTION HERE");
			Config.WriteString(this, "BlendoBot", "Author", "YOUR BLENDOBOT AUTHOR HERE");
			Config.WriteString(this, "BlendoBot", "ActivityName", "YOUR BLENDOBOT ACTIVITY NAME HERE");
			Config.WriteString(this, "BlendoBot", "ActivityType", "Please replace this with Playing, ListeningTo, Streaming, or Watching.");
			Config.WriteString(this, "BlendoBot", "Token", "YOUR BLENDOBOT TOKEN HERE");
		}

		#region BotFunctions

		public async Task<DiscordMessage> SendMessage(object sender, SendMessageEventArgs e) {
			Log(sender, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Sending message {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})"
			});
			if (e.LogMessage.Length > 2000) {
				int oldLength = e.Message.Length;
				e.LogMessage = e.LogMessage.Substring(0, 2000);
				Log(sender, new LogEventArgs {
					Type = LogType.Warning,
					Message = $"Last message was {oldLength} characters long, truncated to 2000"
				});
			}
			return await e.Channel.SendMessageAsync(e.Message);
		}

		public async Task<DiscordMessage> SendFile(object sender, SendFileEventArgs e) {
			Log(sender, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Sending file {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})"
			});
			return await e.Channel.SendFileAsync(e.FilePath);
		}

		public async Task<DiscordMessage> SendException(object sender, SendExceptionEventArgs e) {
			Log(sender, new LogEventArgs {
				Type = LogType.Error,
				Message = $"{e.LogExceptionType}\n{e.Exception}"
			});
			string messageHeader = $"A {e.LogExceptionType} occurred. Alert the authorities!\n```\n";
			string messageFooter = "\n```";
			string exceptionString = e.Exception.ToString();
			if (exceptionString.Length + messageHeader.Length + messageFooter.Length > 2000) {
				int oldLength = exceptionString.Length;
				exceptionString = exceptionString.Substring(0, 2000 - messageHeader.Length - messageFooter.Length);
				Log(sender, new LogEventArgs {
					Type = LogType.Warning,
					Message = $"Last message was {oldLength} characters long, truncated to {exceptionString.Length}"
				});
			}
			return await e.Channel.SendMessageAsync(messageHeader + exceptionString + messageFooter);
		}

		public void Log(object sender, LogEventArgs e) {
			string typeString = Enum.GetName(typeof(LogType), e.Type);
			string logMessage = $"[{typeString}] ({DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}) | {e.Message}";
			Console.WriteLine(logMessage);
			if (!Directory.Exists("log")) Directory.CreateDirectory("log");
			File.AppendAllText(LogFile, logMessage + "\n");
		}
		public string ReadConfig(object o, string configHeader, string configKey) {
			return Config.ReadString(o, configHeader, configKey);
		}

		public bool DoesKeyExist(object o, string configHeader, string configKey) {
			return Config.DoesKeyExist(o, configHeader, configKey);
		}

		public void WriteConfig(object o, string configHeader, string configKey, string configValue) {
			Config.WriteString(o, configHeader, configKey, configValue);
		}

		public void AddMessageListener(object sender, ulong guildId, IMessageListener messageListener) {
			if (!GuildMessageListeners.ContainsKey(guildId)) {
				GuildMessageListeners.Add(guildId, new List<IMessageListener> { messageListener });
			} else {
				GuildMessageListeners[guildId].Add(messageListener);
			}
		}

		public void RemoveMessageListener(object sender, ulong guildId, IMessageListener messageListener) {
			if (GuildMessageListeners.ContainsKey(guildId)) {
				GuildMessageListeners[guildId].Remove(messageListener);
			}
			if (messageListener is IDisposable disposable) {
				disposable.Dispose();
			}
		}

		/// <summary>
		/// Returns and instance of a command given the guild ID given the type.
		/// Returns null if the command could not be found.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="guildId"></param>
		/// <returns></returns>
		public T GetCommand<T>(object sender, ulong guildId) where T : CommandBase {
			if (GuildCommands.ContainsKey(guildId)) {
				return GuildCommands[guildId].First(c => c.Value is T).Value as T;
			}
			return null;
		}

		public string GetCommandInstanceDataPath(object sender, CommandBase command) {
			if (!Directory.Exists(Path.Combine(Path.Combine("data", command.GuildId.ToString()), command.Name))) {
				Directory.CreateDirectory(Path.Combine(Path.Combine("data", command.GuildId.ToString()), command.Name));
			}
			return Path.Combine(Path.Combine("data", command.GuildId.ToString()), command.Name);
		}

		public string GetCommandCommonDataPath(object sender, CommandBase command) {
			if (!Directory.Exists(Path.Combine(Path.Combine("data", "common"), command.Name))) {
				Directory.CreateDirectory(Path.Combine(Path.Combine("data", "common"), command.Name));
			}
			return Path.Combine(Path.Combine("data", "common"), command.Name);
		}

		public async Task<bool> IsUserAdmin(object o, DiscordGuild guild, DiscordChannel channel, DiscordUser user) {
			return GetCommand<Admin>(this, guild.Id).IsUserAdmin(user) || (await guild.GetMemberAsync(user.Id)).PermissionsIn(channel).HasFlag(Permissions.Administrator);
		}

		public async Task<DiscordChannel> GetChannel(object o, ulong channelId) {
			return await DiscordClient.GetChannelAsync(channelId);
		}

		#endregion

		#region Discord Client Methods

		private async Task DiscordReady(ReadyEventArgs e) {
			if (Config.ActivityType.HasValue) {
				await DiscordClient.UpdateStatusAsync(new DiscordActivity(Config.ActivityName, Config.ActivityType.Value), UserStatus.Online, DateTime.Now);
			}
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"{Config.Name} ({Config.Version}) is connected to Discord!"
			});
		}

		private async Task DiscordMessageCreated(MessageCreateEventArgs e) {
			await Task.Delay(0);
			// The rule is: don't react to my own messages, and commands need to be triggered with a
			// ? character.
			if (!e.Author.IsCurrent && !e.Author.IsBot) {
				if (e.Message.Content.Length > 1 && e.Message.Content[0] == '?' && IsAlphabetical(e.Message.Content[1])) {
					string commandTerm = e.Message.Content.Split(' ')[0].ToLower();
					if (GuildCommands[e.Guild.Id].ContainsKey(commandTerm)) {
						try {
							await GuildCommands[e.Guild.Id][commandTerm].OnMessage(e);
						} catch (Exception exc) {
							// This should hopefully make it such that the bot never crashes (although it hasn't stopped it).
							await SendException(this, new SendExceptionEventArgs {
								Exception = exc,
								Channel = e.Channel,
								LogExceptionType = "GenericExceptionNotCaught"
							});
						}
					} else {
						await SendMessage(this, new SendMessageEventArgs {
							Message = $"I didn't know what you meant by that, {e.Author.Username}. Use {"?help".Code()} to see what I can do!",
							Channel = e.Channel,
							LogMessage = "UnknownMessage"
						});
					}
				}
				foreach (var listener in GuildMessageListeners[e.Guild.Id]) {
					await listener.OnMessage(e);
				}
			}
		}

		private async Task DiscordGuildCreated(GuildCreateEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Guild created: {e.Guild.Name} ({e.Guild.Id})"
			});

			await Task.Delay(0);
		}
		private async Task DiscordGuildAvailable(GuildCreateEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Guild available: {e.Guild.Name} ({e.Guild.Id})"
			});

			await InstantiateCommandsForGuild(e.Guild.Id);

			await Task.Delay(0);
		}

		private async Task DiscordClientErrored(ClientErrorEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"ClientErrored triggered: {e.Exception}"
			});

			await Task.Delay(0);
		}

		private async Task DiscordSocketClosed(SocketCloseEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"SocketClosed triggered: {e.CloseCode} - {e.CloseMessage}"
			});

			await Task.Delay(0);
		}

		private async Task DiscordSocketErrored(SocketErrorEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"SocketErrored triggered: {e.Exception}"
			});

			//HACK: This should try and reconnect should something wrong happen.
			await DiscordClient.ConnectAsync();

			await Task.Delay(0);
		}

		#endregion

		public async Task<bool> AddCommand(object _, ulong guildId, string commandClassName) {
			var commandType = LoadedCommands[commandClassName];
			try {
				var commandInstance = Activator.CreateInstance(commandType, new object[] { guildId, this }) as CommandBase;
				if (await commandInstance.Startup()) {
					GuildCommands[guildId].Add(commandInstance.Term, commandInstance);
					Log(this, new LogEventArgs {
						Type = LogType.Log,
						Message = $"Successfully loaded external module {commandInstance.Name} ({commandInstance.Term}) for guild {guildId}"
					});
					return true;
				} else {
					Log(this, new LogEventArgs {
						Type = LogType.Error,
						Message = $"Could not load module {commandInstance.Name} ({commandInstance.Term}), startup failed"
					});
				}
			} catch (Exception exc) {
				Log(this, new LogEventArgs {
					Type = LogType.Error,
					Message = $"Could not load module {commandType.FullName}, instantiation failed and exception thrown\n{exc}"
				});
			}
			return false;
		}

		public async Task RemoveCommand(object o, ulong guildId, string classTerm) {
			var command = GuildCommands[guildId][classTerm];
			int messageListenerCount = 0;
			foreach (var messageListener in GuildMessageListeners[guildId].Where(ml => ml.Command == command)) {
				RemoveMessageListener(o, guildId, messageListener);
				++messageListenerCount;
			}
			GuildCommands[guildId].Remove(classTerm);
			if (command is IDisposable disposable) {
				disposable.Dispose();
			}
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"Successfully unloaded module {command.GetType().FullName} and {messageListenerCount} message listener{(messageListenerCount == 1 ? "" : "s")}"
			});
			await Task.Delay(0);
		}

		private void LoadCommands() {
			foreach (var type in Assembly.GetCallingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(CommandBase)))) {
				SystemCommands.Add(type.FullName, type);
			}

			var dlls = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).ToList().FindAll(s => Path.GetExtension(s) == ".dll");
			dlls.RemoveAll(s => Path.GetFileName(s) == "BlendoBot.dll" || Path.GetFileName(s) == "BlendoBotLib.dll");

			foreach (string dll in dlls) {
				var assembly = Assembly.LoadFrom(dll);
				var types = assembly.ExportedTypes.ToList().FindAll(t => t.IsSubclassOf(typeof(CommandBase)));
				foreach (var type in types) {
					LoadedCommands.Add(type.FullName, type);
					Log(this, new LogEventArgs {
						Type = LogType.Log,
						Message = $"Detected command {type.FullName}"
					});
				}
			}
		}

		private async Task InstantiateCommandsForGuild(ulong guildId) {
			if (GuildCommands.ContainsKey(guildId)) {
				GuildCommands[guildId].Clear();
			} else {
				GuildCommands.Add(guildId, new Dictionary<string, CommandBase>());
			}
			if (GuildMessageListeners.ContainsKey(guildId)) {
				GuildMessageListeners[guildId].Clear();
			} else {
				GuildMessageListeners.Add(guildId, new List<IMessageListener>());
			}
			foreach (var commandType in SystemCommands.Values) {
				var commandInstance = Activator.CreateInstance(commandType, new object[] { guildId, this }) as CommandBase;
				GuildCommands[guildId].Add(commandInstance.Term, commandInstance);
				await commandInstance.Startup();
				Log(this, new LogEventArgs {
					Type = LogType.Log,
					Message = $"Successfully loaded internal module {commandInstance.Name} ({commandInstance.Term}) for guild {guildId}"
				});
			}

			var adminCommand = GetCommand<Admin>(this, guildId);
			foreach (var commandType in LoadedCommands.Values) {
				if (!adminCommand.IsCommandNameDisabled(commandType.FullName)) {
					try {
						var commandInstance = Activator.CreateInstance(commandType, new object[] { guildId, this }) as CommandBase;
						if (await commandInstance.Startup()) {
							GuildCommands[guildId].Add(commandInstance.Term, commandInstance);
							Log(this, new LogEventArgs {
								Type = LogType.Log,
								Message = $"Successfully loaded external module {commandInstance.Name} ({commandInstance.Term}) for guild {guildId}"
							});
						} else {
							Log(this, new LogEventArgs {
								Type = LogType.Error,
								Message = $"Could not load module {commandInstance.Name} ({commandInstance.Term}), startup failed"
							});
						}
					} catch (Exception exc) {
						Log(this, new LogEventArgs {
							Type = LogType.Error,
							Message = $"Could not load module {commandType.FullName}, instantiation failed and exception thrown\n{exc}"
						});
					}
				} else {
					Log(this, new LogEventArgs {
						Type = LogType.Log,
						Message = $"Module {commandType.FullName} is disabled and will not be instatiated"
					});
				}
			}

			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"All modules have finished loading for guild {guildId.ToString()}"
			});
		}
		private static bool IsAlphabetical(char c) {
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}
	}
}
