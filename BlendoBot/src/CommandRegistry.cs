namespace BlendoBot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BlendoBotLib;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.Logging;

    public class CommandRegistry : ICommandRegistry
    {
        public CommandRegistry(
            IServiceProvider serviceProvider,
            IDictionary<string, Type> verbMap,
            IDictionary<Type, CommandLifetime> lifetimes
        )
        {
            this.verbMap = new ConcurrentDictionary<string, Type>(verbMap);
            this.lifetimes = new Dictionary<Type, CommandLifetime>(lifetimes);
            this.guildScopedCommandInstances = new ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>>();
            this.serviceProvider = serviceProvider;
            this.logger = (ILogger<CommandRegistry>)this.serviceProvider.GetService(typeof(ILogger<CommandRegistry>));
        }

        public async Task ExecuteForAsync(
            MessageCreateEventArgs e,
            Func<Task> onUnmatchedCommand,
            Func<Exception, Task> onException
        )
        {
            string verb = e.Message.Content.Split(' ')[0].ToLowerInvariant().Substring(1);
            if (!this.verbMap.TryGetValue(verb, out var commandType))
            {
                // No verb
                await onUnmatchedCommand();
                return;
            }

            ICommand? cmd = null;
            switch (this.lifetimes[commandType])
            {
                case CommandLifetime.Transient:
                    // Instantiate a new command object
                    cmd = (ICommand)this.serviceProvider.GetService(commandType);
                    break;

                case CommandLifetime.GuildScoped:
                    // Get previously instantiated command object for that guild,
                    // otherwise request a new one from service provider
                    var guildInstances = this.guildScopedCommandInstances.GetOrAdd(
                        e.Guild.Id,
                        _ => new ConcurrentDictionary<Type, ICommand>()
                    );
                    cmd = guildInstances.GetOrAdd(
                        commandType,
                        (ICommand)this.serviceProvider.GetService(commandType)
                    );
                    break;

                case CommandLifetime.Singleton:
                    // CommandRegistryBuilder registers commands with a singleton lifetime as
                    // singleton in the DI framework directly. We can just request the service.
                    cmd = (ICommand)this.serviceProvider.GetService(commandType);
                    break;

                default:
                    // This should never happen
                    var msg = "Enum variant of CommandLifetime not handled. This should never happen!";
                    this.logger.LogError(msg);
                    await onException(new NotImplementedException(msg));
                    return;
            }

            try
            {
                await cmd.OnMessage(e);
            }
            catch (Exception ex)
            {
                await onException(ex);
            }
        }

        private IServiceProvider serviceProvider;

        private ILogger<CommandRegistry> logger;

        private ConcurrentDictionary<string, Type> verbMap;

        private readonly Dictionary<Type, CommandLifetime> lifetimes;

        private ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>> guildScopedCommandInstances;
    }
}
