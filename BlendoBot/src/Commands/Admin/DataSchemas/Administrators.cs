namespace BlendoBot.Commands.Admin.DataSchemas
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

#pragma warning disable CS8618

    public class Administrators
    {
        [JsonPropertyName("userIds")]
        public HashSet<ulong> UserIds { get; set; }
    }

#pragma warning restore CS8618

}