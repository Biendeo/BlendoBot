namespace BlendoBot.CommandDiscovery
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
            IDictionary<Type, CommandLifetime> lifetimes
        )
        {
            this.lifetimes = new Dictionary<Type, CommandLifetime>(lifetimes);
            this.guildScopedCommandInstances = new ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>>();
            this.serviceProvider = serviceProvider;
            this.logger = (ILogger<CommandRegistry>)this.serviceProvider.GetService(typeof(ILogger<CommandRegistry>));
        }

        public async Task ExecuteForAsync(
            Type commandType,
            MessageCreateEventArgs e,
            Func<Exception, Task> onException
        )
        {
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

        public ISet<Type> RegisteredCommandTypes => new HashSet<Type>(this.lifetimes.Keys);

        private IServiceProvider serviceProvider;

        private ILogger<CommandRegistry> logger;

        private readonly Dictionary<Type, CommandLifetime> lifetimes;

        private ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>> guildScopedCommandInstances;
    }
}
