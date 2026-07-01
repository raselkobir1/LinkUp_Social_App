using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkUp.Modules.VideoCall.DTOs;

/// <summary>
/// WebRTC's ICE-server `urls` may be a single string or an array. Metered returns a
/// string; the WebRTC client accepts an array. This reads either form into a list and
/// always writes an array, so the shape the browser receives is consistent.
/// </summary>
public class StringOrArrayConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return [reader.GetString()!];

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                list.Add(reader.GetString()!);
            return list;
        }

        return [];
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var url in value)
            writer.WriteStringValue(url);
        writer.WriteEndArray();
    }
}
