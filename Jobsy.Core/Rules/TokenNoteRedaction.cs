using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Strips payment hashes and GUIDs from employer-facing token-log notes.</summary>
public static partial class TokenNoteRedaction
{
    public static string Sanitize(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return "";
        }

        var text = note.Trim();
        text = MolliePaymentId().Replace(text, "");
        text = GuidInParens().Replace(text, "");
        text = DashedGuid().Replace(text, "");
        text = HexId().Replace(text, "");
        text = text.Replace("Mollie stub", "Betaling", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Mollie", "Betaling", StringComparison.OrdinalIgnoreCase);
        text = LeftoverPunctuation().Replace(text, " ");
        return text.Trim(' ', '-', '·', '/', '|', ':');
    }

    [GeneratedRegex(@"\b(?:tr_|stub_pay_)[A-Za-z0-9]+\b", RegexOptions.IgnoreCase)]
    private static partial Regex MolliePaymentId();

    [GeneratedRegex(@"\([0-9a-f]{32}\)", RegexOptions.IgnoreCase)]
    private static partial Regex GuidInParens();

    [GeneratedRegex(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase)]
    private static partial Regex DashedGuid();

    [GeneratedRegex(@"\b[0-9a-f]{32}\b", RegexOptions.IgnoreCase)]
    private static partial Regex HexId();

    [GeneratedRegex(@"[\s·/|,;:-]{2,}")]
    private static partial Regex LeftoverPunctuation();
}
