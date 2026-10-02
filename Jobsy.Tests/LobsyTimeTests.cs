using System.Globalization;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

/// <summary>Carrière 04 §3: Amsterdam wall-clock time in the UI culture, never the server zone.</summary>
public class LobsyTimeTests
{
    [Fact]
    public void Summer_time_is_two_hours_ahead_of_utc()
    {
        var cest = LobsyTime.ToAmsterdam(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 7, 1, 12, 0, 0), cest);
    }

    [Fact]
    public void Winter_time_is_one_hour_ahead_of_utc()
    {
        var cet = LobsyTime.ToAmsterdam(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 1, 15, 11, 0, 0), cet);
    }

    [Fact]
    public void Unspecified_kind_is_read_as_utc()
    {
        var value = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(LobsyTime.ToAmsterdam(DateTime.SpecifyKind(value, DateTimeKind.Utc)), LobsyTime.ToAmsterdam(value));
    }

    [Theory]
    [InlineData("nl-NL")]
    [InlineData("en-GB")]
    [InlineData("pl-PL")]
    [InlineData("ro-RO")]
    [InlineData("ar")]
    public void All_five_cultures_format_a_deadline_and_a_day(string culture)
    {
        var info = CultureInfo.GetCultureInfo(culture);
        var utc = new DateTime(2026, 3, 29, 23, 30, 0, DateTimeKind.Utc);

        var deadline = LobsyTime.Deadline(utc, info);
        var day = LobsyTime.Day(utc, info);

        Assert.False(string.IsNullOrWhiteSpace(deadline));
        Assert.False(string.IsNullOrWhiteSpace(day));
        Assert.Contains(":", deadline, StringComparison.Ordinal);
    }

    [Fact]
    public void Arabic_keeps_latin_digits_and_the_gregorian_calendar()
    {
        var arabic = CultureInfo.GetCultureInfo("ar");
        var text = LobsyTime.Deadline(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), arabic);

        Assert.Contains("12", text, StringComparison.Ordinal);
        Assert.DoesNotContain("١", text, StringComparison.Ordinal);
        Assert.DoesNotContain("٢٠٢٦", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatting_uses_the_current_ui_culture_by_default()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl-NL");
            var utc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);
            Assert.Equal(LobsyTime.Deadline(utc, CultureInfo.GetCultureInfo("nl-NL")), LobsyTime.Deadline(utc));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
