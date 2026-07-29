using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class MailTestSendTests
{
    [Fact]
    public async Task Send_test_mail_rejects_invalid_address()
    {
        await using var db = CreateDb();
        var sut = CreateHealth(db);

        var result = await sut.SendTestMailAsync("not-an-email");

        Assert.False(result.Ok);
        Assert.False(result.SentViaSmtp);
        Assert.Contains("geldig", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Send_test_mail_without_smtp_goes_to_stub_and_reports_failure()
    {
        await using var db = CreateDb();
        var sut = CreateHealth(db);

        var result = await sut.SendTestMailAsync("tester@example.com");

        Assert.False(result.Ok);
        Assert.False(result.SentViaSmtp);
        Assert.Contains("PlatformLog", result.Message, StringComparison.OrdinalIgnoreCase);

        var log = Assert.Single(db.PlatformLogs);
        Assert.Equal("MailTest", log.Category);
        Assert.Contains("testmail", log.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mail_save_with_host_and_port_stores_combined_base_url()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());

        await credentials.UpsertAsync(
            IntegrationKey.Mail,
            new IntegrationCredentialUpdate(BaseUrl: "smtp.gmail.com:465"));

        var secrets = await credentials.GetSecretsAsync(IntegrationKey.Mail);
        Assert.Equal("smtp.gmail.com:465", secrets?.BaseUrl);
        Assert.True(SmtpEmailService.TryResolveSmtp(
            secrets with
            {
                ClientId = "u@gmail.com",
                ClientSecret = "app-pass",
                FromAddress = "u@gmail.com"
            },
            out var settings));
        Assert.Equal("smtp.gmail.com", settings.Host);
        Assert.Equal(465, settings.Port);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static IntegrationHealthStub CreateHealth(JobsyDbContext db)
    {
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        var email = new SmtpEmailService(
            credentials,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            db,
            NullLogger<SmtpEmailService>.Instance);
        return new IntegrationHealthStub(
            credentials,
            new FakeHttpClientFactory(),
            email,
            Options.Create(new OpenAiOptions()),
            NullLogger<IntegrationHealthStub>.Instance);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
