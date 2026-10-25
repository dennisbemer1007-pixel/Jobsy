using Jobsy.Core.Rules;

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

        PatchAvailabilityEducationPl(pl);
        PatchAvailabilityEducationRo(ro);
        PatchAvailabilityEducationAr(ar);
        PatchWorkStatusPl(pl);
        PatchWorkStatusRo(ro);
        PatchWorkStatusAr(ar);
        PatchFinaleV2(nl, en, pl, ro, ar);

        nl["Onboarding.Age.TooYoung"] = "Je moet minstens {0} jaar zijn om Lobsy te gebruiken.";
        en["Onboarding.Age.TooYoung"] = "You must be at least {0} to use Lobsy.";
        pl["Onboarding.Age.TooYoung"] = "Musisz mieć co najmniej {0} lat, aby korzystać z Lobsy.";
        ro["Onboarding.Age.TooYoung"] = "Trebuie să ai cel puțin {0} ani ca să folosești Lobsy.";
        ar["Onboarding.Age.TooYoung"] = "يجب أن يكون عمرك {0} سنة على الأقل لاستخدام لوبسي.";
        nl["Onboarding.Age.Parent"] = "Vanaf {0} jaar mag je meedoen. Jonger dan {1}? Dan vragen we eerst je ouder of voogd. Tot die toestemming blijven tests en AI dicht.";
        en["Onboarding.Age.Parent"] = "From age {0} you can join. Under {1}? Then we ask your parent or guardian first. Until that consent, tests and AI stay closed.";
        pl["Onboarding.Age.Parent"] = "Od {0} lat możesz dołączyć. Mniej niż {1}? Najpierw pytamy rodzica lub opiekuna. Do tej zgody testy i AI są zamknięte.";
        ro["Onboarding.Age.Parent"] = "De la {0} ani poți participa. Sub {1}? Întâi întrebăm părintele sau tutorele. Până la acord, testele și AI rămân închise.";
        ar["Onboarding.Age.Parent"] = "من عمر {0} يمكنك المشاركة. أقل من {1}؟ نسأل والدك أو وليّك أولاً. حتى الموافقة تبقى الاختبارات والذكاء الاصطناعي مغلقة.";

        nl[OnboardingImpressionLibrary.ResultLabelKey] = OnboardingImpressionLibrary.ResultLabel;
        en[OnboardingImpressionLibrary.ResultLabelKey] = "First impression · based on 20 questions";
        pl[OnboardingImpressionLibrary.ResultLabelKey] = "Pierwsze wrażenie · na podstawie 20 pytań";
        ro[OnboardingImpressionLibrary.ResultLabelKey] = "Prima impresie · pe baza a 20 de întrebări";
        ar[OnboardingImpressionLibrary.ResultLabelKey] = "انطباع أول · بناءً على 20 سؤالاً";
    }

    private static void Add(Dictionary<string, string> map, string language, string resumeTitle, string resumeLead, string resumeCta)
    {
        map["Onboarding.Resume.Title"] = resumeTitle;
        map["Onboarding.Resume.Lead"] = resumeLead;
        map["Onboarding.Resume.Cta"] = resumeCta;
        var english = language != "nl";
        map["Onboarding.AccountPrivacyLink"] = english ? "My data & account" : "Mijn gegevens & account";
        map["Onboarding.Step1.Title"] = english ? "Let’s get acquainted" : "Even kennismaken";
        map["Onboarding.Step1.Lead"] = english ? "Is this correct? We copied your name from your account." : "Klopt dit? We hebben je naam overgenomen van je account.";
        map["Onboarding.UseMyLocation"] = english ? "Use my location" : "Gebruik mijn locatie";
        map["Onboarding.LocationDenied"] = english
            ? "Location access was denied. You can enter your postcode manually."
            : "Locatie is geweigerd. Je kunt je postcode handmatig invullen.";
        map["Onboarding.LocationFailed"] = english
            ? "We couldn’t determine your postcode. Try entering it manually."
            : "We konden je postcode niet bepalen. Vul hem handmatig in.";
        map["Onboarding.LocationTimeout"] = english
            ? "Finding your location took too long. Try again or enter your address manually."
            : "Locatie ophalen duurde te lang. Probeer opnieuw of vul je adres handmatig in.";
        map["Onboarding.LocationUnavailable"] = english
            ? "We couldn’t determine your location. Enter your address manually."
            : "We konden je locatie niet bepalen. Vul je adres handmatig in.";
        map["Onboarding.LocationRequiresHttps"] = english
            ? "Sharing your location requires a secure connection (HTTPS)."
            : "Locatie delen vereist een beveiligde verbinding (HTTPS).";
        map["Onboarding.Geolocation.ApproximateHint"] = english
            ? "We know your location only roughly (± {0} m). Is this wrong? Type your street or postcode."
            : "We weten je locatie maar ongeveer (± {0} m). Klopt dit niet? Typ je straat of postcode.";
        map["Onboarding.Geolocation.ConfirmPrecise"] = english
            ? "We filled in your address from your location. Does this look right?"
            : "We vulden je adres in via je locatie. Klopt dit?";
        map["Onboarding.Geolocation.ConfirmCta"] = english ? "Yes, this address is correct" : "Ja, dit adres klopt";
        map["Onboarding.Step2.Title"] = english ? "When can you work?" : "Wanneer kun je werken?";
        map["Onboarding.Step2.Lead"] = english ? "Pick one or more options. We fill in the hours and days for you." : "Kies één of meer opties. Wij zetten uren en dagen voor je klaar.";
        map["Onboarding.AvailabilityPresetsLegend"] = english ? "1. Choose what suits you" : "1. Kies wat bij je past";
        map["Onboarding.AvailabilityPresetsHint"] = english ? "You can pick more than one." : "Je mag er meer dan één kiezen.";
        map["Onboarding.AvailabilityStartLegend"] = english ? "2. When can you start?" : "2. Wanneer kun je beginnen?";
        map["Onboarding.AvailabilityStartImmediate"] = english ? "Right away" : "Per direct";
        map["Onboarding.AvailabilityStartImmediateHint"] = english ? "You can start as soon as possible." : "Je kunt zo snel mogelijk beginnen.";
        map["Onboarding.AvailabilityStartFromDate"] = english ? "From a date" : "Vanaf een datum";
        map["Onboarding.AvailabilityStartFromDateHint"] = english ? "You know when you can start." : "Je weet wanneer je kunt beginnen.";
        map["Onboarding.AvailabilityStartDateInvalid"] = english ? "Choose today or a date in the future." : "Kies vandaag of een datum in de toekomst.";
        map["Onboarding.AvailabilityStartSummary"] = english ? "When you start" : "Start";
        map["Onboarding.AvailabilityImmediateCheck"] = english ? "I can start right away" : "Ik kan meteen beginnen";
        map["Onboarding.AvailabilityStartDateHint"] = english ? "Pick the first day you can work." : "Kies de eerste dag waarop je kunt werken.";
        map["Onboarding.AvailabilitySummaryTitle"] = english ? "Your choice" : "Jouw keuze";
        map["Onboarding.AvailabilityFineTuneCta"] = english ? "Change hours or days" : "Uren of dagen aanpassen";
        map["Onboarding.AvailabilityFineTuneClose"] = english ? "Hide details" : "Details verbergen";
        map["Onboarding.AvailabilityFineTuneLegend"] = english ? "Set hours and days yourself" : "Stel zelf uren en dagen in";
        map["Onboarding.AvailabilityMatrixHint"] = english ? "Tap a block for each time you can work." : "Tik op elk vakje waarop je kunt werken.";
        map["Onboarding.Preset.Custom.GridLegend"] = english ? "When are you available?" : "Wanneer ben je beschikbaar?";
        map["Onboarding.Preset.Custom.GridHint"] = english
            ? "Tap morning, afternoon, evening or night for each day."
            : "Tik per dag aan: ochtend, middag, avond of nacht.";
        map["Onboarding.Step3.Title"] = english ? "How do you get to work?" : "Hoe kom je op je werk?";
        map["Onboarding.Step3.Lead"] = english ? "We only show jobs that are easy to reach." : "Dan laten we alleen banen zien die goed te bereiken zijn.";
        map["Onboarding.Step4.Title"] = english ? "Have you worked before?" : "Heb je al gewerkt?";
        map["Onboarding.Step4.Lead"] = english ? "A side job, volunteering or internship counts too." : "Bijbaan, vrijwilligerswerk of stage telt ook mee.";
        map["Onboarding.Step5.Title"] = english ? "What is your education?" : "Wat is je opleiding?";
        map["Onboarding.Step5.Lead"] = english ? "Pick your highest finished school level. No diploma? Choose None." : "Kies je hoogste afgeronde opleiding. Geen diploma? Kies Geen.";
        map["Onboarding.EducationPickLevel"] = english ? "School level" : "Kies je opleidingsniveau";
        map["Onboarding.EducationMboLegend"] = english ? "Which MBO level?" : "Welk MBO-niveau?";
        map["Onboarding.EducationMboLevel"] = english ? "MBO {0}" : "MBO {0}";
        map["Onboarding.EducationDirectionHint"] = english
            ? "Optional. Start typing or pick a suggestion."
            : "Optioneel. Begin met typen of kies een suggestie.";
        map["Onboarding.EducationDirectionField"] = english ? "Or type yourself" : "Of typ zelf";
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
        map["Onboarding.Validation.FirstName"] = english ? "Enter your first name." : "Vul je voornaam in.";
        map["Onboarding.Validation.LastName"] = english ? "Enter your last name." : "Vul je achternaam in.";
        map["Onboarding.Validation.BirthDate"] = english ? "Choose your date of birth." : "Kies je geboortedatum.";
        map["Onboarding.Validation.Location"] = english
            ? "Enter your postcode or city, or use your location."
            : "Vul je postcode of woonplaats in, of gebruik je locatie.";
        map["Onboarding.Validation.AvailabilityGrid"] = english
            ? "Tap at least one time block when you can work."
            : "Tik minstens één vakje aan waarop je kunt werken.";
        map["Onboarding.Validation.HoursRange"] = english ? "Set a valid hours range." : "Stel een geldig urenbereik in.";
        map["Onboarding.Validation.Transport"] = english ? "Choose how you travel to work." : "Kies hoe je naar werk reist.";
        map["Onboarding.Validation.MaxTravel"] = english ? "Choose a maximum travel time." : "Kies een maximale reistijd.";
        map["Onboarding.Validation.Dream"] = english ? "Pick or type a dream job." : "Kies of typ een droombaan.";
        map["Onboarding.Validation.Consent"] = english ? "Tick consent to continue with the mini tests." : "Vink toestemming aan om verder te gaan met de mini-tests.";
        map["Onboarding.Transport"] = english ? "Transport" : "Vervoer"; map["Onboarding.MaxTravel"] = english ? "Maximum travel" : "Maximaal reizen"; map["Onboarding.Minutes"] = english ? "min" : "min"; map["Onboarding.Other"] = english ? "Other" : "Anders";
        map["Profile.CurrentJob"] = language switch
        {
            "nl" => "Dit is mijn huidige baan",
            "pl" => "To moja obecna praca",
            "ro" => "Acesta este jobul meu actual",
            "ar" => "هذه وظيفتي الحالية",
            _ => "This is my current job"
        };
        map["Onboarding.TransportMulti"] = language switch
        {
            "nl" => "Je kunt meer dan één kiezen. De maximale reistijd geldt voor allemaal.",
            "pl" => "Możesz wybrać więcej niż jeden. Maksymalny czas dojazdu dotyczy wszystkich.",
            "ro" => "Poți alege mai multe. Timpul maxim de deplasare se aplică tuturor.",
            "ar" => "يمكنك اختيار أكثر من وسيلة. الحد الأقصى لوقت السفر ينطبق عليها جميعًا.",
            _ => "You can choose more than one. The maximum travel time applies to all of them."
        };
        map["Onboarding.DeleteJob"] = english ? "Delete {0}" : "Verwijder {0}";
        map["Onboarding.DreamSearch"] = english ? "Search or type your dream job" : "Zoek of typ je droombaan";
        map["Onboarding.DreamOwn"] = english ? "Use “{0}” as my own dream job" : "Gebruik “{0}” als eigen droombaan";
        map["Onboarding.Consent.Title"] = english ? "Now a little about you" : "Nu even over jou";
        map["Onboarding.Consent.Lead"] = english ? "Four short tests. There are no wrong answers: choose what comes to mind first." : "4 korte tests met een paar vragen per test. Er zijn geen foute antwoorden: kies wat het eerst in je opkomt.";
        map["Onboarding.Consent.Check"] = english ? "I consent to analysing my answers for my Compass." : "Ik geef toestemming om mijn antwoorden te analyseren voor mijn Kompas.";
        map["Onboarding.Consent.More"] = english ? "More explanation" : "Meer uitleg"; map["Onboarding.Consent.MoreText"] = english ? "Your answers are only used for your personal Compass." : "Je antwoorden worden alleen gebruikt voor jouw persoonlijke Kompas.";
        map["Onboarding.Consent.Private"] = english ? "Employers cannot see your answers." : "Werkgevers zien je antwoorden niet.";
        map["Onboarding.Consent.Cta"] = english ? "Start the first test" : "Start de eerste test"; map["Onboarding.Consent.Failed"] = english ? "Consent could not be saved. Please try again." : "Toestemming opslaan lukt niet. Probeer het opnieuw.";
        map["Onboarding.Journey.Headline"] = english ? "In ±6 minutes you discover what fits you" : "In ±6 minuten ontdek je wat bij je past";
        map["Onboarding.Journey.Subline"] = english ? "That is how we find jobs that truly match you." : "Zo vinden we banen die écht bij jou passen.";
        map["Onboarding.Journey.StationsAria"] = english ? "Four short tests on your journey" : "Vier korte tests op je reis";
        map["Onboarding.Journey.Benefit.Work"] = english ? "Where you are strong" : "Waar je sterk in bent";
        map["Onboarding.Journey.Benefit.Fun"] = english ? "Which careers energise you" : "Welke beroepen je energie geven";
        map["Onboarding.Journey.Benefit.Vibe"] = english ? "Where you feel at home" : "Waar je je thuis voelt";
        map["Onboarding.Journey.Benefit.Values"] = english ? "What matters to you" : "Wat je belangrijk vindt";
        map["Onboarding.Journey.Now"] = english ? "Now" : "Nu";
        map["Onboarding.Journey.Status.Done"] = english ? "Completed" : "Voltooid";
        map["Onboarding.Journey.Status.Current"] = english ? "Current test" : "Huidige test";
        map["Onboarding.Journey.Status.Upcoming"] = english ? "Upcoming" : "Nog te doen";
        map["Onboarding.Journey.Duration.One"] = english ? "~1.5 min" : "~1,5 min";
        map["Onboarding.Journey.CtaStart"] = english ? "Start with test 1" : "Start met test 1";
        map["Onboarding.Journey.CtaContinue"] = english ? "Continue with test {0}" : "Ga verder met test {0}";
        map["Onboarding.Journey.Consent.Check"] = english
            ? "I consent to analysing my answers."
            : "Ik geef toestemming om mijn antwoorden te analyseren.";
        map["Onboarding.Journey.Consent.MoreText"] = english
            ? "Your answers are only used for your personal Compass. Employers cannot see your answers."
            : "Je antwoorden worden alleen gebruikt voor jouw persoonlijke Kompas. Werkgevers zien je antwoorden niet.";
        map["Onboarding.Journey.Validation.Consent"] = english
            ? "Please tick consent to continue."
            : "Vink toestemming aan om verder te gaan.";
        map["Onboarding.Test.Lead"] = english ? "How well does each sentence fit you?" : "Hoe goed past elke zin bij jou?";
        map["Onboarding.Test.Competency"] = english ? "How you work" : "Hoe je werkt"; map["Onboarding.Test.Competency.Lead"] = english ? "Competencies such as collaborating" : "Bijvoorbeeld samenwerken";
        map["Onboarding.Test.Career"] = english ? "Fun" : "Leuk"; map["Onboarding.Test.Career.Lead"] = english ? "Careers that fit you" : "Beroepen die bij je passen";
        map["Onboarding.Test.Culture"] = english ? "Atmosphere" : "Sfeer"; map["Onboarding.Test.Culture.Lead"] = english ? "Work culture" : "Waar je je thuis voelt";
        map["Onboarding.Test.Values"] = english ? "Values" : "Waarden"; map["Onboarding.Test.Values.Lead"] = english ? "What matters to you" : "Wat je belangrijk vindt";
        map["Onboarding.Likert.OfFive"] = english ? "of 5" : "van 5"; map["Onboarding.Likert.Low"] = english ? "Does not fit" : "Past niet"; map["Onboarding.Likert.High"] = english ? "Fits very well" : "Past heel goed";
        map["Onboarding.NextTest"] = english ? "Next test" : "Volgende test";
        map["Onboarding.ViewKompas"] = english ? "View my Compass" : "Bekijk mijn Kompas";
        map["Onboarding.Done"] = english ? "Done" : "Klaar";
        map["Onboarding.NotSaved"] = english ? "Not saved · Retry" : "Niet bewaard · Opnieuw"; map["Onboarding.AllSaved"] = english ? "Everything is saved" : "Alles is bewaard";
        map["Onboarding.ClientFormNotReady"] = english
            ? "The form is still loading. Wait a moment and try again."
            : "Het formulier laadt nog. Wacht even en probeer opnieuw.";
        map["Onboarding.And"] = english ? "and" : "en";
        map["Onboarding.Finish.Badge"] = english ? "First impression" : "Eerste indruk"; map["Onboarding.Finish.Title"] = english ? "Your Compass is ready, {0}" : "Je Kompas staat klaar, {0}";
        map["Onboarding.Finish.Lead"] = english ? "This already stands out:" : "Dit valt ons nu al op:"; map["Onboarding.Finish.Strength"] = english ? "Your strongest point" : "Je sterkste punt"; map["Onboarding.Finish.Work"] = english ? "Work that fits you" : "Werk dat bij je past"; map["Onboarding.Finish.Value"] = english ? "Important to you" : "Belangrijk voor jou";
        map["Onboarding.Finish.Matches"] = english ? "jobs already fit you" : "vacatures passen al bij je"; map["Onboarding.Finish.Cta"] = english ? "Go to my Compass" : "Naar mijn Kompas"; map["Onboarding.Finish.FullTests"] = english ? "Take full tests for more precision" : "Volledige tests doen voor meer precisie";
        map["Onboarding.Early.Nearby.Badge"] = english ? "First signal" : "Eerste signaal";
        map["Onboarding.Early.Nearby.TitleZero"] = english ? "We are looking for jobs near you" : "We zoeken banen bij jou in de buurt";
        map["Onboarding.Early.Nearby.TitleWithCount"] = english
            ? "There are already <span class=\"ob-vroeg__count\">{0}</span> jobs near you"
            : "Er zijn al <span class=\"ob-vroeg__count\">{0}</span> banen bij jou in de buurt";
        map["Onboarding.Early.Nearby.JobTravelBike"] = english ? "{0} min by bike" : "{0} min fietsen";
        map["Onboarding.Early.Nearby.JobTravelWalk"] = english ? "{0} min walking" : "{0} min lopen";
        map["Onboarding.Early.Nearby.JobTravelCar"] = english ? "{0} min driving" : "{0} min rijden";
        map["Onboarding.Early.Nearby.JobTravelTransit"] = english ? "{0} min transit" : "{0} min OV";
        map["Onboarding.Early.Nearby.JobTravelGeneric"] = english ? "{0} min travel" : "{0} min reizen";
        map["Onboarding.Early.Nearby.JobHours"] = english ? "{0} h/week" : "{0} uur";
        map["Onboarding.Early.Nearby.FallbackCtaLead"] = english
            ? "Explore the map — your basics are already saved."
            : "Verken de banenkaart. Je basisgegevens zijn al bewaard.";
        map["Onboarding.Early.Nearby.Lead"] = english ? "Based on your address, hours and {1} (max. {0} min). More detail follows in your Compass." : "Op basis van je adres, uren en {1} (max. {0} min). Meer detail volgt in je Kompas.";
        map["Onboarding.Early.Nearby.LeadNoLocation"] = english ? "Share your location so we can count reachable jobs." : "Deel je locatie zodat we bereikbare banen kunnen tellen.";
        map["Onboarding.Early.Nearby.LeadEmployersOff"] = english ? "Job matching is not available in this environment yet." : "Banenmatching is in deze omgeving nog niet beschikbaar.";
        map["Onboarding.Early.Nearby.TimeBanner"] = english ? "About <strong>2 minutes</strong> until your first personal result after the short tests." : "Nog <strong>±2 minuten</strong> tot je eerste persoonlijke resultaat na de korte tests.";
        map["Onboarding.Early.Nearby.Examples"] = english ? "{0} examples nearby" : "{0} voorbeelden dichtbij";
        map["Onboarding.Early.Nearby.Fineprint"] = english ? "Teaser on location and hours — no personality score. Full match after the tests." : "Teaser op locatie en uren. Geen persoonlijkheidsscore. Volledige match na de tests.";
        map["Onboarding.Early.Nearby.FallbackZero"] = english ? "Nothing within your travel time yet — that can change quickly as new jobs go live." : "Nog niets binnen je reistijd. Dat kan snel veranderen als er nieuwe vacatures live gaan.";
        map["Onboarding.Early.Nearby.FallbackNoLocation"] = english ? "We need your postcode or city to show nearby jobs." : "Vul je postcode of woonplaats in om banen in de buurt te tonen.";
        map["Onboarding.Early.Nearby.FallbackEmployersOff"] = english ? "You can explore the job map while we finish matching here." : "Verken intussen de banenkaart. Matching komt hier zo snel mogelijk.";
        map["Onboarding.Early.Nearby.WiderHint"] = english ? "With a slightly wider radius there are already {0} jobs." : "Met iets ruimere reistijd zijn er al {0} banen.";
        map["Onboarding.Early.Nearby.Banenkaart"] = english ? "Open the job map" : "Naar de banenkaart";
        map["Onboarding.Early.Nearby.Cta"] = english ? "Continue profile" : "Verder met profiel";
        map["Onboarding.Early.Nearby.Meta"] = english ? "Step 3 · done" : "Stap 3 · klaar";
        map["Onboarding.Panel.Nearby.TitleInitial"] = english
            ? "<span class=\"ob-nearby-panel__count\">{0}</span> jobs near you"
            : "<span class=\"ob-nearby-panel__count\">{0}</span> banen bij jou in de buurt";
        map["Onboarding.Panel.Nearby.TitleRefined"] = english
            ? "Now <span class=\"ob-nearby-panel__count\">{0}</span> jobs that really fit you"
            : "Nu <span class=\"ob-nearby-panel__count\">{0}</span> banen die echt bij je passen";
        map["Onboarding.Panel.Nearby.TitleZero"] = english
            ? "We are counting jobs near you"
            : "We tellen banen bij jou in de buurt";
        map["Onboarding.Panel.Nearby.MobileLine"] = english
            ? "{0} matching jobs nearby"
            : "{0} passende banen in de buurt";
        map["Onboarding.Panel.Nearby.MobileInitial"] = english ? "{0} jobs nearby" : "{0} banen in de buurt";
        map["Onboarding.Panel.Nearby.MobileRefined"] = english ? "Now {0} fitting jobs nearby" : "Nu {0} passende banen in de buurt";
        map["Onboarding.Panel.Nearby.MobileZero"] = english ? "Jobs nearby" : "Banen in de buurt";
        map["Onboarding.Panel.Nearby.Footnote"] = english
            ? "Based on your home location, travel time and hours — examples only."
            : "Op basis van je adres, reistijd en uren. Voorbeelden ter inspiratie.";
        map["Onboarding.Panel.Nearby.MapCaption"] = english
            ? "Your area on the job map"
            : "Jouw buurt op de banenkaart";
        map["Onboarding.Panel.Nearby.RailAria"] = english ? "Jobs nearby (informational)" : "Banen in de buurt (ter info)";
        map["Onboarding.Early.Insight.Badge"] = english ? "First insight" : "Eerste inzicht";
        map["Onboarding.Early.Insight.Title"] = english ? "Your strongest point so far" : "Je sterkste punt tot nu toe";
        map["Onboarding.Early.Insight.Lead"] = english ? "Based on your first short test. It gets sharper after the next three." : "Op basis van je eerste korte test. Het wordt scherper na de volgende drie.";
        map["Onboarding.Early.Insight.TimeBanner"] = english ? "<strong>{0} short tests</strong> left · about <strong>10 minutes</strong> until your Compass is ready." : "Nog <strong>{0} korte tests</strong> · samen ongeveer <strong>10 minuten</strong> tot je Kompas klaar is.";
        map["Onboarding.Early.Insight.Meta"] = english ? "Test 1 · done" : "Test 1 · klaar";
        map["Onboarding.Early.Insight.PillsAria"] = english ? "Test progress" : "Testvoortgang";
        map["Onboarding.Tests.LaterDoen"] = english ? "Do later" : "Later doen";
        map["Onboarding.Tests.LaterDoenHint"] = english
            ? "You can finish the tests anytime from your passport or The discovery journey."
            : "Je maakt de tests later af via je paspoort of De ontdekkingsreis.";
        map["Onboarding.Welcome.Title"] = english ? "Hi {0}, nice to have you here" : "Hoi {0}, fijn dat je er bent";
        map["Onboarding.Welcome.TitleNoName"] = english ? "Hi, nice to have you here" : "Hoi, fijn dat je er bent";
        map["Onboarding.Welcome.JourneyTitle"] = english ? "Welcome to your journey, {0}" : "Welkom bij je reis, {0}";
        map["Onboarding.Welcome.JourneyTitleNoName"] = english ? "Welcome to your journey" : "Welkom bij je reis";
        map["Onboarding.Welcome.BeforeStartTitle"] = english ? "Before you begin" : "Voor je begint";
        map["Onboarding.Welcome.BeforeStartBody"] = english
            ? "Everything is saved as you go. You can stop anytime and pick up where you left off."
            : "Alles wordt onderweg bewaard. Stoppen mag, je gaat later verder waar je was.";
        map["Onboarding.Kreeft.Welcome.Lead"] = english
            ? "You walk five calm stretches from the beach to the light. Along the way you get to know yourself better."
            : "Je loopt in vijf rustige stukken van het strand naar het licht. Onderweg leer je jezelf beter kennen.";
        map["Onboarding.Kreeft.Welcome.ZoneTagSub"] = english ? "Start · welcome and consent" : "Start · welkom en toestemming";
        map["Onboarding.Kreeft.Welcome.Z1Title"] = english ? "Beach · Start" : "Strand · Start";
        map["Onboarding.Kreeft.Welcome.Z1Body"] = english
            ? "This is where you begin. You read what happens to your data."
            : "Hier begin je. Je leest wat er met je gegevens gebeurt.";
        map["Onboarding.Kreeft.Welcome.Z2Title"] = english ? "Coast · About you" : "Kust · Over jou";
        map["Onboarding.Kreeft.Welcome.Z2Body"] = english
            ? "A few basics: when and where you can work."
            : "Een paar basisvragen: wanneer en waar je kunt werken.";
        map["Onboarding.Kreeft.Welcome.Z3Title"] = english ? "Rocks · Background and Dream" : "Rotsen · Achtergrond en Droom";
        map["Onboarding.Kreeft.Welcome.Z3Body"] = english
            ? "What you have done and where you want to go."
            : "Wat je al hebt gedaan en waar je naartoe wilt.";
        map["Onboarding.Kreeft.Welcome.Z4Title"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Onboarding.Kreeft.Welcome.Z4Body"] = english
            ? "Four short tests about how you work and what matters to you."
            : "Vier korte tests over hoe je werkt en wat je belangrijk vindt.";
        map["Onboarding.Kreeft.Welcome.Z5Title"] = english ? "Light · Your Compass" : "Licht · Jouw Kompas";
        map["Onboarding.Kreeft.Welcome.Z5Body"] = english
            ? "You see what fits you and your passport is ready."
            : "Je ziet wat bij je past en je paspoort is klaar.";
        map["Onboarding.Kreeft.Welcome.ConsentRequired"] = english
            ? "I understand that Lobsy uses my answers to find work that fits me."
            : "Ik snap dat Lobsy mijn antwoorden gebruikt om werk te vinden dat bij me past.";
        map["Onboarding.Kreeft.Welcome.ConsentRequiredSub"] = english
            ? "Employers only see something when you agree."
            : "Werkgevers zien pas iets als jij dat goed vindt.";
        map["Onboarding.Kreeft.Welcome.ConsentEmail"] = english
            ? "Lobsy may send me an occasional tip by email."
            : "Lobsy mag me af en toe een tip sturen per e-mail.";
        map["Onboarding.Kreeft.Welcome.ConsentEmailSub"] = english
            ? "Not required. You can change this later."
            : "Niet verplicht. Je kunt dit later aanpassen.";
        map["Onboarding.Kreeft.Welcome.PrivacyLink"] = english
            ? "Read what happens to your data"
            : "Lees wat er met je gegevens gebeurt";
        map["Onboarding.Kreeft.Welcome.CtaCoast"] = english ? "Start on the coast" : "Begin aan de kust";
        map["Onboarding.Kreeft.Rail.Strand"] = english ? "Beach · Start" : "Strand · Start";
        map["Onboarding.Kreeft.Rail.StrandSub"] = english ? "Welcome and consent" : "Welkom en toestemming";
        map["Onboarding.Kreeft.Rail.Kust"] = english ? "Coast · About you" : "Kust · Over jou";
        map["Onboarding.Kreeft.Rail.KustSubIdle"] = english ? "3 steps" : "3 stappen";
        map["Onboarding.Kreeft.Rail.Rotsen"] = english ? "Rocks · Background and Dream" : "Rotsen · Achtergrond en Droom";
        map["Onboarding.Kreeft.Rail.RotsenSubIdle"] = english ? "3 steps" : "3 stappen";
        map["Onboarding.Kreeft.Rail.Diepte"] = english ? "Depth · Who you are" : "Diepte · Wie ben jij";
        map["Onboarding.Kreeft.Rail.DiepteSubIdle"] = english ? "4 short tests" : "4 korte tests";
        map["Onboarding.Kreeft.Rail.Licht"] = english ? "Light · Your Compass" : "Licht · Jouw Kompas";
        map["Onboarding.Kreeft.Rail.LichtSubIdle"] = english ? "your result" : "je resultaat";
        map["Onboarding.Kreeft.Rail.StepLine"] = english ? "Step {0} · {1}" : "Stap {0} · {1}";
        map["Onboarding.Welcome.Lead"] = english ? "In about 12 minutes we make your start profile together. Then your Compass shows who you are and which work fits you." : "In ongeveer 12 minuten maken we samen je startprofiel. Daarna zie je in je Kompas wie je bent en welk werk bij je past.";
        map["Onboarding.Rustig.WelcomeLead"] = english
            ? "In about 12 minutes we go through ten short steps together. Then your Compass is ready and you see which work fits you."
            : "In ongeveer 12 minuten doorlopen we samen tien korte stappen. Daarna staat je Kompas klaar en zie je welk werk bij je past.";
        map["Onboarding.StartJourney.Here"] = english ? "You are here" : "Je bent hier";
        map["Onboarding.StartJourney.Hint"] = english
            ? "Your answers are saved when you click Next. You can stop anytime and continue later where you left off."
            : "Je antwoorden worden bewaard als je op Volgende klikt. Stoppen mag, je gaat later verder waar je was.";
        map["Onboarding.StartJourney.Stop1Sub"] = english ? "Where and when" : "Waar en wanneer";
        map["Onboarding.StartJourney.Stop2Sub"] = english ? "Optional" : "Optioneel";
        map["Onboarding.StartJourney.Stop3Sub"] = english ? "1 step" : "1 stap";
        map["Onboarding.StartJourney.Stop4Sub"] = english ? "4 short tests" : "4 korte tests";
        map["Onboarding.StartJourney.Stop5Sub"] = english ? "Your result" : "Je resultaat";
        map["Onboarding.StartJourney.Sidebar1"] = english ? "3 steps" : "3 stappen";
        map["Onboarding.StartJourney.Sidebar2"] = english ? "optional" : "optioneel";
        map["Onboarding.StartJourney.Sidebar3"] = english ? "1 step" : "1 stap";
        map["Onboarding.StartJourney.Sidebar4"] = english ? "4 short tests" : "4 korte tests";
        map["Onboarding.StartJourney.Sidebar5"] = english ? "your result" : "je resultaat";
        map["Onboarding.Rustig.StartMeta"] = english ? "Getting started" : "Start";
        map["Onboarding.Rustig.MinutesShort"] = english ? "minutes" : "minuten";
        map["Onboarding.Rustig.Phase.Droom"] = english ? "Dream job" : "Droom";
        map["Onboarding.Rustig.Saved"] = english ? "✓ Saved" : "✓ Bewaard";
        map["Onboarding.Rustig.Phase.WieBenJij"] = english ? "Who you are" : "Wie ben jij";
        map["Onboarding.Rustig.Phase.Kompas"] = english ? "Your Compass" : "Jouw Kompas";
        map["Onboarding.Rustig.Kompas.ShellLead"] = english
            ? "Discover what Lobsy can do for you from here."
            : "Je gaat nu de kracht van Lobsy meemaken. Dit is wat je vanaf nu kunt verwachten.";
        map["Onboarding.Rustig.Kompas.Badge"] = english ? "Onboarding and tests complete" : "Onboarding en tests afgerond";
        map["Onboarding.Rustig.Kompas.TestsTitle"] = english ? "What your four tests show together" : "Wat je vier tests samen laten zien";
        map["Onboarding.Rustig.Kompas.TestsLead"] = english
            ? "In words, without scores. This is your starting point for your Compass."
            : "In woorden, zonder scores. Dit is je startpunt voor je Kompas.";
        map["Onboarding.Rustig.Kompas.DirectionsTitle"] = english ? "Directions that fit you" : "Richtingen die bij je passen";
        map["Onboarding.Rustig.Kompas.DirectionsLead"] = english
            ? "Warm examples to explore further. Not promises — a first compass."
            : "Warme voorbeelden om op verder te verkennen. Geen beloftes, wel een eerste kompas.";
        map["Onboarding.Rustig.Kompas.Dir.Zorg.Title"] = english ? "Care & wellbeing" : "Zorg & welzijn";
        map["Onboarding.Rustig.Kompas.Dir.Zorg.Body"] = english
            ? "Places where you work with people and make a difference."
            : "Passende plekken waar je met mensen werkt en verschil maakt.";
        map["Onboarding.Rustig.Kompas.Dir.Groen.Title"] = english ? "Green technology" : "Groene techniek";
        map["Onboarding.Rustig.Kompas.Dir.Groen.Body"] = english
            ? "Outdoors and hands-on, with room to learn."
            : "Buiten en met je handen, met ruimte om te leren.";
        map["Onboarding.Rustig.Kompas.Dir.Team.Title"] = english ? "Organising & teamwork" : "Organiseren & samenwerken";
        map["Onboarding.Rustig.Kompas.Dir.Team.Body"] = english
            ? "Structure, overview and contact with a team."
            : "Structuur, overzicht en contact met een team.";
        map["Kompas.Ssr.Eyebrow"] = english
            ? "Light · Your Compass · onboarding and tests complete"
            : "Licht · Jouw Kompas · onboarding en tests afgerond";
        map["Kompas.Ssr.HubTitle"] = english ? "Your Compass" : "Jouw Kompas";
        map["Kompas.Ssr.HubLead"] = english
            ? "What your four tests show together — in words, without scores. This is your starting point for your Compass."
            : "Wat je vier tests samen laten zien — in woorden, zonder scores. Dit is je startpunt voor je Kompas.";
        map["Kompas.Ssr.Lead"] = english
            ? "Open your profile for the full Compass experience, or go straight to your passport."
            : "Open je profiel voor het volledige Kompas, of ga direct naar je paspoort.";
        map["Kompas.Ssr.OpenProfile"] = english ? "Open my profile" : "Open mijn profiel";
        map["Kompas.Ssr.FullTestsLead"] = english
            ? "The full tests take a bit longer and sharpen your Compass."
            : "De volledige tests duren iets langer en maken je Kompas scherper.";
        map["Kompas.Ssr.FullTestsCta"] = english ? "Start with the first test" : "Begin met de eerste test";
        map["Onboarding.Rustig.DreamOwnLabel"] = english ? "Or type your own dream job" : "Of typ je eigen droombaan";
        map["Onboarding.Welcome.Hint"] = english ? "Everything is saved right away. You can stop and continue later." : "Alles wordt meteen bewaard. Stoppen mag, je gaat later verder waar je was."; map["Onboarding.Welcome.Cta"] = english ? "Start" : "Beginnen";
        map["Onboarding.Welcome.CtaRustig"] = english ? "Get started" : "Aan de slag";
        map["Onboarding.Welcome.OverJou"] = english ? "Where you live and when you can work" : "Waar je woont en wanneer je kunt"; map["Onboarding.Welcome.Background"] = english ? "Your background" : "Je achtergrond"; map["Onboarding.Welcome.BackgroundLead"] = english ? "Work and education, you can skip it" : "Werk en opleiding, mag je overslaan"; map["Onboarding.Welcome.Dream"] = english ? "Your dream job" : "Je droombaan"; map["Onboarding.Welcome.DreamLead"] = english ? "Where do you want to go?" : "Waar wil je naartoe?"; map["Onboarding.Welcome.TestsLead"] = english ? "4 short tests" : "4 korte tests"; map["Onboarding.Welcome.Time2"] = "2 min"; map["Onboarding.Welcome.Time1"] = "1 min"; map["Onboarding.Welcome.TimeHalf"] = "½ min";
        map["Onboarding.IllustratedSidebarTitle"] = english ? "Tip for this step" : "Tip bij deze stap";
        map["Onboarding.IllustratedSidebar.SubstepsAria"] = english ? "Steps in this onboarding flow" : "Stappen in deze onboarding";
        map["Onboarding.IllustratedSidebar.Step1"] = english ? "We only use your address for travel time. Employers never see your home address." : "Je adres gebruiken we alleen voor reistijd. Werkgevers zien je woonadres niet.";
        map["Onboarding.IllustratedSidebar.Step2"] = english ? "Pick what fits now. You can fine-tune days and hours later." : "Kies wat nu bij je past. Dagen en uren kun je later nog finetunen.";
        map["Onboarding.IllustratedSidebar.Step3"] = english ? "We match jobs you can actually reach with how you travel." : "We tonen banen die passen bij hoe jij reist en hoe ver je wilt.";
        map["Onboarding.IllustratedSidebar.Step4"] = english ? "Side jobs, volunteering and internships count too. Skip if you prefer." : "Bijbaan, vrijwilligerswerk en stage tellen ook. Overslaan mag.";
        map["Onboarding.IllustratedSidebar.Step5"] = english ? "Your highest finished level is enough. Direction is optional." : "Je hoogste afgeronde niveau is genoeg. Richting is optioneel.";
        map["Onboarding.IllustratedSidebar.Step6"] = english ? "Dream big. In Career we shape a route, even if you are not sure yet." : "Denk groot. In Carrière maken we er een route naartoe, ook als je het nog niet weet.";
        map["Onboarding.Step6.Suggest.FitTitle"] = english ? "Fits you" : "Past bij jou";
        map["Onboarding.Step6.Suggest.RegionalTitle"] = english ? "Popular near you" : "Populair in de buurt";
        map["Onboarding.Step6.Suggest.ReasonProfile"] = english ? "Based on your background so far" : "Op basis van wat je tot nu toe invulde";
        map["Onboarding.Step6.Suggest.ReasonPopular"] = english ? "Often chosen in the region" : "Vaak gekozen in de regio";
        map["Onboarding.Phase.OverJou"] = english ? "About you" : "Over jou"; map["Onboarding.Phase.Achtergrond"] = english ? "Background" : "Achtergrond"; map["Onboarding.Phase.Droom"] = english ? "Dream" : "Droom"; map["Onboarding.Phase.WieBenJij"] = english ? "Who are you" : "Wie ben jij";
        map["Onboarding.Side.OverJou"] = english ? "3 steps" : "3 stappen"; map["Onboarding.Side.Achtergrond"] = english ? "optional" : "optioneel"; map["Onboarding.Side.Droom"] = english ? "1 step" : "1 stap"; map["Onboarding.Side.WieBenJij"] = english ? "4 short tests" : "4 korte tests"; map["Onboarding.Side.Kompas"] = english ? "Your Compass" : "Jouw Kompas"; map["Onboarding.Side.KompasLead"] = english ? "ready soon" : "bijna klaar";
        AddPresets(map, english);
        AddWorkStatuses(map, english);
        AddDreamJobs(map, language);
    }

    private static void PatchFinaleV2(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        nl["Onboarding.FinaleV2.Badge"] = "Onboarding en tests afgerond";
        en["Onboarding.FinaleV2.Badge"] = "Onboarding and tests completed";
        nl["Onboarding.FinaleV2.Lead"] = "Je gaat nu de kracht van Lobsy meemaken. Dit is wat je vanaf nu kunt verwachten.";
        en["Onboarding.FinaleV2.Lead"] = "You are about to experience what Lobsy can do. Here is what to expect from now on.";
        nl["Onboarding.FinaleV2.FullTests"] = "Doe de volledige tests voor meer precisie";
        en["Onboarding.FinaleV2.FullTests"] = "Take the full tests for more precision";
        nl["Onboarding.FinaleV2.Highlight.Title"] = "{0} vacatures passen al bij je";
        en["Onboarding.FinaleV2.Highlight.Title"] = "{0} jobs already fit you";
        nl["Onboarding.FinaleV2.Highlight.Sub"] = "Allemaal binnen {0} minuten reizen vanaf {1}";
        en["Onboarding.FinaleV2.Highlight.Sub"] = "All within {0} minutes travel from {1}";
        nl["Onboarding.FinaleV2.Highlight.Link"] = "Bekijk ze";
        en["Onboarding.FinaleV2.Highlight.Link"] = "View them";
        nl["Onboarding.FinaleV2.Insights.Title"] = "Dit valt ons nu al op";
        en["Onboarding.FinaleV2.Insights.Title"] = "This already stands out";
        nl["Onboarding.FinaleV2.Insights.Lead"] = "Een eerste indruk uit je Quick-Scan. Met de verdieping wordt dit scherper.";
        en["Onboarding.FinaleV2.Insights.Lead"] = "A first impression from your Quick Scan. The deepening makes this sharper.";
        nl["Onboarding.FinaleV2.Do.Title"] = "Wat Lobsy nu voor je doet";
        en["Onboarding.FinaleV2.Do.Title"] = "What Lobsy does for you now";
        nl["Onboarding.FinaleV2.Do.Lead"] = "Vanaf nu werken we met jouw Kompas. Dit merk je ervan:";
        en["Onboarding.FinaleV2.Do.Lead"] = "From now on we work with your Compass. You will notice:";
        nl["Onboarding.FinaleV2.Soon.Title"] = "Binnenkort ook";
        en["Onboarding.FinaleV2.Soon.Title"] = "Coming soon";
        nl["Onboarding.FinaleV2.Soon.Tag"] = "binnenkort";
        en["Onboarding.FinaleV2.Soon.Tag"] = "coming soon";
        nl["Onboarding.FinaleV2.Timeline.Title"] = "Wat je kunt verwachten";
        en["Onboarding.FinaleV2.Timeline.Title"] = "What you can expect";
        nl["Onboarding.FinaleV2.Timeline.Lead"] = "Zo ziet je eerste tijd met Lobsy eruit.";
        en["Onboarding.FinaleV2.Timeline.Lead"] = "This is what your first time with Lobsy looks like.";
        nl["Onboarding.FinaleV2.Timeline.Today"] = "Vandaag";
        en["Onboarding.FinaleV2.Timeline.Today"] = "Today";
        nl["Onboarding.FinaleV2.Timeline.TodayBody"] = "Bekijk je Kompas en je eerste {0} passende vacatures.";
        en["Onboarding.FinaleV2.Timeline.TodayBody"] = "View your Compass and your first {0} matching jobs.";
        nl["Onboarding.FinaleV2.Timeline.Week"] = "Deze week";
        en["Onboarding.FinaleV2.Timeline.Week"] = "This week";
        nl["Onboarding.FinaleV2.Timeline.WeekBody"] = "Doe de gratis verdieping (± 10 min) en vul je paspoort aan.";
        en["Onboarding.FinaleV2.Timeline.WeekBody"] = "Do the free deepening (± 10 min) and complete your passport.";
        nl["Onboarding.FinaleV2.Timeline.Ongoing"] = "Doorlopend";
        en["Onboarding.FinaleV2.Timeline.Ongoing"] = "Ongoing";
        nl["Onboarding.FinaleV2.Timeline.OngoingBody"] = "Elke keer dat je terugkomt staan de nieuwste passende banen klaar en groeit je route mee.";
        en["Onboarding.FinaleV2.Timeline.OngoingBody"] = "Every time you return, the latest matching jobs are ready and your route grows with you.";
        nl["Onboarding.FinaleV2.Timeline.OngoingBody.Proactive"] = "Elke keer dat je terugkomt staan de nieuwste passende banen klaar, krijg je een seintje bij nieuwe matches en groeit je route mee.";
        en["Onboarding.FinaleV2.Timeline.OngoingBody.Proactive"] = "Every time you return, the latest matching jobs are ready, you get a nudge when new matches appear, and your route grows with you.";
        nl["Onboarding.FinaleV2.End.Title"] = "Klaar om te kijken?";
        en["Onboarding.FinaleV2.End.Title"] = "Ready to take a look?";
        nl["Onboarding.FinaleV2.End.Lead"] = "Je Kompas en je matches staan voor je klaar.";
        en["Onboarding.FinaleV2.End.Lead"] = "Your Compass and your matches are ready for you.";

        void Do(string key, string nlText, string enText)
        {
            nl[key] = nlText;
            en[key] = enText;
        }

        Do("Onboarding.FinaleV2.Do.Match.Title", "Banen die echt bij je passen", "Jobs that really fit you");
        Do("Onboarding.FinaleV2.Do.Match.Body", "We matchen op wie je bent en wat je wilt, niet alleen op je cv.", "We match on who you are and what you want, not just your CV.");
        Do("Onboarding.FinaleV2.Do.Map.Title", "Banenkaart met reistijd", "Job map with travel time");
        Do("Onboarding.FinaleV2.Do.Map.Body", "Zie per vacature hoe lang je onderweg bent vanaf {0}.", "See how long each job takes to reach from {0}.");
        Do("Onboarding.FinaleV2.Do.Compass.Title", "Je Kompas, in woorden", "Your Compass, in words");
        Do("Onboarding.FinaleV2.Do.Compass.Body", "Je sterke punten, wat je leuk vindt en wat je nodig hebt, helder uitgelegd.", "Your strengths, what you enjoy and what you need, explained clearly.");
        Do("Onboarding.FinaleV2.Do.Route.Title", "Je route naar {0}", "Your route to {0}");
        Do("Onboarding.FinaleV2.Do.Route.Body", "In Carrière zie je welke opleidingen en stappen je daar brengen.", "In Career you see which courses and steps get you there.");
        Do("Onboarding.FinaleV2.Do.RouteGeneric.Title", "Je route naar je droombaan", "Your route to your dream job");
        Do("Onboarding.FinaleV2.Do.RouteGeneric.Body", "In Carrière zie je welke stappen je verder helpen.", "In Career you see which steps help you forward.");
        Do("Onboarding.FinaleV2.Do.Passport.Title", "Je paspoort in plaats van een cv", "Your passport instead of a CV");
        Do("Onboarding.FinaleV2.Do.Passport.Body", "Jij bepaalt wat een werkgever van je te zien krijgt.", "You decide what an employer gets to see.");
        Do("Onboarding.FinaleV2.Do.Deeper.Title", "Steeds scherper", "Sharper over time");
        Do("Onboarding.FinaleV2.Do.Deeper.Body", "Met de gratis verdieping worden je Kompas en je matches nauwkeuriger.", "The free deepening makes your Compass and matches more accurate.");
        Do("Onboarding.FinaleV2.Do.References.Title", "Geverifieerde referenties", "Verified references");
        Do("Onboarding.FinaleV2.Do.References.Body", "Oud-collega's of je stagebegeleider bevestigen wat je kunt.", "Former colleagues or your supervisor confirm what you can do.");
        Do("Onboarding.FinaleV2.Do.Notifications.Title", "Meldingen bij nieuwe matches", "Notifications for new matches");
        Do("Onboarding.FinaleV2.Do.Notifications.Body", "Een seintje zodra er een baan bijkomt die bij je past.", "A nudge when a job appears that fits you.");
        Do("Onboarding.FinaleV2.Do.Employers.Title", "Werkgevers vinden jou", "Employers find you");
        Do("Onboarding.FinaleV2.Do.Employers.Body", "Werkgevers zien je profiel anoniem en nodigen je zelf uit.", "Employers see your profile anonymously and invite you.");
        Do("Onboarding.FinaleV2.Soon.References.Title", "Geverifieerde referenties", "Verified references");
        Do("Onboarding.FinaleV2.Soon.References.Body", "Oud-collega's of je stagebegeleider bevestigen wat je kunt.", "Former colleagues or your supervisor confirm what you can do.");
        Do("Onboarding.FinaleV2.Soon.Notifications.Title", "Meldingen bij nieuwe matches", "Notifications for new matches");
        Do("Onboarding.FinaleV2.Soon.Notifications.Body", "Een seintje zodra er een baan bijkomt die bij je past.", "A nudge when a job appears that fits you.");
        Do("Onboarding.FinaleV2.Soon.Employers.Title", "Werkgevers vinden jou", "Employers find you");
        Do("Onboarding.FinaleV2.Soon.Employers.Body", "Werkgevers zien je profiel anoniem en nodigen je zelf uit.", "Employers see your profile anonymously and invite you.");

        foreach (var key in en.Keys.Where(k => k.StartsWith("Onboarding.FinaleV2.", StringComparison.Ordinal)))
        {
            var enText = en[key];
            pl.TryAdd(key, enText);
            ro.TryAdd(key, enText);
            ar.TryAdd(key, enText);
        }
    }

    private static void AddWorkStatuses(Dictionary<string, string> map, bool english)
    {
        map["WorkStatus.Legend"] = english ? "What is your current work status?" : "Wat is je huidige werkstatus?";
        map["WorkStatus.Hint"] = english ? "Optional. You can choose more than one." : "Optioneel. Je mag er meer dan één kiezen.";
        foreach (var (code, nl, en) in WorkStatusLabelRows())
        {
            map[$"WorkStatus.{code}"] = english ? en : nl;
        }
    }

    private static IEnumerable<(string Code, string Nl, string En)> WorkStatusLabelRows()
    {
        yield return ("scholier", "Scholier", "School pupil");
        yield return ("student", "Student (mbo/hbo/wo)", "Student (vocational / university)");
        yield return ("stagiair", "Stagiair / afstudeerder", "Intern / graduate");
        yield return ("loondienst-fulltime", "In loondienst, fulltime", "Employed, full-time");
        yield return ("loondienst-parttime", "In loondienst, parttime", "Employed, part-time");
        yield return ("flex", "Flexwerker / uitzendkracht / oproepkracht", "Flexible / temp / on-call worker");
        yield return ("zzp", "Zzp'er / freelancer", "Self-employed / freelancer");
        yield return ("zoekend", "Op dit moment geen baan (zoekend)", "Currently unemployed (looking)");
        yield return ("herintreder", "Herintreder (terug na zorg, ziekte of pauze)", "Returner (after care, illness or break)");
        yield return ("gepensioneerd-werkt", "Gepensioneerd, wil nog werken", "Retired, still wants to work");
        yield return ("ondernemer", "Ondernemer met eigen bedrijf", "Business owner");
    }

    private static void PatchWorkStatusPl(Dictionary<string, string> map)
    {
        map["WorkStatus.Legend"] = "Jaki jest Twój obecny status zawodowy?";
        map["WorkStatus.Hint"] = "Opcjonalnie. Możesz wybrać więcej niż jedną opcję.";
        map["WorkStatus.scholier"] = "Uczeń";
        map["WorkStatus.student"] = "Student (zawodowe/technikum/studia)";
        map["WorkStatus.stagiair"] = "Stażysta / absolwent";
        map["WorkStatus.loondienst-fulltime"] = "Etat – pełny";
        map["WorkStatus.loondienst-parttime"] = "Etat – niepełny";
        map["WorkStatus.flex"] = "Praca tymczasowa / zlecenie / na wezwanie";
        map["WorkStatus.zzp"] = "Samozatrudniony / freelancer";
        map["WorkStatus.zoekend"] = "Obecnie bez pracy (szukam)";
        map["WorkStatus.herintreder"] = "Powrót po opiece, chorobie lub przerwie";
        map["WorkStatus.gepensioneerd-werkt"] = "Emeryt, chcę jeszcze pracować";
        map["WorkStatus.ondernemer"] = "Właściciel firmy";
    }

    private static void PatchWorkStatusRo(Dictionary<string, string> map)
    {
        map["WorkStatus.Legend"] = "Care este situația ta profesională actuală?";
        map["WorkStatus.Hint"] = "Opțional. Poți alege mai multe opțiuni.";
        map["WorkStatus.scholier"] = "Elev";
        map["WorkStatus.student"] = "Student (profesional / universitate)";
        map["WorkStatus.stagiair"] = "Stagiar / absolvent";
        map["WorkStatus.loondienst-fulltime"] = "Angajat – full-time";
        map["WorkStatus.loondienst-parttime"] = "Angajat – part-time";
        map["WorkStatus.flex"] = "Muncitor flexibil / temp / la apel";
        map["WorkStatus.zzp"] = "Independent / freelancer";
        map["WorkStatus.zoekend"] = "Fără job momentan (caut)";
        map["WorkStatus.herintreder"] = "Revenire după îngrijire, boală sau pauză";
        map["WorkStatus.gepensioneerd-werkt"] = "Pensionar, vreau să mai lucrez";
        map["WorkStatus.ondernemer"] = "Antreprenor cu firmă proprie";
    }

    private static void PatchWorkStatusAr(Dictionary<string, string> map)
    {
        map["WorkStatus.Legend"] = "‏ما وضعك المهني الحالي؟";
        map["WorkStatus.Hint"] = "‏اختياري. يمكنك اختيار أكثر من خيار.";
        map["WorkStatus.scholier"] = "‏طالب مدرسة";
        map["WorkStatus.student"] = "‏طالب (مهني / جامعي)";
        map["WorkStatus.stagiair"] = "‏متدرب / خريج";
        map["WorkStatus.loondienst-fulltime"] = "‏موظف – دوام كامل";
        map["WorkStatus.loondienst-parttime"] = "‏موظف – دوام جزئي";
        map["WorkStatus.flex"] = "‏عمل مرن / مؤقت / عند الطلب";
        map["WorkStatus.zzp"] = "‏عمل حر / مستقل";
        map["WorkStatus.zoekend"] = "‏بدون عمل حالياً (أبحث)";
        map["WorkStatus.herintreder"] = "‏عودة بعد رعاية أو مرض أو استراحة";
        map["WorkStatus.gepensioneerd-werkt"] = "‏متقاعد وأرغب بالعمل";
        map["WorkStatus.ondernemer"] = "‏صاحب عمل";
    }

    private static void AddPresets(Dictionary<string, string> map, bool english)
    {
        var values = new[] { ("Direct", "Per direct", "Ik kan meteen beginnen", "Immediately", "I can start right away"), ("School", "Bijbaan naast school", "Na school en in het weekend", "Job alongside school", "After school and weekends"), ("Weekend", "Weekenden", "Za en zo", "Weekends", "Saturday and Sunday"), ("Evening", "Avonden", "Ma t/m vr na 18:00", "Evenings", "Weekdays after 18:00"), ("Office", "Kantoordagen", "Ma t/m vr, 9 tot 17 uur", "Office days", "Weekdays, 9 to 17"), ("Holiday", "Vakantiewerk", "In de schoolvakanties", "Holiday work", "During school holidays"), ("Parttime", "Parttime", "12 tot 32 uur per week", "Part-time", "12 to 32 hours a week"), ("Fulltime", "Fulltime", "36 tot 40 uur per week", "Full-time", "36 to 40 hours a week"), ("Custom", "Zelf aangeven", "Kies zelf je dagen en dagdelen", "Specify yourself", "Pick your own days and times") };
        foreach (var item in values) { map[$"Onboarding.Preset.{item.Item1}"] = english ? item.Item4 : item.Item2; map[$"Onboarding.Preset.{item.Item1}.Lead"] = english ? item.Item5 : item.Item3; }
    }

    private static void AddDreamJobs(Dictionary<string, string> map, string language)
    {
        foreach (var (key, title) in new[] { ("dierenarts", "Dierenarts"), ("piloot", "Piloot"), ("advocaat", "Advocaat"), ("leraar", "Leraar"), ("arts", "Arts"), ("architect", "Architect"), ("kok", "Kok"), ("brandweer", "Brandweerman/-vrouw"), ("game-developer", "Game developer"), ("astronaut", "Astronaut"), ("politie", "Politieagent"), ("verpleegkundige", "Verpleegkundige"), ("ondernemer", "Ondernemer"), ("journalist", "Journalist"), ("fotograaf", "Fotograaf"), ("kapper", "Kapper"), ("programmeur", "Programmeur"), ("bouwkundige", "Bouwkundige"), ("tandarts", "Tandarts"), ("psycholoog", "Psycholoog"), ("fysiotherapeut", "Fysiotherapeut"), ("verloskundige", "Verloskundige"), ("apotheker", "Apotheker"), ("rechter", "Rechter"), ("notaris", "Notaris"), ("ingenieur", "Ingenieur"), ("elektricien", "Elektricien"), ("automonteur", "Automonteur"), ("timmerman", "Timmerman"), ("hovenier", "Hovenier"), ("bioloog", "Bioloog"), ("wetenschapper", "Wetenschapper"), ("grafisch-ontwerper", "Grafisch ontwerper"), ("muzikant", "Muzikant"), ("acteur", "Acteur"), ("profsporter", "Profsporter"), ("personal-trainer", "Personal trainer"), ("contentmaker", "Contentmaker"), ("marketeer", "Marketeer"), ("accountant", "Accountant"), ("makelaar", "Makelaar"), ("cabinepersoneel", "Cabinepersoneel"), ("militair", "Militair"), ("pedagogisch-medewerker", "Pedagogisch medewerker"), ("dierenverzorger", "Dierenverzorger"), ("evenementenorganisator", "Evenementenorganisator"), ("data-scientist", "Data scientist"), ("bakker", "Bakker") }) map[$"DreamJob.{key}"] = title;

        foreach (var (key, nl, en, pl, ro, ar) in new (string Key, string Nl, string En, string Pl, string Ro, string Ar)[]
        {
            ("logistiek-medewerker", "Logistiek medewerker", "Logistics worker", "Pracownik logistyki", "Lucrător logistică", "عامل لوجستيات"),
            ("verkoper", "Verkoper", "Shop assistant", "Sprzedawca", "Vânzător", "بائع"),
            ("beveiliger", "Beveiliger", "Security guard", "Ochroniarz", "Agent de pază", "حارس أمن"),
            ("installateur", "Installateur", "Installer", "Monter instalacji", "Instalator", "فنّي تركيب"),
            ("verzorgende-ig", "Verzorgende IG", "Care worker IG", "Opiekun IG", "Îngrijitor IG", "مقدّم رعاية IG"),
            ("schilder", "Schilder", "House painter", "Malarz", "Zugrav", "دهّان"),
            ("loodgieter", "Loodgieter", "Plumber", "Hydraulik", "Instalator sanitar", "سباك"),
            ("doktersassistent", "Doktersassistent", "Doctor's assistant", "Asystent lekarza", "Asistent medical", "مساعد طبيب"),
            ("ict-medewerker", "ICT-medewerker", "IT support", "Pracownik IT", "Specialist IT", "موظف تقنية معلومات"),
            ("horecamedewerker", "Horecamedewerker", "Hospitality worker", "Pracownik gastronomii", "Lucrător HoReCa", "عامل ضيافة"),
        })
        {
            map[$"DreamJob.{key}"] = language switch
            {
                "en" => en,
                "pl" => pl,
                "ro" => ro,
                "ar" => ar,
                _ => nl
            };
        }
    }

    private static void PatchAvailabilityEducationPl(Dictionary<string, string> map)
    {
        map["Onboarding.Step2.Lead"] = "Wybierz jedną lub więcej opcji. Uzupełnimy godziny i dni za Ciebie.";
        map["Onboarding.AvailabilityPresetsLegend"] = "1. Wybierz, co Ci pasuje";
        map["Onboarding.AvailabilityPresetsHint"] = "Możesz wybrać więcej niż jedną opcję.";
        map["Onboarding.AvailabilityStartLegend"] = "2. Kiedy możesz zacząć?";
        map["Onboarding.AvailabilityStartImmediate"] = "Od razu";
        map["Onboarding.AvailabilityStartImmediateHint"] = "Możesz zacząć tak szybko, jak to możliwe.";
        map["Onboarding.AvailabilityStartFromDate"] = "Od wybranej daty";
        map["Onboarding.AvailabilityStartFromDateHint"] = "Wiesz, kiedy możesz zacząć.";
        map["Onboarding.AvailabilityStartDateInvalid"] = "Wybierz dziś lub datę w przyszłości.";
        map["Onboarding.AvailabilityStartSummary"] = "Kiedy zaczynasz";
        map["Onboarding.AvailabilityImmediateCheck"] = "Mogę zacząć od razu";
        map["Onboarding.AvailabilityStartDateHint"] = "Wybierz pierwszy dzień, w którym możesz pracować.";
        map["Onboarding.AvailabilitySummaryTitle"] = "Twój wybór";
        map["Onboarding.AvailabilityFineTuneCta"] = "Zmień godziny lub dni";
        map["Onboarding.AvailabilityFineTuneClose"] = "Ukryj szczegóły";
        map["Onboarding.AvailabilityFineTuneLegend"] = "Ustaw godziny i dni samodzielnie";
        map["Onboarding.AvailabilityMatrixHint"] = "Dotknij każdego pola, w którym możesz pracować.";
        map["Onboarding.Preset.Custom"] = "Podaj samodzielnie";
        map["Onboarding.Preset.Custom.Lead"] = "Wybierz dni i pory dnia";
        map["Onboarding.Preset.Custom.GridLegend"] = "Kiedy jesteś dostępny/a?";
        map["Onboarding.Preset.Custom.GridHint"] = "Dotknij: rano, popołudnie, wieczór lub noc dla każdego dnia.";
        map["Onboarding.Step5.Lead"] = "Wybierz najwyższy ukończony poziom. Brak dyplomu? Wybierz Brak.";
        map["Onboarding.EducationPickLevel"] = "Wybierz poziom wykształcenia";
        map["Onboarding.EducationMboLegend"] = "Który poziom MBO?";
        map["Onboarding.EducationDirectionHint"] = "Opcjonalnie. Dotknij kierunku lub wpisz własny.";
        map["Onboarding.EducationDirectionField"] = "Albo wpisz samodzielnie";
    }

    private static void PatchAvailabilityEducationRo(Dictionary<string, string> map)
    {
        map["Onboarding.Step2.Lead"] = "Alege una sau mai multe opțiuni. Completăm noi orele și zilele.";
        map["Onboarding.AvailabilityPresetsLegend"] = "1. Alege ce ți se potrivește";
        map["Onboarding.AvailabilityPresetsHint"] = "Poți alege mai multe opțiuni.";
        map["Onboarding.AvailabilityStartLegend"] = "2. Când poți începe?";
        map["Onboarding.AvailabilityStartImmediate"] = "Imediat";
        map["Onboarding.AvailabilityStartImmediateHint"] = "Poți începe cât mai curând.";
        map["Onboarding.AvailabilityStartFromDate"] = "De la o dată";
        map["Onboarding.AvailabilityStartFromDateHint"] = "Știi când poți începe.";
        map["Onboarding.AvailabilityStartDateInvalid"] = "Alege azi sau o dată viitoare.";
        map["Onboarding.AvailabilityStartSummary"] = "Când începi";
        map["Onboarding.AvailabilityImmediateCheck"] = "Pot începe imediat";
        map["Onboarding.AvailabilityStartDateHint"] = "Alege prima zi în care poți lucra.";
        map["Onboarding.AvailabilitySummaryTitle"] = "Alegerea ta";
        map["Onboarding.AvailabilityFineTuneCta"] = "Schimbă orele sau zilele";
        map["Onboarding.AvailabilityFineTuneClose"] = "Ascunde detaliile";
        map["Onboarding.AvailabilityFineTuneLegend"] = "Setează orele și zilele singur";
        map["Onboarding.AvailabilityMatrixHint"] = "Atinge fiecare pătrat când poți lucra.";
        map["Onboarding.Preset.Custom"] = "Specifică tu";
        map["Onboarding.Preset.Custom.Lead"] = "Alege zilele și intervalele";
        map["Onboarding.Preset.Custom.GridLegend"] = "Când ești disponibil/ă?";
        map["Onboarding.Preset.Custom.GridHint"] = "Atinge dimineața, după-amiaza, seara sau noaptea pentru fiecare zi.";
        map["Onboarding.Step5.Lead"] = "Alege cel mai înalt nivel terminat. Fără diplomă? Alege Niciunul.";
        map["Onboarding.EducationPickLevel"] = "Alege nivelul de școală";
        map["Onboarding.EducationMboLegend"] = "Ce nivel MBO?";
        map["Onboarding.EducationDirectionHint"] = "Opțional. Atinge o direcție sau scrie tu.";
        map["Onboarding.EducationDirectionField"] = "Sau scrie tu";
    }

    private static void PatchAvailabilityEducationAr(Dictionary<string, string> map)
    {
        map["Onboarding.Step2.Lead"] = "‏اختر خيارًا واحدًا أو أكثر. نحن نملأ الساعات والأيام لك.";
        map["Onboarding.AvailabilityPresetsLegend"] = "‏1. اختر ما يناسبك";
        map["Onboarding.AvailabilityPresetsHint"] = "‏يمكنك اختيار أكثر من خيار.";
        map["Onboarding.AvailabilityStartLegend"] = "‏2. متى يمكنك البدء؟";
        map["Onboarding.AvailabilityStartImmediate"] = "‏فوراً";
        map["Onboarding.AvailabilityStartImmediateHint"] = "‏يمكنك البدء في أقرب وقت.";
        map["Onboarding.AvailabilityStartFromDate"] = "‏من تاريخ";
        map["Onboarding.AvailabilityStartFromDateHint"] = "‏تعرف متى يمكنك البدء.";
        map["Onboarding.AvailabilityStartDateInvalid"] = "‏اختر اليوم أو تاريخاً في المستقبل.";
        map["Onboarding.AvailabilityStartSummary"] = "‏البدء";
        map["Onboarding.AvailabilityImmediateCheck"] = "‏يمكنني البدء فورًا";
        map["Onboarding.AvailabilityStartDateHint"] = "‏اختر أول يوم يمكنك فيه العمل.";
        map["Onboarding.AvailabilitySummaryTitle"] = "‏اختيارك";
        map["Onboarding.AvailabilityFineTuneCta"] = "‏غيّر الساعات أو الأيام";
        map["Onboarding.AvailabilityFineTuneClose"] = "‏إخفاء التفاصيل";
        map["Onboarding.AvailabilityFineTuneLegend"] = "‏حدّد الساعات والأيام بنفسك";
        map["Onboarding.AvailabilityMatrixHint"] = "‏المس كل مربع يمكنك العمل فيه.";
        map["Onboarding.Preset.Custom"] = "‏حدّد بنفسك";
        map["Onboarding.Preset.Custom.Lead"] = "‏اختر الأيام والفترات";
        map["Onboarding.Preset.Custom.GridLegend"] = "‏متى أنت متاح؟";
        map["Onboarding.Preset.Custom.GridHint"] = "‏المس الصباح أو الظهر أو المساء أو الليل لكل يوم.";
        map["Onboarding.Step5.Lead"] = "‏اختر أعلى مستوى أنهيتَه. لا شهادة؟ اختر لا يوجد.";
        map["Onboarding.EducationPickLevel"] = "‏اختر مستوى التعليم";
        map["Onboarding.EducationMboLegend"] = "‏أي مستوى MBO؟";
        map["Onboarding.EducationDirectionHint"] = "‏اختياري. المس اتجاهًا أو اكتب بنفسك.";
        map["Onboarding.EducationDirectionField"] = "‏أو اكتب بنفسك";
    }
}
