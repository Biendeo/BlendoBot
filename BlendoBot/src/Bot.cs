namespace BlendoBot
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BlendoBot.CommandDiscovery;
    using BlendoBot.ConfigSchemas;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Bot : BackgroundService
    {
        public Bot(
            BlendoBotConfig botConfig,
            ICommandRegistryBuilder registryBuilder,
            ICommandRouterFactory commandRouterFactory,
            IDiscordClient discordClientService,
            ILogger<Bot> logger,
            IServiceProvider serviceProvider
        )
        {
            this.botConfig = botConfig;
            this.discordClientService = discordClientService;
            this.logger = logger;

            // Build the command registry
            this.commandRegistry = registryBuilder.Build(
                serviceProvider,
                (ILogger<ICommandRegistryBuilder>)serviceProvider.GetService(typeof(ILogger<ICommandRegistryBuilder>)));

            // Command routers are tied to guilds. Build them on guild available event
            this.commandRouters = new Dictionary<ulong, ICommandRouter>();
            this.commandRouterFactory = commandRouterFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                // Register discord client event handlers
                this.discordClientService.Ready += DiscordReady;
                this.discordClientService.MessageCreated += DiscordMessageCreated;
                this.discordClientService.GuildCreated += DiscordGuildCreated;
                this.discordClientService.GuildAvailable += DiscordGuildAvailable;

                //? These are for debugging in the short-term.
                this.discordClientService.ClientErrored += DiscordClientErrored;
                this.discordClientService.SocketClosed += DiscordSocketClosed;
                this.discordClientService.SocketErrored += DiscordSocketErrored;

                // Start listening
                await this.discordClientService.ConnectAsync();

                // Wait on stopping token
                WaitHandle.WaitAny(new[] { stoppingToken.WaitHandle });
                this.logger.LogInformation("Service received stopping signal, bot is shutting down");
            }
        }

        #region Discord Client Methods

        private async Task DiscordReady(ReadyEventArgs e)
        {
            await this.discordClientService.UpdateStatusAsync(
                new DiscordActivity(this.botConfig.ActivityName, this.botConfig.ActivityType),
                UserStatus.Online,
                DateTimeOffset.UtcNow);
            this.logger.LogInformation(
                "{} ({}) is connected to Discord!",
                this.botConfig.Name,
                this.botConfig.Version
            );
        }

        private async Task DiscordMessageCreated(MessageCreateEventArgs e)
        {
            // Do not react to my own messages, not messages from other bots
            if (e.Author.IsCurrent || e.Author.IsBot)
            {
                return;
            }

            // TODO dynamic runtime listeners
            // Run these in a separate task?
            // foreach (var listener in something)
            // {
            // }

            // Commands need to be triggered with a '?' character, followed by a letter
            // i.e. no "??"
            if (e.Message.Content.Length > 1 && e.Message.Content[0] == '?' && char.IsLetter(e.Message.Content[1]))
            {
                string term = e.Message.Content.Split(' ')[0].ToLowerInvariant().Substring(1);

                // Get the router for the current guild.
                // Router is responsible for translating the term to a command
                if (!this.commandRouters.TryGetValue(e.Guild.Id, out var router))
                {
                    this.logger.LogError("Command router for guild {} not found", e.Guild.Id);
                    await this.discordClientService.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"Internal error, command router for guild id {e.Guild.Id} not found",
                        Channel = e.Channel,
                        LogMessage = "CommandRouterNotFound"
                    });
                    return;
                }

                // Attempt to map the term to a command
                if (router!.TryTranslateTerm(term, out Type commandType))
                {
                    await this.commandRegistry.ExecuteForAsync(
                        commandType,
                        e,
                        onException: ex =>
                            this.discordClientService.SendException(this, new SendExceptionEventArgs
                            {
                                Exception = ex,
                                Channel = e.Channel,
                                LogExceptionType = "GenericExceptionNotCaught"
                            })
                    );
                }
                else
                {
                    await this.discordClientService.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"I didn't know what you meant by that, {e.Author.Username}. Use {"?help".Code()} to see what I can do!",
                        Channel = e.Channel,
                        LogMessage = "UnknownMessage"
                    });
                };
            }
        }

        private async Task DiscordGuildCreated(GuildCreateEventArgs e)
        {
            this.logger.LogInformation($"Guild created: {e.Guild.Name} ({e.Guild.Id})");
            await Task.CompletedTask;
        }

        private async Task DiscordGuildAvailable(GuildCreateEventArgs e)
        {
            this.logger.LogInformation($"Guild available: {e.Guild.Name} ({e.Guild.Id})");

            // Create a command router for the newly available guild
            var guildId = e.Guild.Id;
            var router = await this.commandRouterFactory.CreateForGuild(guildId, this.commandRegistry.RegisteredCommandTypes);
            this.commandRouters.Add(guildId, router);
            await Task.CompletedTask;
        }

        private async Task DiscordClientErrored(ClientErrorEventArgs e)
        {
            this.logger.LogError(e.Exception, "ClientErrored triggered");
            await Task.CompletedTask;
        }

        private async Task DiscordSocketClosed(SocketCloseEventArgs e)
        {
            this.logger.LogError($"SocketClosed triggered: {e.CloseCode} - {e.CloseMessage}");
            await Task.CompletedTask;
        }

        private async Task DiscordSocketErrored(SocketErrorEventArgs e)
        {
            this.logger.LogError(e.Exception, "SocketErrored triggered");

            // HACK: This should try and reconnect should something wrong happen.
            await this.discordClientService.ConnectAsync();
        }

        #endregion

        private BlendoBotConfig botConfig;

        private ICommandRegistry commandRegistry;

        private Dictionary<ulong, ICommandRouter> commandRouters;

        private ICommandRouterFactory commandRouterFactory;

        private IDiscordClient discordClientService;

        private ILogger<Bot> logger;
    }
}
