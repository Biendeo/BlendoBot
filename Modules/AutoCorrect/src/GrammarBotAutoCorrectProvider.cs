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
            this.httpClient = new HttpClient();
            this.apiKey = apiKey;
            this.botMethods = botMethods;
        }

        public async Task<string> CorrectAsync(string input)
        {
            try
            {
                var escaped = Uri.EscapeDataString(input);
                string uriString;
                if (!string.IsNullOrEmpty(this.apiKey))
                {
                    uriString = $"{endpoint}?api_key={this.apiKey}&language=en-AU&text={escaped}";
                }
                else
                {
                    uriString = $"{endpoint}?language=en-AU&text={escaped}";
                }

                var uri = new Uri(uriString);
                var httpResponse = await this.httpClient.GetAsync(uri).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                var responseJson = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var response = JsonConvert.DeserializeObject<GrammarBotResponse>(responseJson);

                // Incrementally replace matches from the input string with their replacements
                var outputString = input;
                foreach (var match in response.matches)
                {
                    if (match.replacements.Count == 0) continue;
                    var bestReplacement = match.replacements[0].value;
                    var substringToReplace = input.Substring(match.offset, match.length);
                    var offsetInOutputString = outputString.IndexOf(substringToReplace);
                    outputString = outputString.Substring(0, offsetInOutputString)
                                    + bestReplacement
                                    + outputString.Substring(offsetInOutputString + substringToReplace.Length);
                }

                return outputString;
            }
            catch (Exception ex)
            {
                botMethods.Log(this, new LogEventArgs
                    {
                        Type = LogType.Error,
                        Message = $"Exception occurred in GrammarBotAutoCorrectProvider.CorrectAsync: {ex}"
                    });
                return string.Empty;
            }
        }

        public void Dispose()
        {
            this.httpClient?.Dispose();
        }

        private HttpClient httpClient { get; }

        private string apiKey { get; }

        private IBotMethods botMethods { get; }
    }
}