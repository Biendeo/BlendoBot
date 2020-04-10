namespace BlendoBot.Commands.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using DSharpPlus.Entities;
    using Microsoft.Extensions.Logging;

    internal class Membership
    {
        public Membership(
            ulong guildId,
            IDiscordClient discordClient,
            ILogger<Membership> logger,
            IInstancedDataStore<AdminV3> dataStore)
        {
            this.logger = logger;
            this.guildId = guildId;
            this.discordClient = discordClient;
            this.dataStore = dataStore;
            this.admins = new HashSet<DiscordUser>();

            this.InitFromDataStore().Wait();
        }

        public bool IsAdmin(DiscordUser user) => this.admins.Contains(user);

        public async Task<bool> AddAdmin(DiscordUser user)
        {
            if (!this.IsAdmin(user))
            {
                var newAdmins = new HashSet<DiscordUser>(this.admins);
                if (newAdmins.Add(user))
                {
                    await this.dataStore.WriteAsync(this.guildId, "administrators", newAdmins);
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> RemoveAdmin(DiscordUser user)
        {
            if (this.IsAdmin(user))
            {
                var newAdmins = new HashSet<DiscordUser>(this.admins);
                if (newAdmins.Remove(user))
                {
                    await this.dataStore.WriteAsync(this.guildId, "administrators", newAdmins);
                    return true;
                }
            }

            return false;
        }

        public Task<HashSet<DiscordUser>> GetAdmins() => Task.FromResult(this.admins);

        private async Task InitFromDataStore()
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation("Load admin membership for guild {}.", guildId);

            try
            {
                this.admins = await this.dataStore.ReadAsync<HashSet<DiscordUser>>(this.guildId, "administrators");
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                this.logger.LogInformation("administrators not found for guild {}, creating new", this.guildId);
                var adminIds = new HashSet<DiscordUser>();
                await this.dataStore.WriteAsync(guildId, "administrators", adminIds);
                this.admins = adminIds;
            }

            this.logger.LogInformation("Admin membership loaded for guild {} in {}ms", guildId, sw.Elapsed.TotalMilliseconds);
        }

        private HashSet<DiscordUser> admins;

        private IDiscordClient discordClient;

        private IInstancedDataStore<AdminV3> dataStore;

        private ILogger<Membership> logger;

        private ulong guildId;
    }
}