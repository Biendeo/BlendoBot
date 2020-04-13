namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using BlendoBot.Commands;
    using BlendoBot.ConfigSchemas;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    internal class CommandRouter : ICommandRouter
    {
        public CommandRouter(
            ulong guildId,
            ILogger<CommandRouter> logger,
            IDataStore<CommandRouter, CommandRouterConfig> dataStore,
            ISet<Type> commandTypes)
        {
            this.guildId = guildId;
            this.logger = logger;
            this.dataStore = dataStore;
            this.commandMap = new Dictionary<string, Type>();
            this.disabledCommands = new HashSet<Type>();

            // Read config from data store
            CommandRouterConfig config;
            try
            {
                config = this.dataStore.ReadAsync(this.dataStorePath).Result;
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                this.logger.LogWarning("CommandRouterConfig not found for guild {}, creating empty config.", guildId);
                config = new CommandRouterConfig
                {
                    Commands = new List<CommandConfig>()
                };

                this.dataStore.WriteAsync(this.dataStorePath, config).Wait();
            }

            var joined = config.Commands.Join(
                commandTypes,
                c => c.Command,
                t => t.Name,
                (c, t) => new
                {
                    CommandType = t,
                    Term = c.Term,
                    Enabled = c.Enabled
                }).ToList();

            // Handle defaults
            foreach (Type commandType in commandTypes.Where(t => !config.Commands.Select(c => c.Command).Contains(t.Name)))
            {
                if (commandType.GetCustomAttribute<CommandDefaultsAttribute>() is CommandDefaultsAttribute defaults)
                {
                    this.logger.LogInformation(
                        "Using defaults(term = {}, enabled = {}) for command type {} for guild {}",
                        defaults.DefaultTerm,
                        defaults.EnabledByDefault,
                        commandType.Name,
                        guildId);
                    joined.Add(new
                    {
                        CommandType = commandType,
                        Term = defaults.DefaultTerm,
                        Enabled = defaults.EnabledByDefault
                    });
                }
            }

            foreach (var o in joined)
            {
                this.logger.LogInformation("Mapping term {} to command type {} for guild {}", o.Term, o.CommandType.Name, this.guildId);
                this.commandMap.Add(o.Term, o.CommandType);
                if (!o.Enabled)
                {
                    this.logger.LogInformation("Disabling command type {} for guild {}", o.CommandType.Name, this.guildId);
                    this.disabledCommands.Add(o.CommandType);
                }
            }
        }

        public bool TryTranslateTerm(string term, out Type commandType, bool includeDisabled = false)
        {
            #pragma warning disable CS8601
            return this.commandMap.TryGetValue(term, out commandType) && (includeDisabled || !this.disabledCommands.Contains(commandType));
            #pragma warning restore CS8601 
        }

        public async Task<string> RenameTerm(string termFrom, string termTo)
        {
            if (this.TryTranslateTerm(termFrom, out var type, includeDisabled: true))
            {
                if (this.TryTranslateTerm(termTo, out _, includeDisabled: true))
                {
                    int i = 1;
                    for (; this.TryTranslateTerm($"{termTo}{i}", out _, includeDisabled: true); ++i);
                    termTo = $"{termTo}{i}";
                }

                // FIXME not thread safe
                this.logger.LogInformation("Remapping term {} to term {} for command type {} for guild {}", termFrom, termTo, type.Name, this.guildId);
                var backup = new Dictionary<string, Type>(this.commandMap);
                try
                {
                    this.commandMap.Add(termTo, type);
                    this.commandMap.Remove(termFrom);
                    await this.dataStore.WriteAsync(this.dataStorePath, this.ToConfig());
                    return termTo;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error occurred while remapping term {} to term {} for command type {} for guild {}. Restoring backup", termFrom, termTo, type.Name, this.guildId);
                    this.commandMap = backup;
                    await this.dataStore.WriteAsync(this.dataStorePath, this.ToConfig());
                }
            }

            return string.Empty;
        }

        public async Task<bool> EnableTerm(string term)
        {
            if (this.TryTranslateTerm(term, out var type, includeDisabled: true))
            {
                if (this.disabledCommands.Contains(type))
                {
                    this.logger.LogInformation("Enabling command type {} for guild {}", type.Name, this.guildId);
                    var backup = new HashSet<Type>(this.disabledCommands);
                    try
                    {
                        this.disabledCommands.Remove(type);
                        await this.dataStore.WriteAsync(this.dataStorePath, this.ToConfig());
                        return true;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Error occurred while enabling command type {} for guild {}. Restoring backup.", type.Name, this.guildId);
                        this.disabledCommands = backup;
                        await this.dataStore.WriteAsync(this.dataStorePath, this.ToConfig());
                    }
                }
            }

            return false;
        }

        public async Task<bool> DisableTerm(string term)
        {
            if (this.TryTranslateTerm(term, out var type, includeDisabled: true))
            {
                if (type.GetCustomAttribute<PrivilegedCommandAttribute>() != null)
                {
                    this.logger.LogInformation("Rejecting request to disable privileged command type {} for guild {}", type.Name, this.guildId);
                    return false;
                }

                if (!this.disabledCommands.Contains(type))
                {
                    this.logger.LogInformation("Disabling command type {} for guild {}", type.Name, this.guildId);
                    var backup = new HashSet<Type>(this.disabledCommands);
                    try
                    {
                        this.disabledCommands.Add(type);
                        await this.dataStore.WriteAsync(this.dataStorePath, this.ToConfig());
                        return true;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Error occurred while disabling command type {} for guild {}. Restoring backup", type.Name, this.guildId);
                        this.disabledCommands = backup;
                        await this.dataStore.WriteAsync(this.dataStorePath, this.ToConfig());
                    }
                }
            }

            return false;
        }

        public ISet<string> GetDisabledTerms() =>
            this.disabledCommands.Join(this.commandMap, t => t, kvp => kvp.Value, (t, kvp) => kvp.Key).ToHashSet();

        public ISet<string> GetEnabledTerms() =>
            this.commandMap.Keys.Except(this.GetDisabledTerms()).ToHashSet();

        private string dataStorePath => Path.Join(this.guildId.ToString(), "config");

        private CommandRouterConfig ToConfig()
        {
            var commands = this.commandMap.Select(kvp => new CommandConfig
            {
                Command = kvp.Value.Name,
                Term = kvp.Key,
                Enabled = !this.disabledCommands.Contains(kvp.Value)
            });

            return new CommandRouterConfig
            {
                Commands = commands.ToList()
            };
        }

        private readonly ulong guildId;

        private ILogger<CommandRouter> logger;

        private readonly IDataStore<CommandRouter, CommandRouterConfig> dataStore;

        private Dictionary<string, Type> commandMap;

        private HashSet<Type> disabledCommands;
    }
}
