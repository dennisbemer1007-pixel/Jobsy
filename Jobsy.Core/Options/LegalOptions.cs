namespace Jobsy.Core.Options;

/// <summary>
/// Fallback legal identity from config (<c>Legal__*</c>).
/// A filled Bedrijfsgegevens field wins; these values are used only when that field is empty.
/// </summary>
public sealed class LegalOptions
{
    public const string SectionName = "Legal";

    public const string DefaultTradeName = "Lobsy";
    public const string DefaultSupportEmail = "support@lobsy.nl";
    public const string DefaultCountry = "Nederland";

    public string? Name { get; set; }
    public string? TradeName { get; set; } = DefaultTradeName;
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = DefaultCountry;
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? PrivacyEmail { get; set; }
    public string? SupportEmail { get; set; } = DefaultSupportEmail;
    public string? SchoolsEmail { get; set; }

    public static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
