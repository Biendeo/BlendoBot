using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BlendoBotLib;
using Newtonsoft.Json;

namespace CurrencyConverter
{
    public class AlphavantageClient
    {
        private readonly HttpClient httpClient;
        private readonly CurrencyConverterConfig config;

        public AlphavantageClient(HttpClient httpClient, CurrencyConverterConfig config)
        {
            this.httpClient = httpClient;
            this.config = config;
        }

        public async Task<string> GetCurrencyMessage(double amount, string fromCurrency, IEnumerable<string> toCurrencies)
        {
			int foundMatches = 0;
			var failedMatches = new List<string>();

			var sb = new StringBuilder();

			foreach (var toCurrency in toCurrencies) {
				string convertJsonString = await httpClient.GetStringAsync($"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={fromCurrency}&to_currency={toCurrency}&apikey={this.config.ApiKey}");
				dynamic convertJson = JsonConvert.DeserializeObject(convertJsonString);
				try {
					double rate = convertJson["Realtime Currency Exchange Rate"]["5. Exchange Rate"];
					if (foundMatches == 0) {
						sb.AppendLine($"{amount.ToString("0.00000").Substring(0, 7).Code()} - {convertJson["Realtime Currency Exchange Rate"]["1. From_Currency Code"]} ({((string)convertJson["Realtime Currency Exchange Rate"]["2. From_Currency Name"]).Italics()})");
					}
					sb.AppendLine($"{(amount * rate).ToString("0.00000").Substring(0, 7).Code()} - {convertJson["Realtime Currency Exchange Rate"]["3. To_Currency Code"]} ({((string)convertJson["Realtime Currency Exchange Rate"]["4. To_Currency Name"]).Italics()})");
					++foundMatches;
				} catch (Exception) {
					// Unsuccessful, next one.
					failedMatches.Add(toCurrency);
				}
			}

			if (failedMatches.Count > 0) {
				sb.Append("Failed to match the currency codes: ");
				foreach (string failedCode in failedMatches) {
					sb.Append($"{failedCode.Code()} ");
				}
			}

            return sb.ToString();
        }
    }
}