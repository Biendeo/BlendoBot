namespace BlendoBotLib.Interfaces
{
    using System.Threading.Tasks;

    public interface IDataStore<TConsumer, TData>
    {
        Task<TData> ReadAsync(string path);

        Task WriteAsync(string path, TData value);
    }
}
