using System.Text.Json;

namespace FluentCMS.Configuration.EntityFramework;

public static class JsonConfigFlattener
{
    /// <summary>
    /// Flattens a JSON string to IConfiguration-style keys and string? values.
    /// Examples:
    ///   { "A": { "B": 1 }, "C": [true, null] }
    /// -> "A:B" = "1", "C:0" = "True", "C:1" = null
    /// </summary>
    /// <param name="json">JSON text</param>
    /// <param name="rootKey">
    /// Optional prefix for all keys (e.g., "MySection"). If the top-level JSON is a primitive,
    /// you must provide this to name the single key.
    /// </param>
    public static IDictionary<string, string?> ToDictionary(string json, string? rootKey = null)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);

        void Walk(JsonElement el, string prefix)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var p in el.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}:{p.Name}";
                        Walk(p.Value, key);
                    }
                    break;

                case JsonValueKind.Array:
                    int i = 0;
                    foreach (var item in el.EnumerateArray())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? i.ToString() : $"{prefix}:{i}";
                        Walk(item, key);
                        i++;
                    }
                    break;

                case JsonValueKind.String:
                    dict[prefix] = el.GetString();
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    dict[prefix] = el.ToString(); // stringify numbers/bools
                    break;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    dict[prefix] = null;
                    break;
            }
        }

        var root = doc.RootElement;

        if (root.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
        {
            Walk(root, rootKey ?? "");
        }
        else
        {
            if (string.IsNullOrEmpty(rootKey))
                throw new InvalidOperationException("Top-level primitive JSON requires a non-empty rootKey.");
            Walk(root, rootKey);
        }

        return dict;
    }
}
