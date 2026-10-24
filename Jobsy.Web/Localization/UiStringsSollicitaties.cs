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

        Add("Apps.Activiteiten.Placeholder",
            "Mijn activiteiten komt hier. Je ziet straks je wekelijkse zoekactiviteit en kunt die aanvullen.",
            "My activities will appear here. You will soon see weekly job-search activity and can add to it.",
            "Moje aktywności pojawią się tutaj. Wkrótce zobaczysz tygodniową aktywność i uzupełnisz ją.",
            "Activitățile tale vor apărea aici. În curând vei vedea activitatea săptămânală.",
            "ستظهر نشاطاتك هنا. قريباً سترى نشاط البحث الأسبوعي.");

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
