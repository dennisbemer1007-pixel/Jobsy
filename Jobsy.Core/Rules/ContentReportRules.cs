using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Shared rules for the DSA report flow (public-pages 06): input limits, the de-duplication
/// window and which decisions need a reason.
/// </summary>
public static class ContentReportRules
{
    public const int DetailsMaxLength = 1000;
    public const int DecisionReasonMaxLength = 1000;
    public const int ReporterEmailMaxLength = 254;

    /// <summary>The same target + e-mail within this window counts as one report.</summary>
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(24);

    /// <summary>Stored on reports whose target was not publicly visible, so there is no oracle.</summary>
    public const string TargetNotPublicReason = "target not public";

    public static string? NormalizeDetails(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        var trimmed = details.Trim();
        return trimmed.Length > DetailsMaxLength ? trimmed[..DetailsMaxLength] : trimmed;
    }

    public static string? NormalizeDecisionReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length > DecisionReasonMaxLength ? trimmed[..DecisionReasonMaxLength] : trimmed;
    }

    /// <summary>Lower-cased and trimmed; anything that is not an address becomes null.</summary>
    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim().ToLowerInvariant();
        if (trimmed.Length > ReporterEmailMaxLength)
        {
            return null;
        }

        var at = trimmed.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at == trimmed.Length - 1 || trimmed.Contains(' '))
        {
            return null;
        }

        return trimmed.LastIndexOf('.') > at ? trimmed : null;
    }

    public static bool IsDecided(ContentReportStatus status)
        => status != ContentReportStatus.Open;

    /// <summary>Beperken and Verwijderen take content away, so they need a statement of reasons.</summary>
    public static bool RequiresReason(ContentReportStatus decision)
        => decision is ContentReportStatus.Restricted or ContentReportStatus.Removed;

    /// <summary>The employer only hears about decisions that limit their content.</summary>
    public static bool NotifiesOwner(ContentReportStatus decision)
        => RequiresReason(decision);

    /// <summary><c>EmailStrings</c> key for a reason, so mails read in the recipient's language.</summary>
    public static string ReasonEmailKey(ContentReportReason reason)
        => $"Email.Report.Reason.{reason}";

    /// <summary><c>EmailStrings</c> key for a decision.</summary>
    public static string DecisionEmailKey(ContentReportStatus decision)
        => $"Email.Report.Decision.{decision}";

    /// <summary><c>EmailStrings</c> key for the kind of content ("Vacature" / "Bedrijfspagina").</summary>
    public static string TargetKindEmailKey(ContentReportTargetType targetType)
        => targetType == ContentReportTargetType.Company
            ? "Email.Report.What.CompanyPage"
            : "Email.Report.What.Vacancy";

    public static bool IsDecisionAllowed(ContentReportTargetType targetType, ContentReportStatus decision)
        => decision switch
        {
            ContentReportStatus.NoAction => true,
            ContentReportStatus.Removed => true,
            // "Beperken" only exists for a vacancy; a company page is either live or blocked.
            ContentReportStatus.Restricted => targetType == ContentReportTargetType.Vacancy,
            _ => false
        };
}
