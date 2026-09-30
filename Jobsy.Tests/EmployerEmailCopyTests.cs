using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class EmployerEmailCopyTests
{
    private static readonly string[] EmployerKeys =
    [
        "EmployerNewApplication",
        "CandidateWithdrawn",
        "CandidateWithdrawnOtherJob",
        "PendingApproval",
        "VacancyEngagementReminder",
        "DraftVacancyCleanupWarning",
        "CompanyReEngagement",
        "CompanyApiKeyCredentials",
        "UserInvite"
    ];

    [Fact]
    public void Employer_mails_have_exactly_one_cta()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var key in EmployerKeys)
        {
            var mail = TransactionalEmails.Compose(key, ctx);
            Assert.Equal(1, CountOccurrences(mail.Html, "data-lobsy-cta"));
        }
    }

    [Fact]
    public void Employer_mails_never_include_candidate_pii_sample()
    {
        const string candidate = "Sanne van Dijk";
        var mail = TransactionalEmails.EmployerNewApplication(
            "https://lobsy.nl", "Weekendhulp", branchName: "Delft", applicationId: Guid.NewGuid(),
            receivedAtUtc: DateTime.UtcNow, matchPercent: 86, companyName: "Bakkerij");
        Assert.DoesNotContain(candidate, mail.Html, StringComparison.Ordinal);
        Assert.DoesNotContain(candidate, mail.Text, StringComparison.Ordinal);
        Assert.Contains("86 %", mail.Text, StringComparison.Ordinal);

        var withoutMatch = TransactionalEmails.EmployerNewApplication(
            "https://lobsy.nl", "Weekendhulp", companyName: "Bakkerij");
        Assert.DoesNotContain("Match", withoutMatch.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Role_label_with_ampersand_escapes_once()
    {
        var mail = TransactionalEmails.UserInvite(
            "https://lobsy.nl", "Alex", "R&D", "alex@example.nl",
            "https://lobsy.nl/account/wachtwoord-instellen?t=abc", false,
            "Joris", "Bakkerij");
        Assert.Contains("R&amp;D", mail.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("R&amp;amp;D", mail.Html, StringComparison.Ordinal);
        Assert.Contains("R&D", mail.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Day_counts_follow_rule_constants_not_literals_in_values()
    {
        foreach (var (key, value) in EmailStrings.All["nl"])
        {
            Assert.DoesNotMatch(@"\b\d+\s*(minuten|dagen)\b", value);
        }

        var deleteOn = new DateTime(2026, 10, 13, 22, 30, 0, DateTimeKind.Utc);
        var warn = TransactionalEmails.DraftVacancyCleanupWarning(
            "https://lobsy.nl", "Concept", "Bakkerij", Guid.NewGuid(), deleteOn);
        Assert.Contains("14 oktober 2026", warn.Text, StringComparison.Ordinal);
        Assert.Contains(DraftVacancyCleanupRules.WarningAfterDays.ToString(), warn.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Core_email_sources_have_no_legacy_employer_routes_outside_EmailLinks()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var emailDir = Path.Combine(root, "Jobsy.Core", "Email");
        foreach (var file in Directory.EnumerateFiles(emailDir, "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file).Equals("EmailLinks.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("\"/branch/", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/employer/", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Renderer_never_double_escapes_ampersand_across_employer_keys()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var key in EmployerKeys)
        {
            var mail = TransactionalEmails.Compose(key, ctx);
            Assert.DoesNotContain("&amp;amp;", mail.Html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UserInvite_existing_login_uses_login_cta()
    {
        var mail = TransactionalEmails.UserInvite(
            "https://lobsy.nl", "Alex", "Filiaalmanager", "alex@example.nl",
            setPasswordUrl: null, promotedFromCandidate: false, "Joris", "Bakkerij");
        Assert.Contains("/login", mail.Html, StringComparison.Ordinal);
        Assert.Contains("Inloggen", mail.Text, StringComparison.Ordinal);
        Assert.Contains("Joris", mail.Text, StringComparison.Ordinal);
        Assert.Contains("Bakkerij", mail.Text, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }
}

public class EngagementTipTests
{
    [Theory]
    [InlineData(10, 2, 0, 0, 0, EngagementTipKind.LowVisibility)]
    [InlineData(10, 8, 0, 0, 0, EngagementTipKind.ViewsNoApplications)]
    [InlineData(10, 25, 1, 1, 2, EngagementTipKind.ManyViewsFewApplications)]
    [InlineData(10, 10, 0, 0, 5, EngagementTipKind.NotShared)]
    [InlineData(60, 5, 1, 1, 5, EngagementTipKind.LowClickThrough)]
    [InlineData(10, 10, 1, 1, 5, EngagementTipKind.General)]
    public void Heuristic_maps_to_kind(
        int impressions, int views, int shares, int saved, int applications, EngagementTipKind expected)
    {
        Assert.Equal(expected, VacancyEngagementReminderRules.BuildHeuristicTipKind(
            impressions, views, shares, saved, applications));
    }

    [Fact]
    public void Every_tip_kind_has_five_language_keys()
    {
        foreach (EngagementTipKind kind in Enum.GetValues<EngagementTipKind>())
        {
            var key = $"Email.VacancyEngagementReminder.Tip.{kind}";
            foreach (var lang in EmailStrings.Languages)
            {
                Assert.True(EmailStrings.TryGet(lang, key, out var value));
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.DoesNotContain("PushBom", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Highlight", value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
