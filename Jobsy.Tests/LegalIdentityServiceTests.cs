using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Jobsy.Core.Entities;

namespace Jobsy.Tests;

public class LegalIdentityServiceTests
{
    [Fact]
    public async Task Config_wins_over_bedrijfsgegevens()
    {
        await using var db = CreateDb();
        var company = new PlatformCompanySettingsService(db, new MemoryCache(new MemoryCacheOptions()));
        await company.UpdateAsync(new PlatformCompanyUpdate(
            "Admin BV", null, "Straat 1", "1234 AB", "Delft", "NL", "11112222", "NL001", null, null));

        var sut = CreateSut(company, new LegalOptions
        {
            Name = "Config BV",
            Street = "Configstraat 2",
            PostalCode = "9999 ZZ",
            City = "Den Haag",
            KvkNumber = "87654321",
            VatNumber = "NL999",
            SupportEmail = "support@lobsy.nl"
        });

        var snap = await sut.GetAsync();
        Assert.Equal("Config BV", snap.Name);
        Assert.Equal("Configstraat 2", snap.Street);
        Assert.Equal("87654321", snap.KvkNumber);
        Assert.Contains("Config BV", snap.FooterLine, StringComparison.Ordinal);
        Assert.Contains("KvK 87654321", snap.FooterLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Empty_config_falls_back_to_bedrijfsgegevens()
    {
        await using var db = CreateDb();
        var company = new PlatformCompanySettingsService(db, new MemoryCache(new MemoryCacheOptions()));
        await company.UpdateAsync(new PlatformCompanyUpdate(
            "Fallback BV", null, "Kaai 3", "2671 AB", "Naaldwijk", "NL", "11223344", "NL112", null, null));

        var sut = CreateSut(company, new LegalOptions());
        var snap = await sut.GetAsync();
        Assert.Equal("Fallback BV", snap.Name);
        Assert.Equal("Kaai 3", snap.Street);
        Assert.Equal("support@lobsy.nl", snap.PrivacyContact);
        Assert.Equal("Fallback BV", snap.DisplayName);
    }

    [Fact]
    public async Task Both_empty_footer_is_trade_name_only()
    {
        await using var db = CreateDb();
        var company = new PlatformCompanySettingsService(db, new MemoryCache(new MemoryCacheOptions()));
        var sut = CreateSut(company, new LegalOptions { TradeName = "Lobsy" });
        var snap = await sut.GetAsync();
        Assert.Equal("Lobsy", snap.FooterLine);
        Assert.Equal("support@lobsy.nl", snap.PrivacyContact);
    }

    private static LegalIdentityService CreateSut(IPlatformCompanySettingsService company, LegalOptions options)
    {
        var monitor = new StaticOptionsMonitor<LegalOptions>(options);
        return new LegalIdentityService(
            monitor,
            company,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LegalIdentityService>.Instance);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T current) => CurrentValue = current;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
