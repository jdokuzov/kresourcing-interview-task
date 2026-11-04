using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claims.Contracts;

/// <summary>
/// Reads every incoming date as UTC; a date without an offset is taken to already be UTC.
/// Without this, MongoDB would treat such a date as local server time and shift it on save.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        return value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
