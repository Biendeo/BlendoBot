using System.Text.Json.Serialization;

namespace Weather
{
    public class WeatherConfig
    {
        [JsonPropertyName("weatherApiKey")]
        public string WeatherApiKey { get; set; }
        [JsonPropertyName("timezoneApiKey")]
        public string TimezoneApiKey { get; set; }
    }
}