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

namespace Weather {
	public class Weather : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly string ConfigPath = "blendobot-weather-config.json";
		private static string WeatherAPIKey = "";
		private static string TimezoneAPIKey = "";

		private static readonly CommandProps properties = new CommandProps {
			Term = "?weather",
			Name = "Weather",
			Description = "Returns the weather for a given address.\nUsage: ?weather [location]",
			Usage = $"Usage: {"?weather [location]".Code()}",
			Author = "Biendeo",
			Version = "0.1.0",
			Startup = async () => { await Task.Delay(0); return true; },
			OnMessage = WeatherCommand,
		};

		private static bool Startup() {
			return LoadConfig();
		}

		private static bool LoadConfig() {
			if (!File.Exists(ConfigPath)) {
				Methods.Log(null, new LogEventArgs {
					Type = LogType.Error,
					Message = $"BlendoBot Weather cannot find the config at {ConfigPath}"
				});
				return false;
			}
			dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(ConfigPath));
			WeatherAPIKey = json.WeatherAPIKey;
			TimezoneAPIKey = json.TimezoneDBAPIKey;
			return true;
		}

		public static async Task WeatherCommand(MessageCreateEventArgs e) {
			if (WeatherAPIKey == "" || TimezoneAPIKey == "") {
				LoadConfig();
			}

			if (e.Message.Content.Length < 9) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = "Too few arguments specified to `?weather`",
					Channel = e.Channel,
					LogMessage = "WeatherErrorTooFewArgs"
				});
				return;
			}

			string locationInput = e.Message.Content.Substring($"{properties.Term} ".Length);

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
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "WeatherSuccess"
				});
			} else {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"API returned a bad error ({weatherResult.ResultCode}): *{weatherResult.ResultMessage}*",
					Channel = e.Channel,
					LogMessage = "WeatherErrorAPINotOK"
				});
			}

			await Task.Delay(0);
		}

		private static async Task<WeatherResult> GetWeather(string inputLocation) {
			string weatherJsonString = "";
			try {
				using (var wc = new WebClient()) {
					weatherJsonString = await wc.DownloadStringTaskAsync($"https://api.openweathermap.org/data/2.5/weather?q={inputLocation.Replace(" ", "+")}&type=like&mode=json&APPID={WeatherAPIKey}");
				}
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
