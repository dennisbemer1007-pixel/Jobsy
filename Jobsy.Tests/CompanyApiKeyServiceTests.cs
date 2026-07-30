using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class CompanyApiKeyServiceTests
{
    [Fact]
    public async Task Generate_stores_hash_only_and_returns_plaintext_once()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var email = new RecordingEmailService();
        var sut = CreateSut(db, email);

        var generated = await sut.GenerateAsync(companyId, "ATS");

        Assert.StartsWith("lobsy_", generated.PlaintextKey);
        Assert.DoesNotContain(generated.PlaintextKey, await db.ApiKeys.Select(k => k.ApiKeyHash).FirstAsync());
        Assert.Equal(ApiKeyHasher.Hash(generated.PlaintextKey), (await db.ApiKeys.SingleAsync()).ApiKeyHash);
        Assert.True((await db.ApiKeys.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task Generate_deactivates_previous_active_key()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var first = await sut.GenerateAsync(companyId);
        var second = await sut.GenerateAsync(companyId);

        var keys = await db.ApiKeys.OrderBy(k => k.CreatedAt).ToListAsync();
        Assert.Equal(2, keys.Count);
        Assert.False(keys[0].IsActive);
        Assert.True(keys[1].IsActive);
        Assert.NotEqual(first.PlaintextKey, second.PlaintextKey);
    }

    [Fact]
    public async Task Generate_rejects_oversized_name()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GenerateAsync(companyId, new string('x', CompanyApiKeyService.MaxNameLength + 1)));
    }

    [Fact]
    public async Task FindActive_rejects_inactive_and_wrong_key()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var sut = CreateSut(db);
        var generated = await sut.GenerateAsync(companyId);

        Assert.NotNull(await sut.FindActiveByPlaintextAsync(generated.PlaintextKey));
        Assert.Null(await sut.FindActiveByPlaintextAsync(generated.PlaintextKey + "x"));

        await sut.DeactivateAsync(generated.Id);
        Assert.Null(await sut.FindActiveByPlaintextAsync(generated.PlaintextKey));
    }

    [Fact]
    public async Task EmailCredentials_sends_before_persisting_and_rotates()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var email = new RecordingEmailService();
        var sut = CreateSut(db, email);
        var first = await sut.GenerateAsync(companyId);

        var result = await sut.EmailCredentialsAsync(companyId, "manager@example.com");

        Assert.True(result.Sent);
        Assert.Equal("manager@example.com", result.RecipientEmail);
        Assert.Single(email.Messages);
        Assert.Contains("lobsy_", email.Messages[0].BodyHtml);
        Assert.Contains("https://api.example.test/api/external/vacancies", email.Messages[0].BodyHtml);
        Assert.Contains("https://api.example.test/swagger", email.Messages[0].BodyHtml);
        Assert.DoesNotContain(first.PlaintextKey, email.Messages[0].BodyHtml);
        Assert.Equal(1, await db.ApiKeys.CountAsync(k => k.IsActive));
        Assert.Equal(2, await db.ApiKeys.CountAsync());
        Assert.Null(await sut.FindActiveByPlaintextAsync(first.PlaintextKey));
    }

    [Fact]
    public async Task EmailCredentials_mail_failure_keeps_existing_key_active()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var failing = new FailingEmailService();
        var sut = CreateSut(db, failing);
        var first = await sut.GenerateAsync(companyId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EmailCredentialsAsync(companyId, "manager@example.com"));

        Assert.Contains("mislukt", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.ApiKeys.CountAsync());
        Assert.NotNull(await sut.FindActiveByPlaintextAsync(first.PlaintextKey));
    }

    [Fact]
    public async Task ApiKey_auth_scope_is_strictly_company_plus_children()
    {
        await using var db = CreateDb();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        db.Companies.AddRange(
            new Company { Id = parentId, Name = "Parent", KvkNumber = "1", Address = "a", Location = new GeoPoint(52, 4) },
            new Company
            {
                Id = childId,
                Name = "Child",
                KvkNumber = "1",
                Address = "b",
                Location = new GeoPoint(52.1, 4.1),
                ParentCompanyId = parentId
            },
            new Company { Id = foreignId, Name = "Other", KvkNumber = "9", Address = "c", Location = new GeoPoint(52.2, 4.2) });
        await db.SaveChangesAsync();

        var auth = new CompanyAuthorizationService(db);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JobsyClaimTypes.CompanyId, parentId.ToString()),
            new Claim(ApiKeyAuthDefaults.ApiKeyIdClaim, Guid.NewGuid().ToString())
        ], ApiKeyAuthDefaults.AuthenticationScheme));

        var ids = await auth.GetAccessibleCompanyIdsAsync(principal);
        Assert.NotNull(ids);
        Assert.Contains(parentId, ids!);
        Assert.Contains(childId, ids);
        Assert.DoesNotContain(foreignId, ids);
        Assert.False(await auth.CanAccessCompanyAsync(principal, foreignId));
    }

    [Fact]
    public void Hasher_uses_sha256_hex()
    {
        var hash = ApiKeyHasher.Hash("lobsy_test");
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, ApiKeyHasher.Hash("lobsy_test"));
        Assert.NotEqual(hash, ApiKeyHasher.Hash("lobsy_other"));
    }

    private static CompanyApiKeyService CreateSut(JobsyDbContext db, IEmailService? email = null)
        => new(
            db,
            email ?? new RecordingEmailService(),
            new FixedConfig("PublicApiBaseUrl", "https://api.example.test"),
            NullLogger<CompanyApiKeyService>.Instance);

    private sealed class FixedConfig : Microsoft.Extensions.Configuration.IConfiguration
    {
        private readonly Dictionary<string, string?> _values;

        public FixedConfig(string key, string? value)
            => _values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { [key] = value };

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }

        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new NoopChangeToken();

        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
            => new FixedSection(this, key);

        private sealed class FixedSection : Microsoft.Extensions.Configuration.IConfigurationSection
        {
            private readonly FixedConfig _root;
            public FixedSection(FixedConfig root, string key)
            {
                _root = root;
                Key = key;
                Path = key;
            }

            public string Key { get; }
            public string Path { get; }
            public string? Value
            {
                get => _root[Key];
                set => _root[Key] = value;
            }

            public string? this[string key]
            {
                get => _root[Path + ":" + key];
                set => _root[Path + ":" + key] = value;
            }

            public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
            public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new NoopChangeToken();
            public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
                => new FixedSection(_root, Path + ":" + key);
        }

        private sealed class NoopChangeToken : Microsoft.Extensions.Primitives.IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => EmptyDisposable.Instance;
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private static async Task<Guid> SeedCompanyAsync(JobsyDbContext db)
    {
        var id = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = id,
            Name = "Test Co",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4)
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class RecordingEmailService : IEmailService
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmailService : IEmailService
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("SMTP down");
    }
}
