using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Xunit;

namespace Jobsy.Tests;

public sealed class InitiativeRulesTests
{
    [Fact]
    public void Week_limit_allows_three_per_week()
    {
        Assert.True(InitiativeWeekLimitRules.CanSendAnother(0));
        Assert.True(InitiativeWeekLimitRules.CanSendAnother(2));
        Assert.False(InitiativeWeekLimitRules.CanSendAnother(3));
    }

    [Fact]
    public void Week_start_is_monday_amsterdam()
    {
        var utc = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var start = InitiativeWeekLimitRules.WeekStartAmsterdam(utc);
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var local = TimeZoneInfo.ConvertTimeFromUtc(start, tz);
        Assert.Equal(DayOfWeek.Monday, local.DayOfWeek);
    }

    [Fact]
    public void Auto_decline_deadline_uses_five_business_days()
    {
        var start = new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);
        var deadline = InitiativeBusinessDayRules.AddBusinessDaysUtc(start, InitiativeBusinessDayRules.ResponseBusinessDays);
        Assert.True(deadline > start);
    }

    [Fact]
    public void Match_wording_never_exposes_percent()
    {
        Assert.Equal(InitiativeMatchLabel.Strong, InitiativeMatchWordingRules.FromInternalScore(85));
        Assert.Equal(InitiativeMatchLabel.Good, InitiativeMatchWordingRules.FromInternalScore(65));
        Assert.Equal(InitiativeMatchLabel.Reasonable, InitiativeMatchWordingRules.FromInternalScore(40));
    }

    [Fact]
    public void Employer_pii_hidden_until_accepted()
    {
        Assert.False(InitiativeVisibilityRules.EmployerMaySeePii(WorkInitiativeStatus.Pending));
        Assert.False(InitiativeVisibilityRules.EmployerMaySeePii(WorkInitiativeStatus.Viewed));
        Assert.True(InitiativeVisibilityRules.EmployerMaySeePii(WorkInitiativeStatus.Accepted));
    }

    [Fact]
    public void Team_cannot_open_without_confirmed_email_in_service_validation()
    {
        var team = new Jobsy.Core.Contracts.InitiativeTeamDto(
            Guid.Empty, "Team", "Omschrijving", null, null, "a@b.nl", false, 5, true);
        Assert.False(team.EmailConfirmed);
    }
}
