using System;
using System.Net.Http;
using System.Threading.Tasks;
using AutoCorrect.Schemas;
using BlendoBotLib;
using Newtonsoft.Json;

namespace AutoCorrect
{
    public class GrammarBotAutoCorrectProvider : IAutoCorrectProvider
    {
        private const string endpoint = @"http://api.grammarbot.io/v2/check";

        public GrammarBotAutoCorrectProvider(IBotMethods botMethods, string apiKey = null)
        {
            this.HttpClient = new HttpClient();
            this.ApiKey = apiKey;
            this.BotMethods = botMethods;
        }

        public async Task<string> CorrectAsync(string input)
        {
            try
            {
                string escaped = Uri.EscapeDataString(input);
                string uriString;
                if (!string.IsNullOrEmpty(this.ApiKey))
                {
                    uriString = $"{endpoint}?api_key={this.ApiKey}&language=en-AU&text={escaped}";
                }
                else
                {
                    uriString = $"{endpoint}?language=en-AU&text={escaped}";
                }

                var uri = new Uri(uriString);
                var httpResponse = await this.HttpClient.GetAsync(uri).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string responseJson = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var response = JsonConvert.DeserializeObject<GrammarBotResponse>(responseJson);

                // Incrementally replace matches from the input string with their replacements
                string outputString = input;
                foreach (var match in response.matches)
                {
                    if (match.replacements.Count == 0) continue;
                    string bestReplacement = match.replacements[0].value;
                    string substringToReplace = input.Substring(match.offset, match.length);
                    int offsetInOutputString = outputString.IndexOf(substringToReplace);
                    outputString = outputString.Substring(0, offsetInOutputString)
                                    + bestReplacement
                                    + outputString.Substring(offsetInOutputString + substringToReplace.Length);
                }

                return outputString;
            }
            catch (Exception ex)
            {
                BotMethods.Log(this, new LogEventArgs
                    {
                        Type = LogType.Error,
                        Message = $"Exception occurred in GrammarBotAutoCorrectProvider.CorrectAsync: {ex}"
                    });
                return string.Empty;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.HttpClient?.Dispose();
            }
        }

        private HttpClient HttpClient { get; }

        private string ApiKey { get; }

        private IBotMethods BotMethods { get; }
    }
}