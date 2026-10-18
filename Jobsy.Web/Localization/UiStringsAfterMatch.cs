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
        Add("AfterMatch.Screen13.FeedbackTitle", "Wat Bakkerij De Korenaar zegt", "What Bakkerij De Korenaar says");
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

        Add("AfterMatch.Demo.Team", "Team Bakkerij", "Bakery team");
        Add("AfterMatch.Employer.YesOn", "ja gezegd op {0}", "said yes on {0}");
        Add("AfterMatch.Employer.Share.Title", "Wat Sanne deelt", "What Sanne shares");
        Add("AfterMatch.Employer.Share.Passport", "Paspoort, met onderschreven competenties", "Passport with endorsed competencies");
        Add("AfterMatch.Employer.Share.Phone", "Telefoon zie je na het gesprek, als Sanne dat wil", "Phone after the interview if Sanne agrees");
        Add("AfterMatch.Employer.Share.Bank", "Bankgegevens pas na een aanbod, voor je eigen administratie", "Bank details only after an offer");
        Add("AfterMatch.Employer.WhatNow.Slots.Title", "Nu: momenten voorstellen", "Now: propose times");
        Add("AfterMatch.Employer.WhatNow.Slots.Body", "Stel 2 of 3 momenten voor, of deel je agenda. Doe het liefst binnen twee werkdagen.", "Propose 2 or 3 times or share your calendar within two working days.");
        Add("AfterMatch.Employer.WhatNow.Interview.Body", "Lobsy stuurt jullie allebei een bevestiging en herinneringen.", "Lobsy sends both of you a confirmation and reminders.");
        Add("AfterMatch.Employer.WhatNow.Outcome.Body", "Na het gesprek geef je de uitkomst met een korte reden.", "After the interview record the outcome with a short reason.");
        Add("AfterMatch.Employer.Propose.Title", "Momenten voorstellen", "Propose times");
        Add("AfterMatch.Employer.Propose.Lead", "Kies 2 of 3 momenten.", "Pick 2 or 3 times.");
        Add("AfterMatch.Employer.Propose.Manual", "Zelf momenten kiezen", "Pick times yourself");
        Add("AfterMatch.Employer.Propose.ShareAgenda", "Mijn agenda delen", "Share my calendar");
        Add("AfterMatch.Employer.Propose.PickHint", "Klik in de weekweergave om momenten te selecteren (2–3).", "Click the week view to select 2–3 times.");
        Add("AfterMatch.Employer.Propose.AgendaSoon", "Agenda koppelen komt later. Gebruik intussen de weekweergave om handmatig momenten te kiezen.", "Calendar linking comes later. Use the week view to pick times manually for now.");
        Add("AfterMatch.Employer.Propose.Where", "Locatie of link", "Location or link");
        Add("AfterMatch.Employer.Propose.WithWhom", "Met wie", "With whom");
        Add("AfterMatch.Employer.Propose.Note", "Bericht erbij (mag kort)", "Short note (optional)");
        Add("AfterMatch.Employer.Action.Propose", "Momenten voorstellen", "Propose times");
        Add("AfterMatch.Employer.Action.Messages", "Bericht sturen", "Send message");
        Add("AfterMatch.Employer.Expectation", "Sanne verwacht momenten voor donderdag 15 oktober", "Sanne expects times before Thursday 15 October");

        Add("AfterMatch.Mail.CandidateSubject", "Onderwerp: Je gesprek bij Bakkerij De Korenaar staat erin", "Subject: Your interview at Bakkerij De Korenaar is booked");
        Add("AfterMatch.Mail.EmployerSubject", "Onderwerp: Gesprek met {0} gepland", "Subject: Interview with {0} scheduled");
        Add("AfterMatch.Mail.PlannedTag", "Gesprek gepland", "Interview scheduled");
        Add("AfterMatch.Mail.CandidateTitle", "Je gesprek staat erin, {0}", "Your interview is booked, {0}");
        Add("AfterMatch.Mail.CandidateLead", "Fijn dat het gelukt is. Hieronder staat alles op een rij. Je hoeft niets voor te bereiden.", "Glad it worked out. Everything is below. No preparation needed.");
        Add("AfterMatch.Mail.When", "Wanneer", "When");
        Add("AfterMatch.Mail.Where", "Waar", "Where");
        Add("AfterMatch.Mail.With", "Met wie", "With whom");
        Add("AfterMatch.Mail.Travel", "Reistijd", "Travel time");
        Add("AfterMatch.Mail.Candidate", "Kandidaat", "Candidate");
        Add("AfterMatch.Mail.After", "Na afloop", "Afterwards");
        Add("AfterMatch.Mail.AfterBody", "Geef binnen 5 werkdagen de uitkomst", "Record the outcome within 5 working days");
        Add("AfterMatch.Mail.EmployerTitle", "Sanne komt donderdag", "Sanne is coming Thursday");
        Add("AfterMatch.Mail.EmployerLead", "Sanne koos een van je momenten. Het gesprek staat ook in Lobsy onder Gesprekken.", "Sanne picked one of your times. The interview is also in Lobsy under Interviews.");
        Add("AfterMatch.Mail.OutlookTitle", "Zo ziet het agenda-item eruit in Outlook", "How the calendar item looks in Outlook");
        Add("AfterMatch.Mail.OutlookSave", "Opslaan en sluiten", "Save and close");
        Add("AfterMatch.Mail.OutlookDelete", "Verwijderen", "Delete");
        Add("AfterMatch.Mail.OutlookBusy", "Beschikbaarheid: Bezet", "Show as: Busy");
        Add("AfterMatch.Mail.OutlookEventTitle", "Gesprek {0} (via Lobsy)", "Interview {0} (via Lobsy)");
        Add("AfterMatch.Mail.OutlookStart", "Begin", "Start");
        Add("AfterMatch.Mail.OutlookEnd", "Einde", "End");
        Add("AfterMatch.Mail.OutlookReminder", "Herinnering", "Reminder");
        Add("AfterMatch.Mail.OutlookReminderVal", "2 uur van tevoren", "2 hours before");
        Add("AfterMatch.Mail.OutlookBody", "Kennismaking voor {0} met {1}.", "Introduction for {0} with {1}.");

        Add("AfterMatch.WhatNext.ReminderDay", "Woensdag 21 oktober, 18:00", "Wednesday 21 October, 18:00");
        Add("AfterMatch.WhatNext.ReminderDayBody", "Herinnering per mail en melding, met route en tips voor je gesprek", "E-mail and push reminder with route and tips");
        Add("AfterMatch.WhatNext.Reminder2h", "Donderdag 22 oktober, 8:30", "Thursday 22 October, 8:30");
        Add("AfterMatch.WhatNext.Reminder2hBody", "Melding 2 uur van tevoren, met je reistijd op de fiets", "Notification 2 hours ahead with bike travel time");
        Add("AfterMatch.WhatNext.Interview", "Donderdag 22 oktober, 10:30", "Thursday 22 October, 10:30");
        Add("AfterMatch.WhatNext.InterviewBody", "Het gesprek met Linda bij Bakkerij De Korenaar", "The interview with Linda at Bakkerij De Korenaar");
        Add("AfterMatch.WhatNext.Outcome", "Binnen 5 werkdagen daarna", "Within 5 working days after");
        Add("AfterMatch.WhatNext.OutcomeBody", "Je hoort de uitkomst, met een korte reden. Ook als het nee is.", "You hear the outcome with a short reason, even when it is no.");

        Add("AfterMatch.Screen06.PageTitle", "Bevestiging voor allebei, met agenda-item", "Confirmation for both, with calendar item");
        Add("AfterMatch.Screen07.PageTitle", "Herinneringen", "Reminders");
        Add("AfterMatch.Employer.Reschedule.Note", "Sanne vroeg om een ander moment. Hieronder zie je de reden en het voorstel.", "Sanne asked for another time. See the reason and proposal below.");
        Add("AfterMatch.Employer.Reschedule.Accept", "Akkoord met voorstel", "Accept proposal");
        Add("AfterMatch.Employer.Reschedule.Counter", "Ander moment voorstellen", "Propose another time");
        Add("AfterMatch.Employer.Outcome.PreviewTitle", "Zo ziet Sanne het", "How Sanne sees it");
        Add("AfterMatch.Employer.Outcome.PreviewLead", "Dank je wel voor het gesprek. Hier komt je korte uitleg.", "Thank you for the interview. Your short explanation appears here.");
    }
}
