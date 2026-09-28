namespace Jobsy.Web.Localization;

public static class UiStringsCandidateInsights
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = enText;
            ro[key] = enText;
            ar[key] = enText;
        }

        Add("Nav.CandidateInsights", "Kandidaatinzichten", "Candidate insights");
        Add("Insights.Tab.Talent", "Talentpool", "Talent pool");
        Add("Insights.Tab.Insights", "Kandidaatinzichten", "Candidate insights");
        Add("Insights.Title", "Wat beweegt kandidaten in jouw regio", "What moves candidates in your region");
        Add("Insights.Lead", "Anonieme inzichten over kandidaten rondom je vestiging.", "Anonymous insights about candidates around your branch.");
        Add("Insights.FreeBadge", "Gratis versie", "Free version");
        Add("Insights.Cta.Full", "Volledige inzichten", "Full insights");
        Add("Insights.Cta.Story", "Bekijk als story", "View as story");
        Add("Insights.Cta.LockedWithTokens", "Volledige inzichten met tokens", "Full insights with tokens");
        Add("Insights.Filter.Branch", "Vestiging", "Branch");
        Add("Insights.Filter.AllBranches", "Alle vestigingen ({0})", "All branches ({0})");
        Add("Insights.Filter.Radius", "Straal", "Radius");
        Add("Insights.Filter.Period", "Periode", "Period");
        Add("Insights.Filter.Period.30", "Laatste 30 dagen", "Last 30 days");
        Add("Insights.Filter.Period.90", "Laatste 90 dagen", "Last 90 days");
        Add("Insights.Filter.Period.365", "Laatste jaar", "Last year");
        Add("Insights.Filter.Km", "{0} km", "{0} km");
        Add("Insights.Anonymity", "Anoniem, minimaal 10 kandidaten per groep. Kleinere groepen tonen we als 'te weinig data'.", "Anonymous, at least 10 candidates per group. Smaller groups show as 'too little data'.");
        Add("Insights.Insufficient", "Te weinig data", "Too little data");
        Add("Insights.Loading", "Inzichten laden…", "Loading insights…");
        Add("Insights.Error", "Kon inzichten niet laden.", "Could not load insights.");
        Add("Insights.RegionalReadOnly", "Je bekijkt inzichten alleen-lezen voor je regio.", "You view insights read-only for your region.");

        Add("Insights.Kpi.Candidates", "Kandidaten in straal", "Candidates in radius");
        Add("Insights.Kpi.AvgHours", "Gem. uren per week", "Avg hours per week");
        Add("Insights.Kpi.Hours32", "32+ uur beschikbaar", "Available 32+ hours");
        Add("Insights.Kpi.Active30", "Actief laatste 30 dagen", "Active last 30 days");
        Add("Insights.Kpi.Matching", "Match met jouw vacatures", "Match with your vacancies");

        Add("Insights.Section.DreamJobs", "Droombanen", "Dream jobs");
        Add("Insights.Section.Map", "Waar wonen kandidaten", "Where candidates live");
        Add("Insights.Section.Competences", "Competenties & werk-DNA", "Competencies & work DNA");
        Add("Insights.Section.Priorities", "Wat kandidaten belangrijk vinden", "What candidates find important");
        Add("Insights.Section.Tip", "Maak je vacature aantrekkelijker", "Make your vacancy more attractive");
        Add("Insights.Section.WorkFields", "Werkvelden", "Work fields");
        Add("Insights.Section.WorkKinds", "Soort werk", "Type of work");
        Add("Insights.Section.Trends", "Trends", "Trends");
        Add("Insights.Section.Vacancies", "Jouw vacatures", "Your vacancies");
        Add("Insights.Section.Dna", "Werk-DNA", "Work DNA");
        Add("Insights.Section.Personality", "Persoonlijkheid", "Personality");
        Add("Insights.Section.Availability", "Beschikbaarheid", "Availability");

        Add("Insights.Priority.travel", "Reistijd", "Travel time");
        Add("Insights.Priority.flexibility", "Flexibiliteit", "Flexibility");
        Add("Insights.Priority.culture", "Sfeer & cultuur", "Atmosphere & culture");
        Add("Insights.Priority.stability", "Zekerheid en duidelijke afspraken", "Certainty and clear agreements");

        Add("Insights.WorkKind.fulltime", "Fulltime", "Full-time");
        Add("Insights.WorkKind.parttime", "Parttime", "Part-time");
        Add("Insights.WorkKind.bijbaan", "Bijbaan", "Side job");
        Add("Insights.WorkKind.stage", "Stage", "Internship");
        Add("Insights.WorkKind.vrijwilliger", "Vrijwilliger", "Volunteer");

        Add("Insights.Tip.Travel", "Noem de reistijd en OV-verbinding in je vacature.", "Mention travel time and public transport in your vacancy.");
        Add("Insights.Tip.Flexibility", "Benoem flexibele uren of diensten waar dat kan.", "Mention flexible hours or shifts where possible.");
        Add("Insights.Tip.Culture", "Schrijf iets over sfeer, team en werkwijze.", "Write something about atmosphere, team and way of working.");
        Add("Insights.Tip.Stability", "Maak afspraken en zekerheid concreet in de tekst.", "Make agreements and certainty concrete in the text.");
        Add("Insights.Tip.Generic", "Maak je vacature concreet over uren, sfeer en bereikbaarheid.", "Make your vacancy concrete about hours, atmosphere and accessibility.");

        Add("Insights.Checklist.Title", "Zo scoort je vacature hierop", "How your vacancy scores on this");
        Add("Insights.Checklist.Salary", "Salaris genoemd", "Salary mentioned");
        Add("Insights.Checklist.Flex", "Flexibele uren", "Flexible hours");
        Add("Insights.Checklist.Culture", "Iets over sfeer en team", "Something about atmosphere and team");
        Add("Insights.Checklist.Yes", "Ja", "Yes");
        Add("Insights.Checklist.No", "Nee", "No");
        Add("Insights.ImproveVacancy", "Vacature verbeteren", "Improve vacancy");

        Add("Insights.Map.LegendDensity", "Minder → Meer kandidaten", "Fewer → More candidates");
        Add("Insights.Map.LegendRadius", "Straal vanaf vestiging", "Radius from branch");
        Add("Insights.Map.LegendPrivacy", "Geen individuele locaties · gebieden met minder dan 10 kandidaten blijven leeg", "No individual locations · areas with fewer than 10 candidates stay empty");

        Add("Insights.Trend.Placeholder", "Beschikbaar zodra er genoeg data is", "Available once there is enough data");
        Add("Insights.Trend.InsufficientHistory", "Beschikbaar zodra er genoeg data is", "Available once there is enough data");

        Add("Insights.Locked.BlurHint", "Volledige inzichten met tokens", "Full insights with tokens");
        Add("Insights.MatchingCount", "{0} passende kandidaten", "{0} matching candidates");
        Add("Insights.Candidates", "kandidaten", "candidates");

        Add("Insights.Story.Title", "Kandidaatinzichten · {0}", "Candidate insights · {0}");
        Add("Insights.Story.Progress", "{0} van 10 · {1} · {2}", "{0} of 10 · {1} · {2}");
        Add("Insights.Story.Close", "Sluiten", "Close");
        Add("Insights.Story.Prev", "Vorige", "Previous");
        Add("Insights.Story.Next", "Volgende", "Next");
        Add("Insights.Story.Card1", "Regio", "Region");
        Add("Insights.Story.Card2", "Droombanen", "Dream jobs");
        Add("Insights.Story.Card3", "Beschikbaarheid", "Availability");
        Add("Insights.Story.Card4", "Wat ze belangrijk vinden", "What they find important");
        Add("Insights.Story.Card5", "Soort werk", "Type of work");
        Add("Insights.Story.Card6", "Jouw vacatures", "Your vacancies");
        Add("Insights.Story.Card7", "Werk-DNA", "Work DNA");
        Add("Insights.Story.Card8", "Werkvelden", "Work fields");
        Add("Insights.Story.Card9", "Trends", "Trends");
        Add("Insights.Story.Card10", "Volledige inzichten", "Full insights");
        Add("Insights.Story.Card10.Lead", "Ontgrendel droombanen 4–10, competenties, werk-DNA en meer met tokens.", "Unlock dream jobs 4–10, competencies, work DNA and more with tokens.");
    }
}
