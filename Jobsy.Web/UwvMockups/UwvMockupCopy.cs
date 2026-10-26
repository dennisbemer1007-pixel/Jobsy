namespace Jobsy.Web.UwvMockups;

/// <summary>Local NL/EN copy for UWV werkmap mockups only (no production i18n keys).</summary>
public static class UwvMockupCopy
{
    public static bool IsEnglish(string? lang) =>
        string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);

    public static string T(string nl, string en, string? lang) =>
        IsEnglish(lang) ? en : nl;

    /// <summary>Consistent demo candidate name (matches test sign-in).</summary>
    public static string SampleCandidateName(string? lang) =>
        T("Sanne de Vries", "Sanne de Vries", lang);

    public static class Career
    {
        public static string Eyebrow(string? lang) => T("Carrière", "Career", lang);
        public static string Title(string? lang) => T("Jouw route naar logistiek medewerker", "Your path to logistics associate", lang);
        public static string Lead(string? lang) => T(
            "Op basis van je paspoort: huidige beroep administratief medewerker, regio Haaglanden.",
            "Based on your passport: current role office administrator, Haaglanden region.",
            lang);
        public static string EmptyTitle(string? lang) => T("Jouw carrièrekansen", "Your career opportunities", lang);
        public static string EmptyLead(string? lang) => T(
            "Vul je voorkeuren aan, dan laten we routes zien die bij je passen.",
            "Fill in your preferences and we will show routes that fit you.",
            lang);
    }

    public static class Kansberoepen
    {
        public static string Title(string? lang) => T("Kansberoepen", "Opportunity occupations", lang);
        public static string Lead(string? lang) => T(
            "Beroepen met goede kansen in jouw regio, met passende opleidingsroutes.",
            "Occupations with good prospects in your region, with matching training routes.",
            lang);
        public static string Source(string? lang) => T(
            "Gebaseerd op open arbeidsmarktcijfers van CBS en UWV-kansberoepen.",
            "Based on open labour market figures from CBS and UWV opportunity occupations.",
            lang);
        public static string Region(string? lang) => T("Regio Haaglanden", "Haaglanden region", lang);
        public static string VacanciesLink(string? lang, int count) => IsEnglish(lang)
            ? $"{count} vacancies on job map"
            : $"{count} vacatures op banenkaart";
    }

    public static class Similar
    {
        public static string Eyebrow(string? lang) => T("Ook iets voor jou", "Also worth a look", lang);
        public static string Title(string? lang) => T(
            "Beroepen die lijken op je huidige beroep",
            "Occupations similar to your current role",
            lang);
        public static string EmptyTitle(string? lang) => T(
            "Beroepen om te verkennen",
            "Occupations to explore",
            lang);
        public static string Lead(string? lang) => T(
            "Beroepen waar je met je huidige ervaring makkelijk naartoe kunt.",
            "Roles you can move into fairly easily from your current experience.",
            lang);
        public static string RoutesToggle(string? lang) => T("Bekijk opleidingsroutes", "View training routes", lang);
        public static string RoutesHint(string? lang) => T(
            "MBO of BBL, afhankelijk van je startniveau.",
            "Vocational training or apprenticeship, depending on your starting level.",
            lang);
    }

    public static class Omscholen
    {
        public static string Title(string? lang) => T("Omscholen", "Retraining", lang);
        public static string Lead(string? lang) => T(
            "Afgestemd op jouw profiel: routes met baangarantie, leerbanen en onafhankelijk advies.",
            "Matched to your profile: job-guarantee routes, apprenticeships and independent advice.",
            lang);
        public static string WwNote(string? lang) => T(
            "Heb je een WW-uitkering? Soms hoef je sollicitaties voor omscholing niet mee te tellen voor je sollicitatieplicht. Check wat voor jou geldt.",
            "On unemployment benefit? Retraining applications may not always count toward your obligation to apply. Check what applies to you.",
            lang);
        public static string WwLink(string? lang) => T("Meer op uwv.nl", "More at uwv.nl", lang);
        public static string Werkcentrum(string? lang) => T(
            "Gratis en onafhankelijk advies bij een Werkcentrum in je regio.",
            "Free independent advice at a Werkcentrum in your region.",
            lang);
        public static string WerkcentrumLink(string? lang) => T("werkcentrum.nl", "werkcentrum.nl", lang);
        public static string MapLink(string? lang) => T("Bekijk op banenkaart", "View on job map", lang);
    }

    public static class Workshops
    {
        public static string Title(string? lang) => T("Workshops en opleidingen", "Workshops and training", lang);
        public static string Lead(string? lang) => T(
            "Verder leren, passend bij je route.",
            "Keep learning, aligned with your path.",
            lang);
        public static string PartnersTitle(string? lang) => T("Partners van UWV", "UWV partners", lang);
    }

    public static class Banenkaart
    {
        public static string KeywordPlaceholder(string? lang) => T("Soort werk", "Type of work", lang);
        public static string KansResultCount(string? lang, int count) => T(
            $"{count} kansberoepen bij jou in de buurt (voorbeeld)",
            $"{count} opportunity occupations near you (sample)",
            lang);
        public static string LayerLabel(string? lang) => T("Kansberoepen bij jou in de buurt", "Opportunity occupations near you", lang);
        public static string LayerHint(string? lang) => T(
            "Laag toont voorbeeld-kansberoepen met vacatures op de kaart (voorbeelddata).",
            "Layer shows sample opportunity occupations with vacancies on the map (sample data).",
            lang);
    }

    public static class Ondernemer
    {
        public static string TestName(string? lang) => T("Past ondernemen bij mij?", "Is self-employment for me?", lang);
        public static string IntroBenefit(string? lang) => T(
            "Helpt je bepalen of zelfstandig ondernemen bij je past.",
            "Helps you see whether self-employment fits you.",
            lang);
        public static string IntroDuration(string? lang) => T("± 6 minuten", "± 6 minutes", lang);
        public static string IntroLead(string? lang) => T(
            "Korte verdieping na je andere tests. Geen cijfers, wel een rustig beeld in woorden.",
            "A short follow-up after your other tests. No numbers, just a calm picture in words.",
            lang);
        public static string IntroDiscoverTitle(string? lang) => T("Wat je ontdekt", "What you find out", lang);
        public static IReadOnlyList<string> IntroDiscoverItems(string? lang) => IsEnglish(lang)
            ?
            [
                "Whether you like trying ideas on a small scale first",
                "How you feel about uncertainty and your own planning",
                "What to discuss with your work adviser next"
            ]
            :
            [
                "Of je kleine ideeën eerst wilt uitproberen",
                "Hoe je omgaat met onzekerheid en zelf plannen",
                "Wat je met je adviseur kunt bespreken"
            ];
        public static string ResultTitle(string? lang) => T("Je bent het meest een verkenner", "You lean most toward exploring", lang);
        public static string ResultLead(string? lang) => T(
            "Je wilt graag zelf plannen en klein beginnen. Ondernemen kan passen als je ruimte hebt om te leren en je netwerk kunt uitbreiden. Bespreek dit gerust met je adviseur.",
            "You like to plan your own work and start small. Self-employment can fit if you have room to learn and grow your network. Discuss this with your adviser.",
            lang);
        public static string HowItWorksTitle(string? lang) => T("Zo werkt het", "How it works", lang);
        public static IReadOnlyList<string> Questions(string? lang) => IsEnglish(lang)
            ?
            [
                "I like trying a small idea before committing to a big step.",
                "I feel comfortable when I can plan my own week.",
                "Uncertainty motivates me to learn quickly.",
                "I prefer building something of my own over following fixed procedures.",
                "I can ask for help from my network when I start something new.",
                "I am willing to invest time before income becomes steady."
            ]
            :
            [
                "Ik probeer liever eerst een klein idee uit voordat ik een grote stap zet.",
                "Ik voel me op mijn gemak als ik mijn week zelf kan plannen.",
                "Onzekerheid zet me aan om snel nieuwe dingen te leren.",
                "Ik bouw liever iets van mezelf op dan vaste procedures te volgen.",
                "Ik durf hulp te vragen aan mijn netwerk als ik iets nieuws begin.",
                "Ik ben bereid tijd te investeren voordat het inkomen stabiel wordt."
            ];

        public static (string Title, string Lead) ResultCopy(string? outcomeKey, string? lang)
        {
            return outcomeKey switch
            {
                "ready" => (
                    T("Je bent het meest klaar om te ondernemen", "You look most ready to start on your own", lang),
                    T(
                        "Je plant graag zelf, durft klein te beginnen en zoekt ruimte om te groeien. Ondernemen kan passen als je concrete stappen wilt zetten. Bespreek wat realistisch is met je adviseur werk.",
                        "You like to plan your own work, start small, and create room to grow. Self-employment can fit if you want concrete next steps. Discuss what is realistic with your work adviser.",
                        lang)),
                "cautious" => (
                    T("Neem de tijd: ondernemen vraagt voorbereiding", "Take your time: self-employment needs preparation", lang),
                    T(
                        "Zelfstandig ondernemen vraagt veel van je. Verken eerst andere routes of versterk je netwerk. Je adviseur kan helpen kiezen wat nu het beste past.",
                        "Self-employment asks a lot of you. Explore other routes or strengthen your network first. Your adviser can help you choose what fits best right now.",
                        lang)),
                _ => (ResultTitle(lang), ResultLead(lang))
            };
        }

        public static string ResultDoneLine(string? lang) => T(
            "Weer een laag eraf. Je ondernemerscheck is klaar.",
            "Another layer off. Your self-employment check is done.",
            lang);

        public static string ResultNextStepsTitle(string? lang) => T("Wat je nu kunt doen", "What you can do next", lang);

        public static IReadOnlyList<(string Label, string Href, bool External)> ResultNextSteps(string? lang) => IsEnglish(lang)
            ?
            [
                ("Discuss this with your work adviser", "/candidate/adviseur-werk", false),
                ("KVK starters information (kvk.nl)", "https://www.kvk.nl/english/starting-a-business-in-the-netherlands/", true),
                ("Browse entrepreneur workshops", "/carriere/leren", false),
            ]
            :
            [
                ("Bespreek dit met je adviseur werk", "/candidate/adviseur-werk", false),
                ("Startersinformatie op kvk.nl", "https://www.kvk.nl/starten/", true),
                ("Bekijk ondernemers-workshops", "/carriere/leren", false),
            ];

        public static IReadOnlyList<string> HowItWorksSteps(string? lang) => IsEnglish(lang)
            ?
            [
                "Answer six short statements at your own pace",
                "Get a calm picture in words, no scores",
                "Discuss the outcome with your work adviser"
            ]
            :
            [
                "Beantwoord zes korte stellingen, in je eigen tempo",
                "Je krijgt een rustig beeld in woorden, zonder cijfers",
                "Bespreek het resultaat met je adviseur werk"
            ];
    }

    public static class Drijfveren
    {
        public static string SectionTitle(string? lang) => T("Drijfveren en motivatie", "Drivers and motivation", lang);
        public static string ResultsTitle(string? lang) => T("Wat drijft jou", "What drives you", lang);
        public static string Lead(string? lang) => T(
            "Uit je waarden- en drijfverentest (voorbeeldresultaat, geen scores).",
            "From your values and drivers test (sample result, no scores).",
            lang);
        public static string CardTitle(string? lang) => T(
            "Dit vind je belangrijk in werk",
            "What matters to you at work",
            lang);
        public static string CardLead(string? lang) => T(
            "Drie drijfveren uit je test, in gewone taal.",
            "Three drivers from your test, in plain language.",
            lang);
        public static string AdviseurLink(string? lang) => T(
            "Bespreek dit met je adviseur werk",
            "Discuss this with your work adviser",
            lang);
    }

    public static class CarriereSegmented
    {
        public static string TabVoorJou(string? lang) => T("Voor jou", "For you", lang);
        public static string TabOmscholen(string? lang) => T("Omscholen", "Retraining", lang);
        public static string TabKansberoepen(string? lang) => T("Kansberoepen", "Opportunity occupations", lang);
        public static string Meer(string? lang) => T("Meer", "More", lang);
        public static string Footnote(string? lang) => T(
            "Suggesties zijn gebaseerd op je paspoort en regionale arbeidsmarktinformatie (UWV, Werkcentrum Haaglanden). Routes en regelingen kunnen wijzigen; check altijd de actuele voorwaarden bij UWV of je werkcoach.",
            "Suggestions are based on your passport and regional labour market information (UWV, Werkcentrum Haaglanden). Routes and rules can change; always check current terms with UWV or your work coach.",
            lang);
        public static string EmptyProfileTitle(string? lang) => T(
            "Vul je voorkeuren aan",
            "Complete your preferences",
            lang);
        public static string EmptyProfileLead(string? lang) => T(
            "Met je voorkeuren en regio kunnen we passende routes en beroepen laten zien.",
            "With your preferences and region we can show fitting routes and occupations.",
            lang);
        public static string EmptyProfileCta(string? lang) => T(
            "Voorkeuren invullen",
            "Fill in preferences",
            lang);
        public static string DreamCoachTitle(string? lang) => T(
            "Goed om te weten",
            "Good to know",
            lang);
        public static string DreamCoachBody(string? lang) => T(
            "Je droom is een richting. Je zet er kleine stappen naartoe.",
            "Your dream is a direction. You take small steps toward it.",
            lang);
    }

    public static class StrongMatches
    {
        public static string Title(string? lang) => T("Sterke matches voor jou", "Strong matches for you", lang);
    }

    public static class Adviseur
    {
        public static string Title(string? lang) => T("Neem mee naar je adviseur werk", "Take this to your work adviser", lang);
        public static string Lead(string? lang) => T(
            "Kies wat je wilt delen. Je bepaalt wat er op je print staat.",
            "Choose what to share. You decide what appears on your printout.",
            lang);
        public static string VerifiedNote(string? lang) => T(
            "Alleen competenties die een referent heeft bevestigd",
            "Only competencies a reference has confirmed",
            lang);
        public static string Print(string? lang) => T("Print / PDF", "Print / PDF", lang);
        public static string ShareLink(string? lang) => T("Deellink (48 uur)", "Share link (48 hours)", lang);
        public static string PreviewTitle(string? lang) => T("Voorbeeld print", "Sample printout", lang);
        public static string MeetingTip(string? lang) => T(
            "Zo gebruik je dit in je gesprek: kies wat past, print of deel de link, en loop samen door drijfveren en kansen.",
            "Use this in your meeting: pick what fits, print or share the link, and review drivers and opportunities together.",
            lang);
    }
}
