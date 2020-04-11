namespace BlendoBot.Commands.Admin
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BlendoBot.CommandDiscovery;
    using BlendoBotLib;
    using BlendoBotLib.DataStore;
    using BlendoBotLib.Interfaces;
    using DSharpPlus;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    [PrivilegedCommand]
    [CommandDefaults(defaultTerm: "admin", enabled: true)]
    internal class AdminV3 : ICommand
    {
        public string Name => "Admin";

        public string Description => "Does admin stuff, but only if you are either an administrator of the server, or if you've been granted permission!";

        public string Author => "mozzarella";

        public string Version => "3.0";

        public AdminV3(
            Guild guild,
            ICommandRegistry commandRegistry,
            ICommandRouter commandRouter,
            IDiscordClient discordClient,
            IInstancedDataStore<AdminV3> dataStore,
            ILogger<AdminV3> logger,
            ILoggerFactory loggerFactory)
        {
            this.guildId = guild.Id;

            this.dataStore = dataStore;
            this.discordClient = discordClient;
            this.logger = logger;
            this.membership = new Membership(
                guild.Id,
                this.discordClient,
                loggerFactory.CreateLogger<Membership>(),
                dataStore);
            this.commandManagement = new CommandManagement(
                commandRouter,
                loggerFactory.CreateLogger<CommandManagement>());
        }

        public static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            services.AddSingleton<
                IDataStore<AdminV3>,
                JsonFileDataStore<AdminV3>>();
            services.AddSingleton<
                IInstancedDataStore<AdminV3>,
                GuildInstancedDataStore<AdminV3>>();
        }

        public string GetUsage(string term) => $"Usage:\n" +
            $"({"All of these commands are only accessible if you are either an administrator role on this Discord guild, or if you have been added to this admin list!".Italics()})\n" +
            $"{$"{term} user add @person".Code()} ({"Adds a new person to be a BlendoBot administrator".Italics()})\n" +
            $"{$"{term} user remove @person".Code()} ({"Removes a person from being a BlendoBot administrator".Italics()})\n" +
            $"{$"{term} user list".Code()} ({"Lists all current BlendoBot admins".Italics()})\n" +
            $"{$"{term} command enable [command term]".Code()} ({"Enables a command currently disabled by BlendoBot".Italics()})\n" +
            $"{$"{term} command disable [command term]".Code()} ({"Disables a command currently enabled by BlendoBot".Italics()})\n" +
            $"{$"{term} command disabled".Code()} ({$"Lists all currently disabled commands from BlendoBot".Italics()})\n" +
            $"{$"{term} command rename [command term] [new term]".Code()} ({"Renames a command to use the new term (must be unique!)".Italics()})\n" +
            $"{$"{term} command unknownprefix".Code()} ({"Lists the current prefix used for the unknown command message".Italics()})\n" +
            $"{$"{term} command unknownprefix [prefix]".Code()} ({"Changes the prefix used for the unkown command message".Italics()})\n" +
            $"{$"{term} command unknowntoggle".Code()} ({"Toggles whether the unknown command message appears".Italics()})";

        public async Task OnMessage(MessageCreateEventArgs e)
        {
            if (!e.TryGetTerm(out var term))
            {
                term = "Admin";
            }

            if (!this.membership.IsAdmin(e.Author) && !(await e.Guild.GetMemberAsync(e.Author.Id)).PermissionsIn(e.Channel).HasFlag(Permissions.Administrator))
            {
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = $"Only administrators can use {$"{term}".Code()}!",
                    Channel = e.Channel,
                    LogMessage = "AdminNotAuthorised"
                });
                return;
            }

            var split = e.Message.Content.Split(' ');
            var module = split.ElementAtOrDefault(1);
            if (string.Equals(module, "user", StringComparison.OrdinalIgnoreCase))
            {
                // Membership module
                var verb = split.ElementAtOrDefault(2);
                if (string.Equals(verb, "add", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.MentionedUsers.Count != 1)
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Please mention only one user when using {$"{term} user add".Code()}.",
                            Channel = e.Channel,
                            LogMessage = "AdminUserAddIncorrectCount"
                        });
                    }
                    else
                    {
                        if (await this.membership.AddAdmin(e.MentionedUsers[0]))
                        {
                            await this.discordClient.SendMessage(this, new SendMessageEventArgs
                            {
                                Message = $"Successfully added {e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} as a BlendoBot admin!",
                                Channel = e.Channel,
                                LogMessage = "AdminUserAddSuccess"
                            });
                        }
                        else
                        {
                            await this.discordClient.SendMessage(this, new SendMessageEventArgs
                            {
                                Message = $"{e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} is already an admin!",
                                Channel = e.Channel,
                                LogMessage = "AdminUserAddFailure"
                            });
                        }
                    }
                    return;
                }
                else if (string.Equals(verb, "remove", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.MentionedUsers.Count != 1)
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Please mention only one user when using {$"{term} user remove".Code()}.",
                            Channel = e.Channel,
                            LogMessage = "AdminUserRemoveIncorrectCount"
                        });
                    }
                    else
                    {
                        if (await this.membership.RemoveAdmin(e.MentionedUsers[0]))
                        {
                            await this.discordClient.SendMessage(this, new SendMessageEventArgs
                            {
                                Message = $"Successfully removed {e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} as a BlendoBot admin!",
                                Channel = e.Channel,
                                LogMessage = "AdminUserRemoveSuccess"
                            });
                        }
                        else
                        {
                            await this.discordClient.SendMessage(this, new SendMessageEventArgs
                            {
                                Message = $"{e.MentionedUsers[0].Username} #{e.MentionedUsers[0].Discriminator.ToString().PadLeft(4, '0')} is already not an admin!",
                                Channel = e.Channel,
                                LogMessage = "AdminUserRemoveFailure"
                            });
                        }
                    }
                    return;
                }
                else if (string.Equals(verb, "list", StringComparison.OrdinalIgnoreCase))
                {
                    var sb = new StringBuilder();

                    var administrators = await this.membership.GetAdmins();
                    if (administrators.Count > 0)
                    {
                        sb.AppendLine("Current BlendoBot administrators:");
                        sb.AppendLine($"{"All current guild administrators plus".Italics()}");
                        foreach (var user in administrators)
                        {
                            sb.AppendLine($"{user.Username} #{user.Discriminator.ToString().PadLeft(4, '0')}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"No BlendoBot administrators have been assigned. If you are a guild administrator and want someone else to administer BlendoBot, please use {$"{term} user add".Code()}.");
                    }

                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = sb.ToString(),
                        Channel = e.Channel,
                        LogMessage = "AdminUserList"
                    });
                }
                else
                {
                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"I couldn't determine what you wanted. Make sure your command is handled by {$"?help admin".Code()}",
                        Channel = e.Channel,
                        LogMessage = "AdminUnknownCommand"
                    });
                }
            }
            else if (string.Equals(module, "command", StringComparison.OrdinalIgnoreCase))
            {
                // Command management module
                var verb = split.ElementAtOrDefault(2);
                if (string.Equals(verb, "enable", StringComparison.OrdinalIgnoreCase) && split.Length >= 4)
                {
                    var commandTerm = split.ElementAtOrDefault(3);
                    if (await this.commandManagement.EnableCommand(commandTerm))
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Command {commandTerm.Code()} has been enabled!",
                            Channel = e.Channel,
                            LogMessage = "AdminCommandEnableSuccess"
                        });
                    }
                    else
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Command {commandTerm.Code()} couldn't be enabled for some reason, please check the logs!",
                            Channel = e.Channel,
                            LogMessage = "AdminCommandEnableFailure"
                        });
                    }
                }
                else if (string.Equals(verb, "disable", StringComparison.OrdinalIgnoreCase) && split.Length >= 4)
                {
                    var commandTerm = split.ElementAtOrDefault(3);
                    if (await this.commandManagement.DisableCommand(commandTerm))
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Command {commandTerm.Code()} has been disabled!",
                            Channel = e.Channel,
                            LogMessage = "AdminCommandDisableSuccess"
                        });
                    }
                    else
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Command {commandTerm.Code()} couldn't be disabled for some reason, please check the logs!",
                            Channel = e.Channel,
                            LogMessage = "AdminCommandDisableFailure"
                        });
                    }
                }
                else if (string.Equals(verb, "rename", StringComparison.OrdinalIgnoreCase) && split.Length >= 5)
                {
                    var commandTermFrom = split.ElementAtOrDefault(3);
                    var commandTermTo = split.ElementAtOrDefault(4);
                    var res = await this.commandManagement.Rename(commandTermFrom.ToLowerInvariant(), commandTermTo.ToLowerInvariant());
                    if (!string.IsNullOrWhiteSpace(res))
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Command {commandTermFrom.Code()} has been renamed to {res.Code()}!",
                            Channel = e.Channel,
                            LogMessage = "AdminCommandRenameSuccess"
                        });
                    }
                    else
                    {
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Command {commandTermFrom.Code()} couldn't be renamed for some reason, please check the logs!",
                            Channel = e.Channel,
                            LogMessage = "AdminCommandRenameFailure"
                        });
                    }
                }
                else if (string.Equals(verb, "disabled", StringComparison.OrdinalIgnoreCase))
                {
                    var sb = new StringBuilder();
                    var disabledCommandTerms = this.commandManagement.GetDisabledCommands();

                    if (disabledCommandTerms.Count == 0)
                    {
                        sb.AppendLine("No BlendoBot commands have been disabled.");
                    }
                    else
                    {
                        sb.AppendLine("Currently disabled commands:");
                        foreach (var disabledTerm in disabledCommandTerms)
                        {
                            sb.AppendLine(disabledTerm.Code());
                        }
                    }

                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = sb.ToString(),
                        Channel = e.Channel,
                        LogMessage = "AdminCommandList"
                    });
                }
                else
                {
                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"I couldn't determine what you wanted. Make sure your command is handled by {$"?help admin".Code()}",
                        Channel = e.Channel,
                        LogMessage = "AdminUnknownCommand"
                    });
                }
            }
            else
            {
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = $"I couldn't determine what you wanted. Make sure your command is handled by {"?help admin".Code()}",
                    Channel = e.Channel,
                    LogMessage = "AdminUnknownCommand"
                });
            }
        }

        private ulong guildId;

        private ILogger<AdminV3> logger;

        private Membership membership;

        private CommandManagement commandManagement;

        private IInstancedDataStore<AdminV3> dataStore;

        private IDiscordClient discordClient;
    }
}
