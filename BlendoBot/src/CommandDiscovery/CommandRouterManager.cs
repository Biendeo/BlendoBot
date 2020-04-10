namespace BlendoBot.CommandDiscovery
{
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;

    internal class CommandRouterManager : ICommandRouterManager
    {
        public CommandRouterManager()
        {
            this.map = new ConcurrentDictionary<ulong, ICommandRouter>();
        }

        public bool TryAddRouter(ulong guildId, ICommandRouter router) =>
            this.map.TryAdd(guildId, router);

        public bool TryGetRouter(ulong guildId, [NotNullWhen(true)] out ICommandRouter? router) =>
            this.map.TryGetValue(guildId, out router);

        public bool TryRemoveRouter(ulong guildId) =>
            this.map.TryRemove(guildId, out _);

        public ConcurrentDictionary<ulong, ICommandRouter> map;
    }
}