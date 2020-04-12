namespace WheelOfFortune
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using BlendoBotLib.Interfaces;
    using Microsoft.Extensions.Logging;

    public class PuzzlesTxtDataStore : IDataStore<WheelOfFortune, IList<Puzzle>>
    {
        public PuzzlesTxtDataStore(
            ILogger<PuzzlesTxtDataStore> logger)
        {
            this.logger = logger;
        }

        public async Task<IList<Puzzle>> ReadAsync(string path)
        {
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

                return puzzles;
            }
        }

        public Task WriteAsync(string path, IList<Puzzle> value)
        {
            throw new System.NotImplementedException();
        }

        private readonly ILogger<PuzzlesTxtDataStore> logger;
    }
}