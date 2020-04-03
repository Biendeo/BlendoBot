namespace BlendoBot
{
    using System;
    using System.Threading.Tasks;
    using DSharpPlus.EventArgs;

    public interface ICommandRegistry
    {
        Task ExecuteForAsync(
            MessageCreateEventArgs e,
            Func<Task> onUnmatchedCommand,
            Func<Exception, Task> onException
        );
    }
}
