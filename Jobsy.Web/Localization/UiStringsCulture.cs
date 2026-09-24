using Jobsy.Core.Localization;

namespace Jobsy.Web.Localization;

internal static class UiStringsCulture
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
        ["Kompas.TabCulture"] = "Cultuurscan",
        ["Kompas.TabDisc"] = "Cultuurscan",
        ["Kompas.CultureLead"] = "Hoe jij graag werkt: zelfstandig of met kaders, informeel of formeler, samen of alleen — plus hoe jij in een team past.",
        ["Kompas.DiscLead"] = "Hoe jij graag werkt: zelfstandig of met kaders, informeel of formeler, samen of alleen — plus hoe jij in een team past.",
        ["CultureScan.Title"] = "Cultuur & persoonlijkheid",
        ["CultureScan.Lead"] = "Achttien korte stellingen (ca. 3 minuten) over hoe jij graag werkt en hoe jij in een team past. Geen moeilijke termen — wel een helder beeld voor matching.",
        ["CultureScan.ScienceNote"] = "Wetenschappelijk onderbouwd: de vragen meten hoe jij graag werkt (zelfstandig, sfeer, samenwerken, tempo) en hoe jij in een team past. Geen diagnose — wel een helder startpunt voor betere matches.",
        ["CultureScan.RadarCulture"] = "Cultuurvoorkeuren",
        ["CultureScan.RadarCultureLegend"] = "Zelfstandig werken, sfeer, samenwerken en meer — met percentage per as.",
        ["CultureScan.RadarPersonality"] = "Persoonlijkheid op het werk",
        ["CultureScan.RadarPersonalityLegend"] = "Hoe jij omgaat met nieuw, netjes, mensen, sfeer en druk.",
        ["CultureScan.PrivacyNote"] = "Je antwoorden blijven in jouw account. Werkgevers zien geen ruwe antwoorden. Export en wissen via Mijn gegevens. Meer in de",
        ["CultureScan.Empty"] = "Je hebt de cultuurscan nog niet ingevuld. In ongeveer drie minuten zie je hoe jij graag werkt.",
        ["CultureScan.Start"] = "Start de cultuurscan",
        ["CultureScan.Continue"] = "Cultuurscan verder invullen",
        ["CultureScan.Retake"] = "Cultuurscan opnieuw invullen / aanpassen",
        ["CultureScan.SavedComplete"] = "Cultuurscan opgeslagen. Je scores en matches zijn bijgewerkt.",
        ["CultureScan.RadarLabel"] = "Cultuur- en persoonlijkheidsscores",
        ["CultureScan.TrainingTitle"] = "Workshops en cursussen",
        ["CultureScan.EmployerTitle"] = "Bedrijfscultuur",
        ["CultureScan.EmployerLead"] = "Vul twaalf stellingen in over hoe jullie team écht werkt. Dat helpt kandidaten te matchen op sfeer, niet alleen op functietitel.",
        ["CultureScan.EmployerNote"] = "Kies wat bij jullie vestiging past — informeel of formeler, veel zelfstandigheid of duidelijke kaders. Geen jargon.",
        ["CultureScan.EmployerResult"] = "Jullie cultuurprofiel",
        ["CultureScan.EmployerSaved"] = "Bedrijfscultuur opgeslagen.",

        ["CultureScan.Cat.Autonomy"] = "Zelfstandig werken",
        ["CultureScan.Cat.Autonomy.Hint"] = "Hoeveel ruimte wil je om zelf te bepalen hoe je het werk aanpakt?",
        ["CultureScan.Cat.Informal"] = "Informele sfeer",
        ["CultureScan.Cat.Informal.Hint"] = "Liever luchtig en direct, of juist formeler en volgens de hiërarchie?",
        ["CultureScan.Cat.Collaboration"] = "Samenwerken",
        ["CultureScan.Cat.Collaboration.Hint"] = "Werk je het liefst met anderen, of juist zelfstandig aan je eigen stuk?",
        ["CultureScan.Cat.Flexibility"] = "Flexibel meebewegen",
        ["CultureScan.Cat.Flexibility.Hint"] = "Houd je van wisselende taken, of van een vaste planning?",
        ["CultureScan.Cat.Innovation"] = "Nieuwe dingen proberen",
        ["CultureScan.Cat.Innovation.Hint"] = "Zoek je graag nieuwe wegen, of werk je liever met bewezen methodes?",
        ["CultureScan.Cat.PeopleFirst"] = "Mensen voorop",
        ["CultureScan.Cat.PeopleFirst.Hint"] = "Kies je eerder voor de mens of voor het resultaat als het schuurt?",
        ["CultureScan.Cat.Openness"] = "Openstaan voor nieuw",
        ["CultureScan.Cat.Openness.Hint"] = "Hoe graag leer je nieuwe dingen en verken je andere manieren van werken?",
        ["CultureScan.Cat.Conscientiousness"] = "Netjes en betrouwbaar",
        ["CultureScan.Cat.Conscientiousness.Hint"] = "Hoe belangrijk zijn afronden, checken en afspraken nakomen voor jou?",
        ["CultureScan.Cat.Extraversion"] = "Energie van mensen",
        ["CultureScan.Cat.Extraversion.Hint"] = "Krijg je energie van contact, of juist van rustiger doorwerken?",
        ["CultureScan.Cat.Agreeableness"] = "Prettig samen optrekken",
        ["CultureScan.Cat.Agreeableness.Hint"] = "Hoe belangrijk is een warme, behulpzame sfeer voor jou?",
        ["CultureScan.Cat.EmotionalStability"] = "Kalm onder druk",
        ["CultureScan.Cat.EmotionalStability.Hint"] = "Blijf je overzicht houden als het hectisch wordt?",

        ["CultureScan.Cat.Autonomy.MeaningHigh"] = "Jij floreert als je zelf mag bepalen hoe je het werk aanpakt.",
        ["CultureScan.Cat.Autonomy.MeaningMid"] = "Je kunt zelfstandig werken, en kunt nog groeien in meer eigen regie.",
        ["CultureScan.Cat.Autonomy.MeaningLow"] = "Duidelijke kaders helpen je. Vraag om heldere afspraken bij de start.",
        ["CultureScan.Cat.Autonomy.Develop"] = "Oefen één taak per week volledig zelf plannen — met een korte check-in achteraf.",
        ["CultureScan.Cat.Informal.MeaningHigh"] = "Jij voelt je thuis in een luchtige, directe werksfeer.",
        ["CultureScan.Cat.Informal.MeaningMid"] = "Je kunt beide: informeel én formeler, afhankelijk van het team.",
        ["CultureScan.Cat.Informal.MeaningLow"] = "Je houdt van duidelijkheid en een formelere toon. Dat is oké — zoek teams die dat waarderen.",
        ["CultureScan.Cat.Informal.Develop"] = "Probeer één informeel moment per shift: een kort praatje of een open vraag.",
        ["CultureScan.Cat.Collaboration.MeaningHigh"] = "Samenwerken geeft je energie. Ploegenwerk ligt je.",
        ["CultureScan.Cat.Collaboration.MeaningMid"] = "Je kunt samenwerken én alleen doorwerken — beide liggen je redelijk.",
        ["CultureScan.Cat.Collaboration.MeaningLow"] = "Je werkt het sterkst zelfstandig. Kies rollen met een eigen stuk werk.",
        ["CultureScan.Cat.Collaboration.Develop"] = "Nodig één collega uit om een taak samen af te ronden.",
        ["CultureScan.Cat.Flexibility.MeaningHigh"] = "Wisselende taken en snelle bijsturing liggen je goed.",
        ["CultureScan.Cat.Flexibility.MeaningMid"] = "Je kunt meebewegen, en houdt ook van enige structuur.",
        ["CultureScan.Cat.Flexibility.MeaningLow"] = "Een vaste planning helpt je. Zoek werk met voorspelbare ronden.",
        ["CultureScan.Cat.Flexibility.Develop"] = "Oefen één kleine wijziging per dag zonder stress — noteer wat wél werkte.",
        ["CultureScan.Cat.Innovation.MeaningHigh"] = "Jij zoekt graag betere manieren. Verbetertrajecten passen bij je.",
        ["CultureScan.Cat.Innovation.MeaningMid"] = "Je kunt vernieuwen als het nodig is, en kunt ook met de standaard meegaan.",
        ["CultureScan.Cat.Innovation.MeaningLow"] = "Bewezen methodes geven je rust. Dat is een sterkte in stabiele teams.",
        ["CultureScan.Cat.Innovation.Develop"] = "Stel één keer per week één verbeteridee voor — klein is genoeg.",
        ["CultureScan.Cat.PeopleFirst.MeaningHigh"] = "Mensen komen bij jou eerst. Zorg, horeca en begeleiding passen sterk.",
        ["CultureScan.Cat.PeopleFirst.MeaningMid"] = "Je weegt mens én resultaat. Dat helpt in de meeste teams.",
        ["CultureScan.Cat.PeopleFirst.MeaningLow"] = "Resultaat telt zwaar voor jou. Kies teams die dat ook zo doen.",
        ["CultureScan.Cat.PeopleFirst.Develop"] = "Vraag één collega hoe het écht gaat — naast de taaklijst.",
        ["CultureScan.Cat.Openness.MeaningHigh"] = "Nieuw leren geeft je energie. Wisselende taken benutten dat.",
        ["CultureScan.Cat.Openness.MeaningMid"] = "Je kunt nieuw leren, en houdt ook van bekende routes.",
        ["CultureScan.Cat.Openness.MeaningLow"] = "Bekende werkwijzen geven rust. Groei stap voor stap.",
        ["CultureScan.Cat.Openness.Develop"] = "Probeer één nieuwe tool of werkwijze per maand.",
        ["CultureScan.Cat.Conscientiousness.MeaningHigh"] = "Jij rondt netjes af. Kwaliteit en checklists liggen je.",
        ["CultureScan.Cat.Conscientiousness.MeaningMid"] = "Je wilt het goed doen, en kunt nog scherper volgens de lijst werken.",
        ["CultureScan.Cat.Conscientiousness.MeaningLow"] = "Lijsten voelen als oponthoud. Een korte cursus kwaliteit maakt het lichter.",
        ["CultureScan.Cat.Conscientiousness.Develop"] = "Vink twee must-checks af vóór tempo.",
        ["CultureScan.Cat.Extraversion.MeaningHigh"] = "Contact met mensen geeft je energie. Balie en teamwerk passen.",
        ["CultureScan.Cat.Extraversion.MeaningMid"] = "Je kunt contact maken, en hebt ook rust nodig.",
        ["CultureScan.Cat.Extraversion.MeaningLow"] = "Veel praten vermoeit. Kies rollen met rustiger doorwerken.",
        ["CultureScan.Cat.Extraversion.Develop"] = "Oefen een kort, warm welkom — één zin is genoeg.",
        ["CultureScan.Cat.Agreeableness.MeaningHigh"] = "Jij houdt de sfeer prettig. Samen optrekken ligt je.",
        ["CultureScan.Cat.Agreeableness.MeaningMid"] = "Je kunt meedenken én je grens aangeven.",
        ["CultureScan.Cat.Agreeableness.MeaningLow"] = "Je bent direct. Dat helpt in teams die helderheid waarderen.",
        ["CultureScan.Cat.Agreeableness.Develop"] = "Oefen één compliment per shift — concreet en kort.",
        ["CultureScan.Cat.EmotionalStability.MeaningHigh"] = "Jij blijft kalm als het druk wordt. Piekdagen liggen je.",
        ["CultureScan.Cat.EmotionalStability.MeaningMid"] = "Je houdt meestal overzicht, en kunt nog groeien onder piekdruk.",
        ["CultureScan.Cat.EmotionalStability.MeaningLow"] = "Hoge druk kost energie. Zoek tempo dat bij je past, of oefen adempauzes.",
        ["CultureScan.Cat.EmotionalStability.Develop"] = "Neem bij piekdruk één adempauze van 30 seconden vóór je reageert.",

        ["CultureScan.Q01"] = "Ik werk het liefst zelfstandig, zonder dat iemand steeds meekijkt.",
        ["CultureScan.Q02"] = "Ik wil duidelijke instructies en vaste kaders voordat ik begin.",
        ["CultureScan.Q03"] = "Op het werk mag het luchtig en informeel zijn.",
        ["CultureScan.Q04"] = "Ik houd van een formelere toon en duidelijke hiërarchie.",
        ["CultureScan.Q05"] = "Ik werk het liefst samen met collega’s aan één doel.",
        ["CultureScan.Q06"] = "Ik presteer beter als ik mijn eigen stuk alleen kan afmaken.",
        ["CultureScan.Q07"] = "Ik vind het prima als de planning tussendoor verandert.",
        ["CultureScan.Q08"] = "Ik werk het best met een vaste, voorspelbare planning.",
        ["CultureScan.Q09"] = "Ik zoek graag nieuwe manieren om het werk beter te doen.",
        ["CultureScan.Q10"] = "Ik werk het liefst met bewezen methodes die al werken.",
        ["CultureScan.Q11"] = "Als het schuurt, kies ik eerder voor de mens dan voor het harde resultaat.",
        ["CultureScan.Q12"] = "Resultaat komt eerst — ook als dat even lastig is voor de sfeer.",
        ["CultureScan.Q13"] = "Ik leer graag iets nieuws, ook als het even onwennig voelt.",
        ["CultureScan.Q14"] = "Ik rond taken netjes af en check of alles klopt.",
        ["CultureScan.Q15"] = "Ik krijg energie van contact met klanten of collega’s.",
        ["CultureScan.Q16"] = "Ik help graag mee zodat iedereen prettig kan werken.",
        ["CultureScan.Q17"] = "Als het hectisch wordt, blijf ik redelijk kalm.",
        ["CultureScan.Q18"] = "Ik blijf liever bij bekende werkwijzen dan iets nieuws uitproberen.",

        // Legacy Disc.* keys redirect to culture copy so old UI/tests don't show jargon.
        ["Disc.Title"] = "Cultuur & persoonlijkheid",
        ["Disc.Lead"] = "Achttien korte stellingen over hoe jij graag werkt. Geen DISC-letters — wel een helder startpunt.",
        ["Disc.Empty"] = "Je hebt de cultuurscan nog niet ingevuld.",
        ["Disc.Start"] = "Start de cultuurscan",
        ["Disc.Continue"] = "Cultuurscan verder invullen",
        ["Disc.Retake"] = "Cultuurscan opnieuw invullen / aanpassen",
        ["Disc.SavedComplete"] = "Cultuurscan opgeslagen.",
        ["Disc.RadarLabel"] = "Verdeling van je cultuurvoorkeuren",
        ["Disc.TrainingTitle"] = "Workshops en cursussen",
        ["Deep.DiscTitle"] = "Cultuurscan",
    };

    private static Dictionary<string, string> En()
    {
        var map = Nl();
        map["Kompas.TabCulture"] = "Culture scan";
        map["Kompas.TabDisc"] = "Culture scan";
        map["Kompas.CultureLead"] = "How you like to work: independently or with structure, informal or more formal, together or alone — plus how you fit a team.";
        map["Kompas.DiscLead"] = map["Kompas.CultureLead"];
        map["CultureScan.Title"] = "Culture & personality";
        map["CultureScan.Lead"] = "Eighteen short statements (about 3 minutes) on how you like to work and how you fit a team. Everyday language — for better matches.";
        map["CultureScan.ScienceNote"] = "Science-based: the questions measure how you like to work (autonomy, atmosphere, collaboration, pace) and how you fit a team. Not a diagnosis — a clear starting point for better matches.";
        map["CultureScan.RadarCulture"] = "Culture preferences";
        map["CultureScan.RadarCultureLegend"] = "Autonomy, atmosphere, collaboration and more — with a percentage per axis.";
        map["CultureScan.RadarPersonality"] = "Personality at work";
        map["CultureScan.RadarPersonalityLegend"] = "How you handle novelty, reliability, people, atmosphere and pressure.";
        map["CultureScan.Empty"] = "You have not completed the culture scan yet.";
        map["CultureScan.Start"] = "Start the culture scan";
        map["CultureScan.Continue"] = "Continue culture scan";
        map["CultureScan.Retake"] = "Retake / edit culture scan";
        map["CultureScan.SavedComplete"] = "Culture scan saved. Your scores and matches were updated.";
        map["CultureScan.RadarLabel"] = "Culture and personality scores";
        map["CultureScan.TrainingTitle"] = "Workshops and courses";
        map["CultureScan.EmployerTitle"] = "Company culture";
        map["CultureScan.EmployerLead"] = "Answer twelve statements about how your team really works. That helps match candidates on atmosphere, not only job title.";
        map["CultureScan.EmployerNote"] = "Pick what fits your branch — informal or more formal, lots of autonomy or clear structure. No jargon.";
        map["CultureScan.EmployerResult"] = "Your culture profile";
        map["CultureScan.EmployerSaved"] = "Company culture saved.";
        map["Disc.Title"] = "Culture & personality";
        map["Disc.Lead"] = "Eighteen short statements on how you like to work. No DISC letters — a clear starting point.";
        map["Disc.Empty"] = "You have not completed the culture scan yet.";
        map["Disc.Start"] = "Start the culture scan";
        map["Disc.Continue"] = "Continue culture scan";
        map["Disc.Retake"] = "Retake / edit culture scan";
        map["Disc.SavedComplete"] = "Culture scan saved.";
        map["Disc.RadarLabel"] = "Your culture preferences";
        map["Disc.TrainingTitle"] = "Workshops and courses";
        map["Deep.DiscTitle"] = "Culture scan";
        return map;
    }
}
