using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class TransactionalEmailCatalogTests
{
    private static readonly Regex HrefRegex = new(
        "href\\s*=\\s*\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] RequiredKeys =
    [
        "ApplicationConfirmation", "ApplicationVerificationCode", "EmployerReactionAccepted",
        "EmployerReactionRejected", "EmployerContacting", "ApplicationHired", "ApplicationFilledElsewhere",
        "PushBom", "AccountUnsubscribeVerification", "ParentalConsent", "EmployerNewApplication",
        "CandidateWithdrawn", "CandidateWithdrawnOtherJob", "PendingApproval", "VacancyEngagementReminder",
        "DraftVacancyCleanupWarning", "CompanyReEngagement", "CompanyApiKeyCredentials", "UserInvite",
        "RegistrationActivation", "RegistrationCredentials", "TakeoverEmailVerification", "TakeoverRequest",
        "TakeoverSubmitted", "TakeoverApproved", "TakeoverRejected", "SalesManagerInvite", "AmbassadeurInvite",
        "AccountLockout", "SupportAccessRequested", "MailTest"
    ];

    public TransactionalEmailCatalogTests()
        => EmailCatalogService.ResetForTests();

    [Fact]
    public void Catalog_contains_at_least_section_M_keys()
    {
        var keys = TransactionalEmails.Templates.Select(t => t.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var key in RequiredKeys)
        {
            Assert.Contains(key, keys);
        }

        Assert.True(keys.Count >= 31);
        Assert.Contains("MfaResetByAdmin", keys);
        Assert.Contains("CompanyVerified", keys);
        Assert.Contains("EmailSignUpCode", keys);
    }

    [Fact]
    public void Every_template_composes_branded_html_with_safe_absolute_links()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl", "reviewer@lobsy.nl");
        foreach (var template in TransactionalEmails.Templates)
        {
            var mail = TransactionalEmails.Compose(template.Key, ctx);
            Assert.Equal(template.Key, mail.Key);
            Assert.False(string.IsNullOrWhiteSpace(mail.Subject));
            Assert.False(string.IsNullOrWhiteSpace(mail.Text));
            Assert.Contains("<!DOCTYPE html>", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-lobsy-layout=\"2\"", mail.Html);
            Assert.Contains("lobsy-mark-72.png", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cid:", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", mail.Html, StringComparison.OrdinalIgnoreCase);

            var def = EmailTemplateRegistry.GetRequired(template.Key);
            var hasCta = mail.Html.Contains("data-lobsy-cta", StringComparison.Ordinal);
            var hasOtp = mail.Html.Contains("data-lobsy-otp=", StringComparison.Ordinal);
            if (def.Kind == EmailKind.Security)
            {
                Assert.True(hasOtp, $"{template.Key} security mail needs OTP");
                Assert.False(hasCta, $"{template.Key} security mail must not have CTA");
            }
            else
            {
                Assert.True(hasCta || hasOtp, $"{template.Key} needs CTA or code");
            }

            foreach (Match m in HrefRegex.Matches(mail.Html))
            {
                var decoded = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
                Assert.True(
                    decoded.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    || decoded.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || decoded.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase),
                    $"{template.Key} has non-absolute href: {decoded}");
            }
        }
    }

    [Fact]
    public void Hired_mail_omits_withdraw_when_no_token_url()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var without = TransactionalEmails.ApplicationHired(
            ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.ApplicationId);
        Assert.DoesNotContain("Andere sollicitaties netjes intrekken", without.Html);
        Assert.Contains("/candidate/applications", without.Html);

        var withToken = TransactionalEmails.ApplicationHired(
            ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.ApplicationId,
            "https://lobsy.nl/candidate/actions/withdraw-others?t=sample");
        Assert.Contains("withdraw-others", withToken.Html);
    }

    [Fact]
    public void Working_cta_targets_match_live_routes()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var hired = TransactionalEmails.Compose("ApplicationHired", ctx);
        Assert.Contains("/candidate/applications", hired.Html);
        Assert.Contains("/candidate/actions/withdraw-others", hired.Html);

        var push = TransactionalEmails.Compose("PushBom", ctx);
        Assert.Contains($"/vacancies/{ctx.VacancyId}", push.Html);
        Assert.Contains("/candidate/actions/set-unavailable", push.Html);

        var unsub = TransactionalEmails.Compose("AccountUnsubscribeVerification", ctx);
        Assert.Contains($"data-lobsy-otp=\"{TransactionalEmails.SampleOtp}\"", unsub.Html);
        Assert.DoesNotContain("data-lobsy-cta", unsub.Html);

        var register = TransactionalEmails.Compose("RegistrationActivation", ctx);
        Assert.Contains($"data-lobsy-otp=\"{TransactionalEmails.SampleOtp}\"", register.Html);

        var sales = TransactionalEmails.Compose("SalesManagerInvite", ctx);
        Assert.Contains("/login", sales.Html);
        Assert.DoesNotContain("/salesmanager/onboarding", sales.Html);
        var salesSetPassword = TransactionalEmails.SalesManagerInvite(
            ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.ContactEmail, ctx.SetPasswordUrl);
        Assert.Contains("/account/wachtwoord-instellen?t=", salesSetPassword.Html);
        Assert.DoesNotContain("/salesmanager/onboarding", salesSetPassword.Html);
    }

    [Fact]
    public void Test_samples_do_not_look_like_live_secrets()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var api = TransactionalEmails.Compose("CompanyApiKeyCredentials", ctx);
        Assert.Contains("/koppeling/sleutel?t=", api.Html);
        Assert.Contains("API-sleutel ophalen", api.Html);
        Assert.DoesNotContain("sk_live", api.Html, StringComparison.OrdinalIgnoreCase);

        var invite = TransactionalEmails.Compose("UserInvite", ctx);
        Assert.Contains("/account/wachtwoord-instellen?t=", invite.Html);
    }

    [Fact]
    public async Task Catalog_service_rejects_invalid_email_and_unknown_key()
    {
        var sut = CreateSut(new RecordingMailer());

        var invalid = await sut.SendAsync("MailTest", "nl", "nope", null);
        Assert.False(invalid.Ok);

        var unknown = await sut.SendAsync("NotARealMail", "nl", "tester@example.com", null);
        Assert.False(unknown.Ok);
        Assert.Contains("Onbekend", unknown.Message, StringComparison.OrdinalIgnoreCase);

        var foreign = await sut.SendAsync("MailTest", "nl", "admin@lobsy.nl", "other@example.com");
        Assert.False(foreign.Ok);
        Assert.Contains("allow-list", foreign.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Catalog_service_sends_to_admin_and_redacts_recipient()
    {
        var audit = new RecordingAudit();
        var mailer = new RecordingMailer();
        var sut = CreateSut(mailer, audit);

        var result = await sut.SendAsync("MailTest", "nl", "reviewer@lobsy.nl", null);
        Assert.True(result.Ok);
        Assert.StartsWith("[Test] ", result.Subject, StringComparison.Ordinal);
        Assert.Single(mailer.Sent);
        Assert.Equal("reviewer@lobsy.nl", mailer.Sent[0].To);
        Assert.True(mailer.Sent[0].Options?.IsTest);
        var entry = Assert.Single(audit.Entries);
        Assert.DoesNotContain("reviewer@lobsy.nl", entry.DetailsJson ?? "");
        Assert.Contains("r***@lobsy.nl", entry.DetailsJson ?? "");
    }

    [Fact]
    public void Preview_uses_fake_data_only_no_db_vacancies()
    {
        var sut = CreateSut(new RecordingMailer());
        var preview = sut.Preview("ApplicationConfirmation", "nl", "light", "https://lobsy.nl");
        Assert.False(string.IsNullOrWhiteSpace(preview.Html));
        Assert.False(string.IsNullOrWhiteSpace(preview.Text));
        Assert.Contains("Bakkerij De Gouden Korrel", preview.Html, StringComparison.Ordinal);
        Assert.Equal("ltr", preview.Dir);

        var ar = sut.Preview("ApplicationConfirmation", "ar", "dark", "https://lobsy.nl");
        Assert.Equal("rtl", ar.Dir);
    }

    [Fact]
    public void Mail_test_page_is_wired_under_admin_settings()
    {
        var root = FindRepoRoot();
        var nav = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Navigation", "AdminNav.cs"));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Admin", "MailTestAdmin.razor"));
        Assert.Contains("/admin/content/emails", nav);
        Assert.Contains("/admin/mail-test", nav);
        Assert.Contains("[Authorize(Roles = \"Admin\")]", page);
        Assert.Contains("sandbox=\"\"", page);
        Assert.Contains("GetEmailTemplatePreviewAsync", page);
    }

    private static EmailCatalogService CreateSut(RecordingMailer mailer, RecordingAudit? audit = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransactionalMailer>(mailer);
        services.AddSingleton<IPlatformFeatureService>(new FakeFeatures());
        services.AddSingleton<IAdminAuditLog>(audit ?? new RecordingAudit());
        services.AddSingleton(Options.Create(new MailOptions()));
        services.AddSingleton<IEmailCatalogService>(sp => new EmailCatalogService(
            sp.GetRequiredService<ITransactionalMailer>(),
            sp.GetRequiredService<IPlatformFeatureService>(),
            sp.GetRequiredService<IAdminAuditLog>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<MailOptions>>(),
            NullLogger<EmailCatalogService>.Instance));
        return (EmailCatalogService)services.BuildServiceProvider().GetRequiredService<IEmailCatalogService>();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
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

    private sealed class RecordingAudit : IAdminAuditLog
    {
        public List<AdminAuditEntry> Entries { get; } = [];

        public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public void Stage(AdminAuditEntry entry) => Entries.Add(entry);
    }

    private sealed class RecordingMailer : ITransactionalMailer
    {
        public List<(ComposedEmail Mail, string To, EmailSendOptions? Options)> Sent { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((mail, to, options));
            return Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Stub));
        }
    }
}
