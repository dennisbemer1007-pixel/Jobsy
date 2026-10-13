using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class DiplomaEvaluationApiTests : IClassFixture<DiplomaEvaluationApiFactory>
{
    private readonly DiplomaEvaluationApiFactory _factory;

    public DiplomaEvaluationApiTests(DiplomaEvaluationApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_and_non_candidate_cannot_write()
    {
        using var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("api/me/diploma-evaluations", ValidBody())).StatusCode);

        using var staff = Authed(_factory.StaffId);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("api/me/diploma-evaluations", ValidBody())).StatusCode);
    }

    [Fact]
    public async Task Candidate_adds_edits_and_profile_exposes_fields_not_file_bytes()
    {
        using var client = Authed(_factory.CandidateId);
        var created = await client.PostAsJsonAsync("api/me/diploma-evaluations", ValidBody());
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var createdJson = await created.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdJson);
        var id = createdDoc.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("vergelijkbaar met hbo-bachelor", createdDoc.RootElement.GetProperty("equivalentLevelText").GetString());
        Assert.False(createdDoc.RootElement.GetProperty("hasDocument").GetBoolean());

        var pngNamedPdf = new ByteArrayContent("not-a-pdf"u8.ToArray());
        pngNamedPdf.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        using var bad = new MultipartFormDataContent();
        bad.Add(pngNamedPdf, "file", "waardering.pdf");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"api/me/diploma-evaluations/{id}/document", bad)).StatusCode);

        var pdfBytes = "%PDF-1.4\n%"u8.ToArray();
        using var upload = new MultipartFormDataContent();
        var file = new ByteArrayContent(pdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        upload.Add(file, "file", "waardering.pdf");
        var uploaded = await client.PostAsync($"api/me/diploma-evaluations/{id}/document", upload);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var uploadedJson = await uploaded.Content.ReadAsStringAsync();
        Assert.Contains("waardering.pdf", uploadedJson, StringComparison.Ordinal);
        Assert.DoesNotContain("JVBERi", uploadedJson, StringComparison.Ordinal);

        var download = await client.GetAsync($"api/me/diploma-evaluations/{id}/document");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var downloaded = await download.Content.ReadAsByteArrayAsync();
        Assert.Equal((byte)'%', downloaded[0]);

        using var other = Authed(_factory.OtherCandidateId);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"api/me/diploma-evaluations/{id}/document")).StatusCode);

        var edited = await client.PutAsJsonAsync($"api/me/diploma-evaluations/{id}", ValidBody() with { ReferenceNumber = "IDW-100" });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Contains("IDW-100", await edited.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var profile = await client.GetAsync("api/me/profile");
        var profileJson = await profile.Content.ReadAsStringAsync();
        Assert.Contains("vergelijkbaar met hbo-bachelor", profileJson, StringComparison.Ordinal);
        Assert.Contains("IDW-100", profileJson, StringComparison.Ordinal);
        Assert.DoesNotContain("documentContent", profileJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JVBERi", profileJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"educations\":[\"hbo-bachelor\"]", profileJson, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/me/diploma-evaluations/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/me/diploma-evaluations/{id}/document")).StatusCode);
    }

    [Fact]
    public async Task Minor_without_parental_consent_cannot_save()
    {
        using var client = Authed(_factory.MinorId);
        var response = await client.PostAsJsonAsync("api/me/diploma-evaluations", ValidBody());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("parental_consent_required", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }

    private static Upsert ValidBody() => new(
        "Lisans",
        "nuffic",
        null,
        "vergelijkbaar met hbo-bachelor",
        "hbo-bachelor",
        new DateOnly(2024, 6, 1),
        "IDW-99");

    private sealed record Upsert(
        string? DiplomaTitle,
        string? IssuingBody,
        string? IssuingBodyOther,
        string? EquivalentLevelText,
        string? EquivalentLevelCode,
        DateOnly? EvaluationDate,
        string? ReferenceNumber);
}

public sealed class DiplomaEvaluationApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    public Guid CandidateId { get; } = Guid.Parse("d1a10000-0000-0000-0000-000000000001");
    public Guid OtherCandidateId { get; } = Guid.Parse("d1a10000-0000-0000-0000-000000000002");
    public Guid MinorId { get; } = Guid.Parse("d1a10000-0000-0000-0000-000000000003");
    public Guid StaffId { get; } = Guid.Parse("d1a10000-0000-0000-0000-000000000004");

    private readonly string _dbName = "DiplomaEval-" + Guid.NewGuid();
    private readonly object _seedGate = new();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
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

            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
            services.RemoveAll<IVacancyContentModerationService>();
            services.AddSingleton<IVacancyContentModerationService>(new AllowAllModeration());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        lock (_seedGate)
        {
            if (_seeded)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Users.AddRange(
                User(CandidateId, "eval-adult@example.com", UserRole.Candidate, new DateOnly(1992, 4, 1)),
                User(OtherCandidateId, "eval-other@example.com", UserRole.Candidate, new DateOnly(1994, 4, 1)),
                User(MinorId, "eval-minor@example.com", UserRole.Candidate, new DateOnly(2012, 6, 1)),
                User(StaffId, "eval-staff@example.com", UserRole.BranchManager, null));
            db.SaveChanges();
            _seeded = true;
        }
    }

    private static User User(Guid id, string email, UserRole role, DateOnly? dob) => new()
    {
        Id = id,
        Email = email,
        FullName = "Eval Tester",
        Role = role,
        IsActive = true,
        DateOfBirth = dob
    };

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
