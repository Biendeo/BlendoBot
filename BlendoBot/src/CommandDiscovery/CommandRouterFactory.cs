namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using BlendoBot.ConfigSchemas;
    using BlendoBotLib.Interfaces;
    using BlendoBotLib.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class CommandRouterFactory : ICommandRouterFactory
    {
        public CommandRouterFactory(
            ILoggerFactory loggerFactory,
            ILogger<CommandRouterFactory> logger,
            IInstancedDataStore<CommandRouterFactory> dataStore)
        {
            this.dataStore = dataStore;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        public static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            services.AddSingleton<
                IDataStore<CommandRouterFactory>,
                JsonFileDataStoreService<CommandRouterFactory>>();
            services.AddSingleton<
                IInstancedDataStore<CommandRouterFactory>,
                GuildInstancedDataStoreService<CommandRouterFactory>>();
        }

        public async Task<ICommandRouter> CreateForGuild(ulong guildId, ISet<Type> commandTypes)
        {
            CommandRouterConfig config;
            try
            {
                config = await this.dataStore.ReadAsync<CommandRouterConfig>(guildId, "config");
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                this.logger.LogWarning("CommandRouterConfig not found for guild {}, creating empty config.", guildId);
                config = new CommandRouterConfig
                {
                    Commands = new List<CommandConfig>()
                };
                await this.dataStore.WriteAsync(guildId, "config", config);
            }

            var router = new CommandRouter(
                this.loggerFactory.CreateLogger<CommandRouter>(),
                config,
                commandTypes
            );

            return router;
        }

        private IInstancedDataStore<CommandRouterFactory> dataStore;

        private ILogger<CommandRouterFactory> logger;

        private ILoggerFactory loggerFactory;
    }
}
