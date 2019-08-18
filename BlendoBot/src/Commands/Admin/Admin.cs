using BlendoBotLib;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public class Admin : CommandBase {
		public Admin(ulong guildId, Program program) : base(guildId, program) {
			this.program = program;
			disabledCommands = new List<DisabledCommand>();
			administrators = new List<DiscordUser>();
		}

		public override string Term => "?admin";
		public override string Name => "Admin";
		public override string Description => "Does admin stuff, but only if you are either an administrator of the server, or if you've been granted permission!";
		public override string Usage => $"Usage:\n({"All of these commands are only accessible if you are either an administrator role on this Discord guild, or if you have been added to this admin list!".Italics()})\n{"?admin user add @person".Code()} ({"Adds a new person to be a BlendoBot administrator".Italics()})\n{"?admin user remove @person".Code()} ({"Removes a person from being a BlendoBot administrator".Italics()})\n{"?admin user list".Code()} ({"Lists all current BlendoBot admins".Italics()})\n{"?admin command enable [command term]".Code()} ({"Enables a command currently disabled by BlendoBot".Italics()})\n{"?admin command disable [command term]".Code()} ({"Disables a command currently enabled by BlendoBot".Italics()})\n{"?admin command list".Code()} ({$"Lists all currently disabled commands from BlendoBot (all enabled commands are in {"?help".Code()})".Italics()})";
		public override string Author => "Biendeo";
		public override string Version => "2.0.0";

		private Program program;

		private List<DisabledCommand> disabledCommands;
		private List<DiscordUser> administrators;

		public override async Task<bool> Startup() {
			LoadData();
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			if (!await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Only administrators can use {"?admin".Code()}!",
					Channel = e.Channel,
					LogMessage = "AdminNotAuthorised"
				});
				return;
			}
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length > 2 && splitString[1] == "user") {
				if (splitString[2] == "add") {
					if (e.MentionedUsers.Count != 1) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Please mention only one user when using {"?admin user add".Code()}.",
							Channel = e.Channel,
							LogMessage = "AdminUserRemoveIncorrectCount"
						});
					} else {
						if (AddAdmin(e.MentionedUsers[0])) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Successfully added {e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} as a BlendoBot admin!",
								Channel = e.Channel,
								LogMessage = "AdminUserAddSuccess"
							});
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"{e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} is already an admin!",
								Channel = e.Channel,
								LogMessage = "AdminUserAddFailure"
							});
						}
					}
				} else if (splitString[2] == "remove") {
					if (e.MentionedUsers.Count != 1) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Please mention only one user when using {"?admin user remove".Code()}.",
							Channel = e.Channel,
							LogMessage = "AdminUserRemoveIncorrectCount"
						});
					} else {
						if (RemoveAdmin(e.MentionedUsers[0])) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Successfully removed {e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} as a BlendoBot admin!",
								Channel = e.Channel,
								LogMessage = "AdminUserRemoveSuccess"
							});
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"{e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} is already not an admin!",
								Channel = e.Channel,
								LogMessage = "AdminUserRemoveFailure"
							});
						}
					}
				} else if (splitString[2] == "list") {
					var sb = new StringBuilder();

					if (administrators.Count > 0) {
						sb.AppendLine("Current BlendoBot administrators:");
						sb.AppendLine($"{"All current guild administrators plus".Italics()}");
						foreach (var user in administrators) {
							sb.AppendLine($"{user.Username} #{user.Discriminator.ToString().PadLeft(4, '0')}");
						}
					} else {
						sb.AppendLine($"No BlendoBot administrators have been assigned. If you are a guild administrator and want someone else to administer BlendoBot, please use {"?admin user add".Code()}.");
					}

					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "AdminUserList"
					});
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help admin".Code()}",
						Channel = e.Channel,
						LogMessage = "AdminUnknownCommand"
					});
				}
			} else if (splitString.Length > 2 && splitString[1] == "command") {
				if (splitString.Length > 3 && splitString[2] == "enable") {
					string commandTerm = splitString[3].ToLower();
					if (commandTerm[0] != '?') {
						commandTerm = $"?{commandTerm}";
					}

					var disabledCommand = disabledCommands.Find(dc => dc.Term == commandTerm);
					if (disabledCommand != null) {
						if (await program.AddCommand(this, GuildId, disabledCommand.ClassName)) {
							disabledCommands.Remove(disabledCommand);
							SaveData();
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Command {commandTerm.Code()} has been enabled!",
								Channel = e.Channel,
								LogMessage = "AdminCommandEnableSuccess"
							});
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Command {commandTerm.Code()} couldn't be re-enabled for some reason, please check the logs!",
								Channel = e.Channel,
								LogMessage = "AdminCommandEnableFailure"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Command {commandTerm.Code()} not found, or is enabled already!",
							Channel = e.Channel,
							LogMessage = "AdminCommandEnableNotFound"
						});
					}
				} else if (splitString.Length > 3 && splitString[2] == "disable") {
					string commandTerm = splitString[3].ToLower();
					if (commandTerm[0] != '?') {
						commandTerm = $"?{commandTerm}";
					}

					var disabledCommand = program.GetCommand(this, GuildId, commandTerm);
					if (disabledCommand != null) {
						if (disabledCommand is Admin || disabledCommand is Help || disabledCommand is About) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Nice try but you can't disable {disabledCommand.Term.Code()}",
								Channel = e.Channel,
								LogMessage = "AdminCommandDisableProhibited"
							});
						} else {
							await program.RemoveCommand(this, GuildId, commandTerm);
							disabledCommands.Add(new DisabledCommand(disabledCommand.Term, disabledCommand.GetType().FullName));
							SaveData();
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Command {commandTerm.Code()} has been disabled!",
								Channel = e.Channel,
								LogMessage = "AdminCommandDisableSuccess"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Command {commandTerm.Code()} not found, or is already disabled!",
							Channel = e.Channel,
							LogMessage = "AdminCommandDisableNotFound"
						});
					}
				} else if (splitString[2] == "list") {
					var sb = new StringBuilder();

					if (disabledCommands.Count > 0) {
						sb.AppendLine("Current disabled commands:");
						foreach (var command in disabledCommands) {
							sb.AppendLine($"{command.Term.Code()}");
						}
					} else {
						sb.AppendLine($"No BlendoBot commands have been disabled.");
					}

					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "AdminCommandList"
					});
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help admin".Code()}",
						Channel = e.Channel,
						LogMessage = "AdminUnknownCommand"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help admin".Code()}",
					Channel = e.Channel,
					LogMessage = "AdminUnknownCommand"
				});
			}
		}

		private void LoadData() {
			if (File.Exists(Path.Combine(BotMethods.GetCommandDataPath(this, this), "disabled-commands.json"))) {
				disabledCommands = JsonConvert.DeserializeObject<List<DisabledCommand>>(File.ReadAllText(Path.Combine(BotMethods.GetCommandDataPath(this, this), "disabled-commands.json")));
			}
			if (File.Exists(Path.Combine(BotMethods.GetCommandDataPath(this, this), "administrators.json"))) {
				administrators = JsonConvert.DeserializeObject<List<DiscordUser>>(File.ReadAllText(Path.Combine(BotMethods.GetCommandDataPath(this, this), "administrators.json")));
			}
		}

		private void SaveData() {
			File.WriteAllText(Path.Combine(BotMethods.GetCommandDataPath(this, this), "disabled-commands.json"), JsonConvert.SerializeObject(disabledCommands));
			File.WriteAllText(Path.Combine(BotMethods.GetCommandDataPath(this, this), "administrators.json"), JsonConvert.SerializeObject(administrators));
		}

		public bool IsUserAdmin(DiscordUser user) {
			return administrators.Contains(user);
		}

		public bool AddAdmin(DiscordUser user) {
			if (!IsUserAdmin(user)) {
				administrators.Add(user);
				SaveData();
				return true;
			} else {
				return false;
			}
		}

		public bool RemoveAdmin(DiscordUser user) {
			if (IsUserAdmin(user)) {
				administrators.Remove(user);
				SaveData();
				return true;
			} else {
				return false;
			}
		}

		public bool IsCommandTermDisabled(string term) {
			return disabledCommands.Exists(dc => dc.Term == term);
		}

		public bool IsCommandNameDisabled(string name) {
			return disabledCommands.Exists(dc => dc.ClassName == name);
		}

		public bool DisableCommand(string term) {
			if (!IsCommandTermDisabled(term)) {
				//TODO: Get the command class name.
				//TODO: Remove the command.
				SaveData();
				return true;
			} else {
				return false;
			}
		}

		public bool EnableCommandTerm(string term) {
			if (IsCommandTermDisabled(term)) {
				disabledCommands.RemoveAll(dc => dc.Term == term);
				//TODO: Instantiate the command again.
				SaveData();
				return true;
			} else {
				return false;
			}
		}
	}
}
