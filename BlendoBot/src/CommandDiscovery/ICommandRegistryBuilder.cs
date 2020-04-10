namespace BlendoBot.CommandDiscovery
{
    using System;
    using BlendoBotLib;

    internal interface ICommandRegistryBuilder
    {
        ICommandRegistry Build(IServiceProvider serviceProvider);

        ICommandRegistryBuilder RegisterGuildScoped<T>() where T : class, ICommand;

        ICommandRegistryBuilder RegisterSingleton<T>() where T : class, ICommand;

        ICommandRegistryBuilder RegisterTransient<T>() where T : class, ICommand;
    }
}