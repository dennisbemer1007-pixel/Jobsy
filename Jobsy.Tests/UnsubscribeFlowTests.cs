using Jobsy.Api.Controllers;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class UnsubscribeFlowTests
{
    [Fact]
    public async Task Post_opts_out_get_preview_does_not_and_tampered_fails()
    {
        await using var db = CreateDb();
        var tokens = CreateTokens();
        var prefs = new EmailPreferenceService(db);
        var sut = new EmailPreferencesUnsubscribeController(tokens, prefs);

        var token = tokens.CreateToken("alex@example.com", "PushBom");
        var preview = sut.Preview(token);
        var previewBody = Assert.IsType<OkObjectResult>(preview.Result).Value as EmailPreferencesUnsubscribeController.UnsubscribePreviewResponse;
        Assert.True(previewBody!.Valid);
        Assert.False(await prefs.IsOptedOutAsync("alex@example.com", "PushBom"));

        var post = await sut.Unsubscribe(new EmailPreferencesUnsubscribeController.UnsubscribeRequest(token), CancellationToken.None);
        Assert.IsType<OkObjectResult>(post.Result);
        Assert.True(await prefs.IsOptedOutAsync("alex@example.com", "PushBom"));

        var bad = await sut.Unsubscribe(new EmailPreferencesUnsubscribeController.UnsubscribeRequest(token + "x"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(bad.Result);

        var expired = tokens.CreateToken("alex@example.com", "PushBom", DateTime.UtcNow.AddDays(-400));
        var expiredResult = await sut.Unsubscribe(new EmailPreferencesUnsubscribeController.UnsubscribeRequest(expired), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(expiredResult.Result);
    }

    [Fact]
    public async Task Opt_out_suppresses_pushbom_opt_in_restores_essential_still_sends()
    {
        await using var db = CreateDb();
        var tokens = CreateTokens();
        var prefs = new EmailPreferenceService(db);
        var email = new RecordingEmail();
        var mailer = CreateMailer(db, email, prefs, tokens);

        await prefs.OptOutAsync("alex@example.com", "PushBom", "Page");
        var push = TransactionalEmails.PushBom(
            "https://lobsy.nl", "Alex", "Bakker", "Bedrijf", Guid.NewGuid(), "Delft", 2.4, 12, 14.5m, "Uurloon",
            "https://lobsy.nl/candidate/actions/set-unavailable");
        var suppressed = await mailer.SendAsync(push, "alex@example.com");
        Assert.True(suppressed.Suppressed);
        Assert.Equal("opted-out", suppressed.Reason);
        Assert.Empty(email.Sent);

        await prefs.OptInAsync("alex@example.com", "PushBom");
        var restored = await mailer.SendAsync(push, "alex@example.com");
        Assert.True(restored.Sent);
        Assert.Single(email.Sent);
        Assert.Contains("List-Unsubscribe", email.Sent[0].Headers!.Keys);
        Assert.Contains("Afmelden voor deze mails", email.Sent[0].BodyHtml, StringComparison.Ordinal);
        Assert.Equal("support@lobsy.nl", email.Sent[0].ReplyTo);

        email.Sent.Clear();
        var essential = TransactionalEmails.ApplicationConfirmation(
            "https://lobsy.nl", "Alex", "Functie", "Bedrijf");
        await prefs.OptOutAsync("alex@example.com", "PushBom", "Page");
        var still = await mailer.SendAsync(essential, "alex@example.com");
        Assert.True(still.Sent);
        Assert.DoesNotContain(email.Sent[0].Headers!.Keys, k => k.Contains("List-Unsubscribe", StringComparison.OrdinalIgnoreCase));
    }

    private static MailUnsubscribeTokenService CreateTokens()
    {
        var provider = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "jobsy-mail-unsub-" + Guid.NewGuid().ToString("N")));
        return new MailUnsubscribeTokenService(provider);
    }

    private static TransactionalMailer CreateMailer(
        JobsyDbContext db,
        IEmailService email,
        IEmailPreferenceService prefs,
        IMailUnsubscribeTokenService tokens)
        => new(
            email,
            new Flags(true),
            new Features(),
            prefs,
            tokens,
            Options.Create(new MailOptions()),
            db,
            new Env("Testing"),
            NullLogger<TransactionalMailer>.Instance);

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class RecordingEmail : IEmailService
    {
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
        public void Invalidate() { }
    }

    private sealed class Features : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, false, "https://lobsy.nl", DateTime.UtcNow));
        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
