namespace Jobsy.Web.Localization;

public static class UiStringsTrainingPartners
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

        Add("AdminSettings.TrainingPartners.Enabled.Title",
            "Workshops en opleidingen (partners)",
            "Workshops and courses (partners)");
        Add("AdminSettings.TrainingPartners.Enabled.Desc",
            "Toont Leren & groeien, partnerfeeds en opleidingskaarten in carrière en paspoort.",
            "Shows Learn & grow, partner feeds and course cards in career and passport.");
        Add("AdminSettings.TrainingPartners.Enabled.ImpactOff",
            "Kandidaten zien geen partneropleidingen; API geeft 404.",
            "Candidates see no partner courses; API returns 404.");

        Add("Training.Type.Workshop", "Workshop", "Workshop");
        Add("Training.Type.Cursus", "Cursus", "Course");
        Add("Training.Type.Opleiding", "Opleiding", "Programme");
        Add("Training.Free", "Gratis", "Free");
        Add("Training.PartnerLink", "Partnerlink", "Partner link");
        Add("Training.Open", "Bekijk details", "View details");
        Add("Training.OpenExternal", "Naar aanbieder", "Go to provider");
        Add("Training.Save", "Bewaar", "Save");
        Add("Training.Saved", "Bewaard", "Saved");
        Add("Training.ClosePanel", "Sluiten", "Close");
        Add("Training.Disclosure",
            "Partnerlink: Lobsy kan een kleine vergoeding krijgen. Dat verandert nooit wat bovenaan staat.",
            "Partner link: Lobsy may receive a small fee. That never changes what appears at the top.");
        Add("Training.LearnTitle", "Wat je leert", "What you learn");
        Add("Training.GainTitle", "Wat het je oplevert", "What you gain");
        Add("Training.Gain.Passport",
            "Wat je afrondt, staat als bewijs in je paspoort.",
            "What you finish appears as proof in your passport.");
        Add("Training.Gain.Matches",
            "Vacatures die erom vragen, zie je eerder in je matches.",
            "Jobs that ask for it show up sooner in your matches.");
        Add("Training.Gain.Route",
            "Je route schuift op: je volgende stap komt dichterbij.",
            "Your route moves forward: your next step gets closer.");

        Add("Training.Reason.NextStep",
            "Dit is je volgende stap op je route.",
            "This is your next step on your route.");
        Add("Training.Reason.General",
            "Handig in bijna elke baan.",
            "Useful in almost any job.");
        Add("Training.Reason.Fallback",
            "Past bij jouw route en groeipunten.",
            "Fits your route and growth areas.");
        Add("Training.Reason.Growth.Samenwerken",
            "Bouwt voort op samenwerken en overleg.",
            "Builds on teamwork and collaboration.");
        Add("Training.Reason.Growth.Resultaatgerichtheid",
            "Helpt bij plannen en afmaken.",
            "Helps with planning and finishing.");
        Add("Training.Reason.Growth.Stressbestendigheid",
            "Groeipunt uit je test: rust onder druk.",
            "Growth area from your test: calm under pressure.");
        Add("Training.Reason.Growth.Innovatie",
            "Nieuwe werkwijzen oefenen.",
            "Practice new ways of working.");
        Add("Training.Reason.Growth.Extraversie",
            "Klantcontact en presenteren.",
            "Customer contact and presenting.");

        Add("Training.CompetencyBand.Strong", "Sterk punt", "Strength");
        Add("Training.CompetencyBand.Growing", "Groeit nog", "Still growing");
        Add("Training.CompetencyBand.GrowHere", "Hier kun je in groeien", "Room to grow here");

        Add("Training.CareerStep.Title", "Workshops en opleidingen voor deze stap", "Workshops and courses for this step");
        Add("Training.CareerStep.Sub", "Gratis staat altijd bovenaan", "Free always appears first");
        Add("Training.CareerStep.DetailsRight", "Details staan rechts", "Details are on the right");
        Add("Training.CareerStep.DetailsHide", "Details verbergen", "Hide details");
        Add("Training.CareerStep.PlainFallback",
            "Denk aan: {0}",
            "Consider: {0}");
        Add("Training.CareerStep.AllLink", "Naar Leren & groeien", "Go to Learn & grow");

        Add("Training.LearnGrow.Title", "Leren en groeien, op jouw manier", "Learn and grow, your way");
        Add("Training.LearnGrow.Lead",
            "Workshops en opleidingen die passen bij jouw route en je test. Gratis staat altijd bovenaan.",
            "Workshops and courses that fit your route and your test. Free always appears first.");
        Add("Training.LearnGrow.SearchPlaceholder", "Zoek op onderwerp", "Search by topic");
        Add("Training.LearnGrow.SearchHint", ", bijvoorbeeld klantcontact", ", e.g. customer contact");
        Add("Training.LearnGrow.Search", "Zoeken", "Search");
        Add("Training.LearnGrow.NavRoute", "Mijn route", "My route");
        Add("Training.LearnGrow.NavLearn", "Leren & groeien", "Learn & grow");
        Add("Training.LearnGrow.NavNote",
            "Alles wat je bewaart of start, blijft hier staan.",
            "Everything you save or start stays here.");
        Add("Training.LearnGrow.FilterPanel", "Filters", "Filters");
        Add("Training.LearnGrow.FilterExtraversieShort", "Extraversie", "Extraversion");
        Add("Training.LearnGrow.FilterSubject", "Onderwerp", "Topic");
        Add("Training.LearnGrow.FilterAll", "Alles", "All");
        Add("Training.LearnGrow.FilterFreeOnly", "Alleen gratis", "Free only");
        Add("Training.LearnGrow.FilterFormat", "Vorm", "Format");
        Add("Training.LearnGrow.FilterDuration", "Duur", "Duration");
        Add("Training.LearnGrow.Format.All", "Alles", "All");
        Add("Training.LearnGrow.Format.Online", "Online", "Online");
        Add("Training.LearnGrow.Format.Classroom", "Klassikaal", "In person");
        Add("Training.LearnGrow.Duration.All", "Alles", "All");
        Add("Training.LearnGrow.Duration.Short", "Kort", "Short");
        Add("Training.LearnGrow.Duration.Weeks", "Weken", "Weeks");
        Add("Training.LearnGrow.Duration.Programme", "Opleiding", "Programme");
        Add("Training.LearnGrow.MobileFilters", "Gratis · vorm · duur", "Free · format · duration");
        Add("Training.LearnGrow.SectionNext", "Voor je volgende stap", "For your next step");
        Add("Training.LearnGrow.SectionGrowth", "Voor je groeipunten", "For your growth areas");
        Add("Training.LearnGrow.SectionGeneral", "Voor iedereen", "For everyone");
        Add("Training.LearnGrow.Count", "{0} resultaten", "{0} results");
        Add("Training.LearnGrow.ShowMore", "Toon meer", "Show more");
        Add("Training.LearnGrow.ShowLess", "Toon minder", "Show less");
        Add("Training.Duration.OneDay", "1 dag", "1 day");
        Add("Training.Duration.OneWeek", "1 week", "1 week");
        Add("Training.Duration.OneMonth", "1 maand", "1 month");
        Add("Training.Duration.OneHour", "1 uur", "1 hour");
        Add("Training.LearnGrow.HowTitle", "Hoe kiezen we dit?", "How do we choose this?");
        Add("Training.LearnGrow.HowBody",
            "We kijken naar je route, je groeipunten en wat dichtbij is. Gratis staat altijd bovenaan. Een partnerlink zie je altijd aan het label.",
            "We look at your route, growth areas and what is nearby. Free always appears first. Partner links are always labelled.");
        Add("Training.LearnGrow.HelpTitle", "Twijfel je wat past?", "Unsure what fits?");
        Add("Training.LearnGrow.HelpLead",
            "Lobsy denkt met je mee, ook over geld en tijd.",
            "Lobsy thinks along with you, including cost and time.");

        Add("Training.Progress.Title", "Jouw voortgang", "Your progress");
        Add("Training.Progress.InProgress", "Bezig", "In progress");
        Add("Training.Progress.Completed", "Afgerond", "Completed");
        Add("Training.Progress.Saved", "Bewaard", "Saved");
        Add("Training.Progress.WeekOf", "week {0} van {1}", "week {0} of {1}");
        Add("Training.Progress.CompletedProof", "Afgerond · staat in je Bewijzen", "Completed · in your Proof");
        Add("Training.Progress.MarkDone", "Klaar? Zet het in je paspoort", "Done? Add it to your passport");
        Add("Training.Progress.SavedSection", "Bewaard", "Saved");

        Add("Training.PassportGrowth.Intro",
            "Je sterke punten en groeipunten",
            "Your strengths and growth areas");
        Add("Training.PassportGrowth.GrowTitle", "Hier kun je in groeien", "Room to grow here");
        Add("Training.PassportGrowth.GrowLead",
            "Deze passen bij jou en bij je route.",
            "These fit you and your route.");
        Add("Training.PassportGrowth.MatchLine",
            "Past bij dit groeipunt",
            "Fits this growth area");
        Add("Training.PassportGrowth.ViewAll", "Bekijk alles", "View all");
        Add("Training.PassportGrowth.PromoTitle", "Leren & groeien", "Learn & grow");
        Add("Training.PassportGrowth.PrivacyTitle", "Alleen jij ziet je groeipunten", "Only you see your growth areas");
        Add("Training.PassportGrowth.PrivacyBody",
            "Werkgevers zien alleen wat jij deelt en wat anderen hebben bevestigd.",
            "Employers only see what you share and what others have confirmed.");
        Add("Training.PassportGrowth.ShowInline", "Bekijk groeikansen", "View growth offers");
        Add("Training.PassportGrowth.HideInline", "Verberg groeikansen", "Hide growth offers");

        Add("Training.Delivery.Online", "Online", "Online");
        Add("Training.Delivery.OnSite", "Op locatie", "On site");
        Add("Training.Delivery.Blended", "Blended", "Blended");
        Add("Training.Meta.Duration", "Duur", "Duration");
        Add("Training.Meta.Format", "Vorm", "Format");
        Add("Training.Meta.Provider", "Aanbieder", "Provider");
        Add("Training.Meta.Starts", "Start", "Starts");

        Add("Admin.TrainingPartners.Title", "Partnerfeeds", "Partner feeds");
        Add("Admin.TrainingPartners.Lead",
            "Importeer workshops en opleidingen van regionale partners (JSON, CSV of API).",
            "Import workshops and courses from regional partners (JSON, CSV or API).");
        Add("Admin.TrainingPartners.Import", "Importeer feed", "Import feed");
        Add("Admin.TrainingPartners.Importing", "Bezig…", "Importing…");
        Add("Admin.TrainingPartners.LastImport", "Laatste import", "Last import");
        Add("Admin.TrainingPartners.Never", "Nog niet", "Not yet");
        Add("Admin.TrainingPartners.Offers", "{0} aanbiedingen", "{0} offers");
        Add("Admin.TrainingPartners.FeedKind", "Feed", "Feed");
        Add("Admin.TrainingPartners.Active", "Actief", "Active");
        Add("Admin.TrainingPartners.Demo", "Demo", "Demo");
    }
}
