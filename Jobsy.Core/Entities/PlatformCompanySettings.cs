namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for Lobsy/Jobsy legal company details (admin Bedrijfsgegevens).
/// Shown on self-billing invoice PDFs.
/// </summary>
public class PlatformCompanySettings
{
    public Guid Id { get; set; }

    public string CompanyName { get; set; } = "Lobsy";
    public string? Slogan { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Knab BTW-rekening IBAN where VAT from token purchases is buffered.
    /// </summary>
    public string? VatBufferIban { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
