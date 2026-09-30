using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class ApiKeyRevealFlowTests
{
    [Fact]
    public async Task Reveal_rotates_once_and_mail_has_no_key()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var email = new RecordingEmail();
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var sut = new CompanyApiKeyService(
            db,
            email,
            new FixedConfig("PublicApiBaseUrl", "https://api.example.test"),
            links,
            new AlwaysOnFeatures(),
            NullLogger<CompanyApiKeyService>.Instance);

        var first = await sut.GenerateAsync(companyId);
        await sut.EmailCredentialsAsync(companyId, "mgr@example.com");
        Assert.DoesNotContain(first.PlaintextKey, email.LastHtml!);
        Assert.Contains("/koppeling/sleutel?t=", email.LastHtml!);
        Assert.NotNull(await sut.FindActiveByPlaintextAsync(first.PlaintextKey));

        var token = ExtractToken(email.LastHtml!);
        var revealed = await sut.RevealFromTokenAsync(token);
        Assert.NotNull(revealed);
        Assert.Null(await sut.FindActiveByPlaintextAsync(first.PlaintextKey));
        Assert.NotNull(await sut.FindActiveByPlaintextAsync(revealed!.PlaintextKey));

        Assert.Null(await sut.RevealFromTokenAsync(token));
    }

    private static string ExtractToken(string html)
    {
        const string marker = "/koppeling/sleutel?t=";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var start = idx + marker.Length;
        var end = html.IndexOf('"', start);
        return Uri.UnescapeDataString(html[start..end]);
    }

    private static async Task<Guid> SeedCompanyAsync(JobsyDbContext db)
    {
        var id = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = id,
            Name = "Reveal Co",
            KvkNumber = "87654321",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class RecordingEmail : IEmailService
    {
        public string? LastHtml { get; private set; }

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            LastHtml = message.BodyHtml;
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class FixedConfig : Microsoft.Extensions.Configuration.IConfiguration
    {
        private readonly Dictionary<string, string?> _values;
        public FixedConfig(string key, string? value)
            => _values = new(StringComparer.OrdinalIgnoreCase) { [key] = value };
        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new Noop();
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
            => new Section(this, key);
        private sealed class Section(FixedConfig root, string key) : Microsoft.Extensions.Configuration.IConfigurationSection
        {
            public string Key => key;
            public string Path => key;
            public string? Value { get => root[key]; set => root[key] = value; }
            public string? this[string k] { get => root[key + ":" + k]; set => root[key + ":" + k] = value; }
            public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
            public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new Noop();
            public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string k) => new Section(root, key + ":" + k);
        }
        private sealed class Noop : Microsoft.Extensions.Primitives.IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => Empty.Instance;
        }
        private sealed class Empty : IDisposable
        {
            public static readonly Empty Instance = new();
            public void Dispose() { }
        }
    }
}
