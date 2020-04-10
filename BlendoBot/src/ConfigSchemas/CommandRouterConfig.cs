namespace BlendoBot.ConfigSchemas
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

#pragma warning disable CS8618

    public class CommandRouterConfig
    {
        [JsonPropertyName("commands")]
        public List<CommandConfig> Commands { get; set; }
    }

    public class CommandConfig
    {
        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("term")]
        public string Term { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }

#pragma warning restore CS8618
}
