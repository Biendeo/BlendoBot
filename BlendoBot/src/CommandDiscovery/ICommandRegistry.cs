namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using DSharpPlus.EventArgs;

    internal interface ICommandRegistry
    {
        Task ExecuteForAsync(
            Type commandType,
            MessageCreateEventArgs e,
            Func<Exception, Task> onException
        );

        bool TryGetCommandInstance(Type commandType, ulong guildId, [NotNullWhen(true)] out ICommand instance);

        Task EagerLoadCommandInstances(ulong guildId);

        Task EagerLoadCommandInstances(ulong guildId, ISet<Type> exceptFor);

        ISet<Type> RegisteredCommandTypes { get; }
    }
}
