using System.Text.RegularExpressions;

namespace Jobsy.Core.Scholen;

/// <summary>English dream copy when <see cref="PupilVerhaalEnglish"/> has no job-specific key yet.</summary>
public static class PupilVerhaalEnglishDreamFallback
{
    public static string FromDutch(string nl)
    {
        if (string.IsNullOrWhiteSpace(nl))
        {
            return nl;
        }

        var t = nl.Trim().TrimEnd('!', '.');
        foreach (var (pattern, replacement) in Replacements)
        {
            t = Regex.Replace(t, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return t;
    }

    private static readonly (string Pattern, string Replacement)[] Replacements =
    [
        ("\\bgroep\\s*8\\b", "Group 8"),
        ("\\bgroep\\s*7\\b", "Group 7"),
        ("\\bgroep\\s*6\\b", "Group 6"),
        ("\\bDiergeneeskunde\\b", "Veterinary study"),
        ("\\bhavo/vwo\\b", "Havo/vwo"),
        ("\\bhavo\\b", "Havo"),
        ("\\bvwo\\b", "Vwo"),
        ("\\bMBO\\b", "Vocational college"),
        ("\\bmbo\\b", "Vocational college"),
        ("\\bpraktijk\\b", "practice"),
        ("\\bdierentuin\\b", "zoo"),
        ("\\bboerderij\\b", "farm"),
        ("\\bJe bent nieuwsgierig\\b", "You are curious"),
        ("\\bDat is een goede start\\b", "That is a good start"),
        ("\\bBlijf oefenen\\b", "Keep practising"),
        ("\\bpraat met je leraar\\b", "talk with your teacher"),
        ("\\bpraat met je docent\\b", "talk with your mentor"),
        ("\\bvolgende stap\\b", "next step"),
        ("\\bIn een\\b", "In a"),
        ("\\bof op de\\b", "or on the"),
        ("\\bof in een\\b", "or in a"),
        ("\\bNu\\b", "Now"),
        ("\\bLater\\b", "Later"),
        ("\\bVolgende\\b", "Next"),
        ("\\bDoel\\b", "Goal"),
        ("\\bWat je al hebt\\b", "What you already have"),
        ("\\bWat je nog nodig hebt\\b", "What you still need"),
        ("\\bSterk\\b", "Strong"),
        ("\\bNog oefenen\\b", "Still practising"),
    ];
}
