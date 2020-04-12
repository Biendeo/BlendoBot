namespace BlendoBot.Commands.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using DataSchemas;
    using DSharpPlus.Entities;
    using Microsoft.Extensions.Logging;

    internal class Membership
    {
        public Membership(
            ulong guildId,
            IDiscordClient discordClient,
            ILogger<Membership> logger,
            IDataStore<AdminV3, Administrators> dataStore)
        {
            this.logger = logger;
            this.guildId = guildId;
            this.discordClient = discordClient;
            this.dataStore = dataStore;

            var sw = Stopwatch.StartNew();
            this.logger.LogInformation("Load admin membership for guild {}.", guildId);

            try
            {
                this.admins = this.dataStore.ReadAsync(this.dataStorePath).Result;
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                this.logger.LogInformation("Admin membership not found in data store for guild {}, creating new", this.guildId);
                this.admins = new Administrators { UserIds = new HashSet<ulong>() };
                this.dataStore.WriteAsync(this.dataStorePath, this.admins).Wait();
            }

            this.logger.LogInformation("Admin membership loaded for guild {}, took {}ms", guildId, sw.Elapsed.TotalMilliseconds);
        }

        public bool IsAdmin(DiscordUser user) => this.admins.UserIds.Contains(user.Id);

        public async Task<bool> AddAdmin(DiscordUser user)
        {
            if (!this.IsAdmin(user))
            {
                var newAdmins = new Administrators { UserIds = this.admins.UserIds };
                if (newAdmins.UserIds.Add(user.Id))
                {
                    await this.dataStore.WriteAsync(this.dataStorePath, newAdmins);
                    this.admins = newAdmins;
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> RemoveAdmin(DiscordUser user)
        {
            if (this.IsAdmin(user))
            {
                var newAdmins = new Administrators { UserIds = this.admins.UserIds };
                if (newAdmins.UserIds.Remove(user.Id))
                {
                    await this.dataStore.WriteAsync(this.dataStorePath, newAdmins);
                    this.admins = newAdmins;
                    return true;
                }
            }

            return false;
        }

        public async Task<HashSet<DiscordUser>> GetAdmins()
        {
            var sw = Stopwatch.StartNew();
            var count = this.admins.UserIds.Count;
            var arr = await Task.WhenAll(this.admins.UserIds.Select(id => this.discordClient.GetUser(id)));
            this.logger.LogInformation(
                "Asynchronously fetched DiscordUsers for {} user ids, took {}ms",
                count,
                sw.Elapsed.TotalMilliseconds);
            return arr.ToHashSet();
        }

        private static Administrators ToDataSchema(HashSet<DiscordUser> adminSet) =>
            new Administrators { UserIds = adminSet.Select(u => u.Id).ToHashSet() };

        private static async Task<HashSet<DiscordUser>> FromDataSchemaAsync(
            Administrators adminIdList,
            IDiscordClient client) => 
            (await Task.WhenAll(adminIdList.UserIds.Select(id => client.GetUser(id)))).ToHashSet();

        private string dataStorePath => Path.Join(this.guildId.ToString(), "administrators");

        private Administrators admins;

        private readonly IDataStore<AdminV3, Administrators> dataStore;

        private readonly ILogger<Membership> logger;

        private readonly ulong guildId;

        private readonly IDiscordClient discordClient;
    }
}
