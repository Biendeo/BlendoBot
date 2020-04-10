namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BlendoBot.ConfigSchemas;
    using Microsoft.Extensions.Logging;

    public class CommandRouter : ICommandRouter
    {
        public CommandRouter(
            ILogger<CommandRouter> logger,
            CommandRouterConfig config,
            ISet<Type> commandTypes)
        {
            this.logger = logger;
            this.commandMap = new Dictionary<string, Type>();
            this.disabledCommands = new HashSet<Type>();

            var joined = config.Commands.Join(
                commandTypes,
                c => c.Command,
                t => t.Name,
                (c, t) =>
                {
                    return new
                    {
                        CommandType = t,
                        Term = c.Term,
                        Enabled = c.Enabled
                    };
                });

            foreach (var o in joined)
            {
                this.logger.LogInformation($"Mapping term {o.Term} to command type {o.CommandType.Name}");
                this.commandMap.Add(o.Term, o.CommandType);
                if (!o.Enabled)
                {
                    this.logger.LogInformation($"Disabling command type {o.CommandType.Name}");
                    this.disabledCommands.Add(o.CommandType);
                }
            }
        }

        public bool TryTranslateTerm(string term, out Type commandType)
        {
            #pragma warning disable CS8601
            return this.commandMap.TryGetValue(term, out commandType) && !this.disabledCommands.Contains(commandType);
            #pragma warning restore CS8601 
        }

        ILogger<CommandRouter> logger;

        private Dictionary<string, Type> commandMap;

        private HashSet<Type> disabledCommands;
    }
}
