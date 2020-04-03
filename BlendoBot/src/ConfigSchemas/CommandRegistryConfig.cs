namespace BlendoBot.ConfigSchemas
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class CommandRegistryConfig
    {
        [JsonPropertyName("verbMapping")]
        public Dictionary<string, string> VerbMapping { get; set; }
    }
}
