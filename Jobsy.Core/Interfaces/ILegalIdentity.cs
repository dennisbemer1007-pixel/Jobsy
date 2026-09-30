using Jobsy.Core.Options;

namespace Jobsy.Core.Interfaces;

public interface ILegalIdentity
{
    Task<LegalIdentitySnapshot> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record LegalIdentitySnapshot(
    string? Name,
    string? TradeName,
    string? Street,
    string? PostalCode,
    string? City,
    string? Country,
    string? KvkNumber,
    string? VatNumber,
    string? PrivacyEmail,
    string? SupportEmail,
    string? SchoolsEmail)
{
    public string DisplayName
        => !string.IsNullOrWhiteSpace(Name) ? Name.Trim()
            : !string.IsNullOrWhiteSpace(TradeName) ? TradeName.Trim()
            : LegalOptions.DefaultTradeName;

    public string? AddressLine
    {
        get
        {
            var street = Street?.Trim();
            var postal = PostalCode?.Trim();
            var city = City?.Trim();
            var cityPart = string.Join(" ", new[] { postal, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(cityPart))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(street))
            {
                return cityPart;
            }

            return string.IsNullOrWhiteSpace(cityPart) ? street : $"{street}, {cityPart}";
        }
    }

    public string? PrivacyContact
        => !string.IsNullOrWhiteSpace(PrivacyEmail) ? PrivacyEmail.Trim()
            : !string.IsNullOrWhiteSpace(SupportEmail) ? SupportEmail.Trim()
            : null;

    public string FooterLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                parts.Add(DisplayName);
            }

            if (!string.IsNullOrWhiteSpace(AddressLine))
            {
                parts.Add(AddressLine);
            }

            if (!string.IsNullOrWhiteSpace(KvkNumber))
            {
                parts.Add("KvK " + KvkNumber.Trim());
            }

            if (!string.IsNullOrWhiteSpace(VatNumber))
            {
                parts.Add("btw " + VatNumber.Trim());
            }

            return string.Join(" · ", parts);
        }
    }
}
