namespace Jobsy.Core.Options;

/// <summary>
/// Lettermint (https://lettermint.co) transactional mail. The key is read from
/// <c>Lettermint:ApiKey</c> / <c>Lettermint__ApiKey</c> or <c>LETTERMINT_API_KEY</c>.
/// Mail is sent with this provider only when <c>Mail:Provider</c> is <c>Lettermint</c> and the key is set.
/// </summary>
public sealed class LettermintOptions
{
    public const string SectionName = "Lettermint";

    public const string DefaultBaseUrl = "https://api.lettermint.co/v1/";

    public string? ApiKey { get; set; }

    /// <summary>Sending API root. The sender posts to <c>send</c> under this URL.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
