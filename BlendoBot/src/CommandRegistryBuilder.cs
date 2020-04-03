namespace BlendoBot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BlendoBot.ConfigSchemas;
    using BlendoBotLib;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class CommandRegistryBuilder
    {
        public CommandRegistryBuilder(IServiceCollection services)
        {
            this.services = services;
            this.verbMap = new Dictionary<string, string>();
            this.lifetimes = new Dictionary<Type, CommandLifetime>();
        }

        public CommandRegistry Build(IServiceProvider serviceProvider, ILogger<CommandRegistryBuilder> logger)
        {
            // Process verb map to build Command Registry, injecting DI container.
            var verbMapToType = new Dictionary<string, Type>(this.verbMap.Count);
            foreach (var type in this.lifetimes.Keys)
            {
                if (this.verbMap.ContainsKey(type.Name))
                {
                    logger.LogInformation($"mapping verb {this.verbMap[type.Name]} to command type {type.Name}");
                    verbMapToType[this.verbMap[type.Name]] = type;
                }
                else
                {
                    logger.LogWarning($"verb map does not contain an entry for command type {type.Name}");
                }
            }

            var registry = new CommandRegistry(
                serviceProvider,
                verbMapToType,
                this.lifetimes);
            return registry;
        }

        public CommandRegistryBuilder RegisterSingleton<T>() where T : class, ICommand
        {
            this.services.AddSingleton<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.Singleton;
            return this;
        }

        public CommandRegistryBuilder RegisterGuildScoped<T>() where T : class, ICommand
        {
            // Here we register as transient.
            // Lifetime is manually managed in CommandRegistryBuilder.
            this.services.AddTransient<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.GuildScoped;
            return this;
        }

        public CommandRegistryBuilder RegisterTransient<T>() where T : class, ICommand
        {
            this.services.AddTransient<T>();
            this.lifetimes[typeof(T)] = CommandLifetime.Transient;
            return this;
        }

        public CommandRegistryBuilder WithConfig(CommandRegistryConfig? config)
        {
            // Merge verb overrides with replacements
            if (config?.VerbMapping?.Any() ?? false)
            {
                config.VerbMapping.ToList().ForEach(x => this.verbMap[x.Key] = x.Value);
            }

            return this;
        }

        private IServiceCollection services;

        private Dictionary<string, string> verbMap;

        private Dictionary<Type, CommandLifetime> lifetimes;
    }
}
