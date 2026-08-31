using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Web.Models;

namespace Jobsy.Web.Tokens;

/// <summary>
/// Employer-facing token-log copy: no technical IDs, explicit token amounts, readable dates.
/// </summary>
public static partial class TokenLogPresentation
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
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return "";
        }

        var text = note.Trim();
        text = MolliePaymentId().Replace(text, "");
        text = StubPaymentId().Replace(text, "");
        text = GuidInParens().Replace(text, "");
        text = DashedGuid().Replace(text, "");
        text = HexId().Replace(text, "");
        text = text.Replace("Mollie stub", "Betaling", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Mollie", "Betaling", StringComparison.OrdinalIgnoreCase);
        text = LeftoverPunctuation().Replace(text, " ");
        return text.Trim(' ', '-', '·', '/', '|', ':');
    }

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

    [GeneratedRegex(@"\b(?:tr_|stub_pay_)[A-Za-z0-9]+\b", RegexOptions.IgnoreCase)]
    private static partial Regex MolliePaymentId();

    [GeneratedRegex(@"\bstub_pay_[A-Za-z0-9]+\b", RegexOptions.IgnoreCase)]
    private static partial Regex StubPaymentId();

    [GeneratedRegex(@"\([0-9a-f]{32}\)", RegexOptions.IgnoreCase)]
    private static partial Regex GuidInParens();

    [GeneratedRegex(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase)]
    private static partial Regex DashedGuid();

    [GeneratedRegex(@"\b[0-9a-f]{32}\b", RegexOptions.IgnoreCase)]
    private static partial Regex HexId();

    [GeneratedRegex(@"[\s·/|,;:-]{2,}")]
    private static partial Regex LeftoverPunctuation();
}
