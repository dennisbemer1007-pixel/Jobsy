namespace Jobsy.Web.Localization;

/// <summary>Na de match UI copy (NL + EN; pl/ro/ar mirror EN).</summary>
public static class UiStringsAfterMatch
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
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

        Add("AfterMatch.Nav.Eyebrow", "Sollicitaties · Na de match", "Applications · After the match");
        Add("AfterMatch.Link.Open", "Naar gesprek en berichten", "Go to interview and messages");
        Add("AfterMatch.Link.BackApplications", "Terug naar sollicitaties", "Back to applications");

        Add("AfterMatch.Timeline.Yes", "Ja", "Yes");
        Add("AfterMatch.Timeline.Plan", "Gesprek plannen", "Plan interview");
        Add("AfterMatch.Timeline.Interview", "Gesprek", "Interview");
        Add("AfterMatch.Timeline.Outcome", "Uitkomst", "Outcome");
        Add("AfterMatch.Timeline.Now", "nu", "now");

        Add("AfterMatch.What.Plan.Title", "Gesprek plannen", "Plan the interview");
        Add("AfterMatch.What.Plan.Body",
            "Linda stelt binnen twee werkdagen een paar momenten voor. Jij kiest er één.",
            "Linda will suggest a few times within two working days. You pick one.");
        Add("AfterMatch.What.Interview.Title", "Gesprek", "Interview");
        Add("AfterMatch.What.Interview.Body",
            "Een kennismaking van ongeveer 30 minuten. We sturen je een dag van tevoren een herinnering.",
            "An introduction of about 30 minutes. We send a reminder the day before.");
        Add("AfterMatch.What.Outcome.Title", "Uitkomst", "Outcome");
        Add("AfterMatch.What.Outcome.Body",
            "Je hoort altijd wat de uitkomst is, ook als het nee is. Met een korte uitleg.",
            "You always hear the outcome, even when it is no. With a short explanation.");

        Add("AfterMatch.Screen01.Hero.Title", "Bakkerij De Korenaar zei ja", "Bakkerij De Korenaar said yes");
        Add("AfterMatch.Screen01.Hero.Lead",
            "Mooi, Sanne. Zo gaat het verder. Je ziet steeds waar je staat en wat je kunt verwachten.",
            "Nice, Sanne. This is what happens next. You always see where you are and what to expect.");
        Add("AfterMatch.Screen01.Wait.Title", "Wacht op momenten van Linda", "Waiting for times from Linda");
        Add("AfterMatch.Screen01.Wait.Body",
            "Je krijgt een melding zodra ze er zijn. Meestal binnen twee werkdagen.",
            "You get a notification when they are ready. Usually within two working days.");
        Add("AfterMatch.Screen01.Preference", "Mijn voorkeur doorgeven", "Share my preference");

        Add("AfterMatch.Screen02.Hero.Title", "Na de match", "After the match");
        Add("AfterMatch.Screen02.Hero.Lead",
            "Plan het gesprek, houd contact en leg de uitkomst vast. Alles bij deze sollicitatie.",
            "Plan the interview, stay in touch and record the outcome. All in one place for this application.");
        Add("AfterMatch.Screen02.Next.Title", "Wat nu", "What now");
        Add("AfterMatch.Screen02.Next.Slots", "Stel 2 of 3 momenten voor", "Propose 2 or 3 times");
        Add("AfterMatch.Screen02.Next.Messages", "Stuur een bericht", "Send a message");

        Add("AfterMatch.Screen03.Hero.Title", "Gesprek plannen", "Plan interview");
        Add("AfterMatch.Screen03.Hero.Lead",
            "Kies hoe je wilt afspreken en stel twee of drie momenten voor.",
            "Choose how you want to meet and propose two or three times.");
        Add("AfterMatch.Screen03.Format.OnSite", "Op locatie", "On site");
        Add("AfterMatch.Screen03.Format.Online", "Online", "Online");
        Add("AfterMatch.Screen03.Format.Phone", "Telefoon", "Phone");
        Add("AfterMatch.Screen03.Submit", "Momenten versturen", "Send times");

        Add("AfterMatch.Screen04.Hero.Title", "Kies een moment voor je gesprek", "Pick a time for your interview");
        Add("AfterMatch.Screen04.Hero.Lead",
            "Linda stelde deze momenten voor. Kies er één of stel zelf een ander moment voor.",
            "Linda proposed these times. Pick one or suggest another time yourself.");
        Add("AfterMatch.Screen04.Confirm", "Dit moment bevestigen", "Confirm this time");
        Add("AfterMatch.Screen04.ProposeOther", "Ander moment voorstellen", "Suggest another time");

        Add("AfterMatch.Screen05.Hero.Title", "Je gesprek staat erin", "Your interview is booked");
        Add("AfterMatch.Screen05.Hero.Lead",
            "Je krijgt een bevestiging per mail. Voeg het moment toe aan je agenda.",
            "You receive a confirmation by e-mail. Add the time to your calendar.");
        Add("AfterMatch.Screen05.Calendar", "Toevoegen aan agenda", "Add to calendar");
        Add("AfterMatch.Screen05.Reminders", "Herinneringen instellen", "Set reminders");

        Add("AfterMatch.Screen06.Crumb", "Voorbeelden van bevestigingsmails", "Confirmation e-mail examples");
        Add("AfterMatch.Screen06.Title", "Wat jij en de werkgever in de mail zien", "What you and the employer see in e-mail");
        Add("AfterMatch.Screen06.CandidateMail", "Mail aan kandidaat", "E-mail to candidate");
        Add("AfterMatch.Screen06.EmployerMail", "Mail aan werkgever", "E-mail to employer");
        Add("AfterMatch.Screen06.After.Title", "Wat er hierna gebeurt", "What happens next");
        Add("AfterMatch.Screen06.After.Body",
            "Beide partijen kunnen het gesprek verzetten of afzeggen met een korte reden. Berichten blijven in Lobsy.",
            "Both sides can reschedule or cancel with a short reason. Messages stay in Lobsy.");

        Add("AfterMatch.Screen07.Crumb", "Voorbeelden van herinneringen", "Reminder examples");
        Add("AfterMatch.Screen07.Title", "De dag ervoor en 2 uur van tevoren", "The day before and 2 hours ahead");
        Add("AfterMatch.Screen07.EmailLabel", "E-mail de dag ervoor, 18:00", "E-mail the day before, 18:00");
        Add("AfterMatch.Screen07.PushLabel", "Meldingen op je telefoon", "Notifications on your phone");
        Add("AfterMatch.Screen07.Settings.Title", "Zelf instellen", "Set yourself");
        Add("AfterMatch.Screen07.Settings.Lead",
            "Bij je gesprek in Lobsy kies je wanneer en hoe je herinnerd wordt.",
            "On your interview page in Lobsy you choose when and how you are reminded.");
        Add("AfterMatch.Screen07.Pref.DayBefore", "De dag ervoor, e-mail en melding", "The day before, e-mail and notification");
        Add("AfterMatch.Screen07.Pref.TwoHours", "2 uur van tevoren, melding met reistijd", "2 hours ahead, notification with travel time");
        Add("AfterMatch.Screen07.Pref.WeekBefore", "Een week van tevoren, e-mail", "One week ahead, e-mail");

        Add("AfterMatch.Screen08.Hero.Title", "Verzetten of afzeggen", "Reschedule or cancel");
        Add("AfterMatch.Screen08.Hero.Lead",
            "Dat kan altijd. Geef een korte reden, dan ziet de ander wat er aan de hand is.",
            "You can always do that. Give a short reason so the other person understands.");
        Add("AfterMatch.Screen08.Reschedule", "Verzetten", "Reschedule");
        Add("AfterMatch.Screen08.Cancel", "Afzeggen", "Cancel");
        Add("AfterMatch.Screen08.Reason", "Korte reden", "Short reason");

        Add("AfterMatch.Screen09.Hero.Title", "Verzoek om te verzetten", "Request to reschedule");
        Add("AfterMatch.Screen09.Hero.Lead",
            "Sanne vroeg om een ander moment. Reageer met nieuwe tijden of een bericht.",
            "Sanne asked for another time. Reply with new times or a message.");

        Add("AfterMatch.Screen10.Hero.Title", "Berichten", "Messages");
        Add("AfterMatch.Screen10.Hero.Lead",
            "Eén draad per sollicitatie. Je ziet wanneer iets gelezen is.",
            "One thread per application. You see when something was read.");
        Add("AfterMatch.Screen10.Compose", "Schrijf een bericht", "Write a message");
        Add("AfterMatch.Screen10.Privacy",
            "Je telefoonnummer deel je pas na het gesprek, je bankgegevens pas na een aanbod. Zo blijft het veilig.",
            "You share your phone number only after the interview and bank details only after an offer. That keeps it safe.");
        Add("AfterMatch.Screen10.SysContact",
            "Contactgegevens gedeeld na jouw ja", "Contact details shared after your yes");

        Add("AfterMatch.Screen11.Hero.Title", "Berichten", "Messages");
        Add("AfterMatch.Screen11.Hero.Lead",
            "Snelle antwoorden en één overzicht met de kandidaat.",
            "Quick replies and one overview with the candidate.");
        Add("AfterMatch.Screen11.QuickReplies", "Snelle antwoorden", "Quick replies");

        Add("AfterMatch.Screen12.Hero.Title", "Uitkomst van het gesprek", "Outcome of the interview");
        Add("AfterMatch.Screen12.Hero.Lead",
            "Leg vast wat de volgende stap is. De kandidaat ziet je reden.",
            "Record what the next step is. The candidate sees your reason.");
        Add("AfterMatch.Screen12.Offer", "Aanbod", "Offer");
        Add("AfterMatch.Screen12.NextRound", "Nog een gesprek", "Another interview");
        Add("AfterMatch.Screen12.NotNow", "Nu niet", "Not now");
        Add("AfterMatch.Screen12.Reason", "Korte uitleg voor de kandidaat", "Short explanation for the candidate");
        Add("AfterMatch.Screen12.Submit", "Uitkomst versturen", "Send outcome");

        Add("AfterMatch.Screen13.Hero.Title", "Dank je wel voor het gesprek", "Thank you for the interview");
        Add("AfterMatch.Screen13.Hero.Lead",
            "Het is nu niet doorgegaan. Hieronder zie je wat Linda schreef en banen die erop lijken.",
            "It did not work out this time. Below you see what Linda wrote and jobs that look similar.");
        Add("AfterMatch.Screen13.Similar", "Banen die erop lijken", "Jobs that look similar");
        Add("AfterMatch.Screen13.Next", "Volgende stap", "Next step");

        Add("AfterMatch.Messages.NewPill", "1 nieuw", "1 new");
        Add("AfterMatch.Messages.ReadBy", "Gelezen door {0}", "Read by {0}");
        Add("AfterMatch.Messages.SensitiveWarning",
            "Dit lijkt op contactgegevens. Deel ze pas wanneer Lobsy dat toestaat.",
            "This looks like contact details. Share them only when Lobsy allows it.");
        Add("AfterMatch.Messages.SendAnyway", "Toch versturen", "Send anyway");
        Add("AfterMatch.Messages.BlockedEmployer",
            "Dit bericht kan nu niet worden verstuurd. Vraag geen bankgegevens vóór een aanbod.",
            "This message cannot be sent now. Do not ask for bank details before an offer.");

        Add("AfterMatch.Demo.Company", "Bakkerij De Korenaar", "Bakkerij De Korenaar");
        Add("AfterMatch.Demo.Vacancy", "Medewerker bakkerij", "Bakery assistant");
        Add("AfterMatch.Demo.Location", "Loosduinseweg 120, Den Haag · 9 min fietsen", "Loosduinseweg 120, The Hague · 9 min by bike");
        Add("AfterMatch.Demo.Contact", "Linda de Wit", "Linda de Wit");

        Add("AfterMatch.Error.Load", "Gesprek laden mislukt. Probeer het later opnieuw.", "Could not load the interview. Try again later.");
        Add("AfterMatch.Error.NotFound", "Geen gesprek gevonden voor deze sollicitatie.", "No interview found for this application.");
    }
}
