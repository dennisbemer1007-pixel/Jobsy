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
        LogMismatchesOnce(legal, company);
        return Compose(legal, company);
    }

    /// <summary>
    /// Bedrijfsgegevens win. <c>Legal:*</c> fills a field only when the database value is empty.
    /// </summary>
    public static LegalIdentitySnapshot Compose(LegalOptions? env, PlatformCompanySnapshot company)
    {
        env ??= new LegalOptions();

        var companyName = LegalOptions.TrimOrNull(company.CompanyName);
        var name = LegalOptions.TrimOrNull(company.LegalName);
        if (name is null
            && companyName is not null
            && !string.Equals(companyName, PlatformCompanySettingsService.DefaultCompanyName, StringComparison.OrdinalIgnoreCase))
        {
            name = companyName;
        }

        name ??= LegalOptions.TrimOrNull(env.Name);

        var trade = LegalOptions.TrimOrNull(company.TradeName)
                    ?? LegalOptions.TrimOrNull(env.TradeName)
                    ?? LegalOptions.DefaultTradeName;
        var support = LegalOptions.TrimOrNull(company.SupportEmail)
                      ?? LegalOptions.TrimOrNull(company.Email)
                      ?? LegalOptions.TrimOrNull(env.SupportEmail)
                      ?? LegalOptions.DefaultSupportEmail;

        return new LegalIdentitySnapshot(
            name,
            trade,
            First(company.Address, env.Street),
            First(company.PostalCode, env.PostalCode),
            First(company.City, env.City),
            First(company.Country, env.Country) ?? LegalOptions.DefaultCountry,
            First(company.KvkNumber, env.KvkNumber),
            First(company.VatNumber, env.VatNumber),
            First(company.PrivacyEmail, env.PrivacyEmail),
            support,
            LegalOptions.TrimOrNull(env.SchoolsEmail),
            LegalOptions.TrimOrNull(company.Phone),
            LegalOptions.TrimOrNull(company.PostalStreet),
            LegalOptions.TrimOrNull(company.PostalPostalCode),
            LegalOptions.TrimOrNull(company.PostalCity));
    }

    private static string? First(string? databaseValue, string? envValue)
        => LegalOptions.TrimOrNull(databaseValue) ?? LegalOptions.TrimOrNull(envValue);

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
