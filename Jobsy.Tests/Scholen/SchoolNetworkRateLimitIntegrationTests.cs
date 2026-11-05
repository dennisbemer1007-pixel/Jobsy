using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Api.Security;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Security;
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

/// <summary>
/// School NAT: many pupils + docent share one visitor IP; limits must partition per identity.
/// </summary>
[Collection("SequentialApi")]
public class SchoolNetworkRateLimitIntegrationTests : IClassFixture<SchoolNetworkRateLimitWebAppFactory>
{
    private const string SchoolVisitorIp = "198.51.100.50";
    private const string InternalSecret = "school-network-rate-limit-secret";

    private readonly SchoolNetworkRateLimitWebAppFactory _factory;

    public SchoolNetworkRateLimitIntegrationTests(SchoolNetworkRateLimitWebAppFactory factory)
        => _factory = factory;

    [Fact]
    public async Task Sixty_pupils_and_one_teacher_from_same_ip_normal_traffic_gets_no_429()
    {
        await EnableSchoolsAsync();
        var seed = await SeedClassWithCodesAsync(60);
        var teacherId = _factory.TeacherId;

        using var teacher = _factory.CreateClient();
        ApplySchoolNetworkHeaders(teacher, SchoolVisitorIp, teacherId);

        for (var i = 0; i < 8; i++)
        {
            using var response = await teacher.GetAsync("api/notifications/unread-count");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        for (var p = 0; p < seed.PlainCodes.Count; p++)
        {
            using var pupil = await LoginPupilClientAsync(seed, seed.PlainCodes[p]);
            for (var i = 0; i < 3; i++)
            {
                using var progress = await pupil.GetAsync("api/pupil/progress");
                Assert.NotEqual(HttpStatusCode.TooManyRequests, progress.StatusCode);
            }
        }
    }

    [Fact]
    public async Task Single_user_exceeding_public_read_budget_gets_429()
    {
        var userId = Guid.Parse("c1000000-0000-0000-0000-000000000077");
        await EnsureUserAsync(userId, "abuser-school-rate@jobsy.local");
        using var client = _factory.CreateClient();
        ApplySchoolNetworkHeaders(client, SchoolVisitorIp, userId);

        HttpResponseMessage? last = null;
        for (var i = 0; i < 105; i++)
        {
            last?.Dispose();
            last = await client.GetAsync("api/notifications/unread-count");
            if (last.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        Assert.NotNull(last);
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
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

    private async Task EnsureUserAsync(Guid userId, string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (await db.Users.AnyAsync(u => u.Id == userId))
        {
            return;
        }

        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Abuser",
            Role = UserRole.SchoolAdmin,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private async Task<ClassSeed> SeedClassWithCodesAsync(int pupilCount)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "NAT Rate Limit School",
            City = "Test",
            AllowedEmailDomains = "[\"nat.test\"]",
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
            Name = "3A",
            Level = SchoolLevel.Havo,
            Year = 3,
            QuestionSet = PupilQuestionSet.Vo,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = pupilCount,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();

        var plain = new List<string>();
        var remaining = pupilCount;
        while (remaining > 0)
        {
            var batch = Math.Min(remaining, 40);
            var generated = await codes.GenerateAsync(batch, cls);
            plain.AddRange(generated.Select(g => codes.Unprotect(g.CodeProtected)!));
            remaining -= batch;
        }

        return new ClassSeed(school.Id, cls.Id, plain);
    }

    private async Task<HttpClient> LoginPupilClientAsync(ClassSeed seed, string plainCode)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        ApplySchoolNetworkHeaders(client, SchoolVisitorIp, userId: null);

        var login = await client.PostAsJsonAsync(
            "api/pupil/login",
            new PupilLoginRequest(seed.SchoolId, seed.ClassId, plainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private static void ApplySchoolNetworkHeaders(HttpClient client, string visitorIp, Guid? userId)
    {
        client.DefaultRequestHeaders.Remove(RateLimitPartitioning.ClientIpHeader);
        client.DefaultRequestHeaders.Remove(RateLimitPartitioning.InternalSecretHeader);
        client.DefaultRequestHeaders.Remove(InternalClientIpHeaders.RateLimitUserIdHeader);
        client.DefaultRequestHeaders.Remove("Authorization");

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            RateLimitPartitioning.ClientIpHeader,
            visitorIp);
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            RateLimitPartitioning.InternalSecretHeader,
            InternalSecret);

        if (userId is Guid id)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JobsyTestAuth.Mint(id));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                InternalClientIpHeaders.RateLimitUserIdHeader,
                id.ToString("D"));
        }
    }

    private sealed record ClassSeed(Guid SchoolId, Guid ClassId, IReadOnlyList<string> PlainCodes);
}

public sealed class SchoolNetworkRateLimitWebAppFactory : ApiWebApplicationFactory
{
    public Guid AdminId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000099");
    public Guid TeacherId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000088");

    private readonly string _dbName = "SchoolNetworkRateLimit-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("RateLimiting:PublicReadPermitLimit", "100");
        builder.UseSetting("RateLimiting:PupilPermitLimit", "120");
        builder.UseSetting(RateLimitPartitioning.ConfigKey, "school-network-rate-limit-secret");
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
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (!db.Users.Any(u => u.Id == TeacherId))
        {
            db.Users.Add(new User
            {
                Id = AdminId,
                Email = "admin-school-rate@jobsy.local",
                FullName = "Admin",
                Role = UserRole.Admin,
                IsActive = true
            });
            db.Users.Add(new User
            {
                Id = TeacherId,
                Email = "teacher-school-rate@jobsy.local",
                FullName = "Docent",
                Role = UserRole.SchoolAdmin,
                IsActive = true
            });
            db.SaveChanges();
        }

        _seeded = true;
    }
}
