namespace Jobsy.Web.Localization;

/// <summary>Wizard v2 additions kept separate from the long base catalog.</summary>
internal static class UiStringsOnboardingV2
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        Add(nl, "nl", "Maak je startprofiel af", "Nog {0} stappen, daarna staat je Kompas klaar.", "Verder waar je was");
        Add(en, "en", "Finish your start profile", "{0} steps left, then your Compass is ready.", "Continue where you left off");
        Add(pl, "pl", "Dokończ profil startowy", "Zostało {0} kroków, potem Twój Kompas będzie gotowy.", "Kontynuuj od miejsca przerwania");
        Add(ro, "ro", "Completează-ți profilul de start", "Mai sunt {0} pași, apoi Busola ta este gata.", "Continuă de unde ai rămas");
        Add(ar, "ar", "أكمل ملفك التعريفي", "تبقّى {0} خطوات ثم تصبح بوصلتك جاهزة.", "تابع من حيث توقفت");
    }

    private static void Add(Dictionary<string, string> map, string language, string resumeTitle, string resumeLead, string resumeCta)
    {
        map["Onboarding.Resume.Title"] = resumeTitle;
        map["Onboarding.Resume.Lead"] = resumeLead;
        map["Onboarding.Resume.Cta"] = resumeCta;
        var english = language != "nl";
        map["Onboarding.Step1.Title"] = english ? "Let’s get acquainted" : "Even kennismaken";
        map["Onboarding.Step1.Lead"] = english ? "Is this correct? We copied your name from your account." : "Klopt dit? We hebben je naam overgenomen van je account.";
        map["Onboarding.Step2.Title"] = english ? "When can you work?" : "Wanneer kun je werken?";
        map["Onboarding.Step2.Lead"] = english ? "Choose what fits you. You can select more than one." : "Kies wat bij je past, meerdere mag. We vullen de details voor je in.";
        map["Onboarding.Step3.Title"] = english ? "How do you get to work?" : "Hoe kom je op je werk?";
        map["Onboarding.Step3.Lead"] = english ? "We only show jobs that are easy to reach." : "Dan laten we alleen banen zien die goed te bereiken zijn.";
        map["Onboarding.Step4.Title"] = english ? "Have you worked before?" : "Heb je al gewerkt?";
        map["Onboarding.Step4.Lead"] = english ? "A side job, volunteering or internship counts too." : "Bijbaan, vrijwilligerswerk of stage telt ook mee.";
        map["Onboarding.Step5.Title"] = english ? "What is your education?" : "Wat is je opleiding?";
        map["Onboarding.Step5.Lead"] = english ? "What you are studying now, or your highest completed level." : "Waar je nu mee bezig bent, of je hoogst afgeronde.";
        map["Onboarding.Step6.Title"] = english ? "What is your dream job?" : "Wat is je droombaan?";
        map["Onboarding.Step6.Lead"] = english ? "Think big. We make a route towards it in Career." : "Denk groot. We maken er in Carrière een route naartoe.";
        map["Onboarding.Step7.Title"] = english ? "How do you work?" : "Hoe werk jij?";
        map["Onboarding.Step8.Title"] = english ? "What do you like?" : "Wat vind je leuk?";
        map["Onboarding.Step9.Title"] = english ? "Where do you feel at home?" : "Waar voel je je thuis?";
        map["Onboarding.Step10.Title"] = english ? "What matters to you?" : "Wat vind je belangrijk?";
        map["Onboarding.Optional"] = english ? "Optional" : "Optioneel";
        map["Onboarding.BirthDate"] = english ? "Date of birth" : "Geboortedatum";
        map["Onboarding.Day"] = english ? "Day" : "Dag"; map["Onboarding.Month"] = english ? "Month" : "Maand"; map["Onboarding.Year"] = english ? "Year" : "Jaar";
        map["Onboarding.BirthHint"] = english ? "This only helps us show jobs that fit your age." : "Zo tonen we alleen banen die bij je leeftijd mogen";
        map["Onboarding.PostcodeResolved"] = english ? "{0} · only used for travel time; employers never see your address" : "{0} · alleen voor reistijd, werkgevers zien je adres niet";
        map["Onboarding.PostcodeNotFound"] = english ? "We cannot find this postcode." : "We vinden deze postcode niet";
        map["Onboarding.EnterCity"] = english ? "Enter your city" : "Vul je woonplaats in";
        map["Onboarding.MinHours"] = english ? "Minimum hours" : "Minimum uren"; map["Onboarding.MaxHours"] = english ? "Maximum hours" : "Maximum uren"; map["Onboarding.Hours"] = english ? "hours" : "uur";
        map["Onboarding.DayPartsCount"] = english ? "{0} moments selected" : "{0} momenten gekozen";
        map["Onboarding.AvailabilitySummary"] = english ? "This is how we filled it in" : "Zo hebben we het ingevuld";
        map["Onboarding.Adjust"] = english ? "Adjust" : "Aanpassen";
        map["Onboarding.ManualOverride"] = english ? "You adjusted this yourself." : "Je hebt het zelf aangepast";
        map["Onboarding.Refill"] = english ? "Fill in again" : "Opnieuw invullen";
        map["Onboarding.AvailabilityRequired"] = english ? "Choose at least one moment when you can work." : "Kies minstens één moment waarop je kunt";
        map["Onboarding.Transport"] = english ? "Transport" : "Vervoer"; map["Onboarding.MaxTravel"] = english ? "Maximum travel" : "Maximaal reizen"; map["Onboarding.Minutes"] = english ? "min" : "min"; map["Onboarding.Other"] = english ? "Other" : "Anders";
        map["Onboarding.DeleteJob"] = english ? "Delete {0}" : "Verwijder {0}";
        map["Onboarding.DreamSearch"] = english ? "Search or type your dream job" : "Zoek of typ je droombaan";
        map["Onboarding.DreamOwn"] = english ? "Use “{0}” as my own dream job" : "Gebruik “{0}” als eigen droombaan";
        map["Onboarding.Consent.Title"] = english ? "Now a little about you" : "Nu even over jou";
        map["Onboarding.Consent.Lead"] = english ? "Four short tests. There are no wrong answers: choose what comes to mind first." : "4 korte tests met een paar vragen per test. Er zijn geen foute antwoorden: kies wat het eerst in je opkomt.";
        map["Onboarding.Consent.Check"] = english ? "I consent to analysing my answers for my Compass." : "Ik geef toestemming om mijn antwoorden te analyseren voor mijn Kompas.";
        map["Onboarding.Consent.More"] = english ? "More explanation" : "Meer uitleg"; map["Onboarding.Consent.MoreText"] = english ? "Your answers are only used for your personal Compass." : "Je antwoorden worden alleen gebruikt voor jouw persoonlijke Kompas.";
        map["Onboarding.Consent.Private"] = english ? "Employers cannot see your answers." : "Werkgevers zien je antwoorden niet.";
        map["Onboarding.Consent.Cta"] = english ? "Start the first test" : "Start de eerste test"; map["Onboarding.Consent.Failed"] = english ? "Consent could not be saved. Please try again." : "Toestemming opslaan lukt niet. Probeer het opnieuw.";
        map["Onboarding.Test.Lead"] = english ? "How well does each sentence fit you?" : "Hoe goed past elke zin bij jou?";
        map["Onboarding.Test.Competency"] = english ? "How you work" : "Hoe je werkt"; map["Onboarding.Test.Competency.Lead"] = english ? "Competencies such as collaborating" : "Competenties, zoals samenwerken";
        map["Onboarding.Test.Career"] = english ? "Fun" : "Leuk"; map["Onboarding.Test.Career.Lead"] = english ? "Careers that fit you" : "Beroepen die bij je passen";
        map["Onboarding.Test.Culture"] = english ? "Atmosphere" : "Sfeer"; map["Onboarding.Test.Culture.Lead"] = english ? "Work culture" : "Waar je je thuis voelt";
        map["Onboarding.Test.Values"] = english ? "Values" : "Waarden"; map["Onboarding.Test.Values.Lead"] = english ? "What matters to you" : "Wat je belangrijk vindt";
        map["Onboarding.Likert.OfFive"] = english ? "of 5" : "van 5"; map["Onboarding.Likert.Low"] = english ? "Does not fit" : "Past niet"; map["Onboarding.Likert.High"] = english ? "Fits very well" : "Past heel goed";
        map["Onboarding.NextTest"] = english ? "Next test" : "Volgende test"; map["Onboarding.Done"] = english ? "Done" : "Klaar";
        map["Onboarding.NotSaved"] = english ? "Not saved · Retry" : "Niet bewaard · Opnieuw"; map["Onboarding.AllSaved"] = english ? "Everything is saved" : "Alles is bewaard";
        map["Onboarding.And"] = english ? "and" : "en";
        map["Onboarding.Finish.Badge"] = english ? "First impression" : "Eerste indruk"; map["Onboarding.Finish.Title"] = english ? "Your Compass is ready, {0}" : "Je Kompas staat klaar, {0}";
        map["Onboarding.Finish.Lead"] = english ? "This already stands out:" : "Dit valt ons nu al op:"; map["Onboarding.Finish.Strength"] = english ? "Your strongest point" : "Je sterkste punt"; map["Onboarding.Finish.Work"] = english ? "Work that fits you" : "Werk dat bij je past"; map["Onboarding.Finish.Value"] = english ? "Important to you" : "Belangrijk voor jou";
        map["Onboarding.Finish.Matches"] = english ? "jobs already fit you" : "vacatures passen al bij je"; map["Onboarding.Finish.Cta"] = english ? "Go to my Compass" : "Naar mijn Kompas"; map["Onboarding.Finish.FullTests"] = english ? "Take full tests for more precision" : "Volledige tests doen voor meer precisie";
        map["Onboarding.Welcome.Title"] = english ? "Hi {0}, nice to have you here" : "Hoi {0}, fijn dat je er bent"; map["Onboarding.Welcome.Lead"] = english ? "In about 6 minutes we make your start profile together. Then your Compass shows who you are and which work fits you." : "In ± 6 minuten maken we samen je startprofiel. Daarna zie je in je Kompas wie je bent en welk werk bij je past.";
        map["Onboarding.Welcome.Hint"] = english ? "Everything is saved right away. You can stop and continue later." : "Alles wordt meteen bewaard. Stoppen mag, je gaat later verder waar je was."; map["Onboarding.Welcome.Cta"] = english ? "Start" : "Beginnen";
        map["Onboarding.Welcome.OverJou"] = english ? "Where you live and when you can work" : "Waar je woont en wanneer je kunt"; map["Onboarding.Welcome.Background"] = english ? "Your background" : "Je achtergrond"; map["Onboarding.Welcome.BackgroundLead"] = english ? "Work and education, you can skip it" : "Werk en opleiding, mag je overslaan"; map["Onboarding.Welcome.Dream"] = english ? "Your dream job" : "Je droombaan"; map["Onboarding.Welcome.DreamLead"] = english ? "Where do you want to go?" : "Waar wil je naartoe?"; map["Onboarding.Welcome.TestsLead"] = english ? "4 short tests" : "4 korte tests"; map["Onboarding.Welcome.Time2"] = "2 min"; map["Onboarding.Welcome.Time1"] = "1 min"; map["Onboarding.Welcome.TimeHalf"] = "½ min";
        map["Onboarding.Phase.OverJou"] = english ? "About you" : "Over jou"; map["Onboarding.Phase.Achtergrond"] = english ? "Background" : "Achtergrond"; map["Onboarding.Phase.Droom"] = english ? "Dream" : "Droom"; map["Onboarding.Phase.WieBenJij"] = english ? "Who are you" : "Wie ben jij";
        map["Onboarding.Side.OverJou"] = english ? "3 steps" : "3 stappen"; map["Onboarding.Side.Achtergrond"] = english ? "optional" : "optioneel"; map["Onboarding.Side.Droom"] = english ? "1 step" : "1 stap"; map["Onboarding.Side.WieBenJij"] = english ? "4 short tests" : "4 korte tests"; map["Onboarding.Side.Kompas"] = english ? "Your Compass" : "Jouw Kompas"; map["Onboarding.Side.KompasLead"] = english ? "ready soon" : "bijna klaar";
        AddPresets(map, english);
        AddDreamJobs(map);
    }

    private static void AddPresets(Dictionary<string, string> map, bool english)
    {
        var values = new[] { ("Direct","Per direct","Ik kan meteen beginnen","Immediately","I can start right away"), ("School","Bijbaan naast school","Na school en in het weekend","Job alongside school","After school and weekends"), ("Weekend","Weekenden","Za en zo","Weekends","Saturday and Sunday"), ("Evening","Avonden","Ma–vr na 18:00","Evenings","Weekdays after 18:00"), ("Office","Kantoordagen","Ma–vr, 9–17 uur","Office days","Weekdays, 9–17"), ("Holiday","Vakantiewerk","In de schoolvakanties","Holiday work","During school holidays"), ("Parttime","Parttime","12–32 uur per week","Part-time","12–32 hours a week"), ("Fulltime","Fulltime","36–40 uur per week","Full-time","36–40 hours a week") };
        foreach (var item in values) { map[$"Onboarding.Preset.{item.Item1}"] = english ? item.Item4 : item.Item2; map[$"Onboarding.Preset.{item.Item1}.Lead"] = english ? item.Item5 : item.Item3; }
    }

    private static void AddDreamJobs(Dictionary<string, string> map)
    {
        foreach (var (key, title) in new[] { ("dierenarts","Dierenarts"),("piloot","Piloot"),("advocaat","Advocaat"),("leraar","Leraar"),("arts","Arts"),("architect","Architect"),("kok","Kok"),("brandweer","Brandweerman/-vrouw"),("game-developer","Game developer"),("astronaut","Astronaut"),("politie","Politieagent"),("verpleegkundige","Verpleegkundige"),("ondernemer","Ondernemer"),("journalist","Journalist"),("fotograaf","Fotograaf"),("kapper","Kapper"),("programmeur","Programmeur"),("bouwkundige","Bouwkundige"),("tandarts","Tandarts"),("psycholoog","Psycholoog"),("fysiotherapeut","Fysiotherapeut"),("verloskundige","Verloskundige"),("apotheker","Apotheker"),("rechter","Rechter"),("notaris","Notaris"),("ingenieur","Ingenieur"),("elektricien","Elektricien"),("automonteur","Automonteur"),("timmerman","Timmerman"),("hovenier","Hovenier"),("bioloog","Bioloog"),("wetenschapper","Wetenschapper"),("grafisch-ontwerper","Grafisch ontwerper"),("muzikant","Muzikant"),("acteur","Acteur"),("profsporter","Profsporter"),("personal-trainer","Personal trainer"),("contentmaker","Contentmaker"),("marketeer","Marketeer"),("accountant","Accountant"),("makelaar","Makelaar"),("cabinepersoneel","Cabinepersoneel"),("militair","Militair"),("pedagogisch-medewerker","Pedagogisch medewerker"),("dierenverzorger","Dierenverzorger"),("evenementenorganisator","Evenementenorganisator"),("data-scientist","Data scientist"),("bakker","Bakker") }) map[$"DreamJob.{key}"] = title;
    }
}
