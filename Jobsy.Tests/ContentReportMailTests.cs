using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Tests;

/// <summary>
/// The three report mails of public-pages 06: the reporter only hears from us when they left an
/// e-mail, and the employer mail never carries the reporter's address.
/// </summary>
public class ContentReportMailTests
{
    [Fact]
    public async Task Without_an_email_no_confirmation_is_sent()
    {
        var harness = await ContentReportHarness.CreateAsync();
        var result = await harness.ReportVacancyAsync();

        Assert.False(result.EmailConfirmationSent);
        Assert.Empty(harness.Mailer.Sent);
    }

    [Fact]
    public async Task With_an_email_the_reporter_gets_a_confirmation()
    {
        var harness = await ContentReportHarness.CreateAsync();
        var result = await harness.ReportVacancyAsync("melder@test.nl");

        Assert.True(result.EmailConfirmationSent);
        var sent = Assert.Single(harness.Mailer.Sent);
        Assert.Equal("melder@test.nl", sent.To);
        Assert.Equal("ReportReceived", sent.Mail.Key);
    }

    [Fact]
    public async Task A_decision_mails_the_reporter_and_the_employer_without_leaking_the_reporter()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync("melder@test.nl");
        harness.Mailer.Sent.Clear();

        await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.Removed,
            "Nep vacature."));

        var toReporter = Assert.Single(harness.Mailer.Sent.Where(m => m.Mail.Key == "ReportDecided"));
        Assert.Equal("melder@test.nl", toReporter.To);

        var toEmployer = Assert.Single(harness.Mailer.Sent.Where(m => m.Mail.Key == "ContentRemoved"));
        Assert.Equal(ContentReportHarness.ManagerEmail, toEmployer.To);
        Assert.DoesNotContain("melder@test.nl", toEmployer.Mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("melder@test.nl", toEmployer.Mail.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("melder", toEmployer.Mail.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_employer_mail_escapes_the_reason()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync();

        await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.Restricted,
            "<script>alert('x')</script> onveilig"));

        var mail = Assert.Single(harness.Mailer.Sent.Where(m => m.Mail.Key == "ContentRemoved"));
        Assert.DoesNotContain("<script>", mail.Mail.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", mail.Mail.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_action_does_not_mail_the_employer()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync("melder@test.nl");
        harness.Mailer.Sent.Clear();

        await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.NoAction,
            null));

        Assert.DoesNotContain(harness.Mailer.Sent, m => m.Mail.Key == "ContentRemoved");
        Assert.Contains(harness.Mailer.Sent, m => m.Mail.Key == "ReportDecided");
    }
}
