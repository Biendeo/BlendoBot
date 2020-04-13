namespace CurrencyConverter
{
    using System.Text.Json.Serialization;

    public class CurrencyConverterConfig
    {
        [JsonPropertyName("apikey")]
        public string ApiKey { get; set; }
    }
}