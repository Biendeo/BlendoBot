namespace BlendoBot.CommandDiscovery
{
    using System;
    using BlendoBotLib.Interfaces;

    internal interface ICommandRegistryBuilder
    {
        ICommandRegistry Build(IServiceProvider serviceProvider);

        ICommandRegistryBuilder RegisterGuildScoped<T>(InstantiationBehaviour behaviour = InstantiationBehaviour.Lazy) where T : class, ICommand;

        ICommandRegistryBuilder RegisterSingleton<T>(InstantiationBehaviour behaviour = InstantiationBehaviour.Lazy) where T : class, ICommand;

        ICommandRegistryBuilder RegisterTransient<T>() where T : class, ICommand;
    }
}