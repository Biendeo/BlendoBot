namespace BlendoBotLib.DataStore
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    public class JsonFileDataStore<TConsumer, TData> : IDataStore<TConsumer, TData>
    {
        public JsonFileDataStore(
            ILogger<JsonFileDataStore<TConsumer, TData>> logger)
        {
            this.logger = logger;
            this.path = "data";
        }

        public async Task<TData> ReadAsync(string path)
        {
            var fullpath = Path.ChangeExtension(Path.Join(this.path, typeof(TConsumer).Name, path), "json");
            this.logger.LogInformation("Reading from {}", fullpath);
            using (var istream = File.OpenRead(fullpath))
            {
                var obj = await JsonSerializer.DeserializeAsync<TData>(istream);
                return obj;
            }
        }

        public async Task WriteAsync(string path, TData value)
        {
            var fullpath = Path.ChangeExtension(Path.Join(this.path, typeof(TConsumer).Name, path), "json");
            this.logger.LogInformation("Writing to {}", fullpath);
            Directory.CreateDirectory(Directory.GetParent(fullpath).ToString());
            using (var ostream = new FileStream(fullpath, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(ostream, value, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        private ILogger<JsonFileDataStore<TConsumer, TData>> logger;

        private string path;
    }
}
