namespace Jobsy.Core.Scholen;

/// <summary>Shared je-form sentence glue for G78 story and tiles.</summary>
internal static class PupilG78JeNarrative
{
    public static string MergeTwo(string a, string b)
    {
        var first = AsSentence(a).TrimEnd('.');
        var tail = JeContinuation(b);
        return $"{first}, en {tail}.";
    }

    public static string AsSentence(string text)
        => PupilQuestionStoryParaphrase.PolishJeForm(text.Trim().TrimEnd('.') + ".");

    private static readonly string[] NeutralOpeners =
    [
        "In je antwoorden zie je dat",
        "Je gaf aan dat",
        "Wat opvalt is dat",
    ];

    public static string NeutralFactSentence(G78AnswerClause clause, int openerIndex)
    {
        if (clause.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
            || clause.Text.StartsWith("Juist hier zie je het anders", StringComparison.Ordinal))
        {
            return "";
        }

        var canonical = PupilG78QuestionJeForms.ForItem(clause.ItemId) ?? clause.Text.Trim();
        var inner = canonical.StartsWith("Je ", StringComparison.Ordinal)
            ? ToDatSubordinate(canonical)
            : ToDatSubordinateNonJe(canonical);
        var opener = NeutralOpeners[Math.Abs(openerIndex) % NeutralOpeners.Length];
        return $"{opener} {inner}.";
    }

    private static string ToDatSubordinateNonJe(string text)
    {
        var s = text.Trim().TrimEnd('.');
        var inNieuweKlas = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^In een nieuwe klas praat je (.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (inNieuweKlas.Success)
        {
            return "je in een nieuwe klas " + inNieuweKlas.Groups[1].Value.TrimEnd('.') + " praat";
        }

        var bijToets = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^Bij een lastige toets blijf je (.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (bijToets.Success)
        {
            return "je bij een lastige toets " + bijToets.Groups[1].Value.TrimEnd('.') + " blijft";
        }

        return ToDatSubordinate("Je " + char.ToLowerInvariant(s[0]) + s[1..]);
    }

    public static string JeContinuation(string text)
    {
        var s = text.Trim().TrimEnd('.');
        if (s.StartsWith("Je ", StringComparison.Ordinal))
        {
            return "je " + s[3..];
        }

        return s;
    }

    /// <summary>Main-clause inversion after connectors like "Daarnaast".</summary>
    public static string InvertedContinuation(string text)
    {
        var s = text.Trim().TrimEnd('.');
        if (!s.StartsWith("Je ", StringComparison.Ordinal))
        {
            if (s.StartsWith("Bij ", StringComparison.Ordinal))
            {
                return "bij" + s[3..].ToLowerInvariant();
            }

            if (s.StartsWith("In ", StringComparison.Ordinal))
            {
                return "in" + s[2..].ToLowerInvariant();
            }

            return char.ToLowerInvariant(s[0]) + s[1..];
        }

        var body = s[3..];
        var idx = body.IndexOf(' ');
        if (idx < 0)
        {
            return JeContinuation(text);
        }

        var verb = body[..idx];
        var rest = body[(idx + 1)..];
        if (!StemAfterConnector.TryGetValue(verb, out var stem))
        {
            return JeContinuation(text);
        }

        return $"{stem} je {rest}";
    }

    public static string MinderVaakDatLine(string? canonicalJe)
    {
        if (string.IsNullOrWhiteSpace(canonicalJe))
        {
            return "";
        }

        var clause = ToDatSubordinate(canonicalJe);
        return $"Je zei minder vaak dat {clause}.";
    }

    internal static string ToDatSubordinate(string jeSentence)
    {
        var s = jeSentence.Trim().TrimEnd('.');
        if (s.StartsWith("Je ", StringComparison.Ordinal))
        {
            s = s[3..];
        }

        var vindtHet = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^vindt\s+het\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (vindtHet.Success)
        {
            return "je het " + vindtHet.Groups[1].Value.TrimEnd('.') + " vindt";
        }

        var wiltLater = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^wilt\s+later\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (wiltLater.Success)
        {
            return "je later " + wiltLater.Groups[1].Value.TrimEnd('.') + " wilt";
        }

        var merkt = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^merkt\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (merkt.Success)
        {
            return "je " + merkt.Groups[1].Value.TrimEnd('.') + " merkt";
        }

        var maaktHuiswerk = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^maakt\s+je\s+huiswerk\s+af,\s*(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (maaktHuiswerk.Success)
        {
            return "je je huiswerk afmaakt, " + maaktHuiswerk.Groups[1].Value.TrimEnd('.');
        }

        var vindtFijnAls = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^vindt\s+het\s+fijn\s+als\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (vindtFijnAls.Success)
        {
            return "je het fijn vindt als " + vindtFijnAls.Groups[1].Value.TrimEnd('.');
        }

        var vindtBelangrijkDat = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^vindt\s+het\s+belangrijk\s+dat\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (vindtBelangrijkDat.Success)
        {
            return "je het belangrijk vindt dat " + vindtBelangrijkDat.Groups[1].Value.TrimEnd('.');
        }

        var vindtBelangrijk = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^vindt\s+het\s+belangrijk\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (vindtBelangrijk.Success)
        {
            return "je het belangrijk vindt " + vindtBelangrijk.Groups[1].Value.TrimEnd('.');
        }

        var maaktGraag = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^maakt\s+graag\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (maaktGraag.Success)
        {
            return "je graag " + maaktGraag.Groups[1].Value.TrimEnd('.') + " maakt";
        }

        var wiltHeelGoedWorden = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^wilt\s+heel\s+goed\s+worden\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (wiltHeelGoedWorden.Success)
        {
            return "je heel goed wilt worden " + wiltHeelGoedWorden.Groups[1].Value.TrimEnd('.');
        }

        var blijft = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^blijft\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (blijft.Success)
        {
            return "je " + blijft.Groups[1].Value.TrimEnd('.') + " blijft";
        }

        var bedenktGraag = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^bedenkt\s+graag\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (bedenktGraag.Success)
        {
            return "je graag " + bedenktGraag.Groups[1].Value.TrimEnd('.') + " bedenkt";
        }

        var helptGraag = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^helpt\s+graag\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (helptGraag.Success)
        {
            return "je graag " + helptGraag.Groups[1].Value.TrimEnd('.') + " helpt";
        }

        var zoektGraag = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^zoekt\s+graag\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (zoektGraag.Success)
        {
            return "je graag " + zoektGraag.Groups[1].Value.TrimEnd('.') + " zoekt";
        }

        var werktGraag = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^werkt\s+graag\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (werktGraag.Success)
        {
            return "je graag " + werktGraag.Groups[1].Value.TrimEnd('.') + " werkt";
        }

        var wiltGraag = System.Text.RegularExpressions.Regex.Match(
            s,
            @"^wilt\s+graag\s+(.+)$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (wiltGraag.Success)
        {
            return "je graag " + wiltGraag.Groups[1].Value.TrimEnd('.') + " wilt";
        }

        return "je " + s;
    }

    private static readonly Dictionary<string, string> StemAfterConnector =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["maakt"] = "maak",
            ["bedenkt"] = "bedenk",
            ["werkt"] = "werk",
            ["helpt"] = "help",
            ["vindt"] = "vind",
            ["houdt"] = "houd",
            ["neemt"] = "neem",
            ["zoekt"] = "zoek",
            ["speelt"] = "speel",
            ["deelt"] = "deel",
            ["voelt"] = "voel",
            ["blijft"] = "blijf",
            ["laat"] = "laat",
            ["past"] = "pas",
            ["overtuigt"] = "overtuig",
        };

    public static bool TooSimilar(string a, string b)
    {
        static IEnumerable<string> Tokens(string t) => t
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4);

        var overlap = Tokens(a).Intersect(Tokens(b)).ToList();
        return overlap.Count >= 2
               || (a.Contains("help", StringComparison.OrdinalIgnoreCase)
                   && b.Contains("help", StringComparison.OrdinalIgnoreCase))
               || (a.Contains("niet snapt", StringComparison.OrdinalIgnoreCase)
                   && b.Contains("niet snapt", StringComparison.OrdinalIgnoreCase));
    }
}
