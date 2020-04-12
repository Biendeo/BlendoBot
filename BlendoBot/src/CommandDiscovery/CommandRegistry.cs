namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using BlendoBot.Commands;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using DSharpPlus.EventArgs;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class CommandRegistry : ICommandRegistry
    {
        internal CommandRegistry(
            IServiceProvider serviceProvider,
            IDictionary<Type, InstantiationBehaviour> instantiationBehaviours,
            IDictionary<Type, CommandLifetime> lifetimes)
        {
            this.lifetimes = new Dictionary<Type, CommandLifetime>(lifetimes);
            this.guildScopedCommandInstances = new ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>>();
            this.serviceProvider = serviceProvider;
            this.logger = (ILogger<CommandRegistry>)this.serviceProvider.GetService(typeof(ILogger<CommandRegistry>));

            this.typesToEagerLoad = instantiationBehaviours
                .Where(kvp => kvp.Value == InstantiationBehaviour.Eager)
                .Select(kvp => kvp.Key)
                .ToHashSet();
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
                            // Use ActivatorUtilites.CreateInstance to inject runtime-defined
                            // parameters into the constructor e.g. guild id
                            var parameters = new HashSet<object>();

                            // HACK ActivatorUtilites.CreateInstance expects all passed parameters
                            // to be used. So we do some reflection to look at constructors to fill
                            // the right parameters
                            var ctors = type.GetConstructors();
                            if (ctors.Length != 1)
                            {
                                throw new InvalidOperationException();
                            }

                            bool isPrivilegedCommand = type.GetCustomAttribute(typeof(PrivilegedCommandAttribute)) != null;
                            foreach (var param in ctors[0].GetParameters())
                            {
                                Type paramType = param.ParameterType;
                                if (paramType == typeof(Guild))
                                {
                                    parameters.Add(new Guild { Id = guildId });
                                }
                                else if (isPrivilegedCommand && paramType == typeof(ICommandRegistry))
                                {
                                    parameters.Add((ICommandRegistry)this);
                                }
                                else if (isPrivilegedCommand && paramType == typeof(ICommandRouter))
                                {
                                    var commandRouterManager = this.serviceProvider.GetService<ICommandRouterManager>();
                                    if (commandRouterManager.TryGetRouter(guildId, out var router))
                                    {
                                        parameters.Add(router);
                                    }
                                }
                            }

                            if (parameters.Count == 0)
                            {
                                return (ICommand)ActivatorUtilities.CreateInstance(this.serviceProvider, type);
                            }
                            else
                            {
                                return (ICommand)ActivatorUtilities.CreateInstance(this.serviceProvider, type, parameters.ToArray());
                            }

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

        public Task EagerLoadCommandInstances(ulong guildId) => this.EagerLoadCommandInstances(guildId, new HashSet<Type>());

        public async Task EagerLoadCommandInstances(ulong guildId, ISet<Type> exceptFor)
        {
            var typesToEagerLoad = this.typesToEagerLoad.Except(exceptFor).ToHashSet();

            var sw = Stopwatch.StartNew();
            this.logger.LogInformation(
                "Eager-instantiating the following command types for guild {}: [{}]",
                guildId,
                string.Join(',', typesToEagerLoad.Select(t => t.Name)));

            await Task.WhenAll(typesToEagerLoad.Select(t => Task.Run(() => this.TryGetCommandInstance(t, guildId, out _))));

            this.logger.LogInformation(
                "Eager-instantiating complete for guild {}, took {}ms",
                guildId,
                sw.Elapsed.TotalMilliseconds);
        }

        public ISet<Type> RegisteredCommandTypes => new HashSet<Type>(this.lifetimes.Keys);

        private readonly IServiceProvider serviceProvider;

        private readonly ISet<Type> typesToEagerLoad;

        private readonly ILogger<CommandRegistry> logger;

        private readonly Dictionary<Type, CommandLifetime> lifetimes;

        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<Type, ICommand>> guildScopedCommandInstances;
    }
}
