namespace UserTimeZone
{
    using System;
    using System.Text.Json.Serialization;

    public class UserTimeZoneSchema
    {
        [JsonPropertyName("userId")]
        public ulong UserId { get; set; }

        [JsonPropertyName("timeZoneInfo")]
        [JsonConverter(typeof(TimeZoneInfoConverter))]
        public TimeZoneInfo TimeZoneInfo { get; set; }
    }
}