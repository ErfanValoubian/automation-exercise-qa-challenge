using System.Text.Json;

namespace Challenge.Support.Api;

public sealed class ContractException(string message) : Exception(message);
public sealed class ServiceUnavailableException(string message) : Exception(message);

public sealed record ApiReply(int HttpStatus, string Raw, JsonElement? Json, long DurationMs)
{
    public int? Code => Json is { ValueKind: JsonValueKind.Object } root &&
        root.TryGetProperty("responseCode", out var code) && code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out var number) ? number : null;
    public string Message => Json is { ValueKind: JsonValueKind.Object } root && root.TryGetProperty("message", out var message)
        && message.ValueKind == JsonValueKind.String ? message.GetString()! : "";

    public JsonElement RequireEnvelope(int code)
    {
        if (HttpStatus is < 200 or >= 300)
            throw new ContractException($"Expected a successful HTTP envelope, got HTTP {HttpStatus}; application code {Code}.");
        if (Json is not { ValueKind: JsonValueKind.Object } root || Code != code)
            throw new ContractException($"Expected JSON object with numeric responseCode={code}; got {Code?.ToString() ?? "missing/invalid"}. See recorded response.");
        return root;
    }

    public IReadOnlyList<Product> Products()
    {
        var root = RequireEnvelope(200);
        if (!root.TryGetProperty("products", out var array) || array.ValueKind != JsonValueKind.Array)
            throw new ContractException("Successful product response must contain a products array.");
        return array.EnumerateArray().Select(Product.From).ToArray();
    }
}

public sealed record Product(int Id, string Name, string Price, string Brand, string Category, string Audience)
{
    public static Product From(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt32(out var number) || number <= 0)
            throw new ContractException("Product requires a positive integer id.");
        if (!item.TryGetProperty("category", out var category) || category.ValueKind != JsonValueKind.Object ||
            !category.TryGetProperty("usertype", out var usertype) || usertype.ValueKind != JsonValueKind.Object)
            throw new ContractException("Product requires category and category.usertype objects.");
        return new Product(number, Text(item, "name"), Text(item, "price"), Text(item, "brand"), Text(category, "category"), Text(usertype, "usertype"));
    }
    public static string Text(JsonElement item, string field)
    {
        if (!item.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ContractException($"Missing, blank or wrong-type field '{field}'.");
        return value.GetString()!;
    }
}
