namespace UserTimeZone
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class TimeZoneInfoConverter : JsonConverter<TimeZoneInfo>
    {
        public override TimeZoneInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeZoneInfo.FromSerializedString(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToSerializedString());
        }
    }
}
