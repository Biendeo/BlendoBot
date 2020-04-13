namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BlendoBot.ConfigSchemas;
    using BlendoBotLib.DataStore;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    internal class CommandRouterFactory : ICommandRouterFactory
    {
        public CommandRouterFactory(
            ILoggerFactory loggerFactory,
            ILogger<CommandRouterFactory> logger,
            IDataStore<CommandRouter, CommandRouterConfig> dataStore)
        {
            this.dataStore = dataStore;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        public static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            services.AddSingleton<
                IDataStore<CommandRouter, CommandRouterConfig>,
                JsonFileDataStore<CommandRouter, CommandRouterConfig>>();
        }

        public Task<ICommandRouter> CreateForGuild(ulong guildId, ISet<Type> commandTypes)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation(
                "Creating command router for guild {}. Supported command types: [{}]",
                guildId,
                string.Join(",", commandTypes.Select(t => t.Name)));

            var router = new CommandRouter(
                guildId,
                this.loggerFactory.CreateLogger<CommandRouter>(),
                this.dataStore,
                commandTypes
            );

            this.logger.LogInformation("Command router created for guild {}, took {}ms", guildId, sw.Elapsed.TotalMilliseconds);

            return Task.FromResult((ICommandRouter)router);
        }

        private IDataStore<CommandRouter, CommandRouterConfig> dataStore;

        private ILogger<CommandRouterFactory> logger;

        private ILoggerFactory loggerFactory;
    }
}
