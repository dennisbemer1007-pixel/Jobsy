using System.Globalization;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Tokens;

/// <summary>
/// Employer-facing token-log copy: no technical IDs, explicit token amounts, readable dates.
/// </summary>
public static class TokenLogPresentation
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    public static string FormatAmount(decimal amount)
    {
        var formatted = amount.ToString("+0.##;-0.##;0", Dutch);
        return formatted;
    }

    public static string AmountToneClass(decimal amount)
        => amount > 0 ? "token-log__amount--in"
            : amount < 0 ? "token-log__amount--out"
            : "token-log__amount--zero";

    public static string FormatWhen(DateTime utc)
    {
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        var day = local.ToString("d MMM yyyy", Dutch).Replace(".", "");
        return $"{day} om {local:HH:mm}";
    }

    public static string FormatDateShort(DateTime utc)
    {
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        return local.ToString("dd-MM-yyyy", Dutch);
    }

    public static string Describe(TokenLogItem log)
    {
        var headline = Headline(log.Kind, log.Reason);
        var note = SanitizeNote(log.Note);
        if (string.IsNullOrWhiteSpace(note) || NoteRepeatsHeadline(note, headline))
        {
            return headline;
        }

        return $"{headline} · {note}";
    }

    public static string Headline(string kind, string reason)
    {
        if (string.Equals(kind, "Spend", StringComparison.OrdinalIgnoreCase))
        {
            return reason switch
            {
                "Publish" => "Publiceren",
                "Highlight" => "Uitlichten",
                "PushBom" => "Pushbericht",
                "Extend" => "Verlengen",
                "ContactUnlock" => "Contact talentpool",
                "InsightsUnlock" => "Kandidaatinzichten",
                _ => "Tokenuitgave"
            };
        }

        return kind switch
        {
            "Purchase" => "Gekocht",
            "Grant" => "Toegekend",
            "Allocation" => "Verdeeld",
            "Goodwill" => "Coulance",
            _ => string.IsNullOrWhiteSpace(kind) ? "Tokentransactie" : kind
        };
    }

    public static string CostLabel(string reason) => reason switch
    {
        "Publish" => "Vacature publiceren",
        "Extend" => "Verlengen",
        "Highlight" => "Uitlichten",
        "PushBom" => "Pushbericht naar kandidaten",
        "ContactUnlock" => "Contact via talentpool",
        "InsightsUnlock" => "Kandidaatinzichten ontgrendelen",
        _ => reason
    };

    public static string SanitizeNote(string? note)
        => TokenNoteRedaction.Sanitize(note);

    private static bool NoteRepeatsHeadline(string note, string headline)
        => note.Equals(headline, StringComparison.OrdinalIgnoreCase)
           || headline.Contains(note, StringComparison.OrdinalIgnoreCase);
}
