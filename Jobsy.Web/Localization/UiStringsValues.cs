using Jobsy.Core.Rules;

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
        Merge(pl, Pl());
        Merge(ro, Ro());
        Merge(ar, Ar());
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

        ["ValuesScan.Cat.Autonomy"] = DimensionLabels.For(SchwartzValuesCatalog.Autonomy),
        ["ValuesScan.Cat.Autonomy.Hint"] = "Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection"] = DimensionLabels.For(SchwartzValuesCatalog.Connection),
        ["ValuesScan.Cat.Connection.Hint"] = "Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement"] = DimensionLabels.For(SchwartzValuesCatalog.Achievement),
        ["ValuesScan.Cat.Achievement.Hint"] = "Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability"] = DimensionLabels.For(SchwartzValuesCatalog.Stability),
        ["ValuesScan.Cat.Stability.Hint"] = "Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact"] = DimensionLabels.For(SchwartzValuesCatalog.Impact),
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

        ["Kompas.TabValues"] = "Waarden",
        ["Kompas.ValuesLead"] = "Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "Diepteanalyse waarden afgerond",
        ["Kompas.ValuesDeepReady"] = "Diepteanalyse waarden ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "Diepteanalyse (150 vragen) nog niet ontgrendeld",
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
        map["Kompas.TabValues"] = "Values";
        map["Kompas.ValuesLead"] = "What matters to you at work: autonomy, connection, achievement, stability and impact.";
        map["Kompas.ValuesDeepDone"] = "Values deep analysis completed";
        map["Kompas.ValuesDeepReady"] = "Values deep analysis unlocked — continue where you left off";
        map["Kompas.ValuesDeepLocked"] = "Deep analysis (150 questions) not unlocked yet";
        map["ValuesScan.Q01"] = "I want to decide myself how I approach my work.";
        map["ValuesScan.Q02"] = "New challenges at work motivate me.";
        map["ValuesScan.Q03"] = "I work best when someone tells me exactly what to do.";
        map["ValuesScan.Q04"] = "I like looking for new ways to do my work.";
        map["ValuesScan.Q05"] = "I feel more comfortable with fixed routines than with lots of change.";
        map["ValuesScan.Q06"] = "I gladly help colleagues, even outside my own tasks.";
        map["ValuesScan.Q07"] = "A warm, helpful atmosphere at work matters to me.";
        map["ValuesScan.Q08"] = "I prefer to stay out of personal talks with colleagues.";
        map["ValuesScan.Q09"] = "I notice how the people around me are doing.";
        map["ValuesScan.Q10"] = "Team spirit matters less to me than my own result.";
        map["ValuesScan.Q11"] = "I want to see that my effort makes a measurable difference.";
        map["ValuesScan.Q12"] = "I set goals for myself and work toward them actively.";
        map["ValuesScan.Q13"] = "I am fine with average performance as long as the work is done.";
        map["ValuesScan.Q14"] = "I am proud when I beat norms or targets.";
        map["ValuesScan.Q15"] = "I do not seek extra responsibility for important results.";
        map["ValuesScan.Q16"] = "Clear agreements about hours and tasks give me calm.";
        map["ValuesScan.Q17"] = "I like working with fixed, proven procedures.";
        map["ValuesScan.Q18"] = "Rules and protocols mostly feel limiting to me.";
        map["ValuesScan.Q19"] = "I look for work with long-term certainty and stability.";
        map["ValuesScan.Q20"] = "I get restless when my tasks become too predictable.";
        map["ValuesScan.Q21"] = "I want my work to do something good for people or the environment.";
        map["ValuesScan.Q22"] = "I prefer an organisation with fair, sustainable practices.";
        map["ValuesScan.Q23"] = "The social purpose of my work is not so important to me.";
        map["ValuesScan.Q24"] = "I watch for waste and try to reduce it at work.";
        map["ValuesScan.Q25"] = "I focus on my own tasks, not on broader social themes.";
        return map;
    }

    private static Dictionary<string, string> Pl() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ValuesScan.Cat.Autonomy"] = "Autonomia i wyzwanie",
        ["ValuesScan.Cat.Connection"] = "Więź i troska",
        ["ValuesScan.Cat.Achievement"] = "Osiągnięcia i rozwój",
        ["ValuesScan.Cat.Stability"] = "Stabilność i tradycja",
        ["ValuesScan.Cat.Impact"] = "Wpływ i sprawiedliwość",
        ["ValuesScan.Title"] = "PL: Waarden op werk",
        ["ValuesScan.Lead"] = "PL: Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "PL: Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "PL: Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["ValuesScan.Cat.Autonomy.Hint"] = "PL: Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection.Hint"] = "PL: Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement.Hint"] = "PL: Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability.Hint"] = "PL: Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact.Hint"] = "PL: Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",
        ["ValuesScan.Q01"] = "Chcę to decide myself how I approach my work.",
        ["ValuesScan.Q02"] = "New challenges at work motivate me.",
        ["ValuesScan.Q03"] = "Pracuję best when someone tells me exactly what to do.",
        ["ValuesScan.Q04"] = "Lubię looking for new ways to do my work.",
        ["ValuesScan.Q05"] = "Czuję more comfortable with fixed routines than with lots of change.",
        ["ValuesScan.Q06"] = "Chętnie help colleagues, even outside my own tasks.",
        ["ValuesScan.Q07"] = "A warm, helpful atmosphere at work matters to me.",
        ["ValuesScan.Q08"] = "Wolę to stay out of personal talks with colleagues.",
        ["ValuesScan.Q09"] = "Zauważam how the people around me are doing.",
        ["ValuesScan.Q10"] = "Team spirit matters less to me than my own result.",
        ["ValuesScan.Q11"] = "Chcę to see that my effort makes a measurable difference.",
        ["ValuesScan.Q12"] = "Ustawiam goals for myself and work toward them actively.",
        ["ValuesScan.Q13"] = "Jestem fine with average performance as long as the work is done.",
        ["ValuesScan.Q14"] = "Jestem proud when I beat norms or targets.",
        ["ValuesScan.Q15"] = "Nie seek extra responsibility for important results.",
        ["ValuesScan.Q16"] = "Clear agreements about hours and tasks give me calm.",
        ["ValuesScan.Q17"] = "Lubię working with fixed, proven procedures.",
        ["ValuesScan.Q18"] = "Rules and protocols mostly feel limiting to me.",
        ["ValuesScan.Q19"] = "Szukam for work with long-term certainty and stability.",
        ["ValuesScan.Q20"] = "Dostaję restless when my tasks become too predictable.",
        ["ValuesScan.Q21"] = "Chcę my work to do something good for people or the environment.",
        ["ValuesScan.Q22"] = "Wolę an organisation with fair, sustainable practices.",
        ["ValuesScan.Q23"] = "The social purpose of my work is not so important to me.",
        ["ValuesScan.Q24"] = "I watch for waste and try to reduce it at work.",
        ["ValuesScan.Q25"] = "Skupiam się on my own tasks, not on broader social themes.",
        ["ValuesScan.SavedComplete"] = "PL: Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "PL: Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "PL: Gebaseerd op het Schwartz Value Model",
        ["Deep.ValuesTitle"] = "PL: Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "PL: Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
        ["Kompas.TabValues"] = "PL: Waarden",
        ["Kompas.ValuesLead"] = "PL: Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "PL: Diepteanalyse waarden afgerond",
        ["Kompas.ValuesDeepReady"] = "PL: Diepteanalyse waarden ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "PL: Diepteanalyse (150 vragen) nog niet ontgrendeld",
    };

    private static Dictionary<string, string> Ro() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ValuesScan.Cat.Autonomy"] = "Autonomie și provocare",
        ["ValuesScan.Cat.Connection"] = "Conexiune și grijă",
        ["ValuesScan.Cat.Achievement"] = "Realizare și creștere",
        ["ValuesScan.Cat.Stability"] = "Stabilitate și tradiție",
        ["ValuesScan.Cat.Impact"] = "Impact și corectitudine",
        ["ValuesScan.Title"] = "RO: Waarden op werk",
        ["ValuesScan.Lead"] = "RO: Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "RO: Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "RO: Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["ValuesScan.Cat.Autonomy.Hint"] = "RO: Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection.Hint"] = "RO: Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement.Hint"] = "RO: Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability.Hint"] = "RO: Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact.Hint"] = "RO: Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",
        ["ValuesScan.Q01"] = "Vreau to decide myself how I approach my work.",
        ["ValuesScan.Q02"] = "New challenges at work motivate me.",
        ["ValuesScan.Q03"] = "Lucrez best when someone tells me exactly what to do.",
        ["ValuesScan.Q04"] = "Îmi place looking for new ways to do my work.",
        ["ValuesScan.Q05"] = "Simt more comfortable with fixed routines than with lots of change.",
        ["ValuesScan.Q06"] = "Cu plăcere help colleagues, even outside my own tasks.",
        ["ValuesScan.Q07"] = "A warm, helpful atmosphere at work matters to me.",
        ["ValuesScan.Q08"] = "Prefer to stay out of personal talks with colleagues.",
        ["ValuesScan.Q09"] = "Observ how the people around me are doing.",
        ["ValuesScan.Q10"] = "Team spirit matters less to me than my own result.",
        ["ValuesScan.Q11"] = "Vreau to see that my effort makes a measurable difference.",
        ["ValuesScan.Q12"] = "Îmi stabilesc goals for myself and work toward them actively.",
        ["ValuesScan.Q13"] = "Sunt fine with average performance as long as the work is done.",
        ["ValuesScan.Q14"] = "Sunt proud when I beat norms or targets.",
        ["ValuesScan.Q15"] = "Nu seek extra responsibility for important results.",
        ["ValuesScan.Q16"] = "Clear agreements about hours and tasks give me calm.",
        ["ValuesScan.Q17"] = "Îmi place working with fixed, proven procedures.",
        ["ValuesScan.Q18"] = "Rules and protocols mostly feel limiting to me.",
        ["ValuesScan.Q19"] = "Caut for work with long-term certainty and stability.",
        ["ValuesScan.Q20"] = "Devin restless when my tasks become too predictable.",
        ["ValuesScan.Q21"] = "Vreau my work to do something good for people or the environment.",
        ["ValuesScan.Q22"] = "Prefer an organisation with fair, sustainable practices.",
        ["ValuesScan.Q23"] = "The social purpose of my work is not so important to me.",
        ["ValuesScan.Q24"] = "I watch for waste and try to reduce it at work.",
        ["ValuesScan.Q25"] = "Mă concentrez on my own tasks, not on broader social themes.",
        ["ValuesScan.SavedComplete"] = "RO: Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "RO: Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "RO: Gebaseerd op het Schwartz Value Model",
        ["Deep.ValuesTitle"] = "RO: Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "RO: Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
        ["Kompas.TabValues"] = "RO: Waarden",
        ["Kompas.ValuesLead"] = "RO: Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "RO: Diepteanalyse waarden afgerond",
        ["Kompas.ValuesDeepReady"] = "RO: Diepteanalyse waarden ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "RO: Diepteanalyse (150 vragen) nog niet ontgrendeld",
    };

    private static Dictionary<string, string> Ar() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ValuesScan.Cat.Autonomy"] = "الاستقلالية والتحدي",
        ["ValuesScan.Cat.Connection"] = "الترابط والرعاية",
        ["ValuesScan.Cat.Achievement"] = "الإنجاز والنمو",
        ["ValuesScan.Cat.Stability"] = "الاستقرار والتقاليد",
        ["ValuesScan.Cat.Impact"] = "الأثر والعدالة",
        ["ValuesScan.Title"] = "‏Waarden op werk",
        ["ValuesScan.Lead"] = "‏Vijfentwintig korte stellingen (ca. 4 minuten) over wat jij belangrijk vindt op werk — van eigen regie tot zekerheid en maatschappelijke bijdrage.",
        ["ValuesScan.ScienceNote"] = "‏Wetenschappelijk model: Schwartz Value Model (werkplek-drivers: autonomie, verbinding, prestatie, stabiliteit, impact). Geen diagnose — wel een helder startpunt voor waardenfit in matching.",
        ["ValuesScan.PrivacyNote"] = "‏Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["ValuesScan.Cat.Autonomy.Hint"] = "‏Hoe belangrijk is het om zelf te bepalen hoe je werkt en nieuwe dingen te proberen?",
        ["ValuesScan.Cat.Connection.Hint"] = "‏Hoe belangrijk zijn warme relaties met collega's, klanten en een behulpzame sfeer?",
        ["ValuesScan.Cat.Achievement.Hint"] = "‏Hoe sterk wil je resultaat zien, doelen halen en jezelf verbeteren?",
        ["ValuesScan.Cat.Stability.Hint"] = "‏Hoe belangrijk zijn vaste afspraken, veiligheid en voorspelbaarheid?",
        ["ValuesScan.Cat.Impact.Hint"] = "‏Hoe belangrijk is het om via je werk iets goed te doen voor mens en omgeving?",
        ["ValuesScan.Q01"] = "أريد to decide myself how I approach my work.",
        ["ValuesScan.Q02"] = "‏New challenges at work motivate me.",
        ["ValuesScan.Q03"] = "أعمل best when someone tells me exactly what to do.",
        ["ValuesScan.Q04"] = "أحب looking for new ways to do my work.",
        ["ValuesScan.Q05"] = "أشعر more comfortable with fixed routines than with lots of change.",
        ["ValuesScan.Q06"] = "بسرور help colleagues, even outside my own tasks.",
        ["ValuesScan.Q07"] = "‏A warm, helpful atmosphere at work matters to me.",
        ["ValuesScan.Q08"] = "أفضل to stay out of personal talks with colleagues.",
        ["ValuesScan.Q09"] = "ألاحظ how the people around me are doing.",
        ["ValuesScan.Q10"] = "‏Team spirit matters less to me than my own result.",
        ["ValuesScan.Q11"] = "أريد to see that my effort makes a measurable difference.",
        ["ValuesScan.Q12"] = "أضع goals for myself and work toward them actively.",
        ["ValuesScan.Q13"] = "أنا fine with average performance as long as the work is done.",
        ["ValuesScan.Q14"] = "أنا proud when I beat norms or targets.",
        ["ValuesScan.Q15"] = "لا seek extra responsibility for important results.",
        ["ValuesScan.Q16"] = "‏Clear agreements about hours and tasks give me calm.",
        ["ValuesScan.Q17"] = "أحب working with fixed, proven procedures.",
        ["ValuesScan.Q18"] = "‏Rules and protocols mostly feel limiting to me.",
        ["ValuesScan.Q19"] = "أبحث for work with long-term certainty and stability.",
        ["ValuesScan.Q20"] = "أصبح restless when my tasks become too predictable.",
        ["ValuesScan.Q21"] = "أريد my work to do something good for people or the environment.",
        ["ValuesScan.Q22"] = "أفضل an organisation with fair, sustainable practices.",
        ["ValuesScan.Q23"] = "‏The social purpose of my work is not so important to me.",
        ["ValuesScan.Q24"] = "‏I watch for waste and try to reduce it at work.",
        ["ValuesScan.Q25"] = "أركّز on my own tasks, not on broader social themes.",
        ["ValuesScan.SavedComplete"] = "‏Waardenscan opgeslagen. Je drijfveren wegen mee in matching en Wie ben ik.",
        ["ValuesScan.Retake"] = "‏Herhaal gratis test",
        ["ProfileHub.ValuesScience"] = "‏Gebaseerd op het Schwartz Value Model",
        ["Deep.ValuesTitle"] = "‏Waarden & drijfveren (diepteanalyse)",
        ["Deep.ValuesLead"] = "‏Honderdvijftig stellingen over competenties, interesses, cultuurfit en drijfveren. Pauzeren mag — je hervat later waar je was.",
        ["Kompas.TabValues"] = "‏Waarden",
        ["Kompas.ValuesLead"] = "‏Wat jij belangrijk vindt op werk: eigen regie, verbinding, prestatie, zekerheid en impact.",
        ["Kompas.ValuesDeepDone"] = "‏Diepteanalyse waarden afgerond",
        ["Kompas.ValuesDeepReady"] = "‏Diepteanalyse waarden ontgrendeld — ga verder waar je was",
        ["Kompas.ValuesDeepLocked"] = "‏Diepteanalyse (150 vragen) nog niet ontgrendeld",
    };
}
