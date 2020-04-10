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
            IInstancedDataStore<CommandRouter> dataStore)
        {
            this.dataStore = dataStore;
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        public static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            services.AddSingleton<
                IDataStore<CommandRouter>,
                JsonFileDataStore<CommandRouter>>();
            services.AddSingleton<
                IInstancedDataStore<CommandRouter>,
                GuildInstancedDataStore<CommandRouter>>();
        }

        public async Task<ICommandRouter> CreateForGuild(ulong guildId, ISet<Type> commandTypes)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation(
                "Creating command router for guild {}. Supported command types: [{}]",
                guildId,
                string.Join(",", commandTypes.Select(t => t.Name)));
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
                guildId,
                this.loggerFactory.CreateLogger<CommandRouter>(),
                this.dataStore,
                config,
                commandTypes
            );

            this.logger.LogInformation("Command router created for guild {} in {}ms", guildId, sw.Elapsed.TotalMilliseconds);

            return router;
        }

        private IInstancedDataStore<CommandRouter> dataStore;

        private ILogger<CommandRouterFactory> logger;

        private ILoggerFactory loggerFactory;
    }
}
