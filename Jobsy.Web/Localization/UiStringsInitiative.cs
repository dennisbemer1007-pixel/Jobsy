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
        Add("Bk.Filter.AllSectors", "Alle sectoren", "All sectors");
        Add("Bk.Filter.AllFields", "Alle vakgebieden", "All fields");
        Add("Bk.Filter.Distance", "Afstand", "Distance");
        Add("Bk.Filter.OpenOnly", "Bedrijf open voor initiatief", "Company open for initiative");
        Add("Bk.Filter.Agencies", "Uitzendbureaus en detacheerders", "Agencies and staffing firms");
        Add("Bk.Filter.RespondsFast", "Reageert snel (≤ 3 dagen)", "Responds fast (≤ 3 days)");
        Add("Bk.Sort.Best", "Past het best", "Best fit");
        Add("Initiative.Match.Strong", "Sterke match", "Strong match");
        Add("Initiative.Match.Good", "Past goed", "Good fit");
        Add("Initiative.Match.Reasonable", "Past redelijk", "Reasonable fit");
        Add("Bk.Apps.FilterInitiatives", "Initiatieven", "Initiatives");
        Add("Bk.Apps.FilterVacancy", "Op vacature", "On vacancy");
        Add("Bk.Initiative.Next.Wait", "Het bedrijf reageert binnen 5 werkdagen.", "The company responds within 5 business days.");
        Add("Bk.AutoDecline.Lead", "Het zegt niets over jou.", "It says nothing about you.");
        Add("Bk.Status.AutoDeclined", "Automatisch nu niet", "Automatically not now");
        Add("Bk.AutoDecline.Title", "Wat betekent dit?", "What does this mean?");
        Add("Bk.AutoDecline.Body", "Het bedrijf heeft niet binnen 5 werkdagen gereageerd. Je sollicitatie staat op Nu niet, zodat je niet blijft wachten.", "The company did not respond within 5 business days. Your application is set to Not now so you are not left waiting.");
        Add("Bk.AutoDecline.GoodToKnow", "Goed om te weten", "Good to know");
        Add("Bk.AutoDecline.WeekLimitFreed", "Deze sollicitatie telt niet meer mee voor je weeklimiet van 3 initiatieven.", "This application no longer counts toward your weekly limit of 3 initiatives.");
        Add("Bk.EmployerApps.InitiativeLead", "Open sollicitaties via Ik wil hier werken.", "Open applications via I want to work here.");
        Add("WgNav.OpenForInitiative", "Open voor initiatief", "Open for initiative");
        Add("WgNav.OpenForInitiative.New", "nieuw", "new");
        Add("Initiative.Panel.Title", "Ik wil hier werken", "I want to work here");
        Add("Initiative.Panel.RespondDays", "Reageert altijd binnen 5 werkdagen", "Always responds within 5 business days");
        Add("Initiative.NoVacancies.Notify", "Laat het me weten bij een nieuwe vacature", "Notify me when a new vacancy opens");
        Add("Initiative.Send.Title", "Klop aan bij {0}", "Knock on the door at {0}");
        Add("Initiative.WeekLimit.Title", "Je hebt je 3 initiatieven deze week gebruikt", "You used your 3 initiatives this week");
        Add("Initiative.WeekLimit.Body", "Je motivatie is bewaard. Maandag kun je het met één tik versturen.", "Your motivation is saved. On Monday you can send it with one tap.");
        Add("Initiative.Sent.Steps", "Verstuurd · Bekeken · Reactie", "Sent · Viewed · Response");
        Add("Initiative.Panel.ChangeTeam", "Wijzigen", "Change");
        Add("Initiative.Panel.Team", "Team", "Team");
        Add("Initiative.Panel.GeneralEntry", "Algemene ingang", "General entry");
        Add("Initiative.Panel.PassportGoes", "Wat gaat er mee", "What goes with it");
        Add("Initiative.Panel.PassportHint", "Je Lobsy-paspoort gaat mee. Zijn je tests nog niet klaar, dan sturen we je Lobsy-CV.", "Your Lobsy passport goes with it. If tests are not done yet, we send your Lobsy CV.");
        Add("Initiative.Panel.Motivation", "Korte motivatie", "Short motivation");
        Add("Initiative.Panel.SeesNow", "Dit ziet het bedrijf meteen", "The company sees this right away");
        Add("Initiative.Panel.SeesNow.Role", "Functie", "Role");
        Add("Initiative.Panel.SeesNow.Region", "Regio", "Region");
        Add("Initiative.Panel.SeesNow.Dna", "Werk-DNA in woorden", "Work DNA in words");
        Add("Initiative.Panel.SeesNow.Match", "Match in woorden", "Match in words");
        Add("Initiative.Panel.SeesAfter", "Pas als ze je accepteren", "Only if they accept you");
        Add("Initiative.Panel.SeesAfter.Name", "Naam", "Name");
        Add("Initiative.Panel.SeesAfter.Motivation", "Motivatie", "Motivation");
        Add("Initiative.Panel.SeesAfter.Experience", "Werkervaring", "Work experience");
        Add("Initiative.Panel.SeesAfter.Contact", "Contact", "Contact");
        Add("Initiative.Panel.Deadline", "Het bedrijf reageert binnen 5 werkdagen. Hoor je niets, dan wordt het vanzelf Nu niet. Het zegt niets over jou.", "The company responds within 5 business days. If you hear nothing, it becomes Not now automatically. It says nothing about you.");
        Add("Common.Change", "Wijzigen", "Change");
        Add("Initiative.Panel.Send", "Verstuur bericht", "Send message");
        Add("Initiative.Panel.PrivacyHint", "Naam en contactgegevens zie je pas na acceptatie.", "Name and contact details are visible only after acceptance.");
        Add("Initiative.Sent.Title", "Bericht verstuurd", "Message sent");
        Add("Initiative.Sent.Step1", "Verstuurd", "Sent");
        Add("Initiative.Sent.Step2", "Bekeken", "Viewed");
        Add("Initiative.Sent.Step3", "Reactie", "Response");
        Add("Initiative.Sent.Lead", "Je ontvangt bericht zodra het bedrijf reageert.", "You will hear back when the company responds.");
        Add("Initiative.WeekLimit.SendMonday", "Maandag met één tik versturen", "Send Monday with one tap");
        Add("CompanyPage.About", "Over ons", "About us");
        Add("CompanyPage.Sfeer", "Sfeer", "Atmosphere");
        Add("CompanyPage.NoVacancies", "Laat het me weten bij een nieuwe vacature", "Notify me when a new vacancy opens");
        Add("CompanyPage.MapVacancies", "Kaart met de vacatures", "Map with vacancies");
        Add("Bk.Apps.SourceInitiative", "Initiatief", "Initiative");
        Add("Bk.Apps.SourceVacancy", "Vacature", "Vacancy");
        Add("Initiative.Employer.OpenLabel", "OPEN", "OPEN");
        Add("Initiative.Employer.Accept", "Accepteren", "Accept");
        Add("Initiative.Employer.AcceptWithToken", "Accepteren · {0} token", "Accept · {0} token");
        Add("Initiative.Employer.Decline", "Nu niet", "Not now");
        Add("Initiative.Employer.DeclineLead", "Je geeft aan dat je deze kandidaat nu niet verder wilt. Het initiatief kan open blijven.", "You are passing on this candidate for now. The initiative can stay open.");
        Add("Initiative.Employer.DeclineKeepOpen", "Open houden voor andere kandidaten", "Keep open for other candidates");
        Add("Initiative.Settings.TeamsEmpty", "Nog geen teams toegevoegd.", "No teams added yet.");
        Add("Initiative.Settings.Lead", "Kandidaten kunnen zonder vacature kloppen. Je reageert binnen 5 werkdagen.", "Candidates can reach out without a vacancy. You respond within 5 business days.");
        Add("Initiative.Settings.GeneralEntry", "Algemene ingang", "General entry");
        Add("Initiative.Settings.ContactEmail", "E-mailadres contactpersoon", "Contact person email");
        Add("Initiative.Settings.ContactEmailHint", "Bevestig dit adres via de mail.", "Confirm this address via email.");
        Add("Initiative.Settings.MaxPerWeek", "Max per week", "Max per week");
        Add("Initiative.Settings.Teams", "Teams", "Teams");
        Add("Initiative.Settings.TeamAdd", "Team toevoegen", "Add team");
        Add("Initiative.Settings.Save", "Opslaan", "Save");
        Add("Initiative.Settings.TeamOpen", "Open voor initiatief", "Open for initiative");
        Add("Initiative.Settings.TeamEmailConfirm", "Bevestig het team-e-mailadres voordat je dit team opent.", "Confirm the team email before opening this team.");
        Add("Initiative.TeamAdd.Title", "Team toevoegen", "Add team");
        Add("Initiative.TeamAdd.Name", "Naam", "Name");
        Add("Initiative.TeamAdd.Description", "Omschrijving (max 140)", "Description (max 140)");
        Add("Initiative.TeamAdd.Email", "E-mail contactpersoon", "Contact email");
        Add("Initiative.EmailMissing.Title", "E-mailadres ontbreekt", "Email address missing");
        Add("Initiative.EmailMissing.Body", "Vul een geldig e-mailadres in voordat je dit team opent voor initiatief.", "Enter a valid email before opening this team for initiative.");
        Add("Initiative.Inbox.NewToday", "Nieuw · vandaag", "New · today");
        Add("Initiative.Inbox.NewOneDayAgo", "Nieuw · {0} dag geleden", "New · {0} day ago");
        Add("Initiative.Inbox.NewDaysAgo", "Nieuw · {0} dagen geleden", "New · {0} days ago");
    }
}
