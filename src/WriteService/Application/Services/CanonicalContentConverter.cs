using System.Text.Json;
using System.Text.Json.Serialization;
using SearchCase.Contracts.Models;

namespace WriteService.Application.Services;

/// <summary>
/// Custom JSON converter for CanonicalContent that handles type discrimination manually
/// </summary>
public class CanonicalContentConverter : JsonConverter<CanonicalContent>
{
    public override CanonicalContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Get the type field
        if (!root.TryGetProperty("type", out var typeElement))
        {
            throw new JsonException("Missing 'type' property");
        }

        var typeStr = typeElement.GetString()?.ToLowerInvariant();

        // Create new options WITHOUT this converter to avoid recursion
        var newOptions = new JsonSerializerOptions(options);
        newOptions.Converters.Remove(this);

        // Deserialize to the correct concrete type
        var json = root.GetRawText();
        return typeStr switch
        {
            "video" => JsonSerializer.Deserialize<CanonicalVideoContent>(json, newOptions),
            "article" => JsonSerializer.Deserialize<CanonicalArticleContent>(json, newOptions),
            _ => throw new JsonException($"Unknown content type: {typeStr}")
        };
    }

    public override void Write(Utf8JsonWriter writer, CanonicalContent value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}