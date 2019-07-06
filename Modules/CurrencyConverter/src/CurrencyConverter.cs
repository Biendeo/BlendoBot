using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter {
	public class CurrencyConverter : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?currency",
			Name = "Currency Converter",
			Description = "Returns the conversion rate between two currencies.",
			Usage = $"Usage: {"?currency [value] [from currency code] [to currency code] ...".Code()}\nYou can write several currencies, and a conversion will be listed for each one.",
			Author = "Biendeo",
			Version = "0.1.1",
			Startup = Startup,
			OnMessage = CurrencyConvertCommand,
		};

		private static async Task<bool> Startup() {
			await Task.Delay(0);
			return LoadConfig();
		}

		private static bool LoadConfig() {
			if (!Methods.DoesKeyExist(null, properties.Name, "ApiKey") || Methods.ReadConfig(null, properties.Name, "ApiKey") == "PLEASE ADD YOUR API KEY") {
				if (!Methods.DoesKeyExist(null, properties.Name, "ApiKey")) {
					Methods.WriteConfig(null, properties.Name, "ApiKey", "PLEASE ADD YOUR API KEY");
				}
				Methods.Log(null, new LogEventArgs {
					Type = LogType.Error,
					Message = $"BlendoBot Currency Converter has not been supplied a valid API key! Please acquire a key from https://www.alphavantage.co/ and add it in the config under the [{properties.Name}] section."
				});
				return false;
			}
			return true;
		}

		public static async Task CurrencyConvertCommand(MessageCreateEventArgs e) {

			string[] splitInput = e.Message.Content.Split(' ');

			if (splitInput.Length < 4) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Too few arguments specified to {"?currency".Code()}",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorTooFewArgs"
				});
				return;
			}

			if (!double.TryParse(splitInput[1], out double amount)) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Incorrect input: the currency value supplied was not a number!",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorNonNumericValue"
				});
				return;
			}

			if (amount < 0.0) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Incorrect input: the currency value supplied was less than 0!",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorNegativeValue"
				});
				return;
			}

			string fromCurrency = splitInput[2];
			int foundMatches = 0;
			var failedMatches = new List<string>();

			var sb = new StringBuilder();

			for (int i = 3; i < splitInput.Length; ++i) {
				using (var wc = new WebClient()) {
					string convertJsonString = await wc.DownloadStringTaskAsync($"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={fromCurrency}&to_currency={splitInput[i]}&apikey={Methods.ReadConfig(null, properties.Name, "ApiKey")}");
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
						failedMatches.Add(splitInput[i]);
					}
				}
			}

			if (failedMatches.Count > 0) {
				sb.Append("Failed to match the currency codes: ");
				foreach (string failedCode in failedMatches) {
					sb.Append($"{failedCode.Code()} ");
				}
			}

			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = sb.ToString(),
				Channel = e.Channel,
				LogMessage = "CurrencySuccess"
			});

			await Task.Delay(0);
		}
	}
}
