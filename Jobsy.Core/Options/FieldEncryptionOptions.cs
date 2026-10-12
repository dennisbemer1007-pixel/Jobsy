namespace Jobsy.Core.Options;

/// <summary>
/// Application-level AES-GCM field encryption (at-rest in Postgres).
/// Keys: <c>JobsyFieldEncryption__ActiveKeyId</c> and
/// <c>JobsyFieldEncryption__Keys__{keyId}</c> (base64, 32 bytes).
/// </summary>
public sealed class FieldEncryptionOptions
{
    public const string SectionName = "JobsyFieldEncryption";

    /// <summary>Key id used for new ciphertext (supports rotation).</summary>
    public string? ActiveKeyId { get; set; }

    /// <summary>Map of key id → base64-encoded 256-bit AES key.</summary>
    public Dictionary<string, string> Keys { get; set; } = new(StringComparer.Ordinal);

    /// <summary>When true (Development only), missing keys disable encryption instead of failing startup.</summary>
    public bool AllowPassthroughWithoutKeys { get; set; }
}
