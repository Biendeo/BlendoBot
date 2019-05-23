using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BlendoBotLib;

namespace AutoCorrect
{
    public class GoogleSearchAutoCorrectProvider : IAutoCorrectProvider
    {
        public GoogleSearchAutoCorrectProvider()
        {
            this.httpClient = new HttpClient();
        }

        public async Task<string> CorrectAsync(string input)
        {
            try
            {
                var uri = new Uri($"http://www.google.com/search?output=toolbar&q={input}");
                var response = await this.httpClient.GetAsync(uri).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Everyone loves parsing HTML
                var markerFront = new[] { @"Did you mean:</span>" };
                var correctionMarkerFront = new[] { @">" };
                var markerBack = new[] { @"</a>" };

                var htmlFrontTrimmedToSentinel = html
                        .Split(markerFront, 2, StringSplitOptions.None)[1]
                        .Trim();
                var sentinelToFrontLength = htmlFrontTrimmedToSentinel
                        .Split(correctionMarkerFront, 2, StringSplitOptions.None)[0]
                        .Length;
                var htmlFrontTrimmed = htmlFrontTrimmedToSentinel
                        .Substring(sentinelToFrontLength + 1);
                var backTrimLength = htmlFrontTrimmed
                        .Split(markerBack, StringSplitOptions.None)
                        .Last()
                        .Length;
                var htmlTrimmed = htmlFrontTrimmed
                        .Substring(0, htmlFrontTrimmed.Length - backTrimLength);

                var output = htmlTrimmed
                        .Replace(@"<b><i>", string.Empty, true, CultureInfo.InvariantCulture)
                        .Replace(@"</i></b>", string.Empty, true, CultureInfo.InvariantCulture);

                return output;
            }
            catch (Exception ex)
            {
                // TODO log something
                Methods.Log(null, new LogEventArgs
                    {
                        Type = LogType.Error,
                        Message = $"Exception occurred in GoogleSearchAutoCorrectProvider.CorrectAsync: {ex}"
                    });
                return string.Empty;
            }
        }

        public void Dispose()
        {
            this.httpClient?.Dispose();
        }

        private HttpClient httpClient { get; }
    }
}