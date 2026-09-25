namespace Jobsy.Web.Localization;

internal static class UiStringsValues
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
        ["ValuesScan.Title"] = "Waarden op werk",
        ["ValuesScan.Lead"] = "Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",

        ["ValuesScan.Cat.Autonomy"] = "Eigen regie & uitdaging",
        ["ValuesScan.Cat.Autonomy.Hint"] = "Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection"] = "Verbinding & zorg",
        ["ValuesScan.Cat.Connection.Hint"] = "Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement"] = "Prestatie & groei",
        ["ValuesScan.Cat.Achievement.Hint"] = "Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability"] = "Zekerheid & traditie",
        ["ValuesScan.Cat.Stability.Hint"] = "Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact"] = "Impact & rechtvaardigheid",
        ["ValuesScan.Cat.Impact.Hint"] = "Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",

        ["ValuesScan.Q01"] = "Ik wil zelf kunnen bepalen hoe ik mijn werk aanpak.",
        ["ValuesScan.Q02"] = "Nieuwe uitdagingen op het werk motiveren mij.",
        ["ValuesScan.Q03"] = "Ik werk het best als iemand precies zegt wat ik moet doen.",
        ["ValuesScan.Q04"] = "Ik zoek graag nieuwe manieren om mijn werk te doen.",
        ["ValuesScan.Q05"] = "Ik voel me prettiger bij vaste routines dan bij veel wisseling.",
        ["ValuesScan.Q06"] = "Ik help collega's graag, ook buiten mijn eigen taken.",
        ["ValuesScan.Q07"] = "Een warme, behulpzame sfeer op het werk is belangrijk voor mij.",
        ["ValuesScan.Q08"] = "Ik houd me liever afzijdig van persoonlijke gesprekken met collega's.",
        ["ValuesScan.Q09"] = "Ik let op hoe het met mensen om me heen gaat.",
        ["ValuesScan.Q10"] = "Teamgevoel vind ik minder belangrijk dan mijn eigen resultaat.",
        ["ValuesScan.Q11"] = "Ik wil zien dat mijn inzet meetbaar verschil maakt.",
        ["ValuesScan.Q12"] = "Ik stel mezelf doelen en werk er actief naar toe.",
        ["ValuesScan.Q13"] = "Ik vind het prima om gemiddeld te presteren zolang het werk af is.",
        ["ValuesScan.Q14"] = "Ik ben trots als ik normen of targets overtreff.",
        ["ValuesScan.Q15"] = "Ik zoek geen extra verantwoordelijkheid voor belangrijke resultaten.",
        ["ValuesScan.Q16"] = "Duidelijke afspraken over uren en taken geven mij rust.",
        ["ValuesScan.Q17"] = "Ik werk graag volgens vaste, bewezen procedures.",
        ["ValuesScan.Q18"] = "Regels en protocollen voelen voor mij vooral belemmerend.",
        ["ValuesScan.Q19"] = "Ik zoek werk met langdurige zekerheid en stabiliteit.",
        ["ValuesScan.Q20"] = "Ik raak onrustig als mijn taken te voorspelbaar worden.",
        ["ValuesScan.Q21"] = "Ik wil dat mijn werk iets goeds doet voor mens of milieu.",
        ["ValuesScan.Q22"] = "Ik kies liever voor een organisatie met eerlijke, duurzame praktijken.",
        ["ValuesScan.Q23"] = "Het maatschappelijke doel van mijn werk is mij niet zo belangrijk.",
        ["ValuesScan.Q24"] = "Ik let op verspilling en probeer die op het werk te verminderen.",
        ["ValuesScan.Q25"] = "Ik focus op mijn eigen taken, niet op bredere maatschappelijke thema's.",

        ["ValuesScan.SavedComplete"] = "Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "Gebaseerd op het Schwartz Value Model",

        ["Deep.ValuesTitle"] = "Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
    };

    private static Dictionary<string, string> En()
    {
        var map = Nl();
        map["ValuesScan.Title"] = "Work values";
        map["ValuesScan.Lead"] = "Twenty-five short statements (about 4 minutes) on what matters to you at work — from autonomy to stability and social impact.";
        map["ValuesScan.ScienceNote"] = "Scientific model: Schwartz Value Model (workplace drivers: autonomy, connection, achievement, stability, impact). Not a diagnosis — a clear starting point for values fit in matching.";
        map["ValuesScan.PrivacyNote"] = "Your answers stay in your account. Employers do not see raw responses. Export and delete via My data. More in the";
        map["ValuesScan.Cat.Autonomy"] = "Autonomy & challenge";
        map["ValuesScan.Cat.Autonomy.Hint"] = "How important is it to choose how you work and try new things?";
        map["ValuesScan.Cat.Connection"] = "Connection & care";
        map["ValuesScan.Cat.Connection.Hint"] = "How important are warm relationships with colleagues, customers, and a helpful atmosphere?";
        map["ValuesScan.Cat.Achievement"] = "Achievement & growth";
        map["ValuesScan.Cat.Achievement.Hint"] = "How strongly do you want visible results, goals, and self-improvement?";
        map["ValuesScan.Cat.Stability"] = "Stability & tradition";
        map["ValuesScan.Cat.Stability.Hint"] = "How important are clear agreements, safety, and predictability?";
        map["ValuesScan.Cat.Impact"] = "Impact & fairness";
        map["ValuesScan.Cat.Impact.Hint"] = "How important is doing good for people and the environment through your work?";
        map["ValuesScan.SavedComplete"] = "Values scan saved. Your drivers count in matching and Who am I.";
        map["ValuesScan.Retake"] = "Retake free test";
        map["ProfileHub.ValuesScience"] = "Based on the Schwartz Value Model";
        map["Deep.ValuesTitle"] = "Values & drivers (deep analysis)";
        map["Deep.ValuesLead"] = "One hundred fifty statements on competencies, interests, culture fit and drivers. Pause anytime — resume later where you left off.";
        return map;
    }
}
