namespace BlendoBot
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BlendoBot.ConfigSchemas;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Bot : BackgroundService
    {
        public Bot(
            BlendoBotConfig botConfig,
            CommandRegistryBuilder registryBuilder,
            IDiscordClientService discordClientService,
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
                (ILogger<CommandRegistryBuilder>)serviceProvider.GetService(typeof(ILogger<CommandRegistryBuilder>)));
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
            // The rule is: don't react to my own messages, and commands need to be triggered with a
            // ? character.
            if (!e.Author.IsCurrent && !e.Author.IsBot)
            {
                if (e.Message.Content.Length > 1 && e.Message.Content[0] == '?' && char.IsLetter(e.Message.Content[1]))
                {
                    await this.commandRegistry.ExecuteForAsync(
                        e,
                        onUnmatchedCommand: () =>
                            this.discordClientService.SendMessage(this, new SendMessageEventArgs
                            {
                                Message = $"I didn't know what you meant by that, {e.Author.Username}. Use {"?help".Code()} to see what I can do!",
                                Channel = e.Channel,
                                LogMessage = "UnknownMessage"
                            }),
                        // This should hopefully make it such that the bot never crashes (although it hasn't stopped it).
                        onException: ex =>
                            this.discordClientService.SendException(this, new SendExceptionEventArgs
                            {
                                Exception = ex,
                                Channel = e.Channel,
                                LogExceptionType = "GenericExceptionNotCaught"
                            })
                    );
                }

                // TODO dynamic runtime listeners
                // foreach (var listener in GuildMessageListeners[e.Guild.Id])
                // {
                //     await listener.OnMessage(e);
                // }
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
            // command lifetimes are now managed by CommandRegistry
            // await InstantiateCommandsForGuild(e.Guild.Id);
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

            //HACK: This should try and reconnect should something wrong happen.
            await this.discordClientService.ConnectAsync();
        }

        #endregion

        private BlendoBotConfig botConfig;

        private CommandRegistry commandRegistry;

        private IDiscordClientService discordClientService;

        private ILogger<Bot> logger;
    }
}