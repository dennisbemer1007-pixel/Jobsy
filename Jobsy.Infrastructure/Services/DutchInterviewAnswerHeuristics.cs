using System.Text.RegularExpressions;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Lightweight Dutch heuristics for mock-interview answers (no external API).
/// </summary>
public static class DutchInterviewAnswerHeuristics
{
    private static readonly string[] InsultMarkers =
    [
        "kanker", "tering", "tyfus", "klootzak", "lul", "hoer", "neger",
        "mongool", "mongolen", "idioot", "debiel", "retard", "kutwijf",
        "sukkel", "loser", "fuck you", "fuckjou", "fuck jou", "rot op",
        "krijg de", "godver", "godverdomme", "shithead", "bitch",
        "nazi", "hitler", "verkracht", "doodwens", "ik vermoord",
        "smeerlap", "eikel", "sukkel", "stomme kut", "kankerlijer"
    ];

    private static readonly string[] SoftRudeMarkers =
    [
        "stomme", "sukkelachtig", "wat een kut", "dit is kut", "je bent dom",
        "hou je bek", "flikker op", "donder op"
    ];

    private static readonly string[] StarMarkers =
    [
        "toen", "bijvoorbeeld", "omdat", "daardoor", "daarna", "uiteindelijk",
        "resultaat", "geleerd", "ik deed", "ik heb", "ik zorgde", "ik hielp",
        "situatie", "daarom"
    ];

    public static bool LooksInsulting(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return false;
        }

        var normalized = Normalize(answer);
        return InsultMarkers.Any(m => normalized.Contains(Normalize(m), StringComparison.Ordinal))
               || SoftRudeMarkers.Any(m => normalized.Contains(Normalize(m), StringComparison.Ordinal));
    }

    public static string FriendlyToneRedirect(string? vacancyHook = null)
    {
        var hook = string.IsNullOrWhiteSpace(vacancyHook)
            ? "deze functie"
            : vacancyHook.Trim();
        return
            "Dat is geen nette reactie voor een sollicitatiegesprek — ik zeg het vriendelijk, " +
            "want je mag best eerlijk of boos zijn, maar zonder beledigingen. " +
            "Laten we het opnieuw proberen over " + hook + ", op een respectvolle manier.";
    }

    public static string? ExtractQuote(string answer, int maxLen = 56)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        var cleaned = Regex.Replace(answer.Trim(), @"\s+", " ");
        if (cleaned.Length <= 8)
        {
            return null;
        }

        // Prefer a clause around a STAR marker.
        foreach (var marker in StarMarkers)
        {
            var idx = cleaned.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                continue;
            }

            var start = Math.Max(0, idx - 12);
            var slice = cleaned[start..];
            if (slice.Length > maxLen)
            {
                slice = slice[..(maxLen - 1)].TrimEnd(',', '.', ';', ' ') + "…";
            }

            return slice;
        }

        if (cleaned.Length <= maxLen)
        {
            return cleaned;
        }

        return cleaned[..(maxLen - 1)].TrimEnd(',', '.', ';', ' ') + "…";
    }

    public static bool LooksVague(string answer)
        => string.IsNullOrWhiteSpace(answer)
           || answer.Trim().Length < 28
           || (!HasStarCue(answer) && answer.Trim().Length < 70);

    public static bool HasStarCue(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return false;
        }

        var text = answer.ToLowerInvariant();
        return StarMarkers.Count(m => text.Contains(m, StringComparison.Ordinal)) >= 1;
    }

    public static string BuildRewriteSuggestion(string answer, string vacancyHook)
    {
        var hook = string.IsNullOrWhiteSpace(vacancyHook) ? "de taken uit de vacature" : vacancyHook.Trim();
        if (LooksInsulting(answer))
        {
            return
                $"Probeer zo: \"Bij {hook} vind ik het belangrijk om rustig te blijven. " +
                "Bijvoorbeeld toen het druk was, deed ik eerst X en checkte daarna of alles klopte.\"";
        }

        if (LooksVague(answer))
        {
            return
                $"Probeer zo: \"Bijvoorbeeld bij {hook}: toen [situatie], deed ik [actie], " +
                "en daardoor [resultaat].\"";
        }

        var quote = ExtractQuote(answer, 40);
        if (!string.IsNullOrWhiteSpace(quote))
        {
            return
                $"Probeer zo: \"{TrimForRewrite(quote)} — dat paste bij {hook} omdat ik " +
                "[wat jij deed] en dat leidde tot [resultaat].\"";
        }

        return
            $"Probeer zo: koppel je antwoord aan {hook} met situatie → actie → resultaat in twee zinnen.";
    }

    private static string TrimForRewrite(string quote)
    {
        var q = quote.Trim().Trim('"', '“', '”');
        if (q.Length > 42)
        {
            q = q[..39].TrimEnd() + "…";
        }

        return q;
    }

    private static string Normalize(string value)
        => Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();
}
