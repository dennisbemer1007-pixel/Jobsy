using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Rules;

public static class CultureFitStoredJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(CultureFitResult result)
        => JsonSerializer.Serialize(result, Options);

    public static CultureFitResult? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "{}" or "null")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CultureFitResult>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
