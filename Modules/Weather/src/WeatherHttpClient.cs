namespace Weather
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class WeatherHttpClient
    {
        private readonly HttpClient httpClient;
        private readonly WeatherConfig config;

        public WeatherHttpClient(HttpClient httpClient, WeatherConfig config)
        {
            this.httpClient = httpClient;
            this.config = config;
        }

        internal async Task<WeatherResult> GetWeather(string inputLocation) {
			int resultCode = 200;
			string weatherJsonString = "";
			try {
				using (var response = await httpClient.GetAsync($"https://api.openweathermap.org/data/2.5/weather?q={inputLocation.Replace(" ", "+")}&type=like&mode=json&APPID={this.config.WeatherApiKey}"))
				{
					resultCode = (int)response.StatusCode;
					response.EnsureSuccessStatusCode();
					weatherJsonString = await response.Content.ReadAsStringAsync();
				}
			} catch (HttpRequestException ex) {
				return new WeatherResult {
					ResultCode = resultCode,
					ResultMessage = ex.Message
				};
			}
			dynamic weatherJson = JsonConvert.DeserializeObject<dynamic>(weatherJsonString);
			if (weatherJson.cod == 200) {
				string timezoneJsonString = "";
				using (var response = await httpClient.GetAsync($"http://api.timezonedb.com/v2.1/get-time-zone?key={this.config.TimezoneApiKey}&format=json&by=position&lat={weatherJson.coord.lat}&lng={weatherJson.coord.lon}"))
				{
					resultCode = (int)response.StatusCode;
					response.EnsureSuccessStatusCode();
					timezoneJsonString = await response.Content.ReadAsStringAsync();
				}

				dynamic timezoneJson = JsonConvert.DeserializeObject<dynamic>(timezoneJsonString);
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
