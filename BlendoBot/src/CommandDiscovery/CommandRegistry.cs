namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Threading.Tasks;
    using BlendoBot.Commands;
    using BlendoBotLib;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class CommandRegistry : ICommandRegistry
    {
        internal CommandRegistry(
            IServiceProvider serviceProvider,
            IDictionary<Type, CommandLifetime> lifetimes)
        {
            this.lifetimes = new Dictionary<Type, CommandLifetime>(lifetimes);
            this.guildScopedCommandInstances = new ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>>();
            this.serviceProvider = serviceProvider;
            this.logger = (ILogger<CommandRegistry>)this.serviceProvider.GetService(typeof(ILogger<CommandRegistry>));
        }

        public async Task ExecuteForAsync(
            Type commandType,
            MessageCreateEventArgs e,
            Func<Exception, Task> onException)
        {
            if (!this.TryGetCommandInstance(commandType, e.Guild.Id, out var cmd))
            {
                var msg = $"Command type {commandType.Name} was requested, but not found in the command registry";
                this.logger.LogError(msg);
                await onException(new NotImplementedException(msg));
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

        public bool TryGetCommandInstance(
            Type commandType,
            ulong guildId,
            [NotNullWhen(returnValue: true)] out ICommand instance)
        {
            if (!this.lifetimes.ContainsKey(commandType))
            {
                instance = null!;
                return false;
            }

            switch (this.lifetimes[commandType])
            {
                case CommandLifetime.Transient:
                    // Instantiate a new command object
                    instance = (ICommand)this.serviceProvider.GetService(commandType);
                    return true;

                case CommandLifetime.GuildScoped:
                    // Get previously instantiated command object for that guild,
                    // otherwise request a new one from service provider
                    var guildInstances = this.guildScopedCommandInstances.GetOrAdd(
                        guildId,
                        _ => new ConcurrentDictionary<Type, ICommand>()
                    );
                    instance = guildInstances.GetOrAdd(
                        commandType,
                        type =>
                        {
                            // Use ActivatorUtilites.CreateInstance to inject a runtime-defined
                            // parameters into the constructor e.g. guild id
                            var parameters = new List<object> { new Guild { Id = guildId } };
                            if (type.GetCustomAttribute(typeof(PrivilegedCommandAttribute)) != null)
                            {
                                // Inject relevant command discovery objects for privileged commands e.g. Admin
                                parameters.Add((ICommandRegistry)this);
                                var commandRouterManager = this.serviceProvider.GetService<ICommandRouterManager>();
                                if (commandRouterManager.TryGetRouter(guildId, out var router))
                                {
                                    parameters.Add((ICommandRouter)router);
                                }
                            }
                            return (ICommand)ActivatorUtilities.CreateInstance(this.serviceProvider, type, parameters.ToArray());
                        }
                    );
                    return true;

                case CommandLifetime.Singleton:
                    // CommandRegistryBuilder registers commands with a singleton lifetime as
                    // singleton in the DI framework directly. We can just request the service.
                    instance = (ICommand)this.serviceProvider.GetService(commandType);
                    return true;

                default:
                    // This should never happen
                    this.logger.LogError("Enum variant of CommandLifetime not handled. This should never happen!");
                    instance = null!;
                    return false;
            }
        }

        public ISet<Type> RegisteredCommandTypes => new HashSet<Type>(this.lifetimes.Keys);

        private IServiceProvider serviceProvider;

        private ILogger<CommandRegistry> logger;

        private readonly Dictionary<Type, CommandLifetime> lifetimes;

        private ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>> guildScopedCommandInstances;
    }
}
