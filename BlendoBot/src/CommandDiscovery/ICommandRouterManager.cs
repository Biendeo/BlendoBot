namespace BlendoBot.CommandDiscovery
{
    using System.Diagnostics.CodeAnalysis;

    internal interface ICommandRouterManager
    {
        bool TryGetRouter(ulong guildId, [NotNullWhen(returnValue: true)] out ICommandRouter? router);

        bool TryAddRouter(ulong guildId, ICommandRouter router);

        bool TryRemoveRouter(ulong guildId);
    }
}
