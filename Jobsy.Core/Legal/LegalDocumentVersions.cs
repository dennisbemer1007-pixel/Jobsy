using System.Globalization;

namespace Jobsy.Core.Legal;

/// <summary>Stable document ids used by the legal pages and <see cref="LegalDocumentVersions"/>.</summary>
public static class LegalDocumentIds
{
    public const string Privacy = "privacy";
    public const string TermsEmployer = "terms-employer";
    public const string TermsCandidate = "terms-candidate";

    public static bool IsKnown(string? documentId)
        => documentId is Privacy or TermsEmployer or TermsCandidate;
}

/// <summary>One published version of a legal document (D15).</summary>
public sealed record LegalVersion(string Version, DateOnly EffectiveFrom);

/// <summary>A history entry for "Wat is er veranderd?" (D16).</summary>
public sealed record LegalVersionEntry(string Version, DateOnly EffectiveFrom, string SummaryKey);

/// <summary>
/// Single source of truth for the version and effective date of every legal document (D15).
/// Never type a date in the markup; the version line is rendered from here.
/// <see cref="Privacy.PrivacyConstants.CurrentConsentVersion"/> is the consent version and is not touched here.
/// </summary>
public static class LegalDocumentVersions
{
    public static readonly LegalVersion Privacy = new("2026-10-07", new DateOnly(2026, 10, 7));

    /// <summary>One version for both terms documents (algemene voorwaarden + gebruiksvoorwaarden).</summary>
    public static readonly LegalVersion Terms = new("2026-10", new DateOnly(2026, 10, 1));

    public static readonly IReadOnlyList<LegalVersionEntry> PrivacyHistory =
    [
        new("2026-10-07", new DateOnly(2026, 10, 7), "Legal.Change.Privacy.2026-10-07"),
        new("2026-10-06", new DateOnly(2026, 10, 6), "Legal.Change.Privacy.2026-10-06"),
        new("2026-10-04", new DateOnly(2026, 10, 4), "Legal.Change.Privacy.2026-10-04"),
        new("2026-10", new DateOnly(2026, 10, 1), "Legal.Change.Privacy.2026-10"),
        new("2026-09", new DateOnly(2026, 9, 26), "Legal.Change.Privacy.2026-09")
    ];

    public static readonly IReadOnlyList<LegalVersionEntry> TermsHistory =
    [
        new("2026-10", new DateOnly(2026, 10, 1), "Legal.Change.Terms.2026-10"),
        new("2026-08", new DateOnly(2026, 8, 2), "Legal.Change.Terms.2026-08")
    ];

    public static LegalVersion For(string? documentId)
        => string.Equals(documentId, LegalDocumentIds.Privacy, StringComparison.Ordinal)
            ? Privacy
            : Terms;

    public static IReadOnlyList<LegalVersionEntry> History(string? documentId)
        => string.Equals(documentId, LegalDocumentIds.Privacy, StringComparison.Ordinal)
            ? PrivacyHistory
            : TermsHistory;

    /// <summary>"oktober 2026" (nl) / "October 2026" (en); always the Gregorian calendar, also for ar.</summary>
    public static string FormatVersionLabel(string version, CultureInfo? culture = null)
    {
        if (!TryParseVersion(version, out var month))
        {
            return version;
        }

        return month.ToString("MMMM yyyy", WithGregorianCalendar(culture));
    }

    /// <summary>"1 oktober 2026"; always the Gregorian calendar, also for ar.</summary>
    public static string FormatDate(DateOnly date, CultureInfo? culture = null)
        => date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", WithGregorianCalendar(culture));

    private static bool TryParseVersion(string version, out DateTime firstOfMonth)
    {
        firstOfMonth = default;
        var parts = version?.Split('-') ?? [];
        if (parts.Length < 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || month is < 1 or > 12)
        {
            return false;
        }

        firstOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return true;
    }

    /// <summary>
    /// ar-SA defaults to the Umm al-Qura calendar; legal dates are Gregorian in every language.
    /// </summary>
    private static CultureInfo WithGregorianCalendar(CultureInfo? culture)
    {
        var source = culture ?? CultureInfo.GetCultureInfo("nl-NL");
        if (source.DateTimeFormat.Calendar is GregorianCalendar)
        {
            return source;
        }

        var gregorian = source.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (gregorian is null)
        {
            return CultureInfo.InvariantCulture;
        }

        var clone = (CultureInfo)source.Clone();
        clone.DateTimeFormat.Calendar = gregorian;
        return clone;
    }
}
