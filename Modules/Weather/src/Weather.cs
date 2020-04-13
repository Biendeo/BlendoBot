using BlendoBotLib;
using BlendoBotLib.Interfaces;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Threading.Tasks;

namespace Weather {
	[CommandDefaults(defaultTerm: "weather")]
	public class Weather : ICommand {
		public string Name => "Weather";
		public string Description => "Returns the weather for a given address.";
		public string GetUsage(string term) => $"Usage: {$"{term} [location]".Code()}";
		public string Author => "Biendeo";
		public string Version => "0.5.0";

		private const string APIKeyMissingMessage = "PLEASE ADD API KEY";
        private readonly IDiscordClient discordClient;
        private readonly WeatherHttpClient weatherClient;

        private static bool IsApiKeyMissing(string apiKey) => apiKey == null || apiKey == APIKeyMissingMessage;

		public Weather(
			IDiscordClient discordClient,
			WeatherHttpClient weatherClient)
		{
            this.discordClient = discordClient;
            this.weatherClient = weatherClient;
        }

		public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			WeatherConfig config = context.Configuration
				.GetSection("Weather")
				.Get<WeatherConfig>();
			services.AddSingleton<WeatherConfig>(config);
			services.AddHttpClient<WeatherHttpClient>();
		}

		public async Task OnMessage(MessageCreateEventArgs e) {
			string[] split = e.Message.Content.Split(' ', 2);
			if (split.Length != 2) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = "Too few arguments specified",
					Channel = e.Channel,
					LogMessage = "WeatherErrorTooFewArgs"
				});
				return;
			}

			string locationInput = e.Message.Content.Split(' ', 2)[1];

			var weatherResult = await this.weatherClient.GetWeather(locationInput);

			if (weatherResult.ResultCode == 200) {
				var sb = new StringBuilder();
				sb.Append($"Weather for **{weatherResult.Location}**, {weatherResult.Country} *({weatherResult.Latitude}, {weatherResult.Longitude})*");
				sb.Append("\n");
				sb.Append($"Temperature: {weatherResult.TemperatureCurrent.Celsius}°C (low: {weatherResult.TemperatureMin.Celsius}°C, high: {weatherResult.TemperatureMax.Celsius}°C)");
				sb.Append("\n");
				sb.Append($"Current condition: {weatherResult.Condition}");
				sb.Append("\n");
				sb.Append($"Pressure: {weatherResult.PressureHPA}hPa");
				sb.Append("\n");
				sb.Append($"Wind: {weatherResult.WindSpeed}kmh at {weatherResult.WindDirection}°T");
				sb.Append("\n");
				sb.Append($"Sunrise: {weatherResult.Sunrise.ToString("hh:mm:ss tt")}, Sunset: {weatherResult.Sunset.ToString("hh:mm:ss tt")} *({weatherResult.TimeZone})*");
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "WeatherSuccess"
				});
			} else {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"API returned a bad error ({weatherResult.ResultCode}): *{weatherResult.ResultMessage}*",
					Channel = e.Channel,
					LogMessage = "WeatherErrorAPINotOK"
				});
			}
		}
	}
}
