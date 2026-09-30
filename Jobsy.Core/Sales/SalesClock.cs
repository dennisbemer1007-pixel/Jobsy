namespace Jobsy.Core.Sales;

/// <summary>
/// Europe/Amsterdam calendar helpers for hold ends, payout runs and IBAN holds.
/// </summary>
public static class SalesClock
{
    private static readonly TimeZoneInfo Amsterdam =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Amsterdam");

    public static DateOnly Today(DateTime? utcNow = null)
        => DateOnly.FromDateTime(ToLocal(utcNow ?? DateTime.UtcNow).DateTime);

    public static DateTimeOffset ToLocal(DateTime utc)
    {
        var u = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTime(u, Amsterdam);
    }

    public static DateTime EndOfLocalDayUtc(DateOnly localDate)
    {
        var localEnd = localDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localEnd, Amsterdam);
    }

    public static DateTime AddLocalDays(DateTime utc, int days)
    {
        var local = ToLocal(utc).DateTime.Date.AddDays(days);
        var asUnspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(asUnspecified, Amsterdam);
    }

    public static DateOnly NextWorkday(DateOnly fromInclusive)
    {
        var d = fromInclusive;
        while (!IsWorkday(d))
        {
            d = d.AddDays(1);
        }

        return d;
    }

    public static DateOnly FirstWorkdayOfMonth(int year, int month)
        => NextWorkday(new DateOnly(year, month, 1));

    public static bool IsWorkday(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        return !DutchHolidays.IsPublicHoliday(date);
    }
}

/// <summary>
/// Fixed and Easter-based Dutch public holidays. Goede Vrijdag is a workday.
/// </summary>
public static class DutchHolidays
{
    public static bool IsPublicHoliday(DateOnly date)
    {
        if (date is { Month: 1, Day: 1 })
        {
            return true;
        }

        if (date is { Month: 12, Day: 25 or 26 })
        {
            return true;
        }

        // Koningsdag: 27 Apr, or 26 Apr when 27 is Sunday
        var koningsdag = date.Year is >= 2014
            ? new DateOnly(date.Year, 4, 27)
            : new DateOnly(date.Year, 4, 30);
        if (koningsdag.DayOfWeek == DayOfWeek.Sunday)
        {
            koningsdag = koningsdag.AddDays(-1);
        }

        if (date == koningsdag)
        {
            return true;
        }

        var easter = EasterSunday(date.Year);
        var easterMonday = easter.AddDays(1);
        var ascension = easter.AddDays(39);
        var whitMonday = easter.AddDays(50);
        return date == easterMonday || date == ascension || date == whitMonday;
    }

    /// <summary>Anonymous Gregorian algorithm.</summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }
}
