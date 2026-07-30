namespace Jobsy.Core.Interfaces;

public interface IPlatformCompanySettingsService
{
    Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task<PlatformCompanySnapshot> UpdateAsync(
        PlatformCompanyUpdate update,
        CancellationToken cancellationToken = default);

    /// <summary>Embedded Lobsy brand logo bytes for PDF rendering.</summary>
    byte[] GetBrandLogoPng();

    /// <summary>Pre-faded logo for full-page PDF watermark.</summary>
    byte[] GetBrandWatermarkPng();
}

public sealed record PlatformCompanySnapshot(
    string CompanyName,
    string Slogan,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? KvkNumber,
    string? VatNumber,
    string? Phone,
    string? Email,
    string? VatBufferIban,
    DateTime? UpdatedAtUtc)
{
    public string FormatAddressBlock()
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(Address))
        {
            lines.Add(Address.Trim());
        }

        var cityLine = string.Join(" ", new[]
        {
            PostalCode?.Trim(),
            City?.Trim()
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(cityLine))
        {
            lines.Add(cityLine);
        }

        if (!string.IsNullOrWhiteSpace(Country))
        {
            lines.Add(Country.Trim());
        }

        return string.Join("\n", lines);
    }
}

public sealed record PlatformCompanyUpdate(
    string CompanyName,
    string? Slogan,
    string? Address,
    string? PostalCode,
    string? City,
    string? Country,
    string? KvkNumber,
    string? VatNumber,
    string? Phone,
    string? Email,
    string? VatBufferIban = null);
