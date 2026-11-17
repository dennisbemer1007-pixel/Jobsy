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
        ["WhoAmI.ProfileTitle"] = "Mijn achtergrond",
        ["WhoAmI.WorkExperience"] = "Werkervaring",
        ["WhoAmI.Courses"] = "Opleiding & cursussen",
        ["WhoAmI.OpenCareerResults"] = "Bekijk mijn Beroepen-kompas",
        ["WhoAmI.Saved"] = "Keuze opgeslagen.",
        ["Profile.CvProposalTitle"] = "Klopt dit uit je CV?",
        ["Profile.CvProposalLead"] = "We vullen niets automatisch in. Jij kiest wat in je profiel komt.",
        ["Profile.CvProposalConfirm"] = "Gekozen velden overnemen",
        ["Profile.CvProposalField.rijbewijs"] = "Rijbewijs",
        ["Profile.CvProposalField.opleiding"] = "Opleiding",
        ["Profile.CvProposalField.gewenste rollen"] = "Gewenste rollen",
        ["Profile.CvProposalField.werkervaring"] = "Werkervaring",
        ["Profile.CvProposalField.certificaten"] = "Certificaten",
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
        ["WhoAmI.ProfileTitle"] = "My background",
        ["WhoAmI.WorkExperience"] = "Work experience",
        ["WhoAmI.Courses"] = "Education & courses",
        ["WhoAmI.OpenCareerResults"] = "Open my career compass",
        ["WhoAmI.Saved"] = "Saved.",
        ["Profile.CvProposalTitle"] = "Does this match your CV?",
        ["Profile.CvProposalLead"] = "We never fill your profile automatically. You choose what to keep.",
        ["Profile.CvProposalConfirm"] = "Copy selected fields",
        ["Profile.CvProposalField.rijbewijs"] = "Driving licence",
        ["Profile.CvProposalField.opleiding"] = "Education",
        ["Profile.CvProposalField.gewenste rollen"] = "Preferred roles",
        ["Profile.CvProposalField.werkervaring"] = "Work experience",
        ["Profile.CvProposalField.certificaten"] = "Certificates",
        ["WhoAmI.Progress"] = "{0} of 4 steps done"
    };
}
