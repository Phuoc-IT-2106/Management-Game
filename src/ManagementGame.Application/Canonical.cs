using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ManagementGame.Domain;

namespace ManagementGame.Application;

public static class Canonical
{
    public const string Version = "campaign-canonical-v1";
    public static string Hash(Campaign campaign) => Digest(Version + "\n" + Json(campaign));
    public static string Digest(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    public static string Json<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer)) Write(writer, document.RootElement);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                { writer.WritePropertyName(property.Name); Write(writer, property.Value); }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                // Collection order is explicitly established by owners; semantic sequences retain order.
                foreach (var item in element.EnumerateArray()) Write(writer, item);
                writer.WriteEndArray(); break;
            case JsonValueKind.String: writer.WriteStringValue(element.GetString()!.Normalize(NormalizationForm.FormC)); break;
            default: element.WriteTo(writer); break;
        }
    }
}
