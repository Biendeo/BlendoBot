namespace UserTimeZone
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    public class UserTimeZoneProvider : IUserTimeZoneProvider
    {
        private ConcurrentDictionary<ulong, Dictionary<string, TimeZoneInfo>> timezones;
        private readonly IDataStore<UserTimeZoneProvider, Dictionary<string, TimeZoneInfo>> dataStore;
        private readonly ILogger<UserTimeZoneProvider> logger;

        public UserTimeZoneProvider(
            IDataStore<UserTimeZoneProvider, Dictionary<string, TimeZoneInfo>> dataStore,
            ILogger<UserTimeZoneProvider> logger)
        {
            this.dataStore = dataStore;
            this.logger = logger;

            // Since we do not have info of all guilds on startup, we have to load on first request
            this.timezones = new ConcurrentDictionary<ulong, Dictionary<string, TimeZoneInfo>>();
        }

        public Task<TimeZoneInfo> GetTimeZone(ulong guildId, ulong userId)
        {
            var timezonesForGuild = this.GetOrAddGuildDict(guildId);

            // Default to UTC if not user set
            return timezonesForGuild.TryGetValue(userId.ToString(), out var ret)
                ? Task.FromResult(ret)
                : Task.FromResult(TimeZoneInfo.Utc);
        }

        public async Task SetTimeZone(ulong guildId, ulong userId, TimeZoneInfo value)
        {
            var timezonesForGuild = this.GetOrAddGuildDict(guildId);

            var copy = new Dictionary<string, TimeZoneInfo>(timezonesForGuild);
            copy[userId.ToString()] = value;
            await this.dataStore.WriteAsync(this.GetDataPathForGuild(guildId), copy);
            this.timezones[guildId] = copy;
        }

        private Dictionary<string, TimeZoneInfo> GetOrAddGuildDict(ulong guildId)
        {
            return this.timezones.GetOrAdd(guildId, guildId =>
            {
                Dictionary<string, TimeZoneInfo> dict;

                var sw = Stopwatch.StartNew();
                this.logger.LogInformation("Loading user timezone info for guild {}", guildId);

                try
                {
                    dict = this.dataStore.ReadAsync(this.GetDataPathForGuild(guildId)).Result;
                }
                catch (AggregateException aex) when (aex.InnerException is DirectoryNotFoundException || aex.InnerException is FileNotFoundException)
                {
                    this.logger.LogInformation("User timezone info not found in data store for guild {}, creating new", guildId);
                    dict = new Dictionary<string, TimeZoneInfo>();
                    this.dataStore.WriteAsync(this.GetDataPathForGuild(guildId), dict).Wait();
                }

                this.logger.LogInformation("User timezone info loaded for guild {}, took {}ms", guildId, sw.Elapsed.TotalMilliseconds);
                return dict;
            });
        }

        private string GetDataPathForGuild(ulong guildId) => Path.ChangeExtension(Path.Join(guildId.ToString()), "json");
    }
}
