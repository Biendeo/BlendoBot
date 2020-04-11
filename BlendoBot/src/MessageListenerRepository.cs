namespace BlendoBot
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using BlendoBotLib.Interfaces;

    internal class MessageListenerRepository : IMessageListenerRepository, IMessageListenerEnumerable
    {
        public MessageListenerRepository()
        {
            this.listeners = new ConcurrentDictionary<IMessageListener, Guid>();
        }

        public void Add(IMessageListener listener)
        {
            this.listeners.TryAdd(listener, Guid.NewGuid());
        }

        public void Remove(IMessageListener listener)
        {
            this.listeners.TryRemove(listener, out _);
        }

        public IEnumerator<IMessageListener> GetEnumerator() => this.listeners.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.listeners.GetEnumerator();

        private ConcurrentDictionary<IMessageListener, Guid> listeners;
    }
}
