using System.Net;
using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class LettermintRequestTests
{
    [Fact]
    public void Payload_has_from_reply_to_text_html_tags_metadata_attachment_and_tracking_off()
    {
        var message = new EmailMessage("alex@example.com", "Tips", "<p>hello there</p>", "PushBom")
        {
            BodyText = "hello there",
            ReplyTo = "support@lobsy.nl",
            Headers = new Dictionary<string, string>
            {
                ["List-Unsubscribe"] = "<https://lobsy.nl/mail/afmelden?t=abc>",
                ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click"
            },
            Tags =
            [
                ("category", "PushBom"),
                ("lang", "nl")
            ],
            IdempotencyKey = "pushbom:1",
            Attachments = [new EmailAttachment("note.pdf", "application/pdf", [1, 2, 3, 4])]
        };

        var json = JsonSerializer.Serialize(
            LettermintEmailSender.CreateRequest(message, "Lobsy <hallo@mail.lobsy.nl>"));

        Assert.Contains("\"reply_to\"", json, StringComparison.Ordinal);
        Assert.Contains("support@lobsy.nl", json, StringComparison.Ordinal);
        Assert.Contains("\"html\"", json, StringComparison.Ordinal);
        Assert.Contains("\"text\"", json, StringComparison.Ordinal);
        Assert.Contains("List-Unsubscribe", json, StringComparison.Ordinal);
        Assert.Contains("\"tags\"", json, StringComparison.Ordinal);
        Assert.Contains("\"metadata\"", json, StringComparison.Ordinal);
        Assert.Contains("PushBom", json, StringComparison.Ordinal);
        Assert.Contains("note.pdf", json, StringComparison.Ordinal);
        Assert.Contains("\"track_opens\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"track_clicks\":false", json, StringComparison.Ordinal);
        Assert.DoesNotContain("open_tracking", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("click_tracking", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Error_text_hides_the_api_key_and_the_recipient()
    {
        const string key = "lm_secret_key_value";
        var message = LettermintEmailSender.FormatError(
            422,
            $"bad recipient alex@example.com token {key}",
            key);

        Assert.DoesNotContain(key, message, StringComparison.Ordinal);
        Assert.DoesNotContain("alex@example.com", message, StringComparison.Ordinal);
        Assert.Contains("a••••@example.com", message, StringComparison.Ordinal);
    }
}

public class MailRecipientAllowListTests
{
    private const string Pattern = @"^test-[^@]+@lobsy\.nl$";

    public MailRecipientAllowListTests() => MailRecipientAllowList.ResetForTests();

    [Fact]
    public void Empty_pattern_allows_every_recipient()
    {
        var decision = MailRecipientAllowList.Evaluate(
            "jan@example.com",
            new MailOptions());

        Assert.False(decision.Blocked);
    }

    [Theory]
    [InlineData("test-kandidaat@lobsy.nl", false)]
    [InlineData("Test-Admin@Lobsy.nl", false)]
    [InlineData("Lobsy <test-beheer@lobsy.nl>", false)]
    [InlineData("jan@lobsy.nl", true)]
    [InlineData("test-jan@example.com", true)]
    public void Acceptatie_pattern_allows_only_test_addresses(string recipient, bool blocked)
    {
        var decision = MailRecipientAllowList.Evaluate(recipient, new MailOptions
        {
            AllowedRecipientPattern = Pattern
        });

        Assert.Equal(blocked, decision.Blocked);
    }

    [Fact]
    public void An_extra_address_is_allowed_beside_the_pattern()
    {
        var decision = MailRecipientAllowList.Evaluate("Dennis <dennis@example.com>", new MailOptions
        {
            AllowedRecipientPattern = Pattern,
            AllowedRecipientAddresses = ["dennis@example.com"]
        });

        Assert.False(decision.Blocked);
    }

    [Fact]
    public void A_blocked_address_is_masked_and_the_full_address_is_not_in_the_mask()
    {
        const string raw = "geheim-persoon@bedrijf.nl";
        var decision = MailRecipientAllowList.Evaluate(raw, new MailOptions
        {
            AllowedRecipientPattern = Pattern
        });

        Assert.True(decision.Blocked);
        Assert.Equal(EmailMask.Mask(raw), decision.MaskedAddress);
        Assert.DoesNotContain("geheim-persoon@", decision.MaskedAddress, StringComparison.Ordinal);
    }

    [Fact]
    public void An_invalid_pattern_blocks_and_warns_once()
    {
        var options = new MailOptions { AllowedRecipientPattern = "(" };
        var first = MailRecipientAllowList.Evaluate("jan@example.com", options);
        var warned = MailRecipientAllowList.ConsumeInvalidPatternWarning();
        var warnedAgain = MailRecipientAllowList.ConsumeInvalidPatternWarning();

        Assert.True(first.Blocked);
        Assert.True(first.PatternInvalid);
        Assert.True(warned);
        Assert.False(warnedAgain);
    }
}

public class MailProviderChoiceTests
{
    [Theory]
    [InlineData(null, false, MailProviderKind.Resend, false)]
    [InlineData("Resend", true, MailProviderKind.Resend, false)]
    [InlineData("lettermint", false, MailProviderKind.NotConfigured, true)]
    [InlineData("Lettermint", true, MailProviderKind.Lettermint, false)]
    public void Provider_follows_config_and_falls_back_without_a_key(
        string? provider,
        bool keyConfigured,
        MailProviderKind expected,
        bool warn)
    {
        var choice = MailProviderChoice.Choose(provider, keyConfigured);
        Assert.Equal(expected, choice.Kind);
        Assert.Equal(warn, choice.WarnMissingLettermintKey);
    }

    [Fact]
    public void Missing_key_warning_is_logged_once()
    {
        MailProviderFallbackLog.ResetForTests();
        Assert.True(MailProviderFallbackLog.ShouldLogLettermintFallback());
        Assert.False(MailProviderFallbackLog.ShouldLogLettermintFallback());
    }
}

public class LettermintSendTests
{
    [Fact]
    public async Task Lettermint_post_uses_the_token_header_and_idempotency_key()
    {
        var handler = new CaptureHandler(HttpStatusCode.Accepted, """{"message_id":"m_1","status":"pending"}""");
        var db = CreateDb();
        var sut = CreateSender(db, handler, new MailOptions { Provider = "Lettermint" }, new LettermintOptions
        {
            ApiKey = "lm_test_key"
        });

        var result = await sut.SendAsync(Sample("alex@example.com") with { IdempotencyKey = "msg-1" });

        Assert.True(result.DeliveredViaProvider);
        Assert.NotNull(handler.Uri);
        Assert.EndsWith("/send", handler.Uri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal("lm_test_key", handler.Token);
        Assert.Equal("msg-1", handler.IdempotencyKey);
        Assert.Contains("\"track_opens\":false", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_lettermint_key_does_not_call_resend()
    {
        MailProviderFallbackLog.ResetForTests();
        var handler = new CaptureHandler(HttpStatusCode.OK, """{"id":"re_1"}""");
        var db = CreateDb();
        var logger = new ListLogger();
        var mail = new MailOptions { Provider = "Lettermint", ResendApiKey = "re_test_key" };
        var sut = CreateSender(db, handler, mail, new LettermintOptions(), logger);

        var first = await sut.SendAsync(Sample("alex@example.com"));
        var second = await sut.SendAsync(Sample("alex@example.com"));

        Assert.Null(handler.Uri);
        Assert.False(first.DeliveredViaProvider);
        Assert.False(second.DeliveredViaProvider);
        Assert.Equal(1, logger.Errors.Count(message => message.Contains("Mail: niet ingesteld", StringComparison.Ordinal)));
        Assert.Contains(db.PlatformLogs, row => row.Message.Contains("Mail: niet ingesteld", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Allowlist_skips_other_recipients_without_logging_the_full_address()
    {
        var handler = new CaptureHandler(HttpStatusCode.Accepted, "{}");
        var db = CreateDb();
        var sut = CreateSender(
            db,
            handler,
            new MailOptions
            {
                Provider = "Lettermint",
                AllowedRecipientPattern = @"^test-[^@]+@lobsy\.nl$",
                AllowedRecipientAddresses = ["dennis@example.com"]
            },
            new LettermintOptions { ApiKey = "lm_test_key" });

        const string blocked = "geheim-persoon@bedrijf.nl";
        var skipped = await sut.SendAsync(new EmailMessage(blocked, "onderwerp " + blocked, "<p>hoi hoi</p>"));
        var allowed = await sut.SendAsync(Sample("test-acc@lobsy.nl"));
        var admin = await sut.SendAsync(Sample("dennis@example.com"));

        Assert.False(skipped.DeliveredViaProvider);
        Assert.True(allowed.DeliveredViaProvider);
        Assert.True(admin.DeliveredViaProvider);
        Assert.Equal(2, handler.Calls);
        var log = Assert.Single(db.PlatformLogs, entry => entry.Message.Contains("allowlist", StringComparison.Ordinal));
        Assert.DoesNotContain(blocked, log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(blocked, log.DetailsJson ?? "", StringComparison.Ordinal);
        Assert.Contains(EmailMask.Mask(blocked), log.Message, StringComparison.Ordinal);
    }

    private static EmailMessage Sample(string to)
        => new(to, "Hallo", "<p>hello there</p>", "UnitMail")
        {
            BodyText = "hello there",
            ReplyTo = "support@lobsy.nl"
        };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static SmtpEmailService CreateSender(
        JobsyDbContext db,
        CaptureHandler handler,
        MailOptions mail,
        LettermintOptions lettermint,
        ListLogger? logger = null)
    {
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector(), Options.Create(mail));
        var http = new SingleClientFactory(handler);
        return new SmtpEmailService(
            credentials,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            db,
            http,
            new ProductionHost(),
            new FlagsOn(),
            Options.Create(mail),
            Options.Create(lettermint),
            logger ?? new ListLogger());
    }

    private sealed class ProductionHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FlagsOn : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(FeatureFlagSnapshot.Defaults);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate()
        {
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false)
            {
                BaseAddress = name == LettermintEmailSender.HttpClientName
                    ? new Uri(LettermintEmailSender.DefaultApiBase)
                    : new Uri(SmtpEmailService.DefaultResendApiBase)
            };
    }

    private sealed class CaptureHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public string? Body { get; private set; }
        public string? Token { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Uri = request.RequestUri;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Token = request.Headers.TryGetValues(LettermintEmailSender.TokenHeaderName, out var values)
                ? values.Single()
                : null;
            IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var keys)
                ? keys.Single()
                : null;
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody)
            };
        }
    }

    private sealed class ListLogger : ILogger<SmtpEmailService>
    {
        public List<string> Warnings { get; } = [];
        public List<string> Errors { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }

            if (logLevel == LogLevel.Error)
            {
                Errors.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
