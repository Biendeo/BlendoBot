namespace WheelOfFortune
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    public class PuzzlesTxtDataStore : IDataStore<WheelOfFortune>
    {
        public PuzzlesTxtDataStore(
            ILogger<PuzzlesTxtDataStore> logger)
        {
            this.logger = logger;
        }

        public async Task<T> ReadAsync<T>(string path)
        {
            if (!typeof(IList<Puzzle>).IsAssignableFrom(typeof(T)))
            {
                var ex = new NotImplementedException();
                this.logger.LogCritical(
                    ex,
                    "{} can only read types compatible with {}",
                    this.GetType().Name,
                    typeof(IList<Puzzle>).Name);
                throw ex;
            }

            var fullpath = Path.Join("data", typeof(WheelOfFortune).Name, path);
            this.logger.LogInformation("Reading from {}", fullpath);
            using (var istream = File.OpenRead(fullpath))
            using (var reader = new StreamReader(istream))
            {
                var puzzles = new List<Puzzle>();
                while (!reader.EndOfStream)
                {
                    if ((await reader.ReadLineAsync())?.Split(";") is string[] split)
                    {
                        puzzles.Add(new Puzzle
                        {
                            Category = split[0],
                            Phrase = split[1]
                        });
                    }
                }

                return (T)(object)puzzles;
            }
        }

        public Task WriteAsync<T>(string path, T value)
        {
            throw new System.NotImplementedException();
        }

        private readonly ILogger<PuzzlesTxtDataStore> logger;
    }
}