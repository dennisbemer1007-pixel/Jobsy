namespace Jobsy.Web.Localization;

public static class UiStringsSollicitaties
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string plText, string roText, string arText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText;
            ro[key] = roText;
            ar[key] = arText;
        }

        Add("Apps.MockupLead",
            "Op vacatures en bij bedrijven waar je zelf aanklopte. Alles op één plek.",
            "On vacancies and at companies you reached out to yourself. All in one place.",
            "Na oferty i u firm, do których sam/a zagadałeś/aś. Wszystko w jednym miejscu.",
            "La joburi și la firme unde ai bătut tu la ușă. Totul într-un loc.",
            "على الوظائف وعند الشركات التي تواصلت معها بنفسك. الكل في مكان واحد.");

        Add("Nav.ActivitiesTab",
            "Mijn activiteiten", "My activities",
            "Moje aktywności", "Activitățile mele", "نشاطاتي");

        Add("Apps.AddApplication",
            "Sollicitatie toevoegen", "Add application",
            "Dodaj aplikację", "Adaugă candidatură", "إضافة طلب");

        Add("Apps.InitiativeWeekBanner",
            "Nog {0} van de {1} keer 'Ik wil hier werken' deze week",
            "{0} of {1} 'I want to work here' left this week",
            "Zostało {0} z {1} razy „Chcę tu pracować” w tym tygodniu",
            "Mai ai {0} din {1} ori „Vreau să lucrez aici” săptămâna asta",
            "تبقّى {0} من {1} مرات «أريد العمل هنا» هذا الأسبوع");

        Add("Bk.Apps.FilterInitiative",
            "Ik wil hier werken", "I want to work here",
            "Chcę tu pracować", "Vreau să lucrez aici", "أريد العمل هنا");

        Add("Apps.SourceExternal",
            "Buiten Lobsy", "Outside Lobsy",
            "Poza Lobsy", "În afara Lobsy", "خارج Lobsy");

        Add("ExtVac.Status.Applied",
            "Gesolliciteerd", "Applied",
            "Aplikowano", "Ai aplicat", "تم التقديم");

        Add("Apps.SentOn",
            "Verstuurd {0}", "Sent {0}",
            "Wysłano {0}", "Trimis {0}", "أُرسل {0}");

        Add("Apps.Activiteiten.PeriodPrefix",
            "Deze periode:", "This period:",
            "Ten okres:", "Această perioadă:", "هذه الفترة:");

        Add("Apps.Activiteiten.TargetMet",
            "Je afspraak is gehaald", "You reached your goal",
            "Cel osiągnięty", "Ținta ta este atinsă", "حققت هدفك");

        Add("Apps.Activiteiten.TargetNotMet",
            "Nog niet genoeg activiteiten deze periode", "Not enough activities yet this period",
            "Za mało aktywności w tym okresie", "Încă nu sunt destule activități", "لا توجد نشاطات كافية بعد");

        Add("Apps.Activiteiten.DoneBadge",
            "{0} activiteiten gedaan", "{0} activities done",
            "{0} aktywności", "{0} activități făcute", "{0} نشاطات");

        Add("Apps.Activiteiten.AgreementBadge",
            "Je afspraak: {0} per 4 weken", "Your goal: {0} per 4 weeks",
            "Twój cel: {0} na 4 tygodnie", "Obiectivul tău: {0} la 4 săptămâni", "هدفك: {0} كل 4 أسابيع");

        Add("Apps.Activiteiten.DaysLeftBadge",
            "Nog {0} dagen in deze periode", "{0} days left in this period",
            "Zostało {0} dni", "Mai sunt {0} zile", "{0} أيام متبقية");

        Add("Apps.Activiteiten.AdjustLead",
            "Meer mag altijd. Klopt je afspraak met UWV niet?",
            "Extra is always fine. Is your UWV agreement different?",
            "Więcej jest OK. Inna umowa z UWV?", "Mai mult e OK. Altă înțelegere cu UWV?", "المزيد مسموح. هل اتفاق UWV مختلف؟");

        Add("Apps.Activiteiten.AdjustLink",
            "Aanpassen", "Adjust",
            "Dostosuj", "Ajustează", "تعديل");

        Add("Apps.Activiteiten.UwvDisclaimer",
            "Lobsy geeft niets door aan het UWV. Je zet het overzicht zelf in Mijn UWV of in de Werkmap. Zo houd jij de regie.",
            "Lobsy does not share anything with UWV. You add this overview yourself in Mijn UWV or your workbook. You stay in control.",
            "Lobsy nie przekazuje nic UWV.", "Lobsy nu trimite nimic către UWV.", "Lobsy لا يشارك UWV أي شيء.");

        Add("Apps.Activiteiten.DownloadPdf",
            "Download overzicht (pdf)", "Download overview (pdf)",
            "Pobierz PDF", "Descarcă PDF", "تنزيل PDF");

        Add("Apps.Activiteiten.PerWeek",
            "Per week", "Per week",
            "Tygodniowo", "Pe săptămână", "أسبوعياً");

        Add("Apps.Activiteiten.FilterAll",
            "Alles", "All",
            "Wszystko", "Tot", "الكل");

        Add("Apps.Activiteiten.FilterApplications",
            "Sollicitaties", "Applications",
            "Aplikacje", "Candidaturi", "طلبات");

        Add("Apps.Activiteiten.FilterInterviews",
            "Gesprekken", "Interviews",
            "Rozmowy", "Interviuri", "مقابلات");

        Add("Apps.Activiteiten.FilterWorkshops",
            "Workshops", "Workshops",
            "Warsztaty", "Ateliere", "ورش");

        Add("Apps.Activiteiten.WeekCount",
            "{0} activiteiten", "{0} activities",
            "{0} aktywności", "{0} activități", "{0} نشاطات");

        Add("Apps.Activiteiten.Empty",
            "Nog geen activiteiten. Sollicitaties en gesprekken via Lobsy verschijnen vanzelf. Voeg handmatig toe wat je buiten Lobsy deed.",
            "No activities yet. Applications and interviews via Lobsy appear automatically. Add what you did outside Lobsy.",
            "Brak aktywności.", "Încă nu sunt activități.", "لا نشاطات بعد.");

        Add("Apps.Activiteiten.AutoTracked",
            "Vanzelf bijgehouden", "Tracked automatically",
            "Automatycznie", "Urmărit automat", "تتبع تلقائي");

        Add("Apps.Activiteiten.Sent",
            "Verstuurd", "Sent",
            "Wysłano", "Trimis", "أُرسل");

        Add("Apps.Activiteiten.AddTitle",
            "Activiteit toevoegen", "Add activity",
            "Dodaj aktywność", "Adaugă activitate", "إضافة نشاط");

        Add("Apps.Activiteiten.AddLead",
            "Iets gedaan buiten Lobsy? Zet het erbij.",
            "Did something outside Lobsy? Add it here.",
            "Coś poza Lobsy?", "Ceva în afara Lobsy?", "نشاط خارج Lobsy؟");

        Add("Apps.Activiteiten.FieldDate",
            "Datum", "Date",
            "Data", "Dată", "التاريخ");

        Add("Apps.Activiteiten.FieldEmployer",
            "Werkgever", "Employer",
            "Pracodawca", "Angajator", "صاحب العمل");

        Add("Apps.Activiteiten.FieldFunction",
            "Functie of onderwerp", "Role or topic",
            "Stanowisko", "Funcție", "الوظيفة");

        Add("Apps.Activiteiten.FieldResult",
            "Wat kwam eruit (mag kort)", "Outcome (short is fine)",
            "Wynik", "Rezultat", "النتيجة");

        Add("Apps.Activiteiten.Save",
            "Opslaan", "Save",
            "Zapisz", "Salvează", "حفظ");

        Add("Apps.Activiteiten.Kind.Applied",
            "Gesolliciteerd", "Applied",
            "Aplikacja", "Aplicat", "تقديم");

        Add("Apps.Activiteiten.Kind.EmployerCalled",
            "Werkgever gebeld", "Called employer",
            "Telefon", "Apel", "اتصال");

        Add("Apps.Activiteiten.Kind.Visited",
            "Langsgegaan", "Visited in person",
            "Wizyta", "Vizită", "زيارة");

        Add("Apps.Activiteiten.Kind.TempAgency",
            "Uitzendbureau", "Temp agency",
            "Agencja", "Agenție", "وكالة");

        Add("Apps.Activiteiten.Kind.Network",
            "Netwerkgesprek", "Network conversation",
            "Networking", "Rețea", "شبكة");

        Add("Apps.Activiteiten.Kind.Workshop",
            "Workshop", "Workshop",
            "Warsztat", "Atelier", "ورشة");

        Add("Apps.Activiteiten.Kind.Interview",
            "Gesprek", "Interview",
            "Rozmowa", "Interviu", "مقابلة");

        Add("Apps.CardPill.InterviewPlanned",
            "Gesprek gepland", "Interview scheduled",
            "Rozmowa zaplanowana", "Interviu programat", "مقابلة مجدولة");

        Add("Apps.Detail.TipTitle",
            "Wat kun je nu doen?", "What can you do now?",
            "Co możesz teraz zrobić?", "Ce poți face acum?", "ماذا يمكنك أن تفعل الآن؟");

        Add("Apps.Detail.TipInterview",
            "Oefen je gesprek en lees de tips voor een kennismaking.",
            "Practice your interview and read tips for an introductory meeting.",
            "Poćwicz rozmowę i przeczytaj wskazówki do spotkania.",
            "Exersează interviul și citește sfaturi pentru o întâlnire.",
            "تدرّب على مقابلتك واقرأ نصائح اللقاء التعارفي.");

        Add("Apps.Ndm.PickSlot",
            "Kies een moment", "Pick a time",
            "Wybierz termin", "Alege un moment", "اختر وقتاً");

        Add("Apps.Ndm.InterviewPlannedShort",
            "Gesprek gepland, {0}", "Interview scheduled, {0}",
            "Rozmowa zaplanowana, {0}", "Interviu programat, {0}", "مقابلة مجدولة، {0}");

        Add("Apps.Ndm.UnreadAria",
            "{0} ongelezen berichten", "{0} unread messages",
            "{0} nieprzeczytanych wiadomości", "{0} mesaje necitite", "{0} رسائل غير مقروءة");

        Add("Apps.Ndm.ViewInterview",
            "Bekijk je gesprek", "View your interview",
            "Zobacz rozmowę", "Vezi interviul", "اعرض مقابلتك");

        Add("Apps.Ndm.Messages",
            "Berichten", "Messages",
            "Wiadomości", "Mesaje", "الرسائل");

        Add("Apps.Berichten.PreviewYou",
            "Jij: ", "You: ",
            "Ty: ", "Tu: ", "أنت: ");

        Add("Apps.Berichten.SubtitleInterview",
            "{0} · gesprek {1}", "{0} · interview {1}",
            "{0} · rozmowa {1}", "{0} · interviu {1}", "{0} · مقابلة {1}");

        Add("Apps.Berichten.SysBothYes",
            "Jullie zeiden allebei ja · {0}", "You both said yes · {0}",
            "Oboje powiedzieliście tak · {0}", "Ați spus amândoi da · {0}", "قال كلٌّ منكما نعم · {0}");

        Add("Apps.Berichten.SysSlots",
            "Er staan {0} momenten klaar · {1}", "{0} time slots ready · {1}",
            "Gotowych terminów: {0} · {1}", "{0} intervale disponibile · {1}", "{0} مواعيد جاهزة · {1}");

        Add("Apps.Berichten.SysScheduled",
            "Gesprek gepland · {0}", "Interview scheduled · {0}",
            "Rozmowa zaplanowana · {0}", "Interviu programat · {0}", "مقابلة مجدولة · {0}");

        Add("Apps.Berichten.AboutRole",
            "Over: {0}", "About: {0}",
            "O: {0}", "Despre: {0}", "حول: {0}");

        Add("Apps.Berichten.ViewApplication",
            "Sollicitatie bekijken", "View application",
            "Zobacz aplikację", "Vezi candidatura", "عرض الطلب");

        Add("Apps.Berichten.BackList",
            "Terug naar berichten", "Back to messages",
            "Wróć do wiadomości", "Înapoi la mesaje", "العودة إلى الرسائل");

        Add("Apps.Berichten.SafePanel",
            "Je telefoonnummer deel je pas na het gesprek, je bankgegevens pas na een aanbod. Gesprekken van afgeronde sollicitaties staan in het archief.",
            "You share your phone number only after the interview, and bank details only after an offer. Conversations from closed applications are archived.",
            "Numer telefonu podajesz dopiero po rozmowie, dane bankowe dopiero po ofercie. Rozmowy z zamkniętych aplikacji trafiają do archiwum.",
            "Numărul de telefon îl partajezi abia după interviu, datele bancare abia după ofertă. Conversațiile din candidaturi închise sunt arhivate.",
            "تشارك رقم هاتفك فقط بعد المقابلة، وبياناتك البنكية فقط بعد عرض. محادثات الطلبات المغلقة في الأرشيف.");

        Add("Apps.Berichten.EmptyTitle",
            "Nog geen berichten", "No messages yet",
            "Brak wiadomości", "Încă nu sunt mesaje", "لا رسائل بعد");

        Add("Apps.Berichten.EmptyBody",
            "Zodra een werkgever reageert op je sollicitatie, zie je het gesprek hier.",
            "When an employer responds to your application, the conversation appears here.",
            "Gdy pracodawca odpowie na aplikację, rozmowa pojawi się tutaj.",
            "Când un angajator răspunde la candidatură, conversația apare aici.",
            "عندما يرد صاحب العمل على طلبك، تظهر المحادثة هنا.");

        Add("Apps.Berichten.ReadSuffix",
            " · Gelezen", " · Read",
            " · Przeczytane", " · Citit", " · مقروء");

        Add("Apps.Ndm.CompanyPage",
            "Bedrijfspagina", "Company page",
            "Strona firmy", "Pagina companie", "صفحة الشركة");

        Add("Apps.Ndm.LobsyCv",
            "Lobsy-cv", "Lobsy CV",
            "CV Lobsy", "CV Lobsy", "سيرة Lobsy");

        Add("Apps.Ndm.WithdrawShort",
            "Intrekken", "Withdraw",
            "Wycofaj", "Retrage", "سحب");

        Add("Apps.Ndm.DetailTipPick",
            "Kies een moment dat jou uitkomt, of stel zelf een ander moment voor.",
            "Pick a time that suits you, or suggest another time yourself.",
            "Wybierz dogodny termin lub zaproponuj inny.",
            "Alege un moment potrivit sau propune altul.",
            "اختر وقتاً يناسبك أو اقترح وقتاً آخر.");

        Add("Apps.Ndm.BackAllApplications",
            "Alle sollicitaties", "All applications",
            "Wszystkie aplikacje", "Toate candidaturile", "كل الطلبات");
    }
}
