namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for Lobsy/Jobsy legal company details (admin Bedrijfsgegevens).
/// Slogan is shown in the site header; the rest appears on self-billing invoice PDFs.
/// </summary>
public class PlatformCompanySettings
{
    public Guid Id { get; set; }

    /// <summary>Brand / trade name shown in the header. The statutory name lives in <see cref="LegalName"/>.</summary>
    public string CompanyName { get; set; } = "Lobsy";

    /// <summary>Statutory name, for example "Dennis Bemer h.o.d.n. Lobsy".</summary>
    public string? LegalName { get; set; }

    /// <summary>Handelsnaam. Empty means the brand name in <see cref="CompanyName"/>.</summary>
    public string? TradeName { get; set; }

    public string? Slogan { get; set; }

    /// <summary>Visiting address street.</summary>
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";

    /// <summary>Optional postal address. Empty means the visiting address is also the postal address.</summary>
    public string? PostalStreet { get; set; }
    public string? PostalPostalCode { get; set; }
    public string? PostalCity { get; set; }

    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Phone { get; set; }

    /// <summary>Legacy single inbox. Kept in sync with <see cref="SupportEmail"/>.</summary>
    public string? Email { get; set; }

    public string? SupportEmail { get; set; }
    public string? PrivacyEmail { get; set; }

    /// <summary>
    /// Knab BTW-rekening IBAN where VAT from token purchases is buffered.
    /// </summary>
    public string? VatBufferIban { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
