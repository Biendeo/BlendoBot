namespace BlendoBot
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    internal class MessageListenerRepository : IMessageListenerRepository, IMessageListenerEnumerable
    {
        public MessageListenerRepository(ILogger<MessageListenerRepository> logger)
        {
            this.listeners = new ConcurrentDictionary<IMessageListener, ulong>();
            this.logger = logger;
        }

        public void Add(ulong guildId, IMessageListener listener)
        {
            if (this.listeners.TryAdd(listener, guildId))
            {
                this.logger.LogInformation("Added message listener for guild {}", guildId);
            }
            else
            {
                this.logger.LogWarning("Failed to add message listener for guild {}", guildId);
            }
        }

        public void Remove(ulong guildId, IMessageListener listener)
        {
            if (this.listeners.TryRemove(listener, out _))
            {
                this.logger.LogInformation("Removed message listener for guild {}", guildId);
            }
            else
            {
                this.logger.LogWarning("Failed to add message listener for guild {}", guildId);
            }
        }

        public IEnumerable<IMessageListener> ForGuild(ulong guildId) =>
            this.listeners.Where(kvp => kvp.Value == guildId).Select(kvp => kvp.Key);

        private readonly ConcurrentDictionary<IMessageListener, ulong> listeners;
        private readonly ILogger<MessageListenerRepository> logger;
    }
}
