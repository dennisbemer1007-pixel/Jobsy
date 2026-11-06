using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jobsy.Core.Sales;

/// <summary>E-mail preferences for sales beneficiaries (stored as JSON on the payout profile).</summary>
public sealed class SalesEmailPrefs
{
    [JsonPropertyName("newEmployer")]
    public bool NewEmployer { get; set; } = true;

    [JsonPropertyName("commissionAvailable")]
    public bool CommissionAvailable { get; set; } = true;

    [JsonPropertyName("payoutStatus")]
    public bool PayoutStatus { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static SalesEmailPrefs Default { get; } = new();

    public static SalesEmailPrefs Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SalesEmailPrefs();
        }

        try
        {
            return JsonSerializer.Deserialize<SalesEmailPrefs>(json, JsonOptions) ?? new SalesEmailPrefs();
        }
        catch (JsonException)
        {
            return new SalesEmailPrefs();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}
