namespace Jobsy.Core.Options;

/// <summary>
/// Outbound mail via Resend. Prefer Admin → Integraties for encrypted storage;
/// env/config (<c>Mail__ResendApiKey</c>, <c>Mail__FromAddress</c>) is used when DB credentials are empty.
/// </summary>
public sealed class MailOptions
{
    public const string SectionName = "Mail";

    /// <summary>Resend API key (<c>re_…</c>). Maps from <c>Mail__ResendApiKey</c> or <c>RESEND_API_KEY</c>.</summary>
    public string? ResendApiKey { get; set; }

    /// <summary>From address on a Resend-verified domain, e.g. <c>Lobsy &lt;noreply@lobsy.nl&gt;</c>.</summary>
    public string? FromAddress { get; set; }
}
