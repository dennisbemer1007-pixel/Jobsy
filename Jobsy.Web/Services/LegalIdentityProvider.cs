using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Services;

/// <summary>Cached legal identity for public pages (5 minutes; last-good on API failure).</summary>
public sealed class LegalIdentityProvider
{
    public const string HttpClientName = "JobsyLegalIdentity";
    private const string CacheKey = "web:legal-identity";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LegalIdentityProvider> _logger;
    private LegalIdentityDto? _lastGood;

    public LegalIdentityProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<LegalIdentityProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LegalIdentityDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out LegalIdentityDto? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var dto = await client.GetFromJsonAsync<LegalIdentityDto>("api/site/legal", cancellationToken);
            if (dto is not null)
            {
                _lastGood = dto;
                _cache.Set(CacheKey, dto, CacheTtl);
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

    public string? PrivacyContact
        => !string.IsNullOrWhiteSpace(PrivacyEmail) ? PrivacyEmail.Trim()
            : !string.IsNullOrWhiteSpace(SupportEmail) ? SupportEmail.Trim()
            : null;
}
