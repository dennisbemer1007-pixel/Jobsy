namespace Jobsy.Core.Options;

/// <summary>
/// Outbound mail via Resend. Prefer Admin → Integraties for encrypted storage;
/// env/config (<c>Mail__ResendApiKey</c>, <c>Mail__FromAddress</c>) is used when DB credentials are empty.
/// </summary>
public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public const string DefaultSupportAddress = "support@lobsy.nl";
    public const string DefaultLegalName = "Lobsy";
    public const string DefaultAssetVersion = "20260930-mail2";
    public const string DefaultFromAddress = "Lobsy <hallo@mail.lobsy.nl>";

    /// <summary>Resend API key (<c>re_…</c>). Maps from <c>Mail__ResendApiKey</c> or <c>RESEND_API_KEY</c>.</summary>
    public string? ResendApiKey { get; set; }

    /// <summary>
    /// From address on a Resend-verified subdomain, e.g. <c>Lobsy &lt;hallo@mail.lobsy.nl&gt;</c>.
    /// Wired in file 03; default documents the approved sender.
    /// </summary>
    public string? FromAddress { get; set; }

    /// <summary>Reply-To header (file 03). Default support inbox.</summary>
    public string? ReplyTo { get; set; } = DefaultSupportAddress;

    /// <summary>Address used for the footer Hulp mailto link.</summary>
    public string? SupportAddress { get; set; } = DefaultSupportAddress;

    /// <summary>Legal entity name in the footer. Empty parts are omitted.</summary>
    public string? LegalName { get; set; } = DefaultLegalName;

    /// <summary>Registered address for the footer. Dennis must provide this (D14).</summary>
    public string? LegalAddress { get; set; }

    /// <summary>KvK number for the footer. Dennis must provide this (D14).</summary>
    public string? KvkNumber { get; set; }

    /// <summary>When true, OTP code mails put the code in the subject (default on).</summary>
    public bool CodeInSubject { get; set; } = true;

    /// <summary>Cache-busting query for hosted email PNGs.</summary>
    public string? AssetVersion { get; set; } = DefaultAssetVersion;

    /// <summary>Optional extra recipients allowed for admin test sends (besides the admin's own address).</summary>
    public string[] TestRecipientAllowList { get; set; } = [];

    /// <summary>Max admin catalog test sends (single + send-all) per admin per Amsterdam day.</summary>
    public int TestDailyCap { get; set; } = 100;

    public bool MissingLegalFooter
        => string.IsNullOrWhiteSpace(LegalAddress) || string.IsNullOrWhiteSpace(KvkNumber);
}
