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
            this.instantiationBehaviours = new Dictionary<Type, InstantiationBehaviour>();
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
                this.instantiationBehaviours,
                this.lifetimes);
            return registry;
        }

        public ICommandRegistryBuilder RegisterSingleton<T>(InstantiationBehaviour behaviour = InstantiationBehaviour.Lazy) where T : class, ICommand
        {
            this.services.AddSingleton<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.Singleton;
            this.instantiationBehaviours[typeof(T)] = behaviour;
            return this;
        }

        public ICommandRegistryBuilder RegisterGuildScoped<T>(InstantiationBehaviour behaviour = InstantiationBehaviour.Lazy) where T : class, ICommand
        {
            // Here we register as transient.
            // Lifetime and instances are manually managed by CommandRegistry.
            this.services.AddTransient<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.GuildScoped;
            this.instantiationBehaviours[typeof(T)] = behaviour;
            return this;
        }

        public ICommandRegistryBuilder RegisterTransient<T>() where T : class, ICommand
        {
            this.services.AddTransient<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.Transient;
            return this;
        }

        private IServiceCollection services;

        private Dictionary<Type, InstantiationBehaviour> instantiationBehaviours;

        private Dictionary<Type, CommandLifetime> lifetimes;
    }
}
