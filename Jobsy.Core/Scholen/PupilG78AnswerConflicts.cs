namespace Jobsy.Core.Scholen;

/// <summary>Detects trait clashes between highs, tile contrasts, and Holland closing lines.</summary>
public static class PupilG78AnswerConflicts
{
    private static readonly Dictionary<int, ConflictProfile> LowProfiles = new()
    {
        [9019] = new(["help", "uitleg", "klasgenoot", "steun", "iemand die iets niet snapt"], ['S']),
        [9024] = new(["steun", "help", "anderen"], ['S']),
        [9006] = new(["klasgenoot", "help", "uitleg"], ['S']),
        [9015] = new(["leiding", "voortouw", "overtuig"], ['E']),
        [9020] = new(["leiding", "voortouw", "overtuig"], ['E']),
        [9016] = new(["maken", "repareren", "handen"], ['R']),
        [9012] = new(["plan", "netjes", "op tijd"], ['C']),
    };

    public static bool TileLowConflictsWithText(int lowItemId, string text)
    {
        if (!LowProfiles.TryGetValue(lowItemId, out var profile))
        {
            return false;
        }

        var t = text.ToLowerInvariant();
        return profile.Tokens.Any(tok => t.Contains(tok, StringComparison.Ordinal));
    }

    public static bool HollandLetterConflictsWithTileLows(char letter, IReadOnlyList<int> tileLowItemIds)
    {
        foreach (var id in tileLowItemIds)
        {
            if (LowProfiles.TryGetValue(id, out var profile) && profile.SuppressedHolland.Contains(letter))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TextsContradict(IReadOnlyList<string> texts)
    {
        var helpHigh = texts.Any(t => ContainsHelpHigh(t));
        var helpLow = texts.Any(t => t.Contains("uitleggen is minder", StringComparison.OrdinalIgnoreCase)
                                    || t.Contains("helpt minder", StringComparison.OrdinalIgnoreCase)
                                    || t.Contains("minder vaak dat je helpt", StringComparison.OrdinalIgnoreCase));
        return helpHigh && helpLow;
    }

    public static bool ContainsHelpHigh(string text)
    {
        var t = text.ToLowerInvariant();
        return (t.Contains("helpt", StringComparison.Ordinal) || t.Contains("help je", StringComparison.Ordinal))
               && !t.Contains("minder", StringComparison.Ordinal)
               && !t.Contains("minder vaak", StringComparison.Ordinal);
    }

    private sealed record ConflictProfile(string[] Tokens, char[] SuppressedHolland);
}
