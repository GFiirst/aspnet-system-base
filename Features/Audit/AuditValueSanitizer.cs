using System.Text.Json;
using System.Text.Json.Nodes;

public static class AuditValueSanitizer
{
    public const string ProtectedValue = "[PROTEGIDO]";

    private static readonly HashSet<string> SensitivePropertyNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Auth",
        "P256dh",
        "Endpoint",
        "UserAgent"
    };

    public static object? SanitizeValue(string propertyName, object? value) =>
        IsSensitive(propertyName) ? ProtectedValue : value;

    public static string? SanitizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            var node = JsonNode.Parse(json);
            SanitizeNode(node);
            return node?.ToJsonString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsSensitive(string propertyName) =>
        propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase)
        || propertyName.Contains("Credential", StringComparison.OrdinalIgnoreCase)
        || propertyName.EndsWith("Encrypted", StringComparison.OrdinalIgnoreCase)
        || propertyName.EndsWith("Hash", StringComparison.OrdinalIgnoreCase)
        || SensitivePropertyNames.Contains(propertyName);

    private static void SanitizeNode(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitive(property.Key))
                    jsonObject[property.Key] = ProtectedValue;
                else
                    SanitizeNode(property.Value);
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
                SanitizeNode(item);
        }
    }
}
