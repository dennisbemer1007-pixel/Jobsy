using System.Net;
using System.Net.Http.Json;
using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Scholen;

[Collection("SequentialApi")]
public class PupilRateLimitIntegrationTests : IClassFixture<PupilRateLimitWebAppFactory>
{
    private const string AuthenticatedVisitorIp = "198.51.100.10";
    private const string AnonymousVisitorIp = "198.51.100.99";
    private const string InternalSecret = "pupil-rate-limit-test-secret";

    private readonly PupilRateLimitWebAppFactory _factory;

    public PupilRateLimitIntegrationTests(PupilRateLimitWebAppFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Authenticated_pupil_progress_partitions_by_pupil_code_not_shared_hop_ip()
    {
        await EnableSchoolsAsync();
        var seed = await SeedTwoCodesAsync();

        using var clientA = await LoginClientAsync(seed.SchoolId, seed.ClassId, seed.PlainCodeA);
        using var clientB = await LoginClientAsync(seed.SchoolId, seed.ClassId, seed.PlainCodeB);

        for (var i = 0; i < 100; i++)
        {
            using var a = await clientA.GetAsync("api/pupil/progress");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, a.StatusCode);
        }

        for (var i = 0; i < 100; i++)
        {
            using var b = await clientB.GetAsync("api/pupil/progress");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, b.StatusCode);
        }

        for (var i = 0; i < 20; i++)
        {
            using var extra = await clientA.GetAsync("api/pupil/progress");
            if (extra.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return;
            }
        }

        using var over = await clientA.GetAsync("api/pupil/progress");
        Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);
    }

    [Fact]
    public async Task Anonymous_pupil_list_stays_on_ip_bucket_at_sixty_per_minute()
    {
        await EnableSchoolsAsync();
        await SeedTwoCodesAsync();

        using var client = _factory.CreateClient();
        ApplyVisitorIp(client, AnonymousVisitorIp);

        HttpResponseMessage? last = null;
        for (var i = 0; i < 61; i++)
        {
            last?.Dispose();
            last = await client.GetAsync("api/pupil/schools");
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        Assert.NotNull(last);
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
    }

    [Fact]
    public void Rate_limit_rejection_logs_hashed_partition_not_raw_pupil_id()
    {
        var pupilId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");
        var partition = "pupil:" + pupilId.ToString("D");
        var hash = RateLimitPartitioning.HashPartitionForLog(partition);

        Assert.DoesNotContain(pupilId.ToString("D"), hash, StringComparison.Ordinal);
        Assert.Matches("^[0-9a-f]{16}$", hash);
        Assert.Equal(hash, RateLimitPartitioning.HashPartitionForLog(partition));
    }

    private async Task EnableSchoolsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true
            });
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task<TwoCodeSeed> SeedTwoCodesAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Rate Limit College",
            City = "Test",
            AllowedEmailDomains = "[\"rate.test\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        };
        db.Schools.Add(school);

        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "2B",
            Level = SchoolLevel.Havo,
            Year = 2,
            QuestionSet = PupilQuestionSet.Vo,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 3,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();

        var generated = await codes.GenerateAsync(3, cls);
        return new TwoCodeSeed(
            school.Id,
            cls.Id,
            codes.Unprotect(generated[0].CodeProtected)!,
            codes.Unprotect(generated[1].CodeProtected)!);
    }

    private async Task<HttpClient> LoginClientAsync(Guid schoolId, Guid classId, string plainCode)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        ApplyVisitorIp(client, AuthenticatedVisitorIp);

        var login = await client.PostAsJsonAsync(
            "api/pupil/login",
            new PupilLoginRequest(schoolId, classId, plainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private static void ApplyVisitorIp(HttpClient client, string visitorIp)
    {
        client.DefaultRequestHeaders.Remove(RateLimitPartitioning.ClientIpHeader);
        client.DefaultRequestHeaders.Remove(RateLimitPartitioning.InternalSecretHeader);
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            RateLimitPartitioning.ClientIpHeader,
            visitorIp);
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            RateLimitPartitioning.InternalSecretHeader,
            InternalSecret);
    }

    private sealed record TwoCodeSeed(
        Guid SchoolId,
        Guid ClassId,
        string PlainCodeA,
        string PlainCodeB);
}

public sealed class PupilRateLimitWebAppFactory : ApiWebApplicationFactory
{
    public Guid AdminId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000022");

    private readonly string _dbName = "PupilRateLimit-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("RateLimiting:PupilPermitLimit", "120");
        builder.UseSetting("RateLimiting:PupilAnonymousPermitLimit", "60");
        builder.UseSetting(RateLimitPartitioning.ConfigKey, "pupil-rate-limit-test-secret");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext", StringComparison.Ordinal))
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            foreach (var d in services.Where(d =>
                         d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureAdminSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureAdminSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (!db.Users.Any(u => u.Id == AdminId))
        {
            db.Users.Add(new User
            {
                Id = AdminId,
                Email = "admin-pupil-rate@jobsy.local",
                FullName = "Admin",
                Role = UserRole.Admin,
                IsActive = true
            });
            db.SaveChanges();
        }

        _seeded = true;
    }
}
