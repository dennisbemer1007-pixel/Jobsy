using Jobsy.Api.Controllers;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Security;
using Jobsy.Core.Time;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

/// <summary>Stack-end E2E coverage for 09.3 (capturing mail sink, no live browser).</summary>
public class EmailStackE2eFlowTests
{
    [Fact]
    public async Task Invite_set_password_login_path_has_no_password_in_mail()
    {
        await using var db = CreateDb();
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var email = new CapturingEmail();
        var invite = new SalesManagerInviteService(
            db, email, links, new AlwaysOnFeatures(), NullLogger<SalesManagerInviteService>.Instance);

        var result = await invite.InviteAsync("new.sm@jobsy.local", "New SM");
        Assert.Contains("/account/wachtwoord-instellen?t=", email.LastHtml);
        Assert.Contains("/account/wachtwoord-instellen?t=", email.LastText);
        Assert.DoesNotContain("Wachtwoord:", email.LastHtml!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", email.LastHtml!, StringComparison.OrdinalIgnoreCase);

        var token = Extract(email.LastHtml!, "/account/wachtwoord-instellen?t=");
        var setup = new AccountSetupController(db, links);
        var preview = await setup.PreviewSetupLink(token, CancellationToken.None);
        var previewOk = Assert.IsType<OkObjectResult>(preview.Result);
        Assert.NotNull(previewOk.Value);

        var ok = await setup.SetupPassword(
            new AccountSetupController.SetupPasswordRequest(token, "GoedWachtwoord12!"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(ok);
        Assert.True(await db.LocalAuthCredentials.AnyAsync(c => c.UserId == result.UserId));

        var reuse = await setup.SetupPassword(
            new AccountSetupController.SetupPasswordRequest(token, "AnderWachtwoord12!"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(reuse);

        email.Clear();
        await invite.InviteAsync("new.sm@jobsy.local", "New SM");
        if (!string.IsNullOrWhiteSpace(email.LastHtml)
            && email.LastHtml.Contains("/account/wachtwoord-instellen?t=", StringComparison.Ordinal))
        {
            var token2 = Extract(email.LastHtml!, "/account/wachtwoord-instellen?t=");
            Assert.NotEqual(token, token2);
        }

        var oldDead = await setup.SetupPassword(
            new AccountSetupController.SetupPasswordRequest(token, "GoedWachtwoord12!"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(oldDead);
    }

    [Fact]
    public async Task Parental_consent_preview_get_do_not_mutate_post_is_idempotent()
    {
        await using var db = CreateDb();
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "child@jobsy.local",
            FullName = "Sanne Kind",
            FirstName = "Sanne",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)),
            ParentalConsentEmail = "parent@example.com",
            ParentalConsentTokenHash = VerificationCodes.Hash(token),
            ParentalConsentTokenExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mail = TransactionalEmails.ParentalConsent(
            "https://lobsy.nl", "Sanne", "https://lobsy.nl/toestemming?t=" + token, DateTime.UtcNow.AddDays(7));
        Assert.Contains("Sanne", mail.Text, StringComparison.Ordinal);
        Assert.Contains("/toestemming", mail.Html, StringComparison.Ordinal);

        var sut = new ParentalConsentController(db, new Features("https://lobsy.nl"));
        Assert.Null((await db.Users.SingleAsync(u => u.Id == user.Id)).ParentalConsentAt);
        var preview = await sut.Preview(token, CancellationToken.None);
        Assert.Null((await db.Users.SingleAsync(u => u.Id == user.Id)).ParentalConsentAt);

        var get = await sut.ConfirmGet(token, CancellationToken.None);
        Assert.IsType<RedirectResult>(get);
        Assert.Null((await db.Users.SingleAsync(u => u.Id == user.Id)).ParentalConsentAt);

        var post = await sut.ConfirmPost(new ParentalConsentController.ParentalConsentConfirmRequest(token), CancellationToken.None);
        Assert.IsType<OkObjectResult>(post.Result);
        Assert.NotNull((await db.Users.SingleAsync(u => u.Id == user.Id)).ParentalConsentAt);

        var second = await sut.ConfirmPost(new ParentalConsentController.ParentalConsentConfirmRequest(token), CancellationToken.None);
        Assert.IsType<OkObjectResult>(second.Result);
    }

    [Fact]
    public async Task Api_key_reveal_mail_has_link_not_key_and_rotates_once()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Reveal Co",
            KvkNumber = "87654321",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var email = new CapturingEmail();
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var sut = new CompanyApiKeyService(
            db, email, new FixedConfig("PublicApiBaseUrl", "https://api.voorbeeld.invalid"),
            links, new AlwaysOnFeatures(), NullLogger<CompanyApiKeyService>.Instance);

        var first = await sut.GenerateAsync(companyId);
        await sut.EmailCredentialsAsync(companyId, "mgr@example.com");
        Assert.DoesNotContain(first.PlaintextKey, email.LastHtml!);
        Assert.Contains("/koppeling/sleutel?t=", email.LastHtml!);
        Assert.NotNull(await sut.FindActiveByPlaintextAsync(first.PlaintextKey));

        var token = Extract(email.LastHtml!, "/koppeling/sleutel?t=");
        var revealed = await sut.RevealFromTokenAsync(token);
        Assert.NotNull(revealed);
        Assert.Null(await sut.FindActiveByPlaintextAsync(first.PlaintextKey));
        Assert.Null(await sut.RevealFromTokenAsync(token));
    }

    [Fact]
    public async Task One_click_unsubscribe_suppresses_optional_not_essential()
    {
        await using var db = CreateDb();
        var tokens = new MailUnsubscribeTokenService(
            DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "jobsy-e2e-unsub-" + Guid.NewGuid().ToString("N"))));
        var prefs = new EmailPreferenceService(db);
        var email = new CapturingEmail();
        var mailer = CreateMailer(db, email, prefs, tokens);

        var push = TransactionalEmails.PushBom(
            "https://lobsy.nl", "Alex", "Bakker", "Bedrijf", Guid.NewGuid(), "Delft", 2.4, 12, 14.5m, "Uurloon",
            "https://lobsy.nl/candidate/actions/set-unavailable");
        await mailer.SendAsync(push, "alex@example.com");
        var headers = email.Sent[0].Headers!;
        Assert.Contains("List-Unsubscribe", headers.Keys);
        Assert.Contains("List-Unsubscribe-Post", headers.Keys);
        var unsubUrl = headers["List-Unsubscribe"].Trim('<', '>');
        Assert.Contains("/mail/afmelden?t=", unsubUrl, StringComparison.OrdinalIgnoreCase);

        var token = Extract(unsubUrl + "\"", "/mail/afmelden?t=");
        await prefs.OptOutAsync("alex@example.com", "PushBom", "OneClick");

        email.Clear();
        var suppressed = await mailer.SendAsync(push, "alex@example.com");
        Assert.True(suppressed.Suppressed);
        Assert.Equal("opted-out", suppressed.Reason);
        Assert.Empty(email.Sent);

        var essential = TransactionalEmails.ApplicationConfirmation("https://lobsy.nl", "Alex", "Functie", "Bedrijf");
        var still = await mailer.SendAsync(essential, "alex@example.com");
        Assert.True(still.Sent);
        Assert.Single(email.Sent);
    }

    [Fact]
    public void Language_ar_and_ro_compose_rtl_and_locale()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var ar = TransactionalEmails.Compose("ApplicationVerificationCode", ctx, EmailCulture.Ar);
        Assert.Contains("dir=\"rtl\"", ar.Html, StringComparison.Ordinal);
        Assert.Contains("lang=\"ar\"", ar.Html, StringComparison.Ordinal);

        var ro = TransactionalEmails.Compose("ApplicationConfirmation", ctx, EmailCulture.Ro);
        Assert.Contains("lang=\"ro\"", ro.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("dir=\"rtl\"", ro.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Support_access_mail_escapes_reason_and_uses_amsterdam()
    {
        var utc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var mail = TransactionalEmails.SupportAccessRequested(
            "https://lobsy.nl", "a***@lobsy.nl", "<b>hack</b>", utc, "PersonalData");
        Assert.Contains("&lt;b&gt;hack&lt;/b&gt;", mail.Html);
        Assert.DoesNotContain("<b>hack</b>", mail.Html);
        Assert.Contains(AmsterdamTime.FormatDateTime(utc), mail.Html);
    }

    private static TransactionalMailer CreateMailer(
        JobsyDbContext db,
        IEmailService email,
        IEmailPreferenceService prefs,
        IMailUnsubscribeTokenService tokens)
        => new(
            email,
            new AlwaysOnFlags(),
            new Features("https://lobsy.nl"),
            prefs,
            tokens,
            Options.Create(new MailOptions()),
            db,
            new Env("Testing"),
            NullLogger<TransactionalMailer>.Instance);

    private static string Extract(string html, string marker)
    {
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, marker);
        var start = idx + marker.Length;
        var end = start;
        while (end < html.Length && html[end] is not ('"' or '\'' or '<' or '>' or ' ' or '\n' or '\r'))
        {
            end++;
        }

        return Uri.UnescapeDataString(html[start..end].TrimEnd(')', ']'));
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class CapturingEmail : IEmailService, ITransactionalMailer
    {
        public List<EmailMessage> Sent { get; } = [];
        public string? LastHtml { get; private set; }
        public string? LastText { get; private set; }

        public void Clear()
        {
            Sent.Clear();
            LastHtml = null;
            LastText = null;
        }

        public async Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var delivery = await SendAsync(
                new EmailMessage(to, mail.Subject, mail.Html ?? string.Empty, mail.Category)
                {
                    BodyText = mail.Text
                },
                cancellationToken);
            return new EmailSendOutcome(true, false, null, delivery.Kind);
        }

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            LastHtml = message.BodyHtml;
            LastText = message.BodyText;
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class AlwaysOnFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, true, "https://lobsy.nl", DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class Features(string url) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, true, url, DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class AlwaysOnFlags : Jobsy.Core.Features.IFeatureFlags
    {
        public ValueTask<Jobsy.Core.Features.FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new Jobsy.Core.Features.FeatureFlagSnapshot(true, false));

        public ValueTask<bool> IsEnabledAsync(Jobsy.Core.Features.PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public void Invalidate() { }
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class FixedConfig : Microsoft.Extensions.Configuration.IConfiguration
    {
        private readonly Dictionary<string, string?> _values;
        public FixedConfig(string key, string? value)
            => _values = new(StringComparer.OrdinalIgnoreCase) { [key] = value };
        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new Noop();
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key)
            => new Section(this, key);
        private sealed class Section(FixedConfig root, string key) : Microsoft.Extensions.Configuration.IConfigurationSection
        {
            public string Key => key;
            public string Path => key;
            public string? Value { get => root[key]; set => root[key] = value; }
            public string? this[string k] { get => root[key + ":" + k]; set => root[key + ":" + k] = value; }
            public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];
            public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new Noop();
            public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string k) => new Section(root, key + ":" + k);
        }
        private sealed class Noop : Microsoft.Extensions.Primitives.IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => Empty.Instance;
        }
        private sealed class Empty : IDisposable
        {
            public static readonly Empty Instance = new();
            public void Dispose() { }
        }
    }
}
