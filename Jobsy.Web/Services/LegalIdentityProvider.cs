using System.Net.Http.Json;

namespace Jobsy.Web.Services;

/// <summary>
/// Legal identity for public pages. Each request asks the API, so a save in Bedrijfsgegevens
/// shows at once (the API drops its own cache on save). The last good answer is kept only for
/// when the API is down.
/// </summary>
public sealed class LegalIdentityProvider
{
    public const string HttpClientName = "JobsyLegalIdentity";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LegalIdentityProvider> _logger;
    private LegalIdentityDto? _lastGood;

    public LegalIdentityProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<LegalIdentityProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<LegalIdentityDto> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var dto = await client.GetFromJsonAsync<LegalIdentityDto>("api/site/legal", cancellationToken);
            if (dto is not null)
            {
                _lastGood = dto;
                return dto;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load legal identity from API");
        }

        if (_lastGood is not null)
        {
            return _lastGood;
        }

        return LegalIdentityDto.Empty;
    }
}

public sealed class LegalIdentityDto
{
    public string? Name { get; set; }
    public string? TradeName { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? PrivacyEmail { get; set; }
    public string? SupportEmail { get; set; }
    public string? SchoolsEmail { get; set; }
    public string? Phone { get; set; }
    public string? PostalStreet { get; set; }
    public string? PostalPostalCode { get; set; }
    public string? PostalCity { get; set; }

    public static LegalIdentityDto Empty { get; } = new()
    {
        TradeName = "Lobsy",
        SupportEmail = "support@lobsy.nl"
    };

    public string DisplayName
        => !string.IsNullOrWhiteSpace(Name) ? Name.Trim()
            : !string.IsNullOrWhiteSpace(TradeName) ? TradeName.Trim()
            : "Lobsy";

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

    public string? PostalAddressLine
    {
        get
        {
            var street = PostalStreet?.Trim();
            var postal = PostalPostalCode?.Trim();
            var city = PostalCity?.Trim();
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

    public string FooterLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                parts.Add(DisplayName.Trim());
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

    public string? PrivacyContact
        => !string.IsNullOrWhiteSpace(PrivacyEmail) ? PrivacyEmail.Trim()
            : !string.IsNullOrWhiteSpace(SupportEmail) ? SupportEmail.Trim()
            : null;
}
