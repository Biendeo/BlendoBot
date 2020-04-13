namespace BlendoBot.Commands.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BlendoBot.CommandDiscovery;
    using BlendoBotLib;
    using Microsoft.Extensions.Logging;

    internal class CommandManagement
    {
        public CommandManagement(
            Guild guild,
            ICommandRouter router,
            ICommandRegistry registry,
            ILogger<CommandManagement> logger)
        {
            this.guildId = guild.Id;
            this.router = router;
            this.registry = registry;
            this.logger = logger;
        }

        public async Task<string> Rename(string termFrom, string termTo)
        {
            return await this.router.RenameTerm(termFrom, termTo);
        }

        public async Task<bool> DisableCommand(string term)
        {
            return await this.router.DisableTerm(term);
        }

        public async Task<bool> EnableCommand(string term)
        {
            if (await this.router.EnableTerm(term))
            {
#pragma warning disable CS4014
                // Start eager load task in background
                Task.Run(() =>
                {
                    var disabledCommandTypes = new HashSet<Type>();
                    foreach (var disabledTerms in this.router.GetDisabledTerms())
                    {
                        if (router.TryTranslateTerm(disabledTerms, out Type type, includeDisabled: true))
                        {
                            disabledCommandTypes.Add(type);
                        }
                    }

                    this.registry.EagerLoadCommandInstances(this.guildId, disabledCommandTypes);
                });
#pragma warning restore CS4014

                return true;
            }

            return false;
        }

        public ISet<string> GetDisabledCommands() =>
            this.router.GetDisabledTerms();

        private readonly ulong guildId;
        private readonly ICommandRouter router;
        private readonly ICommandRegistry registry;
        private readonly ILogger<CommandManagement> logger;

    }
}
