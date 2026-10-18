namespace Jobsy.Core.Email.Localization;

internal static class EmailStringsAfterMatch
{
    public static IReadOnlyDictionary<string, string> For(string language) => language switch
    {
        "en" => En,
        "nl" => Nl,
        _ => En
    };

    private static readonly Dictionary<string, string> Nl = new(StringComparer.Ordinal)
    {
        ["Email.AfterMatchScheduled.Subject"] = "Gesprek bevestigd bij {0}",
        ["Email.AfterMatchScheduled.Preheader"] = "Zo staat je kennismaking gepland",
        ["Email.AfterMatchScheduled.Eyebrow"] = "Na de match",
        ["Email.AfterMatchScheduled.Heading"] = "Je gesprek staat gepland",
        ["Email.AfterMatchScheduled.P1"] = "Bij {0} voor {1} op {2}.",
        ["Email.AfterMatchScheduled.P2"] = "Voeg het moment toe aan je agenda via Lobsy. Moet het verzetten? Dat kan met één klik.",
        ["Email.AfterMatchScheduled.Cta"] = "Bekijk gesprek in Lobsy"
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["Email.AfterMatchScheduled.Subject"] = "Interview confirmed at {0}",
        ["Email.AfterMatchScheduled.Preheader"] = "Your introduction is scheduled",
        ["Email.AfterMatchScheduled.Eyebrow"] = "After the match",
        ["Email.AfterMatchScheduled.Heading"] = "Your interview is scheduled",
        ["Email.AfterMatchScheduled.P1"] = "At {0} for {1} on {2}.",
        ["Email.AfterMatchScheduled.P2"] = "Add it to your calendar via Lobsy. Need to reschedule? One click is enough.",
        ["Email.AfterMatchScheduled.Cta"] = "View interview in Lobsy"
    };
}
