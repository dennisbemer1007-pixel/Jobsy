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
        Add("AfterMatch.Link.BackBerichten", "Terug naar berichten", "Back to messages");
        Add("AfterMatch.Link.BackPickSlot", "Terug naar sollicitaties", "Back to applications");

        Add("AfterMatch.Timeline.Yes", "Ja", "Yes");
        Add("AfterMatch.Timeline.Plan", "Gesprek plannen", "Plan interview");
        Add("AfterMatch.Timeline.Interview", "Gesprek", "Interview");
        Add("AfterMatch.Timeline.Outcome", "Uitkomst", "Outcome");
        Add("AfterMatch.Timeline.Now", "nu", "now");
        Add("AfterMatch.Timeline.PlanNow", "22 okt", "22 Oct");
        Add("AfterMatch.Timeline.PlanWaiting",
            "momenten verstuurd, wacht op de kandidaat",
            "times sent, waiting for the candidate");
        Add("AfterMatch.Employer.Timeline.OutcomeDone", "Verstuurd", "Sent");

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
            "Mooi. Zo gaat het verder. Je ziet steeds waar je staat en wat je kunt verwachten.",
            "Nice. This is what happens next. You always see where you are and what to expect.");
        Add("AfterMatch.Screen01.Hero.LeadCompact",
            "Mooi. Zo gaat het verder.",
            "Nice. This is what happens next.");
        Add("AfterMatch.Screen01.Wait.Title", "Wacht op momenten van Linda", "Waiting for times from Linda");
        Add("AfterMatch.Screen01.Wait.Body",
            "Je krijgt een melding zodra ze er zijn. Meestal binnen twee werkdagen.",
            "You get a notification when they are ready. Usually within two working days.");
        Add("AfterMatch.Screen01.Preference", "Mijn voorkeur doorgeven", "Share my preference");

        Add("AfterMatch.Screen02.Hero.Title", "Na de match", "After the match");
        Add("AfterMatch.Screen02.Hero.Lead",
            "Jullie zeiden allebei ja. Dit is de volgende stap.",
            "You both said yes. Here is the next step.");
        Add("AfterMatch.Screen02.Next.Title", "Wat nu", "What now");
        Add("AfterMatch.Screen02.Next.Slots", "Stel 2 of 3 momenten voor", "Propose 2 or 3 times");
        Add("AfterMatch.Screen02.Next.Messages", "Stuur een bericht", "Send a message");

        Add("AfterMatch.Screen03.Hero.Title", "Gesprek plannen", "Plan interview");
        Add("AfterMatch.Screen03.Hero.Lead",
            "Stel een paar momenten voor. De kandidaat kiest er één.",
            "Propose a few times. Sanne will pick one.");
        Add("AfterMatch.Screen03.Format.OnSite", "Op locatie", "On site");
        Add("AfterMatch.Screen03.Format.Online", "Online", "Online");
        Add("AfterMatch.Screen03.Format.Phone", "Telefonisch", "Phone");
        Add("AfterMatch.Screen03.Submit", "Verstuur momenten", "Send times");

        Add("AfterMatch.Screen04.Eyebrow", "Sollicitaties · Gesprek plannen", "Applications · Plan interview");
        Add("AfterMatch.Screen04.PageTitle", "Gesprek plannen", "Plan interview");
        Add("AfterMatch.Screen04.Hero.Title", "Kies een moment voor je gesprek", "Pick a time for your interview");
        Add("AfterMatch.Screen04.SlotsLabel", "Welk moment past jou?", "Which time works for you?");
        Add("AfterMatch.Screen04.ChooseSlot", "Dit moment kiezen", "Choose this time");
        Add("AfterMatch.Screen04.NoSlotFits", "Past geen van deze?", "None of these work?");
        Add("AfterMatch.Screen04.Hero.Lead",
            "Linda stelde deze momenten voor. Kies er één of stel zelf een ander moment voor.",
            "Linda proposed these times. Pick one or suggest another time yourself.");
        Add("AfterMatch.Screen04.Hero.LeadPick",
            "{0} van {1} stelt {2} momenten voor. Kies wat jou het beste uitkomt.",
            "{0} at {1} proposes {2} times. Pick what works best for you.");
        Add("AfterMatch.Screen04.Hero.LeadCompact",
            "Kies een moment of stel zelf een ander voor.",
            "Pick a time or suggest another yourself.");
        Add("AfterMatch.Screen04.Confirm", "Dit moment bevestigen", "Confirm this time");
        Add("AfterMatch.Screen04.ProposeOther", "Ander moment voorstellen", "Suggest another time");
        Add("AfterMatch.Pick.OnSiteTitle", "Op locatie", "On site");
        Add("AfterMatch.Pick.WithWhomHint", "Contact bij {0}", "Contact at {0}");
        Add("AfterMatch.Pick.TravelFallbackTitle", "Reistijd", "Travel time");
        Add("AfterMatch.Pick.TravelHint", "Op basis van je profiel", "Based on your profile");
        Add("AfterMatch.Pick.ProposeLead", "Linda krijgt je voorstel en laat weten of het past.", "They will reply whether your time works.");
        Add("AfterMatch.Pick.ProposeSubmit", "Voorstel versturen", "Send proposal");
        Add("AfterMatch.Pick.ProposeSubmitShort", "Voorstel sturen", "Send proposal");
        Add("AfterMatch.Pick.ProposeFormTitle", "Een ander moment voorstellen", "Suggest another time");
        Add("AfterMatch.Pick.ProposeDateLabel", "Datum", "Date");
        Add("AfterMatch.Pick.ProposeTimeLabel", "Tijd", "Time");
        Add("AfterMatch.Pick.ProposeNotePlaceholder",
            "Op donderdag en vrijdag breng ik mijn zoon naar school.",
            "On Thursday and Friday I take my son to school.");
        Add("AfterMatch.Pick.ProposeDatePlaceholder", "Dinsdag 27 oktober", "Tuesday 27 October");
        Add("AfterMatch.Pick.ProposeTimePlaceholder", "10:00-10:30", "10:00-10:30");
        Add("AfterMatch.Pick.ProposeError.Required", "Vul datum en tijd in.", "Enter date and time.");
        Add("AfterMatch.Pick.ProposeError.Date", "De datum herkennen we niet. Gebruik bijvoorbeeld: dinsdag 27 oktober.", "We could not read that date. Try e.g. Tuesday 27 October.");
        Add("AfterMatch.Pick.ProposeError.Time", "De tijd herkennen we niet. Gebruik bijvoorbeeld: 10:00-10:30.", "We could not read that time. Try e.g. 10:00-10:30.");
        Add("AfterMatch.Pick.ProposeError.Past", "Kies een moment in de toekomst.", "Pick a time in the future.");
        Add("AfterMatch.Pick.Breadcrumb", "Kruimelpad", "Breadcrumb");
        Add("AfterMatch.Pick.GoodToKnow.Title", "Goed om te weten", "Good to know");
        Add("AfterMatch.Pick.GoodToKnow.Reminders",
            "Je krijgt een herinnering de dag ervoor en 2 uur van tevoren.",
            "You get a reminder the day before and 2 hours ahead.");
        Add("AfterMatch.Pick.GoodToKnow.TipsLink", "Tips voor je gesprek", "Tips for your interview");
        Add("AfterMatch.Pick.GoodToKnow.TipsSuffix", ", of oefen het gesprek met Lobsy.", ", or practise with Lobsy.");
        Add("AfterMatch.Pick.GoodToKnow.Uwv",
            "Het gesprek komt vanzelf in je activiteitenoverzicht voor UWV.",
            "The interview appears in your UWV activity overview.");

        Add("AfterMatch.Screen05.PageTitle", "Gesprek gepland", "Interview scheduled");
        Add("AfterMatch.Screen05.Hero.Title", "Je gesprek staat erin", "Your interview is booked");
        Add("AfterMatch.Screen05.Hero.LeadDetail",
            "{0} bij {1}. We hebben {2} laten weten dat je komt.",
            "{0} at {1}. We let {2} know you are coming.");
        Add("AfterMatch.Screen05.Hero.Lead",
            "Je krijgt een bevestiging per mail. Voeg het moment toe aan je agenda.",
            "You receive a confirmation by e-mail. Add the time to your calendar.");
        Add("AfterMatch.Screen05.ScheduledSub", "Op locatie, {0} · met {1}", "On site, {0} · with {1}");
        Add("AfterMatch.Screen05.ScheduledSubOnline", "{0} · met {1}", "{0} · with {1}");
        Add("AfterMatch.Screen05.CalendarRow", "Zet in je agenda", "Add to your calendar");
        Add("AfterMatch.Screen05.Outlook", "Outlook", "Outlook");
        Add("AfterMatch.Screen05.GoogleCal", "Google Agenda", "Google Calendar");
        Add("AfterMatch.Screen05.AppleCal", "Apple Agenda", "Apple Calendar");
        Add("AfterMatch.Screen05.Ics", "Agendabestand (.ics)", "Calendar file (.ics)");
        Add("AfterMatch.Screen05.Route", "Route bekijken", "View route");
        Add("AfterMatch.Screen05.RemindersTitle", "Herinneringen", "Reminders");
        Add("AfterMatch.Screen05.RemindersLead", "Jij kiest wanneer en hoe.", "You choose when and how.");
        Add("AfterMatch.Screen05.Reminder.DayBeforeTitle", "De dag ervoor, om 18:00", "The day before, at 18:00");
        Add("AfterMatch.Screen05.Reminder.DayBeforeSub", "E-mail en melding", "E-mail and notification");
        Add("AfterMatch.Screen05.Reminder.TwoHoursTitle", "2 uur van tevoren", "2 hours ahead");
        Add("AfterMatch.Screen05.Reminder.TwoHoursSub", "Melding met reistijd", "Notification with travel time");
        Add("AfterMatch.Screen05.Reminder.WeekBeforeTitle", "Een week van tevoren", "One week ahead");
        Add("AfterMatch.Screen05.Reminder.WeekBeforeSub", "E-mail", "E-mail");
        Add("AfterMatch.Screen05.TipsSuffix", "Oefen het gesprek met Lobsy, rustig en zonder oordeel.", "Practise the interview with Lobsy, calmly and without judgement.");
        Add("AfterMatch.Screen05.Calendar", "Toevoegen aan agenda", "Add to calendar");
        Add("AfterMatch.Screen05.Reminders", "Herinneringen instellen", "Set reminders");

        Add("AfterMatch.Screen06.Crumb", "Voorbeelden van de bevestiging", "Confirmation examples");
        Add("AfterMatch.Screen06.Title", "Wat jij en de werkgever in de mail zien", "What you and the employer see in e-mail");
        Add("AfterMatch.Screen06.LabelCandidate", "Kandidaat", "Candidate");
        Add("AfterMatch.Screen06.LabelEmployer", "Werkgever", "Employer");
        Add("AfterMatch.Screen06.CandidateMail", "Mail aan kandidaat", "E-mail to candidate");
        Add("AfterMatch.Screen06.EmployerMail", "Mail aan werkgever", "E-mail to employer");
        Add("AfterMatch.Screen06.CandidateMfootLead", "Lukt het toch niet?", "Still not working out?");
        Add("AfterMatch.Screen06.CandidateMfootReschedule", "Verzetten of afzeggen", "Reschedule or cancel");
        Add("AfterMatch.Screen06.CandidateMfootMid", "kan tot het gesprek.", "is possible until the interview.");
        Add("AfterMatch.Screen06.CandidateMfootTips", "Tips voor je gesprek", "Tips for your interview");
        Add("AfterMatch.Screen06.EmployerMfootLead", "Je krijgt de dag ervoor een herinnering.", "You get a reminder the day before.");
        Add("AfterMatch.Screen06.EmployerMfootLink", "Gesprek openen in Lobsy", "Open interview in Lobsy");
        Add("AfterMatch.Screen06.MfootBrand", "Lobsy · Den Haag", "Lobsy · The Hague");
        Add("AfterMatch.Screen06.PrivacyNote",
            "Je telefoonnummer deel je pas na het gesprek, je bankgegevens pas na een aanbod.",
            "You share your phone number only after the interview and bank details only after an offer.");
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
        Add("AfterMatch.Screen07.ReminderEmailSubject", "Onderwerp: Morgen je gesprek bij {0}", "Subject: Your interview at {0} is tomorrow");
        Add("AfterMatch.Screen07.ReminderEmailTitle", "Morgen is je gesprek", "Your interview is tomorrow");
        Add("AfterMatch.Screen07.ReminderEmailLead", "Hoi {0}, nog even op een rij. Je kunt het.", "Hi {0}, a quick recap. You've got this.");
        Add("AfterMatch.Screen07.ReminderEmailLeadMock",
            "Hoi {0}, nog even op een rij. Je kunt het. Linda kijkt ernaar uit om je te ontmoeten.",
            "Hi {0}, a quick recap. You've got this. Linda looks forward to meeting you.");
        Add("AfterMatch.Screen07.ReminderWhenMock", "Morgen, donderdag 22 oktober, 10:30", "Tomorrow, Thursday 22 October, 10:30");
        Add("AfterMatch.Screen07.ReminderWhenFallback", "Morgen is je gesprek", "Your interview is tomorrow");
        Add("AfterMatch.Screen07.ReminderWhereMock", "Loosduinseweg 120, ingang naast de winkel", "Loosduinseweg 120, entrance next to the shop");
        Add("AfterMatch.Screen07.ReminderTravelMock", "9 min fietsen. Vertrek uiterlijk 10:15", "9 min by bike. Leave by 10:15 at the latest");
        Add("AfterMatch.Screen07.ReminderWeatherMock", "Droog, 13 graden", "Dry, 13 degrees");
        Add("AfterMatch.Screen07.WeatherLabel", "Weer", "Weather");
        Add("AfterMatch.Screen07.BtnRoute", "Route bekijken", "View route");
        Add("AfterMatch.Screen07.BtnTips", "Tips voor je gesprek", "Tips for your interview");
        Add("AfterMatch.Screen07.BtnPractice", "Oefen met Lobsy", "Practise with Lobsy");
        Add("AfterMatch.Screen07.EmailMfootLead", "Herinneringen aanpassen kan bij je gesprek in Lobsy.", "You can change reminders on your interview page in Lobsy.");
        Add("AfterMatch.Screen07.PushTwoHoursTitle", "Over 2 uur je gesprek", "Your interview in 2 hours");
        Add("AfterMatch.Screen07.PushTwoHoursBodyMock", "Bakkerij De Korenaar, 10:30. Met de fiets ben je er in 9 minuten.", "Bakkerij De Korenaar, 10:30. It is 9 minutes by bike.");
        Add("AfterMatch.Screen07.PushTwoHoursBody", "{0}, 10:30.", "{0}, 10:30.");
        Add("AfterMatch.Screen07.PushDayBeforeTitle", "Morgen je gesprek", "Your interview tomorrow");
        Add("AfterMatch.Screen07.PushDayBeforeBodyMock", "Bekijk nog even de tips. Je hoeft niets mee te nemen.", "Skim the tips again. You do not need to bring anything.");
        Add("AfterMatch.Screen07.PushDayBeforeBody", "Bekijk nog even de tips.", "Skim the tips again.");
        Add("AfterMatch.Screen07.LockDateMock", "donderdag 22 oktober", "Thursday 22 October");
        Add("AfterMatch.Screen07.LockDateFallback", "donderdag 22 oktober", "Thursday 22 October");

        Add("AfterMatch.Screen08.Hero.Title", "Verzetten of afzeggen", "Reschedule or cancel");
        Add("AfterMatch.Screen08.Eyebrow", "Sollicitaties · Gesprek gepland", "Applications · Interview scheduled");
        Add("AfterMatch.Screen08.Hero.Lead",
            "Dat kan altijd. Geef een korte reden, dan ziet de ander wat er aan de hand is.",
            "You can always do that. Give a short reason so the other person understands.");
        Add("AfterMatch.Screen08.Hero.LeadDetail",
            "Dat kan gebeuren. Laat {0} weten waarom, dan zoeken jullie samen een nieuw moment.",
            "It happens. Let {0} know why, then you can find a new time together.");
        Add("AfterMatch.Screen08.Reschedule", "Verzetten", "Reschedule");
        Add("AfterMatch.Screen08.Cancel", "Afzeggen", "Cancel");
        Add("AfterMatch.Screen08.Reason", "Korte reden", "Short reason");
        Add("AfterMatch.Screen08.ReasonLong", "Waarom (verplicht, mag kort)", "Why (required, keep it short)");
        Add("AfterMatch.Screen08.ReasonPlaceholder",
            "Mijn zoon is ziek, ik kan donderdag niet weg.",
            "My son is sick, I cannot leave on Thursday.");
        Add("AfterMatch.Screen08.SideTitle.Reschedule", "Gesprek verzetten", "Reschedule interview");
        Add("AfterMatch.Screen08.SideTitle.Cancel", "Gesprek afzeggen", "Cancel interview");
        Add("AfterMatch.Screen08.SideLead", "{0} krijgt meteen bericht.", "{0} is notified right away.");
        Add("AfterMatch.Screen08.ReasonChip.Sick", "Ziek", "Sick");
        Add("AfterMatch.Screen08.ReasonChip.Family", "Zorg voor familie", "Family care");
        Add("AfterMatch.Screen08.ReasonChip.Transport", "Vervoer lukt niet", "Transport issue");
        Add("AfterMatch.Screen08.ReasonChip.OtherAppt", "Andere afspraak", "Other appointment");
        Add("AfterMatch.Screen08.ReasonChip.Other", "Iets anders", "Something else");
        Add("AfterMatch.Screen08.WhenLabel", "Wanneer kan het wel?", "When could it work?");
        Add("AfterMatch.Screen08.ProposeOther", "Ander moment voorstellen", "Suggest another time");
        Add("AfterMatch.Screen08.AlternateSlotHint", "{0} (een van de andere momenten)", "{0} (one of the other times)");
        Add("AfterMatch.Screen08.SendTo", "Verstuur naar {0}", "Send to {0}");
        Add("AfterMatch.Screen08.CancelConfirm", "Afzeggen bevestigen", "Confirm cancellation");
        Add("AfterMatch.Screen08.NeverMind", "Toch niet", "Never mind");
        Add("AfterMatch.Screen08.CancelFootnote",
            "Afzeggen telt niet tegen je. Je sollicitatie blijft gewoon staan.",
            "Cancelling does not count against you. Your application stays active.");
        Add("AfterMatch.Screen08.ScheduledSubOnSite", "Op locatie, {0}", "On site, {0}");
        Add("AfterMatch.Screen08.ScheduledSubOnline", "Online, {0}", "Online, {0}");

        Add("AfterMatch.Screen09.Hero.Title", "Verzoek om te verzetten", "Request to reschedule");
        Add("AfterMatch.Screen09.Hero.Lead",
            "Kies een nieuw moment of stel er zelf een voor.",
            "Pick a new time or propose one yourself.");

        Add("AfterMatch.Screen10.Hero.Title", "Berichten", "Messages");
        Add("AfterMatch.Screen10.Hero.Eyebrow", "Sollicitaties · Berichten", "Applications · Messages");
        Add("AfterMatch.Screen10.Hero.Lead",
            "Per sollicitatie een rustige plek om te praten. Jij bepaalt wanneer je antwoordt.",
            "One calm place to talk per application. You decide when to reply.");
        Add("AfterMatch.Screen10.SafeTitle", "Zo blijft het veilig.", "This keeps it safe.");
        Add("AfterMatch.Screen10.SafeBody",
            "Je telefoonnummer deel je na het gesprek, je bankgegevens pas na een aanbod. Vraagt iemand eerder om geld of je ID?",
            "You share your phone number after the interview and bank details only after an offer. Does someone ask for money or your ID earlier?");
        Add("AfterMatch.Screen10.ReportLink", "Meld het", "Report it");
        Add("AfterMatch.Screen10.ArchiveHint",
            "Gesprekken van afgeronde sollicitaties staan in",
            "Conversations from finished applications are in");
        Add("AfterMatch.Screen10.ArchiveLink", "Archief", "Archive");
        Add("AfterMatch.Screen10.PhoneWaitWarning",
            "Even wachten met je telefoonnummer. Je nummer deel je via Lobsy pas na het gesprek, je bankgegevens pas na een aanbod. Zo blijft het veilig voor jou. Wil je het toch nu sturen? Dat mag, het is jouw keuze.",
            "Hold off on sharing your phone number for now. Via Lobsy you share it only after the interview and bank details only after an offer. That keeps you safe. Still want to send it now? You may, it is your choice.");
        Add("AfterMatch.Screen10.ReplyPace",
            "Linda ziet je bericht wanneer het haar uitkomt. Je hoeft niet meteen te antwoorden, en zij ook niet.",
            "Linda sees your message when it suits her. You do not have to reply right away, and neither does she.");
        Add("AfterMatch.Screen10.Compose", "Schrijf een bericht", "Write a message");
        Add("AfterMatch.Screen10.Privacy",
            "Je telefoonnummer deel je pas na het gesprek, je bankgegevens pas na een aanbod. Zo blijft het veilig.",
            "You share your phone number only after the interview and bank details only after an offer. That keeps it safe.");
        Add("AfterMatch.Screen10.SysContact",
            "Contactgegevens gedeeld na jouw ja", "Contact details shared after your yes");

        Add("AfterMatch.Screen11.Hero.Title", "Berichten", "Messages");
        Add("AfterMatch.Screen11.Hero.Lead",
            "Per sollicitatie één draad. Kandidaten zien wanneer je iets hebt gelezen.",
            "One thread per application. Candidates see when you have read their messages.");
        Add("AfterMatch.Screen11.QuickReplies", "Snelle antwoorden", "Quick replies");
        Add("AfterMatch.Screen11.QuickReply1", "Tot donderdag", "See you Thursday");
        Add("AfterMatch.Screen11.QuickReply2", "Bedankt, ik kijk ernaar", "Thanks, I will look into it");
        Add("AfterMatch.Screen11.QuickReply3", "Kun je iets eerder komen?", "Can you come a bit earlier?");
        Add("AfterMatch.Screen11.SafeTitle", "Contactregels.", "Contact rules.");
        Add("AfterMatch.Screen11.SafeBody",
            "Telefoon van de kandidaat zie je na het gesprek, bankgegevens vraag je pas na een aanbod.",
            "You see the candidate's phone number after the interview; ask for bank details only after an offer.");
        Add("AfterMatch.Screen11.ArchiveHint",
            "Afgeronde sollicitaties staan in",
            "Closed applications are in");
        Add("AfterMatch.Screen11.ArchiveLink", "Archief", "Archive");

        Add("AfterMatch.Screen12.Hero.Title", "Uitkomst van het gesprek", "Outcome of the interview");
        Add("AfterMatch.Screen12.Hero.Lead",
            "Geef binnen 5 werkdagen de uitkomst, met een korte reden.",
            "Record the outcome within 5 working days, with a short reason.");
        Add("AfterMatch.Screen12.Question", "Hoe ging het gesprek?", "How did the interview go?");
        Add("AfterMatch.Screen12.Offer", "Aanbod", "Offer");
        Add("AfterMatch.Screen12.Offer.HintFmt", "Je wilt {0} een plek aanbieden", "You want to offer {0} a role");
        Add("AfterMatch.Screen12.NextRound", "Nog een gesprek", "Another interview");
        Add("AfterMatch.Screen12.NextRound.Hint", "Bijvoorbeeld een proefdag", "For example a trial day");
        Add("AfterMatch.Screen12.NotNow", "Nu niet", "Not now");
        Add("AfterMatch.Screen12.NotNow.Hint", "Het past nu niet", "It does not fit right now");
        Add("AfterMatch.Screen12.Reason", "Korte uitleg voor de kandidaat", "Short explanation for the candidate");
        Add("AfterMatch.Screen12.ReasonRequiredFmt", "Korte reden (verplicht, {0} ziet dit)", "Short reason (required, {0} sees this)");
        Add("AfterMatch.Screen12.ReasonChip.Experience", "Iemand met meer ervaring in de oven", "Someone with more oven experience");
        Add("AfterMatch.Screen12.ReasonChip.Hours", "Uren passen niet", "Hours do not match");
        Add("AfterMatch.Screen12.ReasonChip.Filled", "Plek al vervuld", "Role already filled");
        Add("AfterMatch.Screen12.ReasonChip.Other", "Iets anders", "Something else");
        Add("AfterMatch.Screen12.ReasonChip.OfferStart", "We starten met een proefperiode", "We start with a trial period");
        Add("AfterMatch.Screen12.ReasonChip.OfferHours", "Uren en salaris besproken", "Hours and pay discussed");
        Add("AfterMatch.Screen12.ReasonChip.OfferWelcome", "We kijken ernaar uit je te verwelkomen", "We look forward to welcoming you");
        Add("AfterMatch.Screen12.ReasonChip.OfferOther", "Iets anders", "Something else");
        Add("AfterMatch.Screen12.SubmittedNote", "De uitkomst is verstuurd. De kandidaat ziet je reden in de app.", "The outcome was sent. The candidate sees your reason in the app.");
        Add("AfterMatch.Screen12.Submit", "Uitkomst versturen", "Send outcome");
        Add("AfterMatch.Screen12.TalentPool", "Bewaren in talentpool", "Save to talent pool");

        Add("AfterMatch.Screen13.PageTitle", "Uitkomst", "Outcome");
        Add("AfterMatch.Screen13.Eyebrow", "Sollicitaties · Uitkomst", "Applications · Outcome");
        Add("AfterMatch.Screen13.Hero.Title", "Dank je wel voor het gesprek", "Thank you for the interview");
        Add("AfterMatch.Screen13.Hero.TitleOffer", "Gefeliciteerd, je bent aangenomen", "Congratulations, you got the job");
        Add("AfterMatch.Screen13.Hero.Lead",
            "Dit keer is het geen ja. Dat zegt niets over wie je bent. Je hebt een mooie stap gezet.",
            "This time it is not a yes. That says nothing about who you are. You took a meaningful step.");
        Add("AfterMatch.Screen13.Hero.LeadOffer",
            "De werkgever wil je aannemen. Lees hieronder wat ze met je delen.",
            "The employer wants to hire you. Read what they share with you below.");
        Add("AfterMatch.Screen13.Similar", "Banen die erop lijken", "Jobs that look similar");
        Add("AfterMatch.Screen13.FeedbackTitleFmt", "Wat {0} zegt", "What {0} says");
        Add("AfterMatch.Screen13.Next", "Volgende stap", "Next step");
        Add("AfterMatch.Screen13.WhatNow", "Wat nu?", "What now?");
        Add("AfterMatch.Screen13.WhatNowLead", "Deze banen lijken erop en zijn dichtbij.", "These jobs look similar and are nearby.");
        Add("AfterMatch.Screen13.ViewJob", "Bekijken", "View");
        Add("AfterMatch.Screen13.MoreJobs", "Meer banen zoals deze", "More jobs like these");
        Add("AfterMatch.Screen13.LearnCareer", "Leren en ontwikkelen", "Learn and grow");
        Add("AfterMatch.Screen13.ActivitiesPrefix", "Je sollicitatie en het gesprek staan al in ", "Your application and interview are already in ");
        Add("AfterMatch.Screen13.ActivitiesSuffix", ". Daar hoef je niets voor te doen.", ". You do not need to do anything for that.");
        Add("AfterMatch.Screen13.FeedbackAsideTitle", "Even iets kwijt?", "Need to share something?");
        Add("AfterMatch.Screen13.FeedbackAsideLead", "Open Lobsy-hulp, of praat erover met je begeleider als je die hebt.", "Open Lobsy help, or talk about it with your coach if you have one.");
        Add("AfterMatch.Screen13.FeedbackPrompt", "Hoe was het gesprek voor jou?", "How was the interview for you?");
        Add("AfterMatch.Screen13.FeedbackHint", "Alleen in woorden, gecontroleerd voor plaatsing.", "Words only, checked before posting.");
        Add("AfterMatch.Screen13.Chip.Friendly", "Vriendelijk", "Friendly");
        Add("AfterMatch.Screen13.Chip.Clear", "Duidelijk", "Clear");
        Add("AfterMatch.Screen13.Chip.Quick", "Snel antwoord", "Quick reply");
        Add("AfterMatch.Screen13.DemoReason",
            "Je was heel zorgvuldig en vriendelijk. We kozen nu voor iemand die al zelfstandig aan de oven kan staan.",
            "You were careful and friendly. We chose someone who can already work at the oven independently.");
        Add("AfterMatch.Screen13.FallbackReason",
            "Bedankt voor je tijd en je openheid tijdens het gesprek. We delen de exacte tekst van de werkgever zodra die beschikbaar is.",
            "Thank you for your time and openness during the interview. We will share the employer's exact message when it is available.");
        Add("AfterMatch.Screen13.FallbackVacancyTitle", "Sollicitatie", "Application");
        Add("AfterMatch.Screen13.FallbackCompany", "Werkgever", "Employer");
        Add("AfterMatch.Screen13.DemoJob1Initials", "BK", "BK");
        Add("AfterMatch.Screen13.DemoJob1Title", "Medewerker brood en banket", "Bread and pastry assistant");
        Add("AfterMatch.Screen13.DemoJob1Sub", "Bakker Bart Leyweg · 12 min fietsen", "Bakker Bart Leyweg · 12 min by bike");
        Add("AfterMatch.Screen13.DemoJob1Hint", "Hier leer je de oven op de werkvloer", "Here you learn the oven on the job");
        Add("AfterMatch.Screen13.DemoJob2Initials", "VK", "VK");
        Add("AfterMatch.Screen13.DemoJob2Title", "Verkoper versafdeling", "Fresh produce sales assistant");
        Add("AfterMatch.Screen13.DemoJob2Sub", "Supermarkt Loosduinen · 14 min fietsen", "Supermarket Loosduinen · 14 min by bike");
        Add("AfterMatch.Screen13.DemoJob2Hint", "Vriendelijk en zorgvuldig, net als jij", "Friendly and careful, just like you");

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
        Add("AfterMatch.Employer.Share.Title", "Wat de kandidaat deelt", "What the candidate shares");
        Add("AfterMatch.Employer.Share.Passport", "Paspoort, met onderschreven competenties", "Passport with endorsed competencies");
        Add("AfterMatch.Employer.Share.Phone", "Telefoon zie je na het gesprek, als de kandidaat dat wil", "Phone after the interview if the candidate agrees");
        Add("AfterMatch.Employer.Share.Bank", "Bankgegevens pas na een aanbod, voor je eigen administratie", "Bank details only after an offer");
        Add("AfterMatch.Employer.WhatNow.Slots.Title", "Nu: momenten voorstellen", "Now: propose times");
        Add("AfterMatch.Employer.SlotsSent.Title", "Momenten verstuurd, wacht op de kandidaat", "Times sent, waiting for the candidate");
        Add("AfterMatch.Employer.SlotsSent.TitleFmt", "Momenten verstuurd, wacht op {0}", "Times sent, waiting for {0}");
        Add("AfterMatch.Employer.SlotsSent.Body",
            "De kandidaat kiest één van je momenten, of stelt een ander moment voor. Je krijgt bericht zodra ze heeft gekozen.",
            "The candidate picks one of your times or suggests another. You will be notified once she has chosen.");
        Add("AfterMatch.Employer.SlotsSent.BodyFmt",
            "{0} kiest één van je momenten, of stelt een ander moment voor. Je krijgt bericht zodra {1} heeft gekozen.",
            "{0} picks one of your times or suggests another. You will be notified once {1} has chosen.");
        Add("AfterMatch.Employer.WhatNow.Slots.Body", "Stel 2 of 3 momenten voor, of deel je agenda. Doe het liefst binnen twee werkdagen.", "Propose 2 or 3 times or share your calendar within two working days.");
        Add("AfterMatch.Employer.WhatNow.Interview.Body", "Lobsy stuurt jullie allebei een bevestiging en herinneringen.", "Lobsy sends both of you a confirmation and reminders.");
        Add("AfterMatch.Employer.WhatNow.Outcome.Body", "Na het gesprek geef je de uitkomst met een korte reden.", "After the interview record the outcome with a short reason.");
        Add("AfterMatch.Employer.Propose.Title", "Momenten voorstellen", "Propose times");
        Add("AfterMatch.Employer.Propose.Lead", "Kies 2 of 3 momenten.", "Pick 2 or 3 times.");
        Add("AfterMatch.Employer.Propose.Manual", "Zelf momenten kiezen", "Pick times yourself");
        Add("AfterMatch.Employer.Propose.ShareAgenda", "Mijn agenda delen", "Share my calendar");
        Add("AfterMatch.Employer.Propose.PickHint", "Klik in de weekweergave om momenten te selecteren (2–3).", "Click the week view to select 2–3 times.");
        Add("AfterMatch.Employer.Propose.AgendaSoon", "Agenda koppelen komt later. Gebruik intussen de weekweergave om handmatig momenten te kiezen.", "Calendar linking comes later. Use the week view to pick times manually for now.");
        Add("AfterMatch.Employer.Propose.CandidateNote",
            "De kandidaat kiest zelf uit je momenten, of stelt een ander moment voor. Je krijgt bericht zodra ze heeft gekozen.",
            "The candidate picks from your times or suggests another. You will be notified when she has chosen.");
        Add("AfterMatch.Employer.Propose.CandidateNoteFmt",
            "{0} kiest zelf uit je momenten, of stelt een ander moment voor. Je krijgt bericht zodra {1} heeft gekozen.",
            "{0} picks from your times or suggests another. You will be notified when {1} has chosen.");
        Add("AfterMatch.Employer.Propose.AddSlot", "Nog een moment", "Another time");
        Add("AfterMatch.Employer.Propose.AddDate", "Datum", "Date");
        Add("AfterMatch.Employer.Propose.AddTime", "Tijd", "Time");
        Add("AfterMatch.Employer.Propose.AgendaPlaceholder",
            "Agenda koppelen volgt later. Gebruik intussen zelf momenten kiezen.",
            "Calendar linking comes later. Pick times manually for now.");
        Add("AfterMatch.Employer.Propose.Where", "Locatie of link", "Location or link");
        Add("AfterMatch.Employer.Propose.WithWhom", "Met wie", "With whom");
        Add("AfterMatch.Employer.Propose.Note", "Bericht erbij (mag kort)", "Short note (optional)");
        Add("AfterMatch.Employer.Outcome.NotYet", "De uitkomst kun je invullen na het gesprek. Stel eerst momenten voor of stuur een bericht.", "You can record the outcome after the interview. Propose times or send a message first.");
        Add("AfterMatch.Employer.Scheduled.Tile.When", "Gesprek", "Interview");
        Add("AfterMatch.Employer.Scheduled.Tile.Reminders", "Jullie krijgen bevestiging en herinneringen via Lobsy.", "You both get confirmation and reminders via Lobsy.");
        Add("AfterMatch.Employer.Scheduled.Tile.Outcome", "Na het gesprek geef je binnen vijf werkdagen de uitkomst.", "After the interview, record the outcome within five working days.");
        Add("AfterMatch.Employer.Scheduled.OutcomeAction", "Uitkomst invullen", "Record outcome");
        Add("AfterMatch.Employer.Scheduled.WhenFallback", "Donderdag 22 oktober, 10:30", "Thursday 22 October, 10:30");
        Add("AfterMatch.Employer.Action.Propose", "Momenten voorstellen", "Propose times");
        Add("AfterMatch.Employer.Action.Messages", "Bericht sturen", "Send message");
        Add("AfterMatch.Employer.Expectation", "De kandidaat verwacht je voorstel · rond 22–26 okt", "The candidate expects your proposal · around 22–26 Oct");

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
        Add("AfterMatch.Mail.EmployerTitle", "De kandidaat komt donderdag", "The candidate is coming Thursday");
        Add("AfterMatch.Mail.EmployerLead", "De kandidaat koos een van je momenten. Het gesprek staat ook in Lobsy onder Gesprekken.", "The candidate picked one of your times. The interview is also in Lobsy under Interviews.");
        Add("AfterMatch.Mail.OutlookTitle", "Zo ziet het agenda-item eruit in Outlook", "How the calendar item looks in Outlook");
        Add("AfterMatch.Mail.OutlookSave", "Opslaan en sluiten", "Save and close");
        Add("AfterMatch.Mail.OutlookDelete", "Verwijderen", "Delete");
        Add("AfterMatch.Mail.OutlookBusy", "Beschikbaarheid: Bezet", "Show as: Busy");
        Add("AfterMatch.Mail.OutlookEventTitle", "Gesprek {0} (via Lobsy)", "Interview {0} (via Lobsy)");
        Add("AfterMatch.Mail.OutlookStart", "Begin", "Start");
        Add("AfterMatch.Mail.OutlookEnd", "Einde", "End");
        Add("AfterMatch.Mail.OutlookLocation", "Locatie", "Location");
        Add("AfterMatch.Mail.OutlookReschedule", "Verzetten of afzeggen: lobsy.nl/g/7KX2", "Reschedule or cancel: lobsy.nl/g/7KX2");
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
        Add("AfterMatch.Employer.Reschedule.Note", "De kandidaat vroeg om een ander moment. Hieronder zie je de reden en het voorstel.", "The candidate asked for another time. See the reason and proposal below.");
        Add("AfterMatch.Employer.Reschedule.Accept", "Akkoord met voorstel", "Accept proposal");
        Add("AfterMatch.Employer.Reschedule.AcceptSlotFmt", "Akkoord, {0}", "Accept, {0}");
        Add("AfterMatch.Employer.Reschedule.Counter", "Ander moment voorstellen", "Propose another time");
        Add("AfterMatch.Employer.Reschedule.WarningTitleFmt", "{0} vraagt het gesprek te verzetten", "{0} asks to reschedule the interview");
        Add("AfterMatch.Employer.Reschedule.EmployerInitiatedTitle", "Gesprek verzetten", "Reschedule interview");
        Add("AfterMatch.Employer.Reschedule.EmployerInitiatedLead",
            "Je start zelf een verzet. Kies nieuwe momenten of zeg het gesprek af.",
            "You are rescheduling yourself. Pick new times or cancel the interview.");
        Add("AfterMatch.Employer.Reschedule.ReasonLineFmt", "Reden: {0}", "Reason: {0}");
        Add("AfterMatch.Employer.Reschedule.ProposalLineFmt", "Voorstel: {0} {1}", "Proposal: {0} {1}");
        Add("AfterMatch.Employer.Reschedule.OwnSlotHint", "(een van je eigen momenten)", "(one of your own times)");
        Add("AfterMatch.Employer.Reschedule.EmployerSlotHint", "(jouw voorgestelde moment)", "(your proposed time slot)");
        Add("AfterMatch.Employer.Reschedule.CancelLink", "Gesprek afzeggen", "Cancel interview");
        Add("AfterMatch.Employer.Reschedule.Footnote",
            "Zeg je zelf af of verzet je? Dan vraagt Lobsy ook jou om een korte reden. De kandidaat ziet die reden.",
            "If you cancel or reschedule, Lobsy asks you for a short reason too. The candidate will see it.");
        Add("AfterMatch.Employer.Reschedule.WeekAside", "Gesprekken deze week", "Interviews this week");
        Add("AfterMatch.Employer.Reschedule.RequestLabel", "Verzoek om te verzetten", "Request to reschedule");
        Add("AfterMatch.Employer.Reschedule.ActionPill", "Actie", "Action");
        Add("AfterMatch.Employer.Reschedule.WeekRow4790Name", "Kandidaat #4790", "Candidate #4790");
        Add("AfterMatch.Employer.Reschedule.WeekRow4790When", "Vrijdag 23 okt, 09:00", "Friday 23 Oct, 09:00");
        Add("AfterMatch.Employer.Reschedule.WeekRow4790Status", "Bevestigd", "Confirmed");
        Add("AfterMatch.Employer.Cancel.Lead",
            "Geef een korte reden. De kandidaat ziet die reden in Lobsy.",
            "Give a short reason. The candidate will see it in Lobsy.");
        Add("AfterMatch.Employer.Cancel.HeroLead",
            "Het gesprek wordt afgezegd. Sanne krijgt bericht met je reden.",
            "The interview will be cancelled. The candidate will be notified with your reason.");
        Add("AfterMatch.InterviewCancelled.EmployerTitle", "Gesprek afgezegd", "Interview cancelled");
        Add("AfterMatch.InterviewCancelled.EmployerLeadFmt",
            "De kandidaat heeft het gesprek afgezegd. Reden: {0}",
            "The candidate cancelled the interview. Reason: {0}");
        Add("AfterMatch.InterviewCancelled.CandidateNoteEmployerFmt",
            "Het gesprek is afgezegd door de werkgever. Reden: {0}",
            "The employer cancelled the interview. Reason: {0}");
        Add("AfterMatch.InterviewCancelled.CandidateNoteSelfFmt",
            "Je hebt het gesprek afgezegd. Reden: {0}",
            "You cancelled the interview. Reason: {0}");
        Add("AfterMatch.InterviewCancelled.DossierCta", "Naar gesprek", "View conversation");
        Add("AfterMatch.Employer.Outcome.PreviewTitle", "Zo ziet Sanne het", "How The candidate sees it");
        Add("AfterMatch.Employer.Outcome.PreviewLead", "Dank je wel voor het gesprek. Hier komt je korte uitleg.", "Thank you for the interview. Your short explanation appears here.");
        Add("AfterMatch.Employer.Outcome.PreviewNotNowTitleFmt", "Dank je wel voor het gesprek, {0}", "Thank you for the interview, {0}");
        Add("AfterMatch.Employer.Outcome.PreviewNotNowLeadFmt", "{0} kiest nu voor iemand anders. Dit zeggen ze erover:", "{0} is choosing someone else for now. They say:");
        Add("AfterMatch.Employer.Outcome.PreviewOfferTitleFmt", "Gefeliciteerd, {0}", "Congratulations, {0}");
        Add("AfterMatch.Employer.Outcome.PreviewOfferLeadFmt", "{0} biedt je een plek aan. Dit staat erbij:", "{0} is offering you a role. They add:");
        Add("AfterMatch.Employer.Outcome.PreviewNextTitleFmt", "Nog een gesprek, {0}", "Another interview, {0}");
        Add("AfterMatch.Employer.Outcome.PreviewNextLeadFmt", "{0} wil graag nog een gesprek met je. Dit staat erbij:", "{0} would like another interview. They add:");
        Add("AfterMatch.Employer.Outcome.ToneNote", "Lobsy controleert reden-teksten op toon voordat ze verstuurd worden.", "Lobsy checks reason texts for tone before they are sent.");
        Add("AfterMatch.Employer.Outcome.OfferNote", "Kies je Aanbod? Dan kan Sanne daarna haar bankgegevens met je delen.", "Choosing Offer lets the candidate share bank details with you afterwards.");
    }
}
