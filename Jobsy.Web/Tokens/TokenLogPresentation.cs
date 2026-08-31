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
        var formatted = amount.ToString("+0.00;-0.00;0.00", Dutch);
        return $"{formatted} token";
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

    public static string SanitizeNote(string? note)
        => TokenNoteRedaction.Sanitize(note);

    private static string Headline(string kind, string reason)
    {
        if (string.Equals(kind, "Spend", StringComparison.OrdinalIgnoreCase))
        {
            return reason switch
            {
                "Publish" => "Vacature publiceren",
                "Highlight" => "Highlight",
                "PushBom" => "Pushbom",
                "Extend" => "Vacature verlengen",
                _ => "Tokenuitgave"
            };
        }

        return kind switch
        {
            "Purchase" => "Tokenaankoop",
            "Grant" => "Toekenning",
            "Allocation" => "Uitgifte aan vestiging",
            "Goodwill" => "Compensatie",
            _ => string.IsNullOrWhiteSpace(kind) ? "Tokentransactie" : kind
        };
    }

    private static bool NoteRepeatsHeadline(string note, string headline)
        => note.Equals(headline, StringComparison.OrdinalIgnoreCase)
           || headline.Contains(note, StringComparison.OrdinalIgnoreCase);
}
