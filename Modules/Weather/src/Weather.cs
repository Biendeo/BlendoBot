using BlendoBotLib;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Weather {
	public class Weather : CommandBase {
		public Weather(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string Term => "?weather";
		public override string Name => "Weather";
		public override string Description => "Returns the weather for a given address.\nUsage: ?weather [location]";
		public override string Usage => $"Usage: {"?weather [location]".Code()}";
		public override string Author => "Biendeo";
		public override string Version => "0.1.0";

		private const string APIKeyMissingMessage = "PLEASE ADD API KEY";
		private static bool IsApiKeyMissing(string apiKey) => apiKey == null || apiKey == APIKeyMissingMessage;

		private string WeatherAPIKey {
			get {
				string key = BotMethods.ReadConfig(this, Name, "WeatherApiKey");
				return key ?? null;
			}
		}
		private string TimezoneAPIKey {
			get {
				string key = BotMethods.ReadConfig(this, Name, "TimezoneApiKey");
				return key ?? null;
			}
		}

		public override async Task<bool> Startup() {
			await Task.Delay(0);
			return LoadConfig();
		}

		private bool LoadConfig() {
			if (IsApiKeyMissing(WeatherAPIKey) || IsApiKeyMissing(TimezoneAPIKey)) {
				if (IsApiKeyMissing(WeatherAPIKey)) {
					BotMethods.WriteConfig(this, Name, "WeatherApiKey", APIKeyMissingMessage);
				}
				if (IsApiKeyMissing(TimezoneAPIKey)) {
					BotMethods.WriteConfig(this, Name, "TimezoneApiKey", APIKeyMissingMessage);
				}
				BotMethods.Log(this, new LogEventArgs {
					Type = LogType.Error,
					Message = $"BlendoBot Weather has not been supplied all the necessary API keys! Please acquire a weather API key from https://openweathermap.org/api, and a timezone API key from https://timezonedb.com/. Then, add both to the config under the [{Name}] section."
				});
				return false;
			}
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			if (e.Message.Content.Length < 9) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = "Too few arguments specified to `?weather`",
					Channel = e.Channel,
					LogMessage = "WeatherErrorTooFewArgs"
				});
				return;
			}

			string locationInput = e.Message.Content.Substring($"{Term} ".Length);

			var weatherResult = await GetWeather(locationInput);

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
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "WeatherSuccess"
				});
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"API returned a bad error ({weatherResult.ResultCode}): *{weatherResult.ResultMessage}*",
					Channel = e.Channel,
					LogMessage = "WeatherErrorAPINotOK"
				});
			}

			await Task.Delay(0);
		}

		private async Task<WeatherResult> GetWeather(string inputLocation) {
			string weatherJsonString = "";
			try {
				using var wc = new WebClient();
				weatherJsonString = await wc.DownloadStringTaskAsync($"https://api.openweathermap.org/data/2.5/weather?q={inputLocation.Replace(" ", "+")}&type=like&mode=json&APPID={WeatherAPIKey}");
			} catch (WebException) {
				return new WeatherResult {
					ResultCode = 404,
					ResultMessage = $"city not found"
				};
			}
			dynamic weatherJson = JsonConvert.DeserializeObject(weatherJsonString);
			if (weatherJson.cod == 200) {
				string timezoneJsonString = "";
				using (var wc = new WebClient()) {
					timezoneJsonString = await wc.DownloadStringTaskAsync($"http://api.timezonedb.com/v2.1/get-time-zone?key={TimezoneAPIKey}&format=json&by=position&lat={weatherJson.coord.lat}&lng={weatherJson.coord.lon}");
				}
				//TODO: Probably check that this call worked.
				dynamic timezoneJson = JsonConvert.DeserializeObject(timezoneJsonString);
				return new WeatherResult {
					ResultCode = weatherJson.cod,
					ResultMessage = weatherJson.message,
					Condition = weatherJson.weather[0].main,
					TemperatureCurrent = Temperature.FromKelvin((decimal)weatherJson.main.temp),
					PressureHPA = weatherJson.main.pressure,
					TemperatureMin = Temperature.FromKelvin((decimal)weatherJson.main.temp_min),
					TemperatureMax = Temperature.FromKelvin((decimal)weatherJson.main.temp_max),
					WindSpeed = weatherJson.wind.speed,
					WindDirection = weatherJson.wind.deg,
					Sunrise = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)weatherJson.sys.sunrise).AddSeconds((double)timezoneJson.gmtOffset),
					Sunset = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)weatherJson.sys.sunset).AddSeconds((double)timezoneJson.gmtOffset),
					Location = weatherJson.name,
					Country = weatherJson.sys.country,
					Latitude = (decimal)weatherJson.coord.lat,
					Longitude = (decimal)weatherJson.coord.lon,
					TimeZone = $"UTC{(timezoneJson.gmtOffset >= 0 ? "+" : "")}{((int)timezoneJson.gmtOffset / 3600).ToString("00")}:{((int)timezoneJson.gmtOffset / 60 % 60).ToString("00")}"
				};
			} else {
				return new WeatherResult {
					ResultCode = weatherJson.cod,
					ResultMessage = weatherJson.message
				};
			}
		}
	}
}
