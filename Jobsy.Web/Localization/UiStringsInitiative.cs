namespace Jobsy.Web.Localization;

public static class UiStringsInitiative
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlVal, string enVal)
        {
            nl[key] = nlVal;
            en[key] = enVal;
            pl[key] = enVal;
            ro[key] = enVal;
            ar[key] = enVal;
        }

        Add("Kb.MapToggle.Jobs", "Banenkaart", "Job map");
        Add("Kb.MapToggle.Companies", "Bedrijvenkaart", "Company map");
        Add("Bk.Title", "Bedrijven bij jou in de buurt", "Companies near you");
        Add("Bk.Lead", "Alle bedrijven op Lobsy rond Den Haag. Staat een bedrijf open voor jou, dan kun je ook zonder vacature aankloppen.", "All companies on Lobsy around The Hague. When a company is open for you, you can reach out even without a vacancy.");
        Add("Bk.WeekDots", "{0} van {1} deze week over", "{0} of {1} left this week");
        Add("Bk.InfoBanner", "Alle bedrijven op Lobsy. Bedrijven met het label Staat open voor jou nemen ook een bericht zonder vacature aan en reageren altijd binnen 5 werkdagen.", "All companies on Lobsy. Companies labelled Open for you also accept a message without a vacancy and always respond within 5 business days.");
        Add("Bk.OpenLabel", "Staat open voor jou", "Open for you");
        Add("Bk.Filter.OpenOnly", "Bedrijf open voor initiatief", "Company open for initiative");
        Add("Bk.Filter.Agencies", "Uitzendbureaus en detacheerders", "Agencies and staffing firms");
        Add("Bk.Filter.RespondsFast", "Reageert snel (≤ 3 dagen)", "Responds fast (≤ 3 days)");
        Add("Bk.Sort.Best", "Past het best", "Best fit");
        Add("Initiative.Match.Strong", "Sterke match", "Strong match");
        Add("Initiative.Match.Good", "Past goed", "Good fit");
        Add("Initiative.Match.Reasonable", "Past redelijk", "Reasonable fit");
        Add("Bk.Apps.FilterInitiatives", "Initiatieven", "Initiatives");
        Add("Bk.Apps.FilterVacancy", "Op vacature", "On vacancy");
        Add("Bk.AutoDecline.Lead", "Het zegt niets over jou.", "It says nothing about you.");
        Add("WgNav.OpenForInitiative", "Open voor initiatief", "Open for initiative");
        Add("WgNav.OpenForInitiative.New", "nieuw", "new");
        Add("Initiative.Panel.Title", "Ik wil hier werken", "I want to work here");
        Add("Initiative.Panel.RespondDays", "Reageert altijd binnen 5 werkdagen", "Always responds within 5 business days");
        Add("Initiative.NoVacancies.Notify", "Laat het me weten bij een nieuwe vacature", "Notify me when a new vacancy opens");
        Add("Initiative.Send.Title", "Klop aan bij {0}", "Knock on the door at {0}");
        Add("Initiative.WeekLimit.Title", "Je hebt je 3 initiatieven deze week gebruikt", "You used your 3 initiatives this week");
        Add("Initiative.WeekLimit.Body", "Je motivatie is bewaard. Maandag kun je het met één tik versturen.", "Your motivation is saved. On Monday you can send it with one tap.");
        Add("Initiative.Sent.Steps", "Verstuurd · Bekeken · Reactie", "Sent · Viewed · Response");
    }
}
