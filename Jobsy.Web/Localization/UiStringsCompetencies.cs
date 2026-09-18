using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

internal static class UiStringsCompetencies
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
        ["Competency.Title"] = "Competentietest",
        ["Competency.Section"] = "Competenties",
        ["Competency.Lead"] = "Twintig korte stellingen, gebaseerd op het Big Five / OCEAN-model. We vertalen je antwoorden naar vier werkcompetenties: samenwerken, resultaatgerichtheid, stressbestendigheid en innovatie.",
        ["Competency.ScienceNote"] = "De vragen zijn een werkgerichte Mini-IPIP-stijl (Big Five). Sommige stellingen zijn omgekeerd geformuleerd, zodat we een eerlijk beeld krijgen — niet of je “sociaal wenselijk” antwoordt.",
        ["Competency.Empty"] = "Je hebt de test nog niet ingevuld. In vijf minuten zie je hoe jij scoort — en welke vacatures daarbij passen.",
        ["Competency.Start"] = "Start de test",
        ["Competency.Continue"] = "Test verder invullen",
        ["Competency.Retake"] = "Test opnieuw invullen / aanpassen",
        ["Competency.DraftProgress"] = "Concept: {0} van {1} vragen beantwoord. Je kunt later verdergaan.",
        ["Competency.CompletedOn"] = "Laatst afgerond op {0}.",
        ["Competency.SaveDraft"] = "Tussentijds opslaan",
        ["Competency.Complete"] = "Afronden en opslaan",
        ["Competency.SavedDraft"] = "Concept opgeslagen. Je kunt later verder.",
        ["Competency.SavedComplete"] = "Test opgeslagen. Je scores en matches zijn bijgewerkt.",
        ["Competency.Progress"] = "Vraag {0} van {1}",
        ["Competency.CategoryProgress"] = "{0}: {1} van {2}",
        ["Competency.BackToProfile"] = "Terug naar profiel",
        ["Competency.Likert.1"] = "Helemaal oneens",
        ["Competency.Likert.2"] = "Oneens",
        ["Competency.Likert.3"] = "Neutraal",
        ["Competency.Likert.4"] = "Eens",
        ["Competency.Likert.5"] = "Helemaal eens",
        ["Competency.Cat.Samenwerken"] = "Samenwerken",
        ["Competency.Cat.Samenwerken.Hint"] = "Hoe fijn jij in een team en met klanten of collega’s optrekt (Big Five: vriendelijkheid + extraversie).",
        ["Competency.Cat.Resultaatgerichtheid"] = "Resultaatgerichtheid",
        ["Competency.Cat.Resultaatgerichtheid.Hint"] = "Of je afmaakt wat je belooft en netjes werkt (Big Five: consciëntieusheid).",
        ["Competency.Cat.Stressbestendigheid"] = "Stressbestendigheid",
        ["Competency.Cat.Stressbestendigheid.Hint"] = "Hoe rustig je blijft als het druk of onverwacht is (Big Five: emotionele stabiliteit).",
        ["Competency.Cat.Innovatie"] = "Innovatie / probleemoplossen",
        ["Competency.Cat.Innovatie.Hint"] = "Of je nieuwe wegen zoekt als iets vastloopt (Big Five: openheid).",
        ["Competency.Q01"] = "Ik werk graag samen met anderen om een klus af te ronden.",
        ["Competency.Q02"] = "Ik help collega’s ook als het niet letterlijk bij mijn taak hoort.",
        ["Competency.Q03"] = "Ik luister eerst naar anderen voordat ik mijn mening geef.",
        ["Competency.Q04"] = "Ik houd me liever afzijdig als het team moet overleggen.",
        ["Competency.Q05"] = "Ik vind het lastig om rekening te houden met andermans gevoelens.",
        ["Competency.Q06"] = "Ik maak af wat ik beloof, ook als het tegenzit.",
        ["Competency.Q07"] = "Ik plan mijn werk zodat deadlines haalbaar blijven.",
        ["Competency.Q08"] = "Ik let op details zodat het werk in één keer goed is.",
        ["Competency.Q09"] = "Ik stel klusjes vaak uit tot het laatste moment.",
        ["Competency.Q10"] = "Ik laat rommel of onafgemaakt werk makkelijk liggen.",
        ["Competency.Q11"] = "Ik blijf kalm als het druk is of er iets misgaat.",
        ["Competency.Q12"] = "Ik herstel snel na een tegenslag op het werk.",
        ["Competency.Q13"] = "Ik kan meerdere dingen tegelijk aan zonder in paniek te raken.",
        ["Competency.Q14"] = "Ik pieker lang na over fouten of kritiek.",
        ["Competency.Q15"] = "Ik raak snel van slag als er iets onverwachts gebeurt.",
        ["Competency.Q16"] = "Ik bedenk graag nieuwe manieren om een probleem op te lossen.",
        ["Competency.Q17"] = "Ik vind het leuk om nieuwe taken of systemen te leren.",
        ["Competency.Q18"] = "Ik zie snel verbanden die anderen misschien missen.",
        ["Competency.Q19"] = "Ik hou het liefst vast aan hoe we het altijd al deden.",
        ["Competency.Q20"] = "Ik vind nieuwe ideeën of veranderingen vooral lastig.",
        ["Competency.Matches.Title"] = "Top 10 vacatures",
        ["Competency.Matches.Lead"] = "Alleen matches van 60% of hoger, gesorteerd van hoog naar laag. Het vraagteken legt in gewone taal uit waarom het past — en waar het gat zit.",
        ["Competency.Matches.Empty"] = "Nog geen vacatures met 60% match of hoger. Vul je locatie, uren, ervaring en de competentietest in; dan wordt deze lijst vanzelf scherper.",
        ["Competency.Matches.Why"] = "Waarom deze match hoog is",
        ["Competency.Matches.Gap"] = "Waar de uitdaging ligt",
        ["Competency.Matches.ExplainTitle"] = "Waarom {0}%?",
        ["Competency.Matches.OpenVacancy"] = "Bekijk vacature",
        ["Competency.Matches.Help"] = "Uitleg over dit matchingpercentage",
        ["Competency.RadarLabel"] = "Overzicht van je vier competenties"
    };

    private static Dictionary<string, string> En() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Competency.Title"] = "Competency test",
        ["Competency.Section"] = "Competencies",
        ["Competency.Lead"] = "Twenty short statements based on the Big Five / OCEAN model. We turn your answers into four work competencies: teamwork, drive for results, stress resilience and innovation.",
        ["Competency.ScienceNote"] = "The items follow a workplace Mini-IPIP (Big Five) style. Some statements are reverse-worded so we get a fair picture — not just socially desirable answers.",
        ["Competency.Empty"] = "You have not taken the test yet. In about five minutes you will see your scores — and which vacancies fit.",
        ["Competency.Start"] = "Start the test",
        ["Competency.Continue"] = "Continue the test",
        ["Competency.Retake"] = "Retake / edit the test",
        ["Competency.DraftProgress"] = "Draft: {0} of {1} questions answered. You can continue later.",
        ["Competency.CompletedOn"] = "Last completed on {0}.",
        ["Competency.SaveDraft"] = "Save for later",
        ["Competency.Complete"] = "Finish and save",
        ["Competency.SavedDraft"] = "Draft saved. You can continue later.",
        ["Competency.SavedComplete"] = "Test saved. Your scores and matches have been updated.",
        ["Competency.Progress"] = "Question {0} of {1}",
        ["Competency.CategoryProgress"] = "{0}: {1} of {2}",
        ["Competency.BackToProfile"] = "Back to profile",
        ["Competency.Likert.1"] = "Strongly disagree",
        ["Competency.Likert.2"] = "Disagree",
        ["Competency.Likert.3"] = "Neutral",
        ["Competency.Likert.4"] = "Agree",
        ["Competency.Likert.5"] = "Strongly agree",
        ["Competency.Cat.Samenwerken"] = "Teamwork",
        ["Competency.Cat.Samenwerken.Hint"] = "How well you work with colleagues and customers (Big Five: agreeableness + extraversion).",
        ["Competency.Cat.Resultaatgerichtheid"] = "Drive for results",
        ["Competency.Cat.Resultaatgerichtheid.Hint"] = "Whether you finish what you promise and work carefully (Big Five: conscientiousness).",
        ["Competency.Cat.Stressbestendigheid"] = "Stress resilience",
        ["Competency.Cat.Stressbestendigheid.Hint"] = "How calm you stay when it is busy or unexpected (Big Five: emotional stability).",
        ["Competency.Cat.Innovatie"] = "Innovation / problem-solving",
        ["Competency.Cat.Innovatie.Hint"] = "Whether you look for new ways when something stalls (Big Five: openness).",
        ["Competency.Q01"] = "I like working with others to finish a task.",
        ["Competency.Q02"] = "I help colleagues even when it is not strictly my job.",
        ["Competency.Q03"] = "I listen to others before I give my own opinion.",
        ["Competency.Q04"] = "I prefer to stay on the sidelines when the team needs to discuss.",
        ["Competency.Q05"] = "I find it hard to take other people’s feelings into account.",
        ["Competency.Q06"] = "I finish what I promise, even when it is tough.",
        ["Competency.Q07"] = "I plan my work so deadlines stay realistic.",
        ["Competency.Q08"] = "I pay attention to details so the work is right first time.",
        ["Competency.Q09"] = "I often put jobs off until the last moment.",
        ["Competency.Q10"] = "I easily leave mess or unfinished work lying around.",
        ["Competency.Q11"] = "I stay calm when it is busy or something goes wrong.",
        ["Competency.Q12"] = "I bounce back quickly after a setback at work.",
        ["Competency.Q13"] = "I can handle several things at once without panicking.",
        ["Competency.Q14"] = "I dwell on mistakes or criticism for a long time.",
        ["Competency.Q15"] = "I get upset quickly when something unexpected happens.",
        ["Competency.Q16"] = "I like thinking of new ways to solve a problem.",
        ["Competency.Q17"] = "I enjoy learning new tasks or systems.",
        ["Competency.Q18"] = "I quickly see connections that others might miss.",
        ["Competency.Q19"] = "I prefer to stick to how we have always done things.",
        ["Competency.Q20"] = "I mostly find new ideas or changes difficult.",
        ["Competency.Matches.Title"] = "Top 10 vacancies",
        ["Competency.Matches.Lead"] = "Only matches of 60% or higher, sorted high to low. The question mark explains in plain language why it fits — and where the gap is.",
        ["Competency.Matches.Empty"] = "No vacancies at 60% match or higher yet. Add your location, hours, experience and the competency test; this list will get sharper.",
        ["Competency.Matches.Why"] = "Why this match is high",
        ["Competency.Matches.Gap"] = "Where the challenge is",
        ["Competency.Matches.ExplainTitle"] = "Why {0}%?",
        ["Competency.Matches.OpenVacancy"] = "View vacancy",
        ["Competency.Matches.Help"] = "Explanation of this match percentage",
        ["Competency.RadarLabel"] = "Overview of your four competencies"
    };
}
