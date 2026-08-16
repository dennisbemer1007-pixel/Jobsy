namespace Jobsy.Core.Options;

/// <summary>
/// Cursor Cloud Agents (v0) settings for turning admin-approved feedback into a PR.
/// Leave <see cref="ApiKey"/> empty to generate/store the prompt only (manual Cursor).
/// </summary>
public sealed class CursorCloudOptions
{
    public const string SectionName = "CursorCloud";

    /// <summary>Cursor API key (Basic auth username). Never commit a real value.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.cursor.com";

    /// <summary>GitHub/GitLab HTTPS clone URL the agent should work on.</summary>
    public string? Repository { get; set; }

    /// <summary>Source ref for the agent / PR base (acceptatie: <c>acc</c>).</summary>
    public string Ref { get; set; } = "main";

    public string? Model { get; set; }

    /// <summary>
    /// Public HTTPS URL Cursor will POST status changes to
    /// (typically <c>{PublicApiBaseUrl}/api/feedback/cursor-webhook</c>).
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>HMAC secret for webhook verification (min. 32 characters).</summary>
    public string? WebhookSecret { get; set; }
}
