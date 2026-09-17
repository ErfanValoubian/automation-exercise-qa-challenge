using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Challenge.Support.Diagnostics;

public sealed class Redactor
{
    private readonly HashSet<string> _values = new(StringComparer.Ordinal);
    private static readonly HashSet<string> PrivateKeys = new(StringComparer.OrdinalIgnoreCase)
    { "password", "email", "card_number", "card-number", "cvc", "mobile_number", "address1", "address2" };

    public void Register(params string[] values)
    {
        lock (_values)
            foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
                _values.Add(value);
    }

    public string Clean(string value)
    {
        lock (_values)
            foreach (var secret in _values.OrderByDescending(v => v.Length))
            {
                value = value.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
                value = value.Replace(Uri.EscapeDataString(secret), "[REDACTED]", StringComparison.OrdinalIgnoreCase);
            }
        return Regex.Replace(value, @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", "[EMAIL]");
    }

    public string Body(string value)
    {
        try
        {
            var node = JsonNode.Parse(value);
            Scrub(node);
            return Clean(node?.ToJsonString() ?? "null");
        }
        catch (System.Text.Json.JsonException) { return Clean(value); }
    }

    private static void Scrub(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToArray())
                if (PrivateKeys.Contains(key)) obj[key] = "[REDACTED]";
                else Scrub(obj[key]);
        }
        else if (node is JsonArray array)
            foreach (var child in array) Scrub(child);
    }
}
