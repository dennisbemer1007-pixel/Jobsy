using System.Globalization;
using Jobsy.Core.Legal;
using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

/// <summary>Guards the single source of truth for legal versions and dates (D15 / D16).</summary>
public class LegalVersionsTests
{
    [Theory]
    [InlineData(LegalDocumentIds.Privacy)]
    [InlineData(LegalDocumentIds.TermsEmployer)]
    [InlineData(LegalDocumentIds.TermsCandidate)]
    public void History_is_sorted_descending_and_starts_with_the_current_version(string documentId)
    {
        var history = LegalDocumentVersions.History(documentId);
        Assert.NotEmpty(history);

        var current = LegalDocumentVersions.For(documentId);
        Assert.Equal(current.Version, history[0].Version);
        Assert.Equal(current.EffectiveFrom, history[0].EffectiveFrom);

        for (var i = 1; i < history.Count; i++)
        {
            Assert.True(
                string.CompareOrdinal(history[i - 1].Version, history[i].Version) > 0,
                $"{documentId} history must be sorted descending: {history[i - 1].Version} before {history[i].Version}");
            Assert.True(
                history[i - 1].EffectiveFrom > history[i].EffectiveFrom,
                $"{documentId} history dates must be sorted descending.");
        }

        Assert.All(history, e => Assert.False(string.IsNullOrWhiteSpace(e.SummaryKey)));
    }

    [Fact]
    public void Both_terms_pages_share_one_version()
    {
        Assert.Equal(
            LegalDocumentVersions.For(LegalDocumentIds.TermsEmployer),
            LegalDocumentVersions.For(LegalDocumentIds.TermsCandidate));
        Assert.Equal(LegalDocumentVersions.Terms, LegalDocumentVersions.For(LegalDocumentIds.TermsEmployer));
    }

    [Theory]
    [InlineData("nl-NL", "oktober 2026")]
    [InlineData("en-GB", "October 2026")]
    public void Version_label_is_formatted_per_culture(string cultureName, string expected)
    {
        var label = LegalDocumentVersions.FormatVersionLabel(
            LegalDocumentVersions.Privacy.Version,
            CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(expected, label);
    }

    [Fact]
    public void Arabic_uses_gregorian_months_and_years()
    {
        var arabic = CultureInfo.GetCultureInfo("ar-SA");
        var label = LegalDocumentVersions.FormatVersionLabel(LegalDocumentVersions.Privacy.Version, arabic);
        var date = LegalDocumentVersions.FormatDate(LegalDocumentVersions.Privacy.EffectiveFrom, arabic);

        // Gregorian: the year 2026 (Hijri would be 1447/1448) must appear in both lines.
        Assert.Contains("2026", label, StringComparison.Ordinal);
        Assert.Contains("2026", date, StringComparison.Ordinal);
        Assert.DoesNotContain("1447", label, StringComparison.Ordinal);
        Assert.DoesNotContain("1448", label, StringComparison.Ordinal);
    }

    [Fact]
    public void Dutch_date_reads_as_day_month_year()
        => Assert.Equal(
            "1 oktober 2026",
            LegalDocumentVersions.FormatDate(
                LegalDocumentVersions.Privacy.EffectiveFrom,
                CultureInfo.GetCultureInfo("nl-NL")));

    [Fact]
    public void Consent_version_is_not_touched_by_this_stack()
        => Assert.Equal("2026-09-26", PrivacyConstants.CurrentConsentVersion);
}
