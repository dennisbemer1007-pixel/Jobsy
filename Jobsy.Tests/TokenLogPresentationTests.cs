using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Tokens;

namespace Jobsy.Tests;

public class TokenLogPresentationTests
{
    [Theory]
    [InlineData(-1, "-1,00 token")]
    [InlineData(0.15, "+0,15 token")]
    [InlineData(0, "0,00 token")]
    public void Amount_includes_sign_unit_and_dutch_decimals(decimal amount, string expected)
        => Assert.Equal(expected, TokenLogPresentation.FormatAmount(amount));

    [Fact]
    public void Amount_tone_is_green_for_credits_and_red_for_debits()
    {
        Assert.Equal("token-log__amount--in", TokenLogPresentation.AmountToneClass(0.15m));
        Assert.Equal("token-log__amount--out", TokenLogPresentation.AmountToneClass(-1m));
        Assert.Equal("token-log__amount--zero", TokenLogPresentation.AmountToneClass(0m));
    }

    [Fact]
    public void When_includes_month_year_and_time()
    {
        var utc = new DateTime(2026, 8, 6, 18, 5, 0, DateTimeKind.Utc);
        var text = TokenLogPresentation.FormatWhen(utc);
        Assert.Contains("2026", text);
        Assert.Contains("aug", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(" om ", text);
        Assert.Matches(@"\d{2}:\d{2}", text);
    }

    [Fact]
    public void Notes_drop_guids_and_payment_hashes()
    {
        Assert.Equal(
            "Referralbonus welkomsttoken",
            TokenLogPresentation.SanitizeNote("Referralbonus welkomsttoken (5853fee45726408fa6dfc296af664794)"));
        Assert.Equal(
            "Betaling",
            TokenLogPresentation.SanitizeNote("Mollie tr_WDqmlOZqasxTVd"));
        Assert.Equal(
            "Betaling",
            TokenLogPresentation.SanitizeNote("Mollie stub stub_pay_abc123"));
        Assert.DoesNotContain("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
            TokenLogPresentation.SanitizeNote("Uitgifte aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    }

    [Fact]
    public void Describe_uses_friendly_kind_not_raw_enums()
    {
        var log = new TokenLogItem
        {
            Kind = "Spend",
            Reason = "Publish",
            Note = "Mollie tr_hidden"
        };
        var text = TokenLogPresentation.Describe(log);
        Assert.Equal("Vacature publiceren · Betaling", text);
        Assert.DoesNotContain("Spend", text);
        Assert.DoesNotContain("tr_", text);
    }

    [Fact]
    public void Employer_token_log_api_redacts_notes_for_non_admins()
    {
        var controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api/Controllers/TokenLogsController.cs"));
        Assert.Contains("TokenNoteRedaction.Sanitize", controller);
        Assert.Contains("IsAdmin", controller);
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
}

public class DayPartMatrixDisplayTests
{
    [Fact]
    public void Days_with_night_part_are_listed_without_using_the_night_column()
    {
        var slots = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ma"] = ["Nacht"],
            ["Wo"] = ["Avond", "Nacht"],
            ["Vr"] = ["Ochtend"]
        };

        Assert.Equal(["Ochtend", "Middag", "Avond"], DayPartMatrix.EmployerDisplayDayPartCodes);
        Assert.DoesNotContain("Nacht", DayPartMatrix.EmployerDisplayDayPartCodes);
        Assert.Equal(["Ma", "Wo"], DayPartMatrix.DaysWithDayPart(slots, DayPartMatrix.NightDayPart));
    }
}
