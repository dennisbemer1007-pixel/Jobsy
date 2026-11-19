using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.OpenAi;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public sealed class CvUploadProposalIntegrationTests
{
    private static readonly string[] ForbiddenFragments =
    [
        "ReportLab",
        "opensource",
        "PDF Library",
        "pdf library",
    ];

    [Fact]
    public async Task ReportLab_cv_through_extractor_and_extraction_service_yields_grounded_proposals()
    {
        var pdf = await File.ReadAllBytesAsync(FixturePath("ReportLabDeLindeCv.pdf"));
        var extractor = new CvTextExtractor();
        var visibleText = extractor.Extract(pdf, CandidateCvFileRules.PdfContentType, "cv-reportlab.pdf");

        Assert.DoesNotContain("ReportLab", visibleText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("opensource", visibleText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("De Linde", visibleText, StringComparison.Ordinal);
        Assert.Contains("ROC Mondriaan", visibleText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BHV", visibleText, StringComparison.OrdinalIgnoreCase);

        var openAiJson = JsonSerializer.Serialize(new
        {
            roles = new[] { "opensource" },
            employers = new[] { new { employerName = "ReportLab PDF Library" } },
            educations = new[] { "MBO" },
            drivingLicenses = new[] { "B" },
            certificates = new[]
            {
                new { name = "ReportLab PDF Library", year = (int?)null },
                new { name = "BHV", year = (int?)2022 }
            }
        });
        var handler = new CvOpenAiRecordingHandler
        {
            ResponseJson = WrapChat(openAiJson)
        };
        var sut = new CvExtractionService(
            new CvNamedHttpClientFactory(handler),
            new OpenAiEndpointResolver(
                new CvStubCredentials("sk-test"),
                Options.Create(new OpenAiOptions { ApiKey = "sk-test", Model = "gpt-4o-mini" })),
            NullLogger<CvExtractionService>.Instance);

        var extracted = await sut.ExtractAsync(visibleText, "Test", "Kandidaat");
        var keys = CvExtractionFieldKeys.ListProposalKeys(extracted);
        var serialized = JsonSerializer.Serialize(extracted);

        Assert.Contains(CvExtractionFieldKeys.Employers, keys);
        Assert.Contains(CvExtractionFieldKeys.Educations, keys);
        Assert.Contains(CvExtractionFieldKeys.DrivingLicenses, keys);
        Assert.Contains(CvExtractionFieldKeys.Certificates, keys);
        Assert.DoesNotContain(CvExtractionFieldKeys.Roles, keys);

        Assert.Contains(extracted.Employers!, e => e.EmployerName.Contains("De Linde", StringComparison.OrdinalIgnoreCase));
        var deLinde = extracted.Employers!.First(e =>
            e.EmployerName.Contains("De Linde", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(deLinde.EndMonth);
        Assert.Equal("2024", deLinde.EndMonth![..4]);
        Assert.Contains(extracted.Educations!, e => e.Contains("MBO", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("B", extracted.DrivingLicenses!);
        Assert.Contains(extracted.Certificates!, c => c.Name.Contains("BHV", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(extracted.Certificates!, c => c.Name.Contains("VCA", StringComparison.OrdinalIgnoreCase));

        foreach (var fragment in ForbiddenFragments)
        {
            Assert.DoesNotContain(fragment, serialized, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Upload_endpoint_stores_grounded_suggestions_for_reportlab_cv()
    {
        await using var factory = new CvProposalUploadApiFactory();
        factory.ArmOpenAiRolesOnlyOpensource();

        var pdf = await File.ReadAllBytesAsync(FixturePath("ReportLabDeLindeCv.pdf"));
        using var client = factory.CreateClient();
        JobsyTestAuth.Authorize(client, factory.CandidateId);

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(pdf);
        file.Headers.ContentType = new MediaTypeHeaderValue(CandidateCvFileRules.PdfContentType);
        form.Add(file, "file", "cv-de-linde.pdf");

        var response = await client.PostAsync("api/me/cv", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var suggestedJson = doc.RootElement
            .GetProperty("uploadedCv")
            .GetProperty("suggestedProfile")
            .GetRawText();
        foreach (var fragment in ForbiddenFragments)
        {
            Assert.DoesNotContain(fragment, suggestedJson, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("De Linde", suggestedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MBO", suggestedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BHV", suggestedJson, StringComparison.OrdinalIgnoreCase);
        var pending = doc.RootElement
            .GetProperty("uploadedCv")
            .GetProperty("pendingProposalFields")
            .EnumerateArray()
            .Select(e => e.GetString())
            .Where(s => s is not null)
            .Cast<string>()
            .ToList();
        Assert.Contains(CvExtractionFieldKeys.Employers, pending, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(CvExtractionFieldKeys.Educations, pending, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(CvExtractionFieldKeys.Certificates, pending, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(CvExtractionFieldKeys.Roles, pending, StringComparer.OrdinalIgnoreCase);
    }

    private static string FixturePath(string name)
    {
        var dir = AppContext.BaseDirectory;
        var path = Path.Combine(dir, "Fixtures", "Cv", name);
        Assert.True(File.Exists(path), $"Missing fixture at {path}");
        return path;
    }

    private static string WrapChat(string content) => WrapChatForFactory(content);

    internal static string WrapChatForFactory(string content)
        => JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content } }
            }
        });

    private sealed class CvOpenAiRecordingHandler : HttpMessageHandler
    {
        public string ResponseJson { get; init; } = """{"choices":[{"message":{"content":"{}"}}]}""";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json")
            });
    }

    private sealed class CvNamedHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private sealed class CvStubCredentials(string? apiKey) : IIntegrationCredentialService
    {
        public Task<IntegrationCredentialView?> GetAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialView?>(null);

        public Task<IReadOnlyList<IntegrationCredentialView>> GetConfigurableAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IntegrationCredentialView>>([]);

        public Task<IntegrationCredentialView> UpsertAsync(
            IntegrationKey key,
            IntegrationCredentialUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SavePingResultAsync(
            IntegrationKey key,
            bool ok,
            string message,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetRawApiKeyAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(apiKey);

        public Task<string?> GetModelAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> GetBaseUrlAsync(IntegrationKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<IntegrationCredentialSecrets?> GetSecretsAsync(
            IntegrationKey key,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IntegrationCredentialSecrets?>(null);
    }
}

public sealed class CvProposalUploadApiFactory : ApiWebApplicationFactory
{
    private static readonly Guid CandidateGuid = Guid.Parse("c0ffee00-0000-0000-0000-0000000000c2");

    public Guid CandidateId => CandidateGuid;

    private readonly CvOpenAiStubHandler _openAi = new();

    private readonly string _dbName = "CvProposal-" + Guid.NewGuid();
    private readonly object _seedGate = new();
    private bool _seeded;

    public void ArmOpenAiRolesOnlyOpensource()
    {
        var openAiJson = JsonSerializer.Serialize(new
        {
            roles = new[] { "opensource" },
            employers = new[] { new { employerName = "ReportLab PDF Library" } },
            educations = new[] { "MBO" },
            drivingLicenses = new[] { "B" },
            certificates = new[]
            {
                new { name = "ReportLab PDF Library", year = (int?)null },
                new { name = "BHV", year = (int?)2022 }
            }
        });
        _openAi.ResponseJson = CvUploadProposalIntegrationTests.WrapChatForFactory(openAiJson);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("UploadScan:Enabled", "false");
        builder.UseSetting("OpenAi:ApiKey", "sk-test");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            RemoveEf(services);
            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
            services.RemoveAll<IUploadMalwareScanner>();
            services.AddSingleton<IUploadMalwareScanner, CvProposalAlwaysCleanScanner>();
            services.AddHttpClient("IntegrationProbe")
                .ConfigurePrimaryHttpMessageHandler(() => _openAi);
        });
    }

    private static void RemoveEf(IServiceCollection services)
    {
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
            db.Users.Add(new User
            {
                Id = CandidateId,
                Email = "cv-proposal@example.com",
                FullName = "Cv Proposal Tester",
                Role = UserRole.Candidate,
                IsActive = true,
                DateOfBirth = new DateOnly(1992, 4, 1)
            });
            db.SaveChanges();
            _seeded = true;
        }
    }

    private sealed class CvProposalAlwaysCleanScanner : IUploadMalwareScanner
    {
        public Task<UploadScanResult> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
            => Task.FromResult(UploadScanResult.Clean);
    }

    private sealed class CvOpenAiStubHandler : HttpMessageHandler
    {
        public string ResponseJson { get; set; } = """{"choices":[{"message":{"content":"{}"}}]}""";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json")
            });
    }
}
