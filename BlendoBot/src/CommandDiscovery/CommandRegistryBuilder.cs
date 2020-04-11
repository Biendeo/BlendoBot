namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.DependencyInjection;

    internal class CommandRegistryBuilder : ICommandRegistryBuilder
    {
        public CommandRegistryBuilder(IServiceCollection services)
        {
            this.services = services;
            this.lifetimes = new Dictionary<Type, CommandLifetime>();

            // HACK CommandRegistry manually injects instances of Guild, ICommandRegistry, and ICommandRouter
            // To satisfy the dependency validator, perform a dummy registration here
            this.services.AddTransient<Guild>(sp => null!);
            this.services.AddTransient<ICommandRegistry>(sp => null!);
            this.services.AddTransient<ICommandRouter>(sp => null!);
        }

        public ICommandRegistry Build(IServiceProvider serviceProvider)
        {
            var registry = new CommandRegistry(
                serviceProvider,
                this.lifetimes);
            return registry;
        }

        public ICommandRegistryBuilder RegisterSingleton<T>() where T : class, ICommand
        {
            this.services.AddSingleton<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.Singleton;
            return this;
        }

        public ICommandRegistryBuilder RegisterGuildScoped<T>() where T : class, ICommand
        {
            // Here we register as transient.
            // Lifetime and instances are manually managed by CommandRegistry.
            this.services.AddTransient<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.GuildScoped;
            return this;
        }

        public ICommandRegistryBuilder RegisterTransient<T>() where T : class, ICommand
        {
            this.services.AddTransient<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.Transient;
            return this;
        }

        private IServiceCollection services;

        private Dictionary<Type, CommandLifetime> lifetimes;
    }
}
