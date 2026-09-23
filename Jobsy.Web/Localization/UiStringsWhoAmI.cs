using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

internal static class UiStringsWhoAmI
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Merge(nl, Nl());
        Merge(en, En());
        Merge(pl, Nl());
        Merge(ro, Nl());
        Merge(ar, Nl());
    }

    private static void Merge(Dictionary<string, string> target, Dictionary<string, string> extra)
    {
        foreach (var (key, value) in extra)
        {
            target[key] = value;
        }
    }

    private static Dictionary<string, string> Nl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kompas.TabWhoAmI"] = "Wie ben ik?",
        ["WhoAmI.LeadLocked"] = "Vier stappen tot jouw persoonsprofiel. Zodra elk vinkje groen is, ontvouwt zich je verhaal.",
        ["WhoAmI.LeadOpen"] = "Jouw samenvattende verhaal: wie je bent, hoe je samenwerkt, en wat je sterke punten zijn.",
        ["WhoAmI.StepProfile"] = "Profiel gevuld",
        ["WhoAmI.StepProfileHint"] = "Naam, achtergrond en reistijd/vervoer",
        ["WhoAmI.StepCompetency"] = "Competentietest gevuld",
        ["WhoAmI.StepCareer"] = "Beroepentest gevuld",
        ["WhoAmI.StepCulture"] = "Cultuurscan uitgevoerd",
        ["WhoAmI.StepDisc"] = "Cultuurscan uitgevoerd",
        ["WhoAmI.OpenCulture"] = "Start cultuurscan",
        ["WhoAmI.OpenDisc"] = "Start cultuurscan",
        ["WhoAmI.CultureTitle"] = "Hoe ik graag werk",
        ["WhoAmI.DiscTitle"] = "Hoe ik graag werk",
        ["WhoAmI.OpenProfile"] = "Naar mijn profiel",
        ["WhoAmI.OpenCompetency"] = "Start competentietest",
        ["WhoAmI.OpenCareer"] = "Start beroepentest",
        ["WhoAmI.Keywords"] = "Sterke punten",
        ["WhoAmI.StoryTitle"] = "Mijn verhaal",
        ["WhoAmI.AttachCv"] = "Voeg dit persoonsprofiel toe als officiële bijlage bij mijn Lobsy-cv.",
        ["WhoAmI.AttachHint"] = "Als je solliciteert of meedoet aan batch-hiring in Den Haag / het Westland, gaat dit verhaal als PDF-bijlage mee met je Lobsy-CV. Werkgevers zien het pas na acceptatie.",
        ["WhoAmI.Saved"] = "Keuze opgeslagen.",
        ["WhoAmI.Progress"] = "{0} van 4 stappen klaar"
    };

    private static Dictionary<string, string> En() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kompas.TabWhoAmI"] = "Who am I?",
        ["WhoAmI.LeadLocked"] = "Four steps to your personal profile. When every box is green, your story unlocks.",
        ["WhoAmI.LeadOpen"] = "Your summary story: who you are, how you work with others, and your strengths.",
        ["WhoAmI.StepProfile"] = "Profile complete",
        ["WhoAmI.StepProfileHint"] = "Name, background and travel time/transport",
        ["WhoAmI.StepCompetency"] = "Competency test complete",
        ["WhoAmI.StepCareer"] = "Career test complete",
        ["WhoAmI.StepCulture"] = "Culture scan complete",
        ["WhoAmI.StepDisc"] = "Culture scan complete",
        ["WhoAmI.OpenCulture"] = "Start culture scan",
        ["WhoAmI.OpenDisc"] = "Start culture scan",
        ["WhoAmI.CultureTitle"] = "How I like to work",
        ["WhoAmI.DiscTitle"] = "How I like to work",
        ["WhoAmI.OpenProfile"] = "Go to my profile",
        ["WhoAmI.OpenCompetency"] = "Start competency test",
        ["WhoAmI.OpenCareer"] = "Start career test",
        ["WhoAmI.Keywords"] = "Strengths",
        ["WhoAmI.StoryTitle"] = "My story",
        ["WhoAmI.AttachCv"] = "Add this personal profile as an official attachment to my Lobsy CV.",
        ["WhoAmI.AttachHint"] = "When you apply or join batch hiring in The Hague / Westland, this story goes with your Lobsy CV as a PDF attachment. Employers see it only after they accept.",
        ["WhoAmI.Saved"] = "Saved.",
        ["WhoAmI.Progress"] = "{0} of 4 steps done"
    };
}
