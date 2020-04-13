namespace BlendoBot
{
    using System;
    using System.Threading.Tasks;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.Logging;

    public class DiscordClientService : IDiscordClient
    {
        public DiscordClientService(
            DiscordClient client,
            ILogger<DiscordClientService> logger
        )
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task<DiscordMessage> SendMessage(object sender, SendMessageEventArgs e)
        {
            this.logger.LogInformation($"Sending message {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})");
            if (e.LogMessage.Length > 2000)
            {
                int oldLength = e.Message.Length;
                e.LogMessage = e.LogMessage.Substring(0, 2000);
                this.logger.LogWarning($"Last message was {oldLength} characters long, truncated to 2000");
            }
            return await e.Channel.SendMessageAsync(e.Message);
        }

        public async Task<DiscordMessage> SendFile(object sender, SendFileEventArgs e)
        {
            this.logger.LogInformation($"Sending file {e.LogMessage} to channel #{e.Channel.Name} ({e.Channel.Guild.Name})");
            return await e.Channel.SendFileAsync(e.FilePath);
        }

        public async Task<DiscordMessage> SendException(object sender, SendExceptionEventArgs e)
        {
            this.logger.LogError(e.Exception, e.LogExceptionType);
            string messageHeader = $"A {e.LogExceptionType} occurred. Alert the authorities!\n```\n";
            string messageFooter = "\n```";
            string exceptionString = e.Exception.ToString();
            if (exceptionString.Length + messageHeader.Length + messageFooter.Length > 2000)
            {
                int oldLength = exceptionString.Length;
                exceptionString = exceptionString.Substring(0, 2000 - messageHeader.Length - messageFooter.Length);
                this.logger.LogWarning($"Last message was {oldLength} characters long, truncated to {exceptionString.Length}");
            }
            return await e.Channel.SendMessageAsync(messageHeader + exceptionString + messageFooter);
        }

        public async Task<DiscordUser> GetUser(ulong id)
        {
            return await this.client.GetUserAsync(id);
        }

        public async Task<DiscordChannel> GetChannel(ulong id)
        {
            return await this.client.GetChannelAsync(id);
        }

        public Task ConnectAsync(DiscordActivity? activity = null, UserStatus? status = null, DateTimeOffset? idleSince = null)
        {
            this.logger.LogInformation("Connecting");
            return this.client.ConnectAsync(activity, status, idleSince);
        }

        public Task UpdateStatusAsync(DiscordActivity? activity = null, UserStatus? status = null, DateTimeOffset? idleSince = null)
        {
            this.logger.LogInformation(
                "Updating status: activity={}, status={}, idleSince={}",
                activity is null ? "<null>" : $"{activity.ActivityType} {activity.Name}",
                status,
                idleSince?.ToString("o"));
            return this.client.UpdateStatusAsync(activity, status, idleSince);
        }

        #region Event handlers

        public event AsyncEventHandler<ReadyEventArgs> Ready
        {
            add => this.client.Ready += value;
            remove => this.client.Ready -= value;
        }

        public event AsyncEventHandler<MessageCreateEventArgs> MessageCreated
        {
            add => this.client.MessageCreated += value;
            remove => this.client.MessageCreated -= value;
        }

        public event AsyncEventHandler<GuildCreateEventArgs> GuildCreated
        {
            add => this.client.GuildCreated += value;
            remove => this.client.GuildCreated -= value;
        }

        public event AsyncEventHandler<GuildCreateEventArgs> GuildAvailable
        {
            add => this.client.GuildAvailable += value;
            remove => this.client.GuildAvailable -= value;
        }

        public event AsyncEventHandler<ClientErrorEventArgs> ClientErrored
        {
            add => this.client.ClientErrored += value;
            remove => this.client.ClientErrored -= value;
        }

        public event AsyncEventHandler<SocketCloseEventArgs> SocketClosed
        {
            add => this.client.SocketClosed += value;
            remove => this.client.SocketClosed -= value;
        }

        public event AsyncEventHandler<SocketErrorEventArgs> SocketErrored
        {
            add => this.client.SocketErrored += value;
            remove => this.client.SocketErrored -= value;
        }

        #endregion

        private DiscordClient client;

        private ILogger<DiscordClientService> logger;

    }
}