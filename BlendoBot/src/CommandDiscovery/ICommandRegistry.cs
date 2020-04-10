namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DSharpPlus.EventArgs;

    public interface ICommandRegistry
    {
        Task ExecuteForAsync(
            Type commandType,
            MessageCreateEventArgs e,
            Func<Exception, Task> onException
        );

        ISet<Type> RegisteredCommandTypes { get; }
    }
}
