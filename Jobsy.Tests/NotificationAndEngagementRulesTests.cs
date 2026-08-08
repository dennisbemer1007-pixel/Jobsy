using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class NotificationAndEngagementRulesTests
{
    [Fact]
    public void ApplicationRules_withdraw_only_pending_verified()
    {
        Assert.True(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Pending, DateTime.UtcNow));
        Assert.False(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Pending, null));
        Assert.False(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Accepted, DateTime.UtcNow));
        Assert.False(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Withdrawn, DateTime.UtcNow));
    }

    [Fact]
    public void ApplicationRules_terminal_includes_withdrawn()
    {
        Assert.True(ApplicationRules.IsTerminal(ApplicationStatus.Withdrawn));
        Assert.True(ApplicationRules.IsOpenForEmployerPipeline(ApplicationStatus.Pending));
        Assert.False(ApplicationRules.IsOpenForEmployerPipeline(ApplicationStatus.Withdrawn));
    }

    [Fact]
    public void EngagementReminder_eligible_after_14_days()
    {
        var now = DateTime.UtcNow;
        Assert.True(VacancyEngagementReminderRules.IsEligibleForReminder(
            now.AddDays(-14),
            reminderSentAtUtc: null,
            now));
        Assert.False(VacancyEngagementReminderRules.IsEligibleForReminder(
            now.AddDays(-13),
            reminderSentAtUtc: null,
            now));
        Assert.False(VacancyEngagementReminderRules.IsEligibleForReminder(
            now.AddDays(-20),
            reminderSentAtUtc: now.AddDays(-1),
            now));
    }

    [Fact]
    public void EngagementReminder_goodwill_once_before_deadline()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.True(VacancyEngagementReminderRules.CanApplyGoodwillExtension(
            DateTime.UtcNow.AddDays(-1),
            goodwillExtendedAtUtc: null,
            endDate: today.AddDays(3),
            today));
        Assert.False(VacancyEngagementReminderRules.CanApplyGoodwillExtension(
            DateTime.UtcNow.AddDays(-1),
            goodwillExtendedAtUtc: DateTime.UtcNow,
            endDate: today.AddDays(3),
            today));
        Assert.False(VacancyEngagementReminderRules.CanApplyGoodwillExtension(
            DateTime.UtcNow.AddDays(-1),
            goodwillExtendedAtUtc: null,
            endDate: today.AddDays(-1),
            today));
    }

    [Fact]
    public void EngagementReminder_tip_mentions_views_without_applications()
    {
        var tip = VacancyEngagementReminderRules.BuildHeuristicTip(
            searchAppearances: 10,
            views: 12,
            shares: 1,
            saved: 1,
            applications: 0);
        Assert.Contains("sollicitat", tip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplicationRules_capacity_excludes_withdrawn()
    {
        Assert.True(ApplicationRules.CountsTowardVacancyCapacity(ApplicationStatus.Pending, DateTime.UtcNow));
        Assert.False(ApplicationRules.CountsTowardVacancyCapacity(ApplicationStatus.Pending, null));
        Assert.False(ApplicationRules.CountsTowardVacancyCapacity(ApplicationStatus.Withdrawn, DateTime.UtcNow));
        Assert.True(ApplicationRules.BlocksDuplicateApplication(ApplicationStatus.Pending, DateTime.UtcNow));
        Assert.False(ApplicationRules.BlocksDuplicateApplication(ApplicationStatus.Withdrawn, DateTime.UtcNow));
        Assert.True(ApplicationRules.CanReuseWithdrawnApplication(ApplicationStatus.Withdrawn));
        Assert.False(ApplicationRules.ShouldRejectAsFilledElsewhere(ApplicationStatus.Withdrawn, DateTime.UtcNow));
        Assert.True(ApplicationRules.ShouldRejectAsFilledElsewhere(ApplicationStatus.Pending, DateTime.UtcNow));
    }

    [Fact]
    public void CandidateActionPurposes_known()
    {
        Assert.True(CandidateActionPurposes.IsKnown(CandidateActionPurposes.SetUnavailable));
        Assert.True(CandidateActionPurposes.IsKnown(CandidateActionPurposes.WithdrawOtherApplications));
        Assert.False(CandidateActionPurposes.IsKnown("Nope"));
        Assert.Equal("/candidate/actions/set-unavailable", CandidateActionPurposes.SetUnavailableInAppPath);
        var hiredId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Contains(hiredId.ToString("D"), CandidateActionPurposes.WithdrawOthersInAppPath(hiredId));
        Assert.DoesNotContain("token=", CandidateActionPurposes.WithdrawOthersInAppPath(hiredId));
    }
}
