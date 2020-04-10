namespace BlendoBotLib.Interfaces
{
    using System.Threading.Tasks;

    public interface IInstancedDataStore<TConsumer>
    {
        Task<T> ReadAsync<T>(ulong guildId, string path);

        Task WriteAsync<T>(ulong guildId, string path, T value);
    }
}
