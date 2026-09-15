namespace Jobsy.Core.Options;

/// <summary>
/// Optional KVK Handelsregister settings. Prefer Admin → Integraties for encrypted storage;
/// env/config (<c>Kvk__ApiKey</c>, <c>KVK_API_KEY</c>) fills gaps when the database key is empty.
/// </summary>
public sealed class KvkOptions
{
    public const string SectionName = "Kvk";

    /// <summary>API-key from the KVK Developer Portal (Mijn API-keys).</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL. Empty = production <c>https://api.kvk.nl/api/</c>.
    /// Test environment: <c>https://api.kvk.nl/test/api/</c>.
    /// </summary>
    public string? BaseUrl { get; set; }
}
