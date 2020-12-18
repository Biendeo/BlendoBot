using BlendoBot.Commands;
using BlendoBot.Commands.Admin;
using BlendoBotLib;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace BlendoBot {
	public class Program : IBotMethods {
		private DiscordClient DiscordClient { get; set; }
		private string ConfigPath { get; }
		public Config Config { get; private set; }
		private string LogFile { get; set; }
		public DateTime StartTime { get; private set; }

		private Dictionary<string, Type> LoadedCommands { get; set; }
		private Dictionary<ulong, Dictionary<string, CommandBase>> GuildCommands { get; set; }
		private Dictionary<ulong, List<IMessageListener>> GuildMessageListeners { get; set; }
		private Dictionary<ulong, Dictionary<ulong, List<IReactionListener>>> MessageReactionListeners { get; set; }

		private Timer HeartbeatCheck { get; set; }

		private int ClientRestarts { get; set; }

		public static void Main(string[] args) {
			var program = new Program("config.cfg");
			program.Start(args).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public Program(string configPath) {
			ConfigPath = configPath;
			LoadedCommands = new Dictionary<string, Type>();
			GuildCommands = new Dictionary<ulong, Dictionary<string, CommandBase>>();
			GuildMessageListeners = new Dictionary<ulong, List<IMessageListener>>();
			MessageReactionListeners = new Dictionary<ulong, Dictionary<ulong, List<IReactionListener>>>();
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

			await SetupDiscordClient();

			LoadCommands();

			HeartbeatCheck = new Timer(120000.0);
			HeartbeatCheck.Elapsed += HeartbeatCheck_Elapsed;
			HeartbeatCheck.AutoReset = true;
			HeartbeatCheck.Start();
			ClientRestarts = 0;

			await Task.Delay(-1);
		}

		private async Task SetupDiscordClient() {
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
			DiscordClient.MessageReactionAdded += DiscordReactionAdded;
			DiscordClient.GuildCreated += DiscordGuildCreated;
			DiscordClient.GuildAvailable += DiscordGuildAvailable;

			//? These are for debugging in the short-term.
			DiscordClient.ClientErrored += DiscordClientErrored;
			DiscordClient.SocketClosed += DiscordSocketClosed;
			DiscordClient.SocketErrored += DiscordSocketErrored;

			DiscordClient.Heartbeated += DiscordHeartbeated;

			await DiscordClient.ConnectAsync();
		}

		private void HeartbeatCheck_Elapsed(object sender, ElapsedEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"Heartbeat didn't occur for 120 seconds, re-connecting..."
			});
			DiscordClient.Dispose();
			SetupDiscordClient().RunSynchronously();
		}

		/// <summary>
		/// Returns and instance of a command given the guild ID that it belongs to and the term used to invoke it.
		/// Returns null if the command could not be found.
		/// If the command cannot be found, the unknown command prefix is applied and then checked again.
		/// E.g. if "help" is the commandTerm, and "?" is the unknown command prefix, "?help" will also be looked for.
		/// This is specifically only if "help" is not already an existing command though.
		/// </summary>
		/// <param name="guildId"></param>
		/// <param name="commandTerm"></param>
		/// <returns></returns>
		public CommandBase GetCommand(object _, ulong guildId, string commandTerm) {
			if (GuildCommands.ContainsKey(guildId)) {
				if (GuildCommands[guildId].ContainsKey(commandTerm)) {
					return GuildCommands[guildId][commandTerm];
				} else {
					var adminCommand = GetCommand<Admin>(this, guildId);
					if (GuildCommands[guildId].ContainsKey($"{adminCommand.UnknownCommandPrefix}{commandTerm}")) {
						return GuildCommands[guildId][$"{adminCommand.UnknownCommandPrefix}{commandTerm}"];
					}
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
			string logMessage = $"[{typeString}] ({DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}) [{sender?.GetType().FullName ?? "null"}] | {e.Message}";
			Console.WriteLine(logMessage);
			if (!Directory.Exists("log")) Directory.CreateDirectory("log");
			using (var logStream = File.Open(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) {
				using var writer = new StreamWriter(logStream);
				writer.WriteLine(logMessage);
			}
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

		public void AddReactionListener(object sender, ulong guildId, ulong messageId, IReactionListener reactionListener) {
			if (!MessageReactionListeners.ContainsKey(guildId)) {
				MessageReactionListeners.Add(guildId, new Dictionary<ulong, List<IReactionListener>>());
			}
			if (!MessageReactionListeners[guildId].ContainsKey(messageId)) {
				MessageReactionListeners[guildId].Add(messageId, new List<IReactionListener> { reactionListener });
			} else {
				MessageReactionListeners[guildId][messageId].Add(reactionListener);
			}
		}

		public void RemoveReactionListener(object sender, ulong guildId, ulong messageId, IReactionListener reactionListener) {
			if (MessageReactionListeners.ContainsKey(guildId) && MessageReactionListeners[guildId].ContainsKey(messageId)) {
				MessageReactionListeners[guildId][messageId].Remove(reactionListener);
				if (MessageReactionListeners[guildId][messageId].Count == 0) {
					MessageReactionListeners[guildId].Remove(messageId);
				}
				if (MessageReactionListeners[guildId].Count == 0) {
					MessageReactionListeners.Remove(guildId);
				}
			}
			if (reactionListener is IDisposable disposable) {
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
				return GuildCommands[guildId].FirstOrDefault(c => c.Value is T).Value as T;
			}
			return null;
		}

		public string GetHelpCommandTerm(object o, ulong guildId) {
			return GetCommand<Help>(this, guildId).Term;
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

		public async Task<DiscordUser> GetUser(object o, ulong userId) {
			return await DiscordClient.GetUserAsync(userId);
		}

		#endregion

		#region Discord Client Methods

		private async Task DiscordReady(DiscordClient sender, ReadyEventArgs e) {
			if (Config.ActivityType.HasValue) {
				await DiscordClient.UpdateStatusAsync(new DiscordActivity(Config.ActivityName, Config.ActivityType.Value), UserStatus.Online, DateTime.Now);
			}
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"{Config.Name} ({Config.Version}) is connected to Discord!"
			});
		}

		private async Task DiscordMessageCreated(DiscordClient sender, MessageCreateEventArgs e) {
			await Task.Delay(0);
			// The rule is: don't react to my own messages, and commands need to be triggered with a
			// ? character.
			if (!e.Author.IsCurrent && !e.Author.IsBot) {
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
					var adminCommand = GetCommand<Admin>(this, e.Guild.Id);
					if (adminCommand.IsUnknownCommandEnabled && commandTerm.StartsWith(adminCommand.UnknownCommandPrefix)) {
						await SendMessage(this, new SendMessageEventArgs {
							Message = $"I didn't know what you meant by that, {e.Author.Username}. Use {"?help".Code()} to see what I can do!",
							Channel = e.Channel,
							LogMessage = "UnknownMessage"
						});
					}
				}
				foreach (var listener in GuildMessageListeners[e.Guild.Id]) {
					try {
						await listener.OnMessage(e);
					} catch (Exception exc) {
						await SendException(this, new SendExceptionEventArgs {
							Exception = exc,
							Channel = e.Channel,
							LogExceptionType = "GenericExceptionNotCaught"
						});
					}
				}
			}
		}

		private async Task DiscordReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e) {
			await Task.Delay(0);
			if (!e.User.IsCurrent && !e.User.IsBot) {
				if (MessageReactionListeners.TryGetValue(e.Guild.Id, out var guildMessageReactionListeners)) {
					if (guildMessageReactionListeners.TryGetValue(e.Message.Id, out var reactionListeners)) {
						foreach (var listener in reactionListeners) {
							try {
								await listener.OnReactionAdd(e);
							} catch (Exception exc) {
								await SendException(this, new SendExceptionEventArgs {
									Exception = exc,
									Channel = e.Channel,
									LogExceptionType = "GenericExceptionNotCaught"
								});
							}
						}
					}
				}
			}
		}

		private async Task DiscordGuildCreated(DiscordClient sender, GuildCreateEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Guild created: {e.Guild.Name} ({e.Guild.Id})"
			});

			await Task.Delay(0);
		}
		private async Task DiscordGuildAvailable(DiscordClient sender, GuildCreateEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Guild available: {e.Guild.Name} ({e.Guild.Id})"
			});

			await InstantiateCommandsForGuild(e.Guild.Id);

			await Task.Delay(0);
		}

		private async Task DiscordClientErrored(DiscordClient sender, ClientErrorEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"ClientErrored triggered: {e.Exception}"
			});

			await Task.Delay(0);
		}

		private async Task DiscordSocketClosed(DiscordClient sender, SocketCloseEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"SocketClosed triggered: {e.CloseCode} - {e.CloseMessage}"
			});

			await Task.Delay(0);
		}
		private async Task DiscordSocketErrored(DiscordClient sender, SocketErrorEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Error,
				Message = $"SocketErrored triggered: {e.Exception}"
			});

			//HACK: This should try and reconnect should something wrong happen.
			await DiscordClient.ReconnectAsync();

			await Task.Delay(0);
		}

		private async Task DiscordHeartbeated(DiscordClient sender, HeartbeatEventArgs e) {
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Heartbeat triggered: handled = {e.Handled}, ping = {e.Ping}, timestamp = {e.Timestamp}"
			});
			HeartbeatCheck?.Stop();
			HeartbeatCheck?.Start();

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
			int reactionListenerCount = 0;
			if (GuildMessageListeners.ContainsKey(guildId)) {
				foreach (var messageListener in GuildMessageListeners[guildId].Where(ml => ml.Command == command).ToList()) {
					RemoveMessageListener(o, guildId, messageListener);
					++messageListenerCount;
				}
			}
			if (MessageReactionListeners.ContainsKey(guildId)) {
				foreach (var messageId in MessageReactionListeners[guildId].Keys.ToList()) {
					foreach (var reactionListener in MessageReactionListeners[guildId][messageId].Where(rl => rl.Command == command).ToList()) {
						RemoveReactionListener(o, guildId, messageId, reactionListener);
						++reactionListenerCount;
					}
				}
			}
			GuildCommands[guildId].Remove(classTerm);
			if (command is IDisposable disposable) {
				disposable.Dispose();
			}
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Successfully unloaded module {command.GetType().FullName}, {messageListenerCount} message listener{(messageListenerCount == 1 ? string.Empty : "s")}, and {reactionListenerCount} reaction listener{(reactionListenerCount == 1 ? string.Empty : "s")}"
			});
			await Task.Delay(0);
		}

		public void RenameCommand(object o, ulong guildId, string commandTerm, string newTerm) {
			var command = GuildCommands[guildId][commandTerm];
			GuildCommands[guildId].Remove(commandTerm);
			command.Term = newTerm;
			GuildCommands[guildId].Add(newTerm, command);
			Log(this, new LogEventArgs {
				Type = LogType.Log,
				Message = $"Successfully renamed module {command.GetType().FullName} from {commandTerm} to {newTerm}"
			});
		}

		private void LoadCommands() {
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
				return;
			} else {
				GuildCommands.Add(guildId, new Dictionary<string, CommandBase>());
			}
			if (GuildMessageListeners.ContainsKey(guildId)) {
				GuildMessageListeners[guildId].Clear();
			} else {
				GuildMessageListeners.Add(guildId, new List<IMessageListener>());
			}

			var adminCommand = new Admin(guildId, this);
			var systemCommands = new CommandBase[] { adminCommand, new Help(guildId, this), new About(guildId, this) };

			foreach (var command in systemCommands) {
				await command.Startup();
			}

			foreach (var command in systemCommands) {
				string term = adminCommand.RenameCommandTermFromDatabase(command);
				GuildCommands[guildId].Add(term, command);
				Log(this, new LogEventArgs {
					Type = LogType.Log,
					Message = $"Successfully loaded internal module {command.Name} ({term}) for guild {guildId}"
				});
			}

			foreach (var commandType in LoadedCommands.Values) {
				if (!adminCommand.IsCommandNameDisabled(commandType.FullName)) {
					try {
						var commandInstance = Activator.CreateInstance(commandType, new object[] { guildId, this }) as CommandBase;
						if (await commandInstance.Startup()) {
							commandInstance.Term = adminCommand.RenameCommandTermFromDatabase(commandInstance);
							GuildCommands[guildId].Add(commandInstance.Term, commandInstance);
							Log(this, new LogEventArgs {
								Type = LogType.Log,
								Message = $"Successfully loaded external module {commandInstance.Name} ({commandInstance.Term}) for guild {guildId}"
							});
						} else {
							Log(this, new LogEventArgs {
								Type = LogType.Error,
								Message = $"Could not load module {commandInstance.Name}, startup failed"
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
