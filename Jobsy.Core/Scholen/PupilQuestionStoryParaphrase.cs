using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Turns a answered G78 Likert stem into a short je/jij clause (no invented facts).</summary>
public static class PupilQuestionStoryParaphrase
{
    private static readonly HashSet<int> NegativeStems =
    [
        9011, 9012, 9013, 9014, 9015, 9057
    ];

    public static int EffectiveScore(int answer, bool reverse)
        => reverse ? 6 - answer : answer;

    /// <summary>Clause when the pupil strongly agreed with the measured trait (effective ≥ 4).</summary>
    public static string? StrongAgreeClause(int itemId, bool reverse, int answer, string language = "nl")
    {
        if (answer is < 1 or > 5)
        {
            return null;
        }

        if (EffectiveScore(answer, reverse) < 4)
        {
            return null;
        }

        return PupilVerhaalLocalized.IsEnglish(language)
            ? null
            : ClauseNl(itemId, reverse, answer);
    }

  /// <summary>Clause when the pupil clearly disagreed (effective ≤ 2).</summary>
    public static string? StrongDisagreeClause(int itemId, bool reverse, int answer, string language = "nl")
    {
        if (answer is < 1 or > 5)
        {
            return null;
        }

        if (EffectiveScore(answer, reverse) > 2)
        {
            return null;
        }

        return PupilVerhaalLocalized.IsEnglish(language)
            ? null
            : DisagreeClauseNl(itemId, reverse, answer);
    }

    private static string? ClauseNl(int itemId, bool reverse, int answer)
    {
        var stem = PupilQuestionBankNlStrings.Get($"LeerlingQ.{itemId}");
        if (stem.StartsWith("LeerlingQ.", StringComparison.Ordinal))
        {
            return null;
        }

        var agrees = answer >= 4;
        var negative = NegativeStems.Contains(itemId);
        if (reverse)
        {
            negative = !negative;
        }

        if (negative)
        {
            return agrees ? PositiveOpposite(itemId, stem) : ToJeForm(stem);
        }

        return agrees ? ToJeForm(stem) : SoftOpposite(stem);
    }

    private static string? DisagreeClauseNl(int itemId, bool reverse, int answer)
    {
        var stem = PupilQuestionBankNlStrings.Get($"LeerlingQ.{itemId}");
        if (stem.StartsWith("LeerlingQ.", StringComparison.Ordinal))
        {
            return null;
        }

        var disagrees = answer <= 2;
        if (!disagrees)
        {
            return null;
        }

        var negative = NegativeStems.Contains(itemId);
        if (reverse)
        {
            negative = !negative;
        }

        return negative ? PositiveOpposite(itemId, stem) : SoftOpposite(stem);
    }

    public static string ToJeForm(string stem)
    {
        var s = stem.Trim().TrimEnd('.');
        if (s.StartsWith("Ik ", StringComparison.Ordinal))
        {
            s = "Je " + s[3..];
        }

        s = s.Replace(" ik ", " je ", StringComparison.Ordinal);
        s = s.Replace(" mijn ", " jouw ", StringComparison.Ordinal);
        s = s.Replace("Ik ", "Je ", StringComparison.Ordinal);
        s = FixJeVerb(s);
        if (!s.EndsWith('.'))
        {
            s += ".";
        }

        return s;
    }

    private static string SoftOpposite(string stem)
    {
        var je = ToJeForm(stem).TrimEnd('.');
        return $"Dit zei je minder vaak over jezelf: {je.ToLowerInvariant()}.";
    }

    private static string? PositiveOpposite(int itemId, string stem)
        => ItemPositiveOpposite.TryGetValue(itemId, out var t) ? t : DefaultOpposite(stem);

    private static string DefaultOpposite(string stem)
    {
        var je = ToJeForm(stem).TrimEnd('.');
        return $"Juist hier zie je het anders: {je.ToLowerInvariant()}.";
    }

    private static string FixJeVerb(string s)
    {
        return s
            .Replace("Je help ", "Je helpt ", StringComparison.Ordinal)
            .Replace("Je maak ", "Je maakt ", StringComparison.Ordinal)
            .Replace("Je vind ", "Je vindt ", StringComparison.Ordinal)
            .Replace("Je houd ", "Je houdt ", StringComparison.Ordinal)
            .Replace("Je werk ", "Je werkt ", StringComparison.Ordinal)
            .Replace("Je zoek ", "Je zoekt ", StringComparison.Ordinal)
            .Replace("Je neem ", "Je neemt ", StringComparison.Ordinal)
            .Replace("Je deel ", "Je deelt ", StringComparison.Ordinal)
            .Replace("Je let ", "Je let ", StringComparison.Ordinal)
            .Replace("Je pas ", "Je past ", StringComparison.Ordinal)
            .Replace("Je leer ", "Je leert ", StringComparison.Ordinal)
            .Replace("Je wil ", "Je wilt ", StringComparison.Ordinal)
            .Replace("Je speel ", "Je speelt ", StringComparison.Ordinal)
            .Replace("Je bedenk ", "Je bedenkt ", StringComparison.Ordinal)
            .Replace("Je overtuig ", "Je overtuigt ", StringComparison.Ordinal)
            .Replace("Je blijf ", "Je blijft ", StringComparison.Ordinal)
            .Replace("Je raak ", "Je raakt ", StringComparison.Ordinal)
            .Replace("Je doe ", "Je doet ", StringComparison.Ordinal)
            .Replace("Je voel ", "Je voelt ", StringComparison.Ordinal)
            .Replace(" je doe", " je doet", StringComparison.Ordinal)
            .Replace(" je heb ", " je hebt ", StringComparison.Ordinal)
            .Replace("Je doe ", "Je doet ", StringComparison.Ordinal)
            .Replace("iets doe", "iets doet", StringComparison.Ordinal);
    }

    private static readonly Dictionary<int, string> ItemPositiveOpposite = new()
    {
        [9011] = "Je houdt goed rekening met anderen.",
        [9012] = "Je maakt taken liever op tijd af.",
        [9013] = "Je blijft redelijk rustig als iets tegenzit.",
        [9014] = "Je probeert graag nieuwe manieren uit.",
        [9015] = "Je durft ook in een groep mee te doen en je te laten horen.",
        [9055] = "Je voelt je op je gemak in een informele sfeer.",
        [9057] = "Je vindt het juist fijn om af en toe iets nieuws te doen.",
    };
}
