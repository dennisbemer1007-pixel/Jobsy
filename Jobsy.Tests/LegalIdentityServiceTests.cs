using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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

    [Fact]
    public async Task Mismatch_warning_logs_field_names_once_without_values()
    {
        await using var db = CreateDb();
        var company = new PlatformCompanySettingsService(db, new MemoryCache(new MemoryCacheOptions()));
        await company.UpdateAsync(new PlatformCompanyUpdate(
            "Admin BV", null, "Adminstraat 1", "1111 AA", "Delft", "NL", "11112222", "NL001", null, null));

        var logger = new CollectingLogger();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new LegalIdentityService(
            new StaticOptionsMonitor<LegalOptions>(new LegalOptions
            {
                Name = "Config BV",
                Street = "Configstraat 2",
                PostalCode = "9999 ZZ",
                City = "Den Haag",
                KvkNumber = "87654321",
                VatNumber = "NL999"
            }),
            company,
            cache,
            logger);

        _ = await sut.GetAsync();
        cache.Remove(LegalIdentityService.CacheKey);
        _ = await sut.GetAsync();

        var warnings = logger.Entries
            .Where(e => e.Level == LogLevel.Warning && e.Message.Contains("legal.identity.mismatch", StringComparison.Ordinal))
            .ToList();
        Assert.Single(warnings);
        Assert.Contains("Name", warnings[0].Message, StringComparison.Ordinal);
        Assert.Contains("Street", warnings[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Config BV", warnings[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Admin BV", warnings[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Configstraat", warnings[0].Message, StringComparison.Ordinal);
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

    private sealed class CollectingLogger : ILogger<LegalIdentityService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
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
