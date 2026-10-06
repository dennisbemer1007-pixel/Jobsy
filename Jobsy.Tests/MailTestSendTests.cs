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
        Assert.Contains("PlatformLog", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Resend", result.Message, StringComparison.OrdinalIgnoreCase);

        var log = Assert.Single(db.PlatformLogs);
        Assert.Equal("MailTest", log.Category);
        Assert.Contains("testmail", log.Message, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Resend", view.Description, StringComparison.Ordinal);
        Assert.True(view.LegalFooterMissing);
        Assert.Contains("noreply@lobsy.nl", view.FromAddress);
    }

    [Fact]
    public async Task Mail_card_names_lettermint_when_that_provider_has_a_key()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions { Provider = "Lettermint" }),
            Options.Create(new KvkOptions()),
            cache: null,
            lettermintOptions: Options.Create(new LettermintOptions { ApiKey = "lm_test_key" }));

        var view = await credentials.GetAsync(IntegrationKey.Mail);

        Assert.NotNull(view);
        Assert.Equal("Mail (Lettermint)", view!.DisplayName);
        Assert.Contains("Lettermint", view.Description, StringComparison.Ordinal);
        Assert.Contains("EU", view.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mail_card_stays_neutral_when_lettermint_has_no_key()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions { Provider = "Lettermint" }),
            Options.Create(new KvkOptions()),
            cache: null,
            lettermintOptions: Options.Create(new LettermintOptions()));

        var view = await credentials.GetAsync(IntegrationKey.Mail);

        Assert.Equal("Mail", view!.DisplayName);
        Assert.Contains("nog niet klaar", view.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mail_footer_warning_hides_when_company_details_have_address_and_kvk()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions()),
            Options.Create(new KvkOptions()),
            cache: null,
            lettermintOptions: null,
            legalIdentity: new FixedLegalIdentity(new LegalIdentitySnapshot(
                Name: "Lobsy B.V.",
                TradeName: "Lobsy",
                Street: "Straat 1",
                PostalCode: "1234 AB",
                City: "Delft",
                Country: "Nederland",
                KvkNumber: "87654321",
                VatNumber: null,
                PrivacyEmail: null,
                SupportEmail: "support@lobsy.nl",
                SchoolsEmail: null)));

        var view = await credentials.GetAsync(IntegrationKey.Mail);

        Assert.False(view!.LegalFooterMissing);
    }

    [Fact]
    public async Task Mail_footer_warning_hides_when_mail_env_has_address_and_kvk()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions
            {
                LegalAddress = "Markt 1, Delft",
                KvkNumber = "12345678"
            }));

        var view = await credentials.GetAsync(IntegrationKey.Mail);

        Assert.False(view!.LegalFooterMissing);
    }

    [Fact]
    public async Task Mail_footer_warning_shows_when_only_the_address_is_known()
    {
        await using var db = CreateDb();
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions { LegalAddress = "Markt 1, Delft" }),
            Options.Create(new KvkOptions()),
            cache: null,
            lettermintOptions: null,
            legalIdentity: new FixedLegalIdentity(new LegalIdentitySnapshot(
                Name: null,
                TradeName: "Lobsy",
                Street: null,
                PostalCode: null,
                City: null,
                Country: "Nederland",
                KvkNumber: null,
                VatNumber: null,
                PrivacyEmail: null,
                SupportEmail: "support@lobsy.nl",
                SchoolsEmail: null)));

        var view = await credentials.GetAsync(IntegrationKey.Mail);

        Assert.True(view!.LegalFooterMissing);
    }

    [Fact]
    public async Task Mail_partial_env_key_without_from_uses_default_from()
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
        // 03: From falls through to MailOptions default when DB/config From is empty.
        Assert.True(SmtpEmailService.TryResolveResend(secrets, out var resend, new MailOptions()));
        Assert.Contains("hallo@mail.lobsy.nl", resend.FromAddress, StringComparison.OrdinalIgnoreCase);

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
        return new IntegrationHealthStub(
            credentials,
            new FakeHttpClientFactory(),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new AlwaysOnFeatures(),
            Options.Create(new OpenAiOptions()),
            NullLogger<IntegrationHealthStub>.Instance);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class FixedLegalIdentity : ILegalIdentity
    {
        private readonly LegalIdentitySnapshot _snap;
        public FixedLegalIdentity(LegalIdentitySnapshot snap) => _snap = snap;
        public Task<LegalIdentitySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_snap);
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
