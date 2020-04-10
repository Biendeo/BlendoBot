namespace BlendoBotLib.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    public interface IDiscordClient
    {
        Task ConnectAsync(DiscordActivity? activity = null, UserStatus? status = null, DateTimeOffset? idleSince = null);

        Task UpdateStatusAsync(DiscordActivity? activity = null, UserStatus? status = null, DateTimeOffset? idleSince = null);

        Task<DiscordMessage> SendMessage(object sender, SendMessageEventArgs e);

        Task<DiscordMessage> SendFile(object sender, SendFileEventArgs e);

        Task<DiscordMessage> SendException(object sender, SendExceptionEventArgs e);

        event AsyncEventHandler<ReadyEventArgs> Ready;

        event AsyncEventHandler<MessageCreateEventArgs> MessageCreated;

        event AsyncEventHandler<GuildCreateEventArgs> GuildCreated;

        event AsyncEventHandler<GuildCreateEventArgs> GuildAvailable;

        event AsyncEventHandler<ClientErrorEventArgs> ClientErrored;

        event AsyncEventHandler<SocketCloseEventArgs> SocketClosed;

        event AsyncEventHandler<SocketErrorEventArgs> SocketErrored;
    }
}