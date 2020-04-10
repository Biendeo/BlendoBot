namespace BlendoBotLib.DataStore
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    public class JsonFileDataStore<TConsumer> : IDataStore<TConsumer>
    {
        public JsonFileDataStore(
            ILogger<JsonFileDataStore<TConsumer>> logger)
        {
            this.logger = logger;
            this.path = "data";
        }

        public async Task<T> ReadAsync<T>(string path)
        {
            var fullpath = Path.ChangeExtension(Path.Join(this.path, typeof(TConsumer).Name, path), "json");
            this.logger.LogInformation("Reading from {}", fullpath);
            using (var istream = File.OpenRead(fullpath))
            {
                var obj = await JsonSerializer.DeserializeAsync<T>(istream);
                return obj;
            }
        }

        public async Task WriteAsync<T>(string path, T value)
        {
            var fullpath = Path.ChangeExtension(Path.Join(this.path, typeof(TConsumer).Name, path), "json");
            this.logger.LogInformation("Writing to {}", fullpath);
            Directory.CreateDirectory(Directory.GetParent(fullpath).ToString());
            using (var ostream = File.OpenWrite(fullpath))
            {
                await JsonSerializer.SerializeAsync(ostream, value);
            }
        }

        private ILogger<JsonFileDataStore<TConsumer>> logger;

        private string path;
    }
}
