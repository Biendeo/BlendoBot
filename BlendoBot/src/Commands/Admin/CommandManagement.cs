namespace BlendoBot.Commands.Admin
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BlendoBot.CommandDiscovery;
    using Microsoft.Extensions.Logging;

    internal class CommandManagement
    {
        public CommandManagement(ICommandRouter router, ILogger<CommandManagement> logger)
        {
            this.router = router;
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
            return await this.router.EnableTerm(term);
        }

        public ISet<string> GetDisabledCommands() =>
            this.router.GetDisabledTerms();

        private readonly ICommandRouter router;

        private readonly ILogger<CommandManagement> logger;

    }
}
