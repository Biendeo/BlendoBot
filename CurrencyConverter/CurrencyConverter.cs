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

		private static readonly string ConfigPath = "blendobot-currency-config.json";
		private static string CurrencyConverterAPIKey = "";

		private static readonly CommandProps properties = new CommandProps {
			Term = "?currency",
			Name = "Currency Converter",
			Description = "Returns the conversion rate between two currencies.",
			Usage = $"Usage: {"?currency [value] [from currency code] [to currency code] ...".Code()}\nYou can write several currencies, and a conversion will be listed for each one.",
			Author = "Biendeo",
			Version = "0.1.0",
			Func = CurrencyConvertCommand,
		};

		private static void LoadConfig() {
			if (!File.Exists(ConfigPath)) {
				Methods.Log(null, new LogEventArgs {
					Type = LogType.Error,
					Message = $"BlendoBot Currency Converter cannot find the config at {ConfigPath}"
				});
				return;
			}
			dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(ConfigPath));
			CurrencyConverterAPIKey = json.CurrencyConverterAPIKey;
		}

		public static async Task CurrencyConvertCommand(MessageCreateEventArgs e) {
			if (CurrencyConverterAPIKey == "") {
				LoadConfig();
			}

			string[] splitInput = e.Message.Content.Split(' ');


			if (splitInput.Length < 4) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Too few arguments specified to {"?currency".Code()}",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorTooFewArgs"
				});
				return;
			}

			double amount = double.Parse(splitInput[1]);
			string fromCurrency = splitInput[2];
			int foundMatches = 0;
			var failedMatches = new List<string>();

			var sb = new StringBuilder();

			for (int i = 3; i < splitInput.Length; ++i) {
				using (var wc = new WebClient()) {
					string convertJsonString = await wc.DownloadStringTaskAsync($"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={fromCurrency}&to_currency={splitInput[i]}&apikey={CurrencyConverterAPIKey}");
					dynamic convertJson = JsonConvert.DeserializeObject(convertJsonString);
					try {
						double rate = convertJson["Realtime Currency Exchange Rate"]["5. Exchange Rate"];
						if (foundMatches == 0) {
							sb.AppendLine($"{amount} - {convertJson["Realtime Currency Exchange Rate"]["1. From_Currency Code"]} ({((string)convertJson["Realtime Currency Exchange Rate"]["2. From_Currency Name"]).Italics()})");
						}
						sb.AppendLine($"{amount * rate} - {convertJson["Realtime Currency Exchange Rate"]["3. To_Currency Code"]} ({((string)convertJson["Realtime Currency Exchange Rate"]["4. To_Currency Name"]).Italics()})");
						++foundMatches;
					} catch {
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
