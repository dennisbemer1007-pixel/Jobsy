using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class TransactionalMailerTests
{
    [Fact]
    public async Task Suppresses_when_employers_disabled_for_requires_employers()
    {
        await using var db = CreateDb();
        var email = new RecordingEmail();
        var mailer = CreateMailer(db, email, employersEnabled: false, ambassadorsEnabled: false);
        var mail = TransactionalEmails.ApplicationConfirmation(
            "https://lobsy.nl", "Alex", "Functie", "Bedrijf", false);
        var outcome = await mailer.SendAsync(mail, "alex@example.com");
        Assert.True(outcome.Suppressed);
        Assert.Equal("employers disabled", outcome.Reason);
        Assert.Empty(email.Sent);
        Assert.Contains(db.PlatformLogs, l => l.Category == "email.suppressed" && l.Message.Contains("a***@example.com"));
    }

    [Fact]
    public async Task Suppresses_ambassadeur_when_ambassadors_disabled()
    {
        await using var db = CreateDb();
        var email = new RecordingEmail();
        var mailer = CreateMailer(db, email, employersEnabled: true, ambassadorsEnabled: false);
        var mail = TransactionalEmails.AmbassadeurInvite("https://lobsy.nl", "Sam", "sam@example.com", null);
        var outcome = await mailer.SendAsync(mail, "sam@example.com");
        Assert.True(outcome.Suppressed);
        Assert.Equal("ambassadors disabled", outcome.Reason);
        Assert.Empty(email.Sent);
    }

    [Fact]
    public async Task Sends_with_category_when_allowed()
    {
        await using var db = CreateDb();
        var email = new RecordingEmail();
        var mailer = CreateMailer(db, email, employersEnabled: true, ambassadorsEnabled: true);
        var mail = TransactionalEmails.MailTest("https://lobsy.nl");
        var outcome = await mailer.SendAsync(mail, "admin@example.com");
        Assert.True(outcome.Sent);
        Assert.False(outcome.Suppressed);
        Assert.Single(email.Sent);
        Assert.Equal("MailTest", email.Sent[0].Category);
        Assert.Contains("data-lobsy-layout=\"2\"", email.Sent[0].BodyHtml);
    }

    private static TransactionalMailer CreateMailer(
        JobsyDbContext db,
        IEmailService email,
        bool employersEnabled,
        bool ambassadorsEnabled)
        => new(
            email,
            new Flags(employersEnabled),
            new Features(ambassadorsEnabled),
            db,
            new Env("Testing"),
            NullLogger<TransactionalMailer>.Instance);

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class RecordingEmail : IEmailService, ITransactionalMailer
    {
        public async Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var delivery = await SendAsync(
                new EmailMessage(to, mail.Subject, mail.Html ?? string.Empty, mail.Category),
                cancellationToken);
            return new EmailSendOutcome(true, false, null, delivery.Kind);
        }

        public List<EmailMessage> Sent { get; } = [];
        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class Flags(bool employers) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature != PlatformFeature.Employers || employers);

        public void Invalidate()
        {
        }
    }

    private sealed class Features(bool ambassadors) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                true, false, false, "https://lobsy.nl", DateTime.UtcNow, AmbassadorsEnabled: ambassadors));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
