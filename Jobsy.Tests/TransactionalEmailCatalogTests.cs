using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class TransactionalEmailCatalogTests
{
    private static readonly Regex HrefRegex = new(
        "href\\s*=\\s*\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void Catalog_covers_every_known_transactional_type()
    {
        var keys = TransactionalEmails.Templates.Select(t => t.Key).ToList();
        Assert.Equal(28, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("ApplicationConfirmation", keys);
        Assert.Contains("PushBom", keys);
        Assert.Contains("AccountUnsubscribeVerification", keys);
        Assert.Contains("CompanyApiKeyCredentials", keys);
        Assert.Contains("VacancyEngagementReminder", keys);
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
            Assert.Contains("<!DOCTYPE html>", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("lobsy-128.png", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("https://lobsy.nl/images/brand/lobsy-128.png", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cid:", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/images/brand/lobsy.png?", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(EmailLayout.BrandNavy, mail.Html);
            Assert.DoesNotContain("javascript:", mail.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=\"\"", mail.Html);
            Assert.DoesNotContain("href=\"#\"", mail.Html);

            var hrefs = HrefRegex.Matches(mail.Html).Select(m => m.Groups[1].Value).ToList();
            Assert.NotEmpty(hrefs);
            foreach (var href in hrefs)
            {
                var decoded = System.Net.WebUtility.HtmlDecode(href);
                Assert.True(
                    decoded.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    || decoded.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                    $"{template.Key} has non-absolute href: {decoded}");
                Assert.Contains("lobsy.nl", decoded, StringComparison.OrdinalIgnoreCase);
            }

            var hasButton = mail.Html.Contains("display:inline-block;padding:12px 22px", StringComparison.Ordinal)
                            || mail.Html.Contains("display:inline-block;padding:11px 20px", StringComparison.Ordinal);
            var hasOtp = mail.Html.Contains("data-lobsy-otp=", StringComparison.Ordinal);
            Assert.True(hasButton || hasOtp, $"{template.Key} has neither CTA button nor OTP block.");
        }
    }

    [Fact]
    public void Hired_mail_omits_withdraw_when_no_token_url()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var without = TransactionalEmails.ApplicationHired(
            ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.ApplicationId);
        Assert.DoesNotContain("withdraw-others", without.Html);
        Assert.DoesNotContain("Andere sollicitaties netjes intrekken", without.Html);

        var withToken = TransactionalEmails.ApplicationHired(
            ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.ApplicationId,
            "https://lobsy.nl/candidate/actions/withdraw-others?t=sample");
        Assert.Contains("withdraw-others", withToken.Html);
    }

    [Fact]
    public void Production_senders_use_the_shared_catalog()
    {
        var root = FindRepoRoot();
        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.EndsWith("TransactionalEmails.cs", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Jobsy.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("EmailLayout.Wrap(", source);
        }
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
        Assert.Contains("/privacy/data", unsub.Html);
        Assert.Contains($"data-lobsy-otp=\"{TransactionalEmails.SampleOtp}\"", unsub.Html);

        var register = TransactionalEmails.Compose("RegistrationActivation", ctx);
        Assert.Contains("/register/activate", register.Html);

        var sales = TransactionalEmails.Compose("SalesManagerInvite", ctx);
        Assert.Contains("/login", sales.Html);
        Assert.Contains("/salesmanager/onboarding", sales.Html);
    }

    [Fact]
    public void Test_samples_do_not_look_like_live_secrets()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        var api = TransactionalEmails.Compose("CompanyApiKeyCredentials", ctx);
        Assert.Contains(TransactionalEmails.SampleApiKey, api.Html);
        Assert.DoesNotContain("sk_live", api.Html, StringComparison.OrdinalIgnoreCase);

        var invite = TransactionalEmails.Compose("UserInvite", ctx);
        Assert.Contains(TransactionalEmails.SamplePassword, invite.Html);
    }

    [Fact]
    public async Task Catalog_service_rejects_invalid_email_and_unknown_key()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db, new RecordingEmail());

        var invalid = await sut.SendAsync("MailTest", "nope");
        Assert.False(invalid.Ok);
        Assert.Contains("geldig", invalid.Message, StringComparison.OrdinalIgnoreCase);

        var unknown = await sut.SendAsync("NotARealMail", "tester@example.com");
        Assert.False(unknown.Ok);
        Assert.Contains("Onbekend", unknown.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Catalog_service_sends_all_types_and_redacts_recipient()
    {
        await using var db = CreateDb();
        var email = new RecordingEmail();
        var sut = CreateSut(db, email);

        var results = await sut.SendAllAsync("reviewer@lobsy.nl");

        Assert.Equal(TransactionalEmails.Templates.Count, results.Count);
        Assert.All(results, r => Assert.True(r.Ok));
        Assert.Equal(TransactionalEmails.Templates.Count, email.Sent.Count);
        Assert.All(email.Sent, m => Assert.Equal("reviewer@lobsy.nl", m.To));
        Assert.DoesNotContain(db.PlatformLogs, l => l.Message.Contains("reviewer@lobsy.nl"));
        Assert.Contains(db.PlatformLogs, l => l.Category == "EmailCatalogTest" && l.Message.Contains("r***@lobsy.nl"));
    }

    [Fact]
    public void Mail_test_page_is_wired_under_admin_settings()
    {
        var root = FindRepoRoot();
        var nav = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Admin", "AdminNavItems.cs"));
        var roles = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Navigation", "RoleNavCatalog.cs"));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Admin", "MailTestAdmin.razor"));
        Assert.Contains("/admin/mail-test", nav);
        Assert.Contains("/admin/mail-test", roles);
        Assert.Contains("[Authorize(Roles = \"Admin\")]", page);
        Assert.Contains("SendAllEmailTemplatesAsync", page);
    }

    private static EmailCatalogService CreateSut(JobsyDbContext db, IEmailService email)
        => new(
            email,
            new FakeFeatures(),
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicApiBaseUrl"] = "https://api.lobsy.nl"
            }).Build(),
            NullLogger<EmailCatalogService>.Instance);

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
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

    private sealed class RecordingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }
}
