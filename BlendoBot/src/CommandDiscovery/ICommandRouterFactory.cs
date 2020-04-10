namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ICommandRouterFactory
    {
        Task<ICommandRouter> CreateForGuild(ulong guildId, ISet<Type> commandTypes);
    }
}
