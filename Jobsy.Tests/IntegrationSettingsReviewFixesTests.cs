using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class IntegrationSettingsReviewFixesTests
{
    [Theory]
    [InlineData("https://api.openai.com/v1/", true)]
    [InlineData("https://api.mollie.com/v2", true)]
    [InlineData("http://127.0.0.1/v1", false)]
    [InlineData("http://127.0.0.2/v1", false)]
    [InlineData("https://192.168.1.10/v1", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("", true)]
    public void BaseUrl_validation_blocks_private_and_non_http(string input, bool expectedOk)
    {
        var ok = IntegrationEndpointUrl.TryNormalizeBaseUrl(input, out _, out var error);
        Assert.Equal(expectedOk, ok);
        if (!expectedOk)
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }

    [Theory]
    [InlineData("smtp.gmail.com", true, "smtp.gmail.com")]
    [InlineData("smtp.gmail.com:587", true, "smtp.gmail.com:587")]
    [InlineData("smtp://smtp.gmail.com:587", true, "smtp.gmail.com:587")]
    [InlineData("smtps://smtp.gmail.com:465", true, "smtp.gmail.com:465")]
    [InlineData("https://smtp.gmail.com", false, null)]
    [InlineData("http://smtp.gmail.com", false, null)]
    [InlineData("localhost", false, null)]
    [InlineData("127.0.0.1", false, null)]
    [InlineData("user:pass@smtp.gmail.com", false, null)]
    [InlineData("", true, null)]
    public void Smtp_host_validation_accepts_gmail_formats(string input, bool expectedOk, string? expectedNormalized)
    {
        var ok = IntegrationEndpointUrl.TryNormalizeSmtpHost(input, out var normalized, out var error);
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedNormalized, normalized);
        if (!expectedOk)
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }

    [Fact]
    public async Task Mail_accepts_smtp_host_without_https()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var sut = new IntegrationCredentialService(db, new PassthroughSecretProtector());

        await sut.UpsertAsync(
            Jobsy.Core.Enums.IntegrationKey.Mail,
            new IntegrationCredentialUpdate(BaseUrl: "smtp.gmail.com"));

        var secrets = await sut.GetSecretsAsync(Jobsy.Core.Enums.IntegrationKey.Mail);
        Assert.Equal("smtp.gmail.com", secrets?.BaseUrl);
    }

    [Fact]
    public async Task Moderation_disabled_allows_any_text()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions { VacancyContentModerationEnabled = true }),
            config);
        await features.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: false,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201"));

        var sut = new VacancyContentModerationService(
            new FakeHttpClientFactory(),
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            features,
            Options.Create(new OpenAiOptions()),
            NullLogger<VacancyContentModerationService>.Instance);

        var result = await sut.CheckAsync(
            "Maximaal 25 jaar",
            "Alleen vrouwen. Geen buitenlanders.");

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task Rejects_private_base_url_on_credential_save()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var sut = new IntegrationCredentialService(db, new PassthroughSecretProtector());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpsertAsync(
                Jobsy.Core.Enums.IntegrationKey.OpenAI,
                new IntegrationCredentialUpdate(BaseUrl: "http://127.0.0.1:8080/v1")));

        Assert.Contains("privé", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
