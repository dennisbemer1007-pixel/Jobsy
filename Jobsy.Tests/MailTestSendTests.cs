using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
        Assert.Contains("niet geconfigureerd", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Resend", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(db.PlatformLogs);
    }

    [Fact]
    public async Task Mail_env_resend_credentials_fill_empty_db_secrets()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions
            {
                ResendApiKey = "re_test_key_123",
                FromAddress = "Lobsy <noreply@lobsy.nl>"
            }));

        var secrets = await credentials.GetSecretsAsync(IntegrationKey.Mail);
        Assert.NotNull(secrets);
        Assert.Equal("re_test_key_123", secrets!.ApiKey);
        Assert.Equal("Lobsy <noreply@lobsy.nl>", secrets.FromAddress);
        Assert.True(SmtpEmailService.TryResolveResend(secrets, out var resend));
        Assert.Equal("re_test_key_123", resend.ApiKey);

        var view = await credentials.GetAsync(IntegrationKey.Mail);
        Assert.NotNull(view);
        Assert.True(view!.HasApiKey);
        Assert.True(view.UsesEnvironmentCredentials);
        Assert.False(view.IgnoresEnvironmentCredentials);
        Assert.Equal("Mail (Resend)", view.DisplayName);
        Assert.Contains("noreply@lobsy.nl", view.FromAddress);
    }

    [Fact]
    public async Task Mail_partial_env_key_without_from_is_not_resend_ready()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions
            {
                ResendApiKey = "re_only_key",
                FromAddress = null
            }));

        var secrets = await credentials.GetSecretsAsync(IntegrationKey.Mail);
        Assert.NotNull(secrets);
        Assert.Equal("re_only_key", secrets!.ApiKey);
        Assert.Null(secrets.FromAddress);
        Assert.False(SmtpEmailService.TryResolveResend(secrets, out _));

        var view = await credentials.GetAsync(IntegrationKey.Mail);
        Assert.True(view!.HasApiKey);
        Assert.True(string.IsNullOrWhiteSpace(view.FromAddress));
        Assert.True(view.UsesEnvironmentCredentials);
    }

    [Fact]
    public async Task Mail_clear_secrets_suppresses_env_until_reenabled()
    {
        await using var db = CreateDb();
        var mailOptions = Options.Create(new MailOptions
        {
            ResendApiKey = "re_from_env",
            FromAddress = "env@lobsy.nl"
        });
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector(), mailOptions);

        Assert.True(SmtpEmailService.TryResolveResend(await credentials.GetSecretsAsync(IntegrationKey.Mail), out _));

        await credentials.UpsertAsync(
            IntegrationKey.Mail,
            new IntegrationCredentialUpdate(ClearApiKey: true));

        var afterClear = await credentials.GetSecretsAsync(IntegrationKey.Mail);
        Assert.True(afterClear is null
            || (string.IsNullOrWhiteSpace(afterClear.ApiKey) && string.IsNullOrWhiteSpace(afterClear.FromAddress)));
        Assert.False(SmtpEmailService.TryResolveResend(afterClear, out _));

        var view = await credentials.GetAsync(IntegrationKey.Mail);
        Assert.True(view!.IgnoresEnvironmentCredentials);
        Assert.False(view.HasApiKey);
        Assert.False(view.UsesEnvironmentCredentials);

        await credentials.UpsertAsync(
            IntegrationKey.Mail,
            new IntegrationCredentialUpdate(UseEnvironmentCredentials: true));

        Assert.True(SmtpEmailService.TryResolveResend(await credentials.GetSecretsAsync(IntegrationKey.Mail), out var resend));
        Assert.Equal("re_from_env", resend.ApiKey);
        var reenabled = await credentials.GetAsync(IntegrationKey.Mail);
        Assert.False(reenabled!.IgnoresEnvironmentCredentials);
        Assert.True(reenabled.UsesEnvironmentCredentials);
    }

    [Fact]
    public async Task Mail_db_resend_key_wins_over_env()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions
            {
                ResendApiKey = "re_from_env",
                FromAddress = "env@lobsy.nl"
            }));

        await credentials.UpsertAsync(
            IntegrationKey.Mail,
            new IntegrationCredentialUpdate(
                ApiKey: "re_from_db",
                FromAddress: "db@lobsy.nl"));

        var secrets = await credentials.GetSecretsAsync(IntegrationKey.Mail);
        Assert.Equal("re_from_db", secrets!.ApiKey);
        Assert.Equal("db@lobsy.nl", secrets.FromAddress);
        var view = await credentials.GetAsync(IntegrationKey.Mail);
        Assert.False(view!.UsesEnvironmentCredentials);
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
        Assert.NotNull(secrets);
        Assert.Equal("smtp.gmail.com:465", secrets.BaseUrl);
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
            new FakeHttpClientFactory(),
            new FakeHostEnvironment { EnvironmentName = Environments.Development },
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

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
