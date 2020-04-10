namespace BlendoBotLib.Services
{
    using System.IO;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;

    public class GuildInstancedDataStoreService<TConsumer> : IInstancedDataStore<TConsumer>
    {
        public GuildInstancedDataStoreService(IDataStore<TConsumer> dataStore)
        {
            this.dataStore = dataStore;
        }

        public Task<T> ReadAsync<T>(ulong guildId, string path) =>
            this.dataStore.ReadAsync<T>(Path.Join(guildId.ToString(), path));

        public Task WriteAsync<T>(ulong guildId, string path, T value) =>
            this.dataStore.WriteAsync<T>(Path.Join(guildId.ToString(), path), value);

        private IDataStore<TConsumer> dataStore;
    }
}
