using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

/// <summary>
/// Retention of content reports (public-pages 06): the reporter e-mail goes after 30 days, the whole
/// report after a year, and an undecided report is kept until an admin decides.
/// </summary>
public class ContentReportRetentionTests
{
    [Fact]
    public async Task Reporter_email_is_cleared_30_days_after_the_decision()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync("melder@test.nl");
        await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.NoAction,
            null));

        var now = DateTime.UtcNow;
        var (_, clearedEarly) = await ContentReportService.ApplyRetentionAsync(
            harness.Db,
            now.AddDays(PrivacyConstants.ContentReportEmailRetentionDays - 1));
        Assert.Equal(0, clearedEarly);

        var (_, cleared) = await ContentReportService.ApplyRetentionAsync(
            harness.Db,
            now.AddDays(PrivacyConstants.ContentReportEmailRetentionDays + 1));
        Assert.Equal(1, cleared);

        var report = await harness.Db.ContentReports.FirstAsync();
        Assert.Null(report.ReporterEmail);
        Assert.NotNull(report.ReporterEmailClearedAtUtc);
    }

    [Fact]
    public async Task Decided_reports_are_purged_after_the_retention_period()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync();
        await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.NoAction,
            null));

        var now = DateTime.UtcNow;
        var (purgedEarly, _) = await ContentReportService.ApplyRetentionAsync(
            harness.Db,
            now.AddDays(PrivacyConstants.ContentReportRetentionDays - 1));
        Assert.Equal(0, purgedEarly);

        var (purged, _) = await ContentReportService.ApplyRetentionAsync(
            harness.Db,
            now.AddDays(PrivacyConstants.ContentReportRetentionDays + 1));
        Assert.Equal(1, purged);
        Assert.Empty(await harness.Db.ContentReports.ToListAsync());
    }

    [Fact]
    public async Task Open_reports_survive_retention()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync("melder@test.nl");

        var (purged, cleared) = await ContentReportService.ApplyRetentionAsync(
            harness.Db,
            DateTime.UtcNow.AddDays(PrivacyConstants.ContentReportRetentionDays * 3));

        Assert.Equal(0, purged);
        Assert.Equal(0, cleared);
        var report = await harness.Db.ContentReports.FirstAsync();
        Assert.Equal("melder@test.nl", report.ReporterEmail);
    }

    [Fact]
    public void Both_retention_periods_are_published_in_the_privacy_table()
    {
        var keys = Jobsy.Core.Legal.LegalRetention.Rows
            .SelectMany(r => r.Constants)
            .ToList();
        Assert.Contains(nameof(PrivacyConstants.ContentReportRetentionDays), keys);
        Assert.Contains(nameof(PrivacyConstants.ContentReportEmailRetentionDays), keys);
    }
}
