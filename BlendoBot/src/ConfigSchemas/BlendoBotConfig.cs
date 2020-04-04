namespace BlendoBot.ConfigSchemas
{
    using System.Text.Json.Serialization;
    using DSharpPlus.Entities;

    public class BlendoBotConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("activityName")]
        public string ActivityName { get; set; }

        [JsonPropertyName("activityType")]
        public ActivityType ActivityType { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("prefix")]
        public string Prefix { get; set; }
    }
}
