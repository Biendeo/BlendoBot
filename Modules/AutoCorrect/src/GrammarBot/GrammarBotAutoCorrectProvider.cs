namespace AutoCorrect.GrammarBot
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AutoCorrect.Schemas;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class GrammarBotAutoCorrectProvider : IAutoCorrectProvider
    {
        private const string endpoint = @"http://api.grammarbot.io/v2/check";

        public GrammarBotAutoCorrectProvider(
            HttpClient httpClient,
            GrammarBotConfig? config,
            ILogger<GrammarBotAutoCorrectProvider> logger)
        {
            this.apiKey = config?.ApiKey;
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<string> CorrectAsync(string input)
        {
            try
            {
                string escaped = Uri.EscapeDataString(input);
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
                this.logger.LogError(ex, "Exception occurred in CorrectAsync");
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
                this.httpClient?.Dispose();
            }
        }

        private HttpClient httpClient;

        private readonly string? apiKey;

        private ILogger<GrammarBotAutoCorrectProvider> logger;
    }
}
