namespace BlendoBot.CommandDiscovery
{
    using System;
    using BlendoBotLib;
    using Microsoft.Extensions.Logging;

    public interface ICommandRegistryBuilder
    {
        ICommandRegistry Build(IServiceProvider serviceProvider, ILogger<ICommandRegistryBuilder> logger);

        ICommandRegistryBuilder RegisterGuildScoped<T>() where T : class, ICommand;

        ICommandRegistryBuilder RegisterSingleton<T>() where T : class, ICommand;

        ICommandRegistryBuilder RegisterTransient<T>() where T : class, ICommand;
    }
}