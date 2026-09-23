namespace Jobsy.Core.Rules;

/// <summary>
/// Soft gate before an ATS scrape may become PendingReview in the admin overview.
/// Rejects only hard failures (missing title/company, error pages). Optional fields
/// such as salary, hours, or even location may be empty so admins can complete them.
/// </summary>
public static class AtsListingValidation
{
    private static readonly string[] StrongErrorPageHints =
    [
        "404", "not found", "niet gevonden", "page not found", "pagina niet gevonden",
        "name or service not known", "dns_probe", "this site can’t be reached", "this site can't be reached"
    ];

    private static readonly string[] TitleErrorHints =
    [
        "404", "not found", "niet gevonden", "access denied", "forbidden",
        "service unavailable", "foutmelding"
    ];

    /// <summary>Minimum description length for scrape intake (admin can enrich later).</summary>
    public const int MinDescriptionLengthForIntake = 12;

    public static bool TryValidateForReview(
        string? title,
        string? companyName,
        string? locationLabel,
        string? description,
        out string? rejectReason)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
        {
            rejectReason = "Functietitel ontbreekt of is te kort.";
            return false;
        }

        if (ContainsAny(title, TitleErrorHints))
        {
            rejectReason = "Titel lijkt op een foutpagina.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(companyName) || companyName.Trim().Length < 2)
        {
            rejectReason = "Bedrijfsnaam ontbreekt.";
            return false;
        }

        // Location and salary are optional at intake — missing values stay empty for admin review.
        // Description may be short; only reject empty/near-empty shells and clear error pages.
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < MinDescriptionLengthForIntake)
        {
            rejectReason = "Vacaturetekst ontbreekt of is te kort.";
            return false;
        }

        var head = description.Length <= 240 ? description : description[..240];
        if (ContainsAny(head, StrongErrorPageHints))
        {
            rejectReason = "Omschrijving lijkt op een foutpagina.";
            return false;
        }

        rejectReason = null;
        return true;
    }

    public static bool LooksLikeErrorPage(string? text)
        => ContainsAny(text, StrongErrorPageHints) || ContainsAny(text, TitleErrorHints);

    private static bool ContainsAny(string? text, string[] hints)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var hay = text.ToLowerInvariant();
        return hints.Any(h => hay.Contains(h, StringComparison.Ordinal));
    }

    public static bool IsDemoListing(string? title, string? sourceUrl, string? tagsJson)
    {
        if (!string.IsNullOrWhiteSpace(title)
            && title.Contains("(demo)", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(sourceUrl)
            && sourceUrl.Contains("/demo-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(tagsJson)
               && tagsJson.Contains("\"demo\"", StringComparison.OrdinalIgnoreCase);
    }
}
