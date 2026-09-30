using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class EmailPreviewTests
{
    [Fact]
    public void Preview_every_key_language_and_theme_returns_html_and_text()
    {
        var sut = CreateSut();
        foreach (var key in TransactionalEmails.Templates.Select(t => t.Key))
        {
            foreach (var lang in EmailStrings.Languages)
            {
                foreach (var theme in new[] { "light", "dark" })
                {
                    var preview = sut.Preview(key, lang, theme, "https://lobsy.nl");
                    Assert.False(string.IsNullOrWhiteSpace(preview.Html), $"{key}/{lang}/{theme} html");
                    Assert.False(string.IsNullOrWhiteSpace(preview.Text), $"{key}/{lang}/{theme} text");
                    Assert.False(string.IsNullOrWhiteSpace(preview.Subject));
                    Assert.Equal(lang == "ar" ? "rtl" : "ltr", preview.Dir);
                    Assert.DoesNotContain("sk_live", preview.Html, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("password=", preview.Html, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void Preview_rejects_unknown_key_and_language()
    {
        var sut = CreateSut();
        Assert.Throws<KeyNotFoundException>(() => sut.Preview("Nope", "nl", "light", "https://lobsy.nl"));
        Assert.Throws<ArgumentException>(() => sut.Preview("MailTest", "xx", "light", "https://lobsy.nl"));
        Assert.Throws<ArgumentException>(() => sut.Preview("MailTest", "nl", "neon", "https://lobsy.nl"));
    }

    [Fact]
    public void Preview_does_not_require_db_context()
    {
        // Constructor no longer takes JobsyDbContext — compile-time proof of fake-data-only previews.
        var sut = CreateSut();
        var preview = sut.Preview("ApplicationConfirmation", "nl", "light", "https://lobsy.nl");
        Assert.Contains("Alex", preview.Html, StringComparison.Ordinal);
    }

    private static EmailCatalogService CreateSut()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEmailCatalogService>(_ => null!);
        var sp = services.BuildServiceProvider();
        return new EmailCatalogService(
            new NoopMailer(),
            new FakeFeatures(),
            new NoopAudit(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MailOptions()),
            NullLogger<EmailCatalogService>.Instance);
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, false, "https://lobsy.nl", DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class NoopAudit : IAdminAuditLog
    {
        public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Stage(AdminAuditEntry entry) { }
    }

    private sealed class NoopMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Stub));
    }
}
