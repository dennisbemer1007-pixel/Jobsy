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

        Add("Nav.MessagesTab",
            "Berichten", "Messages",
            "Wiadomości", "Mesaje", "الرسائل");

        Add("Apps.Ndm.PickSlot",
            "Kies een moment", "Pick a time",
            "Wybierz termin", "Alege un moment", "اختر وقتاً");

        Add("Apps.Ndm.ViewInterview",
            "Bekijk je gesprek", "View your interview",
            "Zobacz rozmowę", "Vezi interviul", "اعرض مقابلتك");

        Add("Apps.Ndm.Messages",
            "Berichten", "Messages",
            "Wiadomości", "Mesaje", "الرسائل");

        Add("Apps.Ndm.InterviewPlannedShort",
            "Gesprek gepland, {0}", "Interview scheduled, {0}",
            "Rozmowa zaplanowana, {0}", "Interviu programat, {0}", "مقابلة مجدولة، {0}");

        Add("Apps.Ndm.UnreadAria",
            "{0} ongelezen berichten", "{0} unread messages",
            "{0} nieprzeczytanych wiadomości", "{0} mesaje necitite", "{0} رسائل غير مقروءة");

        Add("Apps.Ndm.CompanyPage",
            "Bedrijfspagina", "Company page",
            "Strona firmy", "Pagina companie", "صفحة الشركة");

        Add("Apps.Ndm.LobsyCv",
            "Lobsy-cv", "Lobsy CV",
            "CV Lobsy", "CV Lobsy", "سيرة Lobsy");

        Add("Apps.Ndm.DetailTipPick",
            "Kies een moment dat jou uitkomt, of stel zelf een ander moment voor.",
            "Pick a time that suits you, or suggest another time yourself.",
            "Wybierz dogodny termin lub zaproponuj inny.",
            "Alege un moment potrivit sau propune altul.",
            "اختر وقتاً يناسبك أو اقترح وقتاً آخر.");

        Add("Apps.Ndm.BerichtenEmpty",
            "Nog geen gesprekken met berichten. Zodra een werkgever reageert, zie je het hier.",
            "No interview threads with messages yet. When an employer replies, you will see it here.",
            "Brak wątków z wiadomościami. Gdy pracodawca odpowie, zobaczysz to tutaj.",
            "Încă nu ai conversații cu mesaje. Când un angajator răspunde, le vezi aici.",
            "لا توجد محادثات بعد. عندما يرد صاحب العمل ستظهر هنا.");

        Add("Apps.Ndm.BerichtenSafe",
            "Je telefoonnummer deel je pas na het gesprek, je bankgegevens pas na een aanbod.",
            "You share your phone number only after the interview, and bank details only after an offer.",
            "Numer telefonu udostępniasz dopiero po rozmowie, dane bankowe po ofercie.",
            "Numărul de telefon îl partajezi după interviu, datele bancare după ofertă.",
            "تشارك رقم هاتفك بعد المقابلة وبياناتك البنكية بعد العرض.");
    }
}
