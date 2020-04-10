namespace BlendoBotLib.Interfaces
{
    using System.Threading.Tasks;

    public interface IDataStore<TConsumer>
    {
        Task<T> ReadAsync<T>(string path);

        Task WriteAsync<T>(string path, T value);
    }
}
