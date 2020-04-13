using BlendoBotLib;
using BlendoBotLib.Interfaces;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyConverter {
	[CommandDefaults(defaultTerm: "currency")]
	public class CurrencyConverter : ICommand {
		public string Name => "Currency Converter";
		public string Description => "Returns the conversion rate between two currencies.";
		public string GetUsage(string term) => $"Usage: {$"{term} [value] [from currency code] [to currency code] ...".Code()}\nYou can write several currencies, and a conversion will be listed for each one.";
		public string Author => "Biendeo";
		public string Version => "0.5.0";

		public CurrencyConverter(
			IDiscordClient discordClient,
			AlphavantageClient client,
			CurrencyConverterConfig config)
		{
            this.discordClient = discordClient;
            this.client = client;
        }

		public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			var config = context.Configuration
				.GetSection("CurrencyConverter")
				.Get<CurrencyConverterConfig>();
			services.AddSingleton<CurrencyConverterConfig>(config);
			services.AddHttpClient<AlphavantageClient>();
		}

		public async Task OnMessage(MessageCreateEventArgs e) {

			string[] splitInput = e.Message.Content.Split(' ');

			if (splitInput.Length < 4) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"Too few arguments specified!",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorTooFewArgs"
				});
				return;
			}

			if (!double.TryParse(splitInput[1], out double amount)) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"Incorrect input: the currency value supplied was not a number!",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorNonNumericValue"
				});
				return;
			}

			if (amount < 0.0) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"Incorrect input: the currency value supplied was less than 0!",
					Channel = e.Channel,
					LogMessage = "CurrencyErrorNegativeValue"
				});
				return;
			}

			string fromCurrency = splitInput[2];
			var toCurrencies = splitInput.Skip(3);
			var message = await this.client.GetCurrencyMessage(amount, fromCurrency, toCurrencies);

			await this.discordClient.SendMessage(this, new SendMessageEventArgs {
				Message = message,
				Channel = e.Channel,
				LogMessage = "CurrencySuccess"
			});
		}

		private readonly AlphavantageClient client;
        private readonly IDiscordClient discordClient;
    }
}
