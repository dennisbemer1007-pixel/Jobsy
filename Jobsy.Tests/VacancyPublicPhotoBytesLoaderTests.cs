using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public sealed class VacancyPublicPhotoBytesLoaderTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public VacancyPublicPhotoBytesLoaderTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Map_popup_photo_returns_multikbyte_webp_for_local_seed_image()
    {
        var client = _factory.CreateClient();
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var pins = await _factory.CreateClient().GetFromJsonAsync<List<Jobsy.Api.Models.VacancyPinDto>>("api/vacancies/pins");
            Assert.NotNull(pins);
            Assert.NotEmpty(pins!);
            id = pins[0].Id;
            var vacancy = await db.Vacancies.FindAsync(id);
            Assert.NotNull(vacancy);
            vacancy!.ImageUrl = "/images/vacancies/tuinbouw.webp";
            await db.SaveChangesAsync();
            scope.ServiceProvider.GetRequiredService<IVacancyDiscoveryIndex>().Invalidate();
        }

        var response = await client.GetAsync($"api/vacancies/{id:D}/map-popup-photo?variant=desktop");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 3000, "Expected real photo WebP, not ~478-byte category tile.");
        Assert.Equal((byte)'R', bytes[0]);
    }

    [Fact]
    public async Task Loader_fetches_same_origin_photo_from_public_web_when_disk_misses()
    {
        var repoRoot = TestRepo.FindRoot();
        var sourcePath = Path.Combine(repoRoot, "Jobsy.Web", "wwwroot", "images", "vacancies", "tuinbouw.webp");
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath);
        Assert.True(sourceBytes.Length > 3000);

        var handler = new StubHttpHandler(req =>
        {
            if (req.RequestUri is not null
                && req.RequestUri.AbsolutePath.Contains("/images/vacancies/acc-fetch-probe.webp", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(sourceBytes)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpFactory = new SingleHandlerClientFactory(handler);
        var loader = new VacancyPublicPhotoBytesLoader(
            httpFactory,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicWebBaseUrl"] = "https://acceptatie.lobsy.nl" })
                .Build(),
            NullLogger<VacancyPublicPhotoBytesLoader>.Instance);

        var bytes = await loader.LoadPathViaPublicWebForTestsAsync(
            "/images/vacancies/acc-fetch-probe.webp",
            CancellationToken.None);

        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > 3000);
        Assert.Equal(1, handler.CallCount);
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_respond(request));
        }
    }

    private sealed class SingleHandlerClientFactory(HttpMessageHandler handler) : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _client = new(handler);

        public HttpClient CreateClient(string name) => _client;

        public void Dispose() => _client.Dispose();
    }
}
