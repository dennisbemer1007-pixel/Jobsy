using System.Reflection;
using Jobsy.Core.Legal;
using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

/// <summary>
/// The privacy retention table is built from <see cref="PrivacyConstants"/>, so a new retention
/// constant cannot be forgotten on the page.
/// </summary>
public class LegalRetentionCatalogTests
{
    [Fact]
    public void Every_retention_constant_appears_in_the_catalog()
    {
        var constants = typeof(PrivacyConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(int) && f.Name.Contains("Retention", StringComparison.Ordinal))
            .Select(f => f.Name)
            .ToList();

        Assert.NotEmpty(constants);

        var covered = LegalRetention.Rows
            .SelectMany(r => r.Constants)
            .ToHashSet(StringComparer.Ordinal);

        var missing = constants.Where(c => !covered.Contains(c)).ToList();
        Assert.True(
            missing.Count == 0,
            $"Add these PrivacyConstants to LegalRetention.Rows: {string.Join(", ", missing)}");

        var unknown = covered.Where(c => !constants.Contains(c, StringComparer.Ordinal)).ToList();
        Assert.True(
            unknown.Count == 0,
            $"LegalRetention references constants that no longer exist: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void Every_row_has_a_label_key_and_a_non_empty_duration()
    {
        Assert.NotEmpty(LegalRetention.Rows);
        foreach (var row in LegalRetention.Rows)
        {
            Assert.StartsWith("Legal.Retention.", row.LabelKey, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(row.Duration()));
        }
    }

    [Theory]
    [InlineData(48, "48 uur")]
    public void Hours_are_formatted_in_plain_dutch(int hours, string expected)
        => Assert.Equal(expected, LegalRetention.FormatHours(hours));

    [Theory]
    [InlineData(10, "10 minuten")]
    public void Minutes_are_formatted_in_plain_dutch(int minutes, string expected)
        => Assert.Equal(expected, LegalRetention.FormatMinutes(minutes));

    [Theory]
    [InlineData(30, "30 dagen")]
    [InlineData(365, "365 dagen")]
    [InlineData(730, "2 jaar")]
    [InlineData(2555, "7 jaar")]
    public void Days_read_as_years_from_two_years_up(int days, string expected)
        => Assert.Equal(expected, LegalRetention.FormatDays(days));

    [Fact]
    public void Account_row_has_no_fixed_term()
    {
        var account = LegalRetention.Rows.Single(r => r.LabelKey == "Legal.Retention.Account");
        Assert.Empty(account.Constants);
        Assert.Equal(LegalRetention.UntilAccountDeleted, account.Duration());
    }

    [Fact]
    public void Processor_catalog_is_filled_and_split_by_status()
    {
        Assert.NotEmpty(LegalProcessors.All);
        Assert.Equal(
            LegalProcessors.All.Count,
            LegalProcessors.Active.Count + LegalProcessors.Planned.Count);
    }
}
