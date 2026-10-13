namespace Jobsy.Core.Entities;

/// <summary>
/// Daily aggregate of outbound KVK Handelsregister calls (no query text stored).
/// </summary>
public class KvkUsageDaily
{
    public Guid Id { get; set; }

    /// <summary>UTC calendar date of the call bucket.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Call type: zoeken | basisprofiel | vestigingen.</summary>
    public string CallType { get; set; } = string.Empty;

    public int Count { get; set; }
}

public static class KvkUsageCallTypes
{
    public const string Zoeken = "zoeken";
    public const string Basisprofiel = "basisprofiel";
    public const string Vestigingen = "vestigingen";
}
