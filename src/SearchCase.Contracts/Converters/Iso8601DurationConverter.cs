using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace SearchCase.Contracts.Converters;

/// <summary>
/// JSON converter for TimeSpan to/from ISO 8601 duration format (e.g., PT22M45S)
/// </summary>
public sealed class Iso8601DurationConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return TimeSpan.Zero;
        }

        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid ISO 8601 duration format: {value}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        var iso8601Duration = XmlConvert.ToString(value);
        writer.WriteStringValue(iso8601Duration);
    }
}
