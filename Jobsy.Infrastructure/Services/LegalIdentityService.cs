using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Services;

public sealed class LegalIdentityService : ILegalIdentity
{
    public const string CacheKey = "legal-identity:snapshot";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IOptionsMonitor<LegalOptions> _options;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LegalIdentityService> _logger;
    private int _mismatchLogged;

    public LegalIdentityService(
        IOptionsMonitor<LegalOptions> options,
        IPlatformCompanySettingsService companySettings,
        IMemoryCache cache,
        ILogger<LegalIdentityService> logger)
    {
        _options = options;
        _companySettings = companySettings;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LegalIdentitySnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out LegalIdentitySnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var snap = await BuildAsync(cancellationToken);
        _cache.Set(CacheKey, snap, CacheTtl);
        return snap;
    }

    public static void Evict(IMemoryCache cache) => cache.Remove(CacheKey);

    public static void EvictCache(IMemoryCache cache) => Evict(cache);

    private async Task<LegalIdentitySnapshot> BuildAsync(CancellationToken cancellationToken)
    {
        var legal = _options.CurrentValue;
        var company = await _companySettings.GetAsync(cancellationToken);

        var name = LegalOptions.TrimOrNull(legal.Name);
        var companyName = LegalOptions.TrimOrNull(company.CompanyName);
        if (name is null
            && companyName is not null
            && !string.Equals(companyName, PlatformCompanySettingsService.DefaultCompanyName, StringComparison.OrdinalIgnoreCase))
        {
            name = companyName;
        }

        var tradeName = LegalOptions.TrimOrNull(legal.TradeName) ?? LegalOptions.DefaultTradeName;
        var street = LegalOptions.TrimOrNull(legal.Street) ?? LegalOptions.TrimOrNull(company.Address);
        var postal = LegalOptions.TrimOrNull(legal.PostalCode) ?? LegalOptions.TrimOrNull(company.PostalCode);
        var city = LegalOptions.TrimOrNull(legal.City) ?? LegalOptions.TrimOrNull(company.City);
        var country = LegalOptions.TrimOrNull(legal.Country) ?? LegalOptions.TrimOrNull(company.Country) ?? LegalOptions.DefaultCountry;
        var kvk = LegalOptions.TrimOrNull(legal.KvkNumber) ?? LegalOptions.TrimOrNull(company.KvkNumber);
        var vat = LegalOptions.TrimOrNull(legal.VatNumber) ?? LegalOptions.TrimOrNull(company.VatNumber);
        var privacy = LegalOptions.TrimOrNull(legal.PrivacyEmail);
        var support = LegalOptions.TrimOrNull(legal.SupportEmail) ?? LegalOptions.DefaultSupportEmail;
        var schools = LegalOptions.TrimOrNull(legal.SchoolsEmail);

        LogMismatchesOnce(legal, company);

        return new LegalIdentitySnapshot(
            name, tradeName, street, postal, city, country, kvk, vat, privacy, support, schools);
    }

    private void LogMismatchesOnce(LegalOptions legal, PlatformCompanySnapshot company)
    {
        if (Interlocked.Exchange(ref _mismatchLogged, 1) == 1)
        {
            return;
        }

        var mismatched = new List<string>();
        void Check(string field, string? config, string? admin)
        {
            var a = LegalOptions.TrimOrNull(config);
            var b = LegalOptions.TrimOrNull(admin);
            if (a is not null && b is not null && !string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            {
                mismatched.Add(field);
            }
        }

        Check("Name", legal.Name, company.CompanyName);
        Check("Street", legal.Street, company.Address);
        Check("PostalCode", legal.PostalCode, company.PostalCode);
        Check("City", legal.City, company.City);
        Check("KvkNumber", legal.KvkNumber, company.KvkNumber);
        Check("VatNumber", legal.VatNumber, company.VatNumber);

        if (mismatched.Count > 0)
        {
            _logger.LogWarning(
                "legal.identity.mismatch fields={Fields}",
                string.Join(',', mismatched));
        }
    }
}
