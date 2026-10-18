using Jobsy.Core.Rules;
using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

public sealed class JobsyDateInputRulesTests
{
    [Theory]
    [InlineData("18101985", "18-10-1985")]
    [InlineData("18-10-1985", "18-10-1985")]
    [InlineData("18/10/1985", "18-10-1985")]
    [InlineData("18.10.1985", "18-10-1985")]
    [InlineData("1985-10-18", "18-10-1985")]
    public void Mask_and_parse_day_first_formats(string input, string expectedDisplay)
    {
        var masked = JobsyDateInputRules.ApplyDateMask(input);
        Assert.Equal(expectedDisplay, masked);
        Assert.True(JobsyDateInputRules.TryParseFlexibleDate(masked, out var date));
        Assert.Equal(new DateOnly(1985, 10, 18), date);
        Assert.Equal("1985-10-18", JobsyDateInputRules.ToDateInputValue(date));
    }

    [Theory]
    [InlineData("31-02-2000")]
    [InlineData("32-01-2000")]
    [InlineData("00-01-2000")]
    public void Invalid_calendar_dates_fail(string text)
    {
        Assert.False(JobsyDateInputRules.TryParseFlexibleDate(text, out _));
        Assert.Equal(JobsyDateValidationCode.InvalidDate, JobsyDateInputRules.ValidateText(text, JobsyDateConstraint.None));
    }

    [Fact]
    public void Leap_day_feb_29_valid_only_in_leap_years()
    {
        Assert.True(JobsyDateInputRules.TryParseFlexibleDate("29-02-2000", out var leap));
        Assert.Equal(new DateOnly(2000, 2, 29), leap);
        Assert.False(JobsyDateInputRules.TryParseFlexibleDate("29-02-2001", out _));
    }

    [Fact]
    public void Birth_date_bounds_exclude_too_recent_and_ancient()
    {
        var today = JobsyDateInputRules.TodayUtc;
        var (min, max) = JobsyDateInputRules.Bounds(JobsyDateConstraint.CandidateBirthDate);
        Assert.True(DateOnly.Parse(min!) <= today.AddYears(-100));
        Assert.True(DateOnly.Parse(max!) <= today.AddYears(-CandidateConsentRules.MinimumCandidateAge));
    }

    [Fact]
    public void Birth_date_validation_covers_future_and_too_young()
    {
        var today = JobsyDateInputRules.TodayUtc;
        Assert.Equal(
            JobsyDateValidationCode.BirthDateFuture,
            JobsyDateInputRules.Validate(today.AddDays(1), JobsyDateConstraint.CandidateBirthDate));
        Assert.Equal(
            JobsyDateValidationCode.BirthDateTooYoung,
            JobsyDateInputRules.Validate(today.AddYears(-5), JobsyDateConstraint.CandidateBirthDate));
    }

    [Fact]
    public void Month_input_round_trips_yyyy_mm()
    {
        Assert.True(JobsyDateInputRules.TryParseMonthInput("2024-08", out var value));
        Assert.Equal("2024-08", value);
        Assert.Equal("2024-08", JobsyDateInputRules.ToMonthInputValue(value));
    }

    [Theory]
    [InlineData("102022", "10-2022", "2022-10")]
    [InlineData("10-2022", "10-2022", "2022-10")]
    [InlineData("2022-10", "10-2022", "2022-10")]
    public void Month_mask_and_parse(string input, string expectedDisplay, string expectedStorage)
    {
        Assert.Equal(expectedDisplay, JobsyDateInputRules.ApplyMonthMask(input));
        Assert.True(JobsyDateInputRules.TryParseFlexibleMonth(expectedDisplay, out var yyyyMm));
        Assert.Equal(expectedStorage, yyyyMm);
    }

    [Fact]
    public void End_month_must_not_be_before_start_month()
    {
        Assert.Equal(
            JobsyDateValidationCode.MonthBeforeStart,
            JobsyDateInputRules.ValidateMonth("2022-01", JobsyDateConstraint.WorkHistoryEndAfterStart, "2022-06"));
        Assert.Equal(
            JobsyDateValidationCode.None,
            JobsyDateInputRules.ValidateMonth("2022-06", JobsyDateConstraint.WorkHistoryEndAfterStart, "2022-06"));
    }

    [Fact]
    public void Work_history_month_cannot_be_in_the_future()
    {
        var future = JobsyDateInputRules.TodayUtc.AddMonths(2);
        var yyyyMm = $"{future.Year:0000}-{future.Month:00}";
        Assert.Equal(
            JobsyDateValidationCode.NotFuture,
            JobsyDateInputRules.ValidateMonth(yyyyMm, JobsyDateConstraint.WorkHistoryMonth));
    }

    [Theory]
    [InlineData("nl", "dd-mm-jjjj")]
    [InlineData("en", "dd-mm-yyyy")]
    public void Placeholder_is_day_first_per_locale(string lang, string expected)
    {
        Assert.Equal(expected, JobsyDateInputRules.Placeholder(lang));
    }

    [Fact]
    public void Dutch_display_uses_dd_MM_yyyy()
    {
        var text = JobsyDateInputRules.FormatDisplayDate(new DateOnly(2026, 10, 19), "nl");
        Assert.Equal("19-10-2026", text);
    }

    [Theory]
    [InlineData("en", JobsyDateValidationCode.InvalidDate, "not a valid date")]
    [InlineData("nl", JobsyDateValidationCode.InvalidDate, "geen geldige datum")]
    public void Validation_messages_are_bilingual(string lang, JobsyDateValidationCode code, string fragment)
    {
        var msg = JobsyDateInputRules.ValidationMessage(code, lang);
        Assert.Contains(fragment, msg, StringComparison.OrdinalIgnoreCase);
    }
}
