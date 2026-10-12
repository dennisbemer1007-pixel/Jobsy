namespace Jobsy.Web.Localization;

public static class UiStringsProactive
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

        Add("ProactiveMatching.Title",
            "Lobsy zoekt voor je", "Lobsy searches for you",
            "Lobsy szuka dla Ciebie", "Lobsy caută pentru tine", "Lobsy يبحث لك");
        Add("ProactiveMatching.Lead",
            "We sturen je tips wanneer een vacature echt bij je past — met uitleg waarom. Jij beslist wat interessant is.",
            "We send tips when a vacancy truly fits you — with an explanation why. You decide what is interesting.",
            "Wysyłamy tipy, gdy oferta naprawdę pasuje — z wyjaśnieniem. Ty decydujesz, co jest interesujące.",
            "Îți trimitem sfaturi când un job ți se potrivește — cu explicație. Tu decizi ce e interesant.",
            "نرسل لك نصائح عندما تناسبك وظيفة — مع شرح السبب. أنت تقرر ما هو مثير للاهتمام.");
        Add("ProactiveMatching.Hero.Eyebrow",
            "Proactieve matching", "Proactive matching",
            "Proaktywne dopasowanie", "Potrivire proactivă", "مطابقة استباقية");

        Add("ProactiveMatching.Consent.Title",
            "Toestemming", "Consent",
            "Zgoda", "Consimțământ", "الموافقة");
        Add("ProactiveMatching.Consent.Body",
            "Lobsy mag je e-mailtips sturen over passende vacatures op basis van je profiel, tests en voorkeuren.",
            "Lobsy may email you tips about matching vacancies based on your profile, tests and preferences.",
            "Lobsy może wysyłać tipy e-mail o pasujących ofertach na podstawie profilu, testów i preferencji.",
            "Lobsy poate trimite sfaturi pe e-mail despre joburi potrivite pe baza profilului, testelor și preferințelor.",
            "يجوز لـ Lobsy إرسال نصائح بالبريد عن وظائف مناسبة بناءً على ملفك واختباراتك وتفضيلاتك.");
        Add("ProactiveMatching.Consent.LineWithdraw",
            "Je kunt dit later uitzetten op deze pagina of via privacy@lobsy.nl.",
            "You can turn this off later on this page or via privacy@lobsy.nl.",
            "Możesz to wyłączyć później na tej stronie lub przez privacy@lobsy.nl.",
            "Poți opri asta mai târziu pe această pagină sau prin privacy@lobsy.nl.",
            "يمكنك إيقاف ذلك لاحقاً من هذه الصفحة أو عبر privacy@lobsy.nl.");
        Add("ProactiveMatching.Consent.Submit",
            "Ik geef toestemming", "I give consent",
            "Wyrażam zgodę", "Îmi dau consimțământul", "أوافق");

        Add("ProactiveMatching.Settings.Title",
            "Instellingen", "Settings",
            "Ustawienia", "Setări", "الإعدادات");
        Add("ProactiveMatching.Settings.Enable",
            "Lobsy zoekt voor me", "Lobsy searches for me",
            "Lobsy szuka dla mnie", "Lobsy caută pentru mine", "Lobsy يبحث عني");
        Add("ProactiveMatching.Settings.EnableHint",
            "Zet uit om geen tips meer te ontvangen.", "Turn off to stop receiving tips.",
            "Wyłącz, aby nie dostawać tipów.", "Oprește pentru a nu mai primi sfaturi.", "أوقف لعدم تلقي النصائح.");
        Add("ProactiveMatching.Settings.Frequency",
            "Hoe vaak?", "How often?",
            "Jak często?", "Cât de des?", "كم مرة؟");
        Add("ProactiveMatching.Settings.Frequency.Immediate",
            "Zodra er een sterke match is", "As soon as there is a strong match",
            "Gdy tylko jest mocne dopasowanie", "Imediat ce e o potrivire puternică", "عند وجود تطابق قوي");
        Add("ProactiveMatching.Settings.Frequency.Daily",
            "Maximaal één tip per dag", "At most one tip per day",
            "Maks. jeden tip dziennie", "Cel mult un sfat pe zi", "بحد أقصى نصيحة يومياً");
        Add("ProactiveMatching.Settings.Frequency.Weekly",
            "Samenvatting per week", "Weekly summary",
            "Podsumowanie tygodniowe", "Rezumat săptămânal", "ملخص أسبوعي");
        Add("ProactiveMatching.Settings.Pause",
            "Pauze tot", "Pause until",
            "Pauza do", "Pauză până la", "إيقاف حتى");
        Add("ProactiveMatching.Settings.PauseHint",
            "Tot deze datum sturen we geen nieuwe tips.", "Until this date we send no new tips.",
            "Do tej daty nie wysyłamy nowych tipów.", "Până la această dată nu trimitem sfaturi noi.", "حتى هذا التاريخ لا نرسل نصائح جديدة.");
        Add("ProactiveMatching.Settings.ClearPause",
            "Pauze opheffen", "Clear pause",
            "Usuń pauzę", "Elimină pauza", "إلغاء الإيقاف");
        Add("ProactiveMatching.Settings.Limits",
            "We tonen alleen matches vanaf {0}% en sturen maximaal {1} tips per week.",
            "We only show matches from {0}% and send at most {1} tips per week.",
            "Pokazujemy dopasowania od {0}% i wysyłamy max {1} tipów tygodniowo.",
            "Arătăm potriviri de la {0}% și trimitem max {1} sfaturi pe săptămână.",
            "نعرض التطابقات من {0}% ونرسل بحد أقصى {1} نصائح أسبوعياً.");
        Add("ProactiveMatching.Settings.Save",
            "Instellingen opslaan", "Save settings",
            "Zapisz ustawienia", "Salvează setările", "حفظ الإعدادات");
        Add("ProactiveMatching.Settings.Saved",
            "Opgeslagen.", "Saved.",
            "Zapisano.", "Salvat.", "تم الحفظ.");

        Add("ProactiveMatching.Suggestions.Title",
            "Jouw tips", "Your tips",
            "Twoje tipy", "Sfaturile tale", "نصائحك");
        Add("ProactiveMatching.Suggestions.Empty",
            "Nog geen tips — zodra we iets passends vinden, zie je het hier (en per mail als je dat aan hebt staan).",
            "No tips yet — when we find a fit, you will see it here (and by email if enabled).",
            "Brak tipów — gdy coś znajdziemy, zobaczysz to tutaj (i mailem, jeśli włączone).",
            "Încă fără sfaturi — când găsim ceva potrivit, apare aici (și pe e-mail dacă e activ).",
            "لا نصائح بعد — عندما نجد تطابقاً سيظهر هنا (وبالبريد إن كان مفعّلاً).");
        Add("ProactiveMatching.Suggestions.Match",
            "{0}% match", "{0}% match",
            "Dopasowanie {0}%", "Potrivire {0}%", "تطابق {0}%");
        Add("ProactiveMatching.Suggestions.Rationale",
            "Waarom dit bij je past", "Why this fits you",
            "Dlaczego pasuje", "De ce ți se potrivește", "لماذا يناسبك");
        Add("ProactiveMatching.Suggestions.Grouped",
            "We hebben {0} vergelijkbare vacatures samengevoegd — dit is de sterkste match.",
            "We merged {0} similar vacancies — this is the strongest match.",
            "Połączyliśmy {0} podobnych ofert — to najlepsze dopasowanie.",
            "Am combinat {0} joburi similare — aceasta e cea mai bună potrivire.",
            "دمجنا {0} وظائف مشابهة — هذا أقوى تطابق.");
        Add("ProactiveMatching.Suggestions.ViewVacancy",
            "Bekijk", "View",
            "Zobacz", "Vezi", "عرض");

        Add("ProactiveMatching.Feedback.Interested",
            "Interessant", "Interesting",
            "Interesujące", "Interesant", "مثير للاهتمام");
        Add("ProactiveMatching.Feedback.NotInterested",
            "Niet interessant", "Not interested",
            "Nie interesuje", "Nu mă interesează", "غير مهتم");
        Add("ProactiveMatching.Feedback.Done",
            "Bedankt voor je reactie.", "Thanks for your feedback.",
            "Dzięki za opinię.", "Mulțumim pentru feedback.", "شكراً على ردك.");

        Add("ProactiveMatching.Promo.Title",
            "Lobsy zoekt voor je", "Lobsy searches for you",
            "Lobsy szuka dla Ciebie", "Lobsy caută pentru tine", "Lobsy يبحث لك");
        Add("ProactiveMatching.Promo.Lead",
            "Ontvang tips wanneer een vacature echt bij je past.",
            "Get tips when a vacancy truly fits you.",
            "Otrzymuj tipy, gdy oferta naprawdę pasuje.",
            "Primește sfaturi când un job ți se potrivește.",
            "احصل على نصائح عندما تناسبك وظيفة.");
        Add("ProactiveMatching.Promo.Cta",
            "Instellingen", "Settings",
            "Ustawienia", "Setări", "الإعدادات");

        Add("ProactiveMatching.Employer.Shortlist.Title",
            "Proactieve shortlist", "Proactive shortlist",
            "Shortlista proaktywna", "Shortlist proactiv", "قائمة استباقية");
        Add("ProactiveMatching.Employer.Shortlist.Lead",
            "Anonieme kandidaten die goed passen bij deze vacature — op volgorde van matchscore.",
            "Anonymous candidates who fit this vacancy — ranked by match score.",
            "Anonimowi kandydaci pasujący do oferty — wg wyniku dopasowania.",
            "Candidați anonimi potriviți postului — sortați după scor.",
            "مرشحون مجهولون يناسبون الوظيفة — مرتبون حسب نسبة التطابق.");
        Add("ProactiveMatching.Employer.Shortlist.Empty",
            "Nog geen kandidaten in de shortlist. Publiceer of verrijk de vacature om matches te zien.",
            "No candidates on the shortlist yet. Publish or enrich the vacancy to see matches.",
            "Brak kandydatów na liście. Opublikuj lub uzupełnij ofertę.",
            "Încă fără candidați. Publică sau completează postul.",
            "لا مرشحين بعد. انشر الوظيفة أو أكملها.");
        Add("ProactiveMatching.Employer.Shortlist.Rank",
            "#{0}", "#{0}",
            "#{0}", "#{0}", "#{0}");
        Add("ProactiveMatching.Employer.Shortlist.Introduce",
            "Stuur kennismakingsverzoek", "Send introduction request",
            "Wyślij prośbę o kontakt", "Trimite cerere de contact", "أرسل طلب تعارف");
        Add("ProactiveMatching.Employer.Shortlist.PageTitle",
            "Proactieve shortlist", "Proactive shortlist",
            "Shortlista proaktywna", "Shortlist proactiv", "قائمة استباقية");
        Add("ProactiveMatching.Employer.Shortlist.BackToVacancy",
            "← Terug naar vacature", "← Back to vacancy",
            "← Wróć do oferty", "← Înapoi la post", "← العودة للوظيفة");
        Add("ProactiveMatching.Employer.Shortlist.PromoLead",
            "Bekijk anonieme kandidaten met matchscore, sterke punten en stuur direct een kennismakingsverzoek.",
            "View anonymous candidates with match score, strengths and send an introduction request.",
            "Zobacz anonimowych kandydatów ze scorem i mocnymi stronami.",
            "Vezi candidați anonimi cu scor și puncte forte.",
            "عرض مرشحين مجهولين مع نسبة التطابق ونقاط القوة.");
        Add("ProactiveMatching.Employer.Shortlist.OpenPage",
            "Open proactieve shortlist", "Open proactive shortlist",
            "Otwórz shortlistę", "Deschide shortlist", "افتح القائمة");
        Add("ProactiveMatching.Employer.Shortlist.Anonymous",
            "Anonieme kandidaat", "Anonymous candidate",
            "Anonimowy kandydat", "Candidat anonim", "مرشح مجهول");
        Add("ProactiveMatching.Employer.Shortlist.Strengths",
            "Sterke punten", "Top strengths",
            "Mocne strony", "Puncte forte", "نقاط القوة");
        Add("ProactiveMatching.Employer.Shortlist.Message",
            "Korte boodschap (optioneel)", "Short message (optional)",
            "Krótka wiadomość (opcjonalnie)", "Mesaj scurt (opțional)", "رسالة قصيرة (اختياري)");
        Add("ProactiveMatching.Employer.Shortlist.IntroduceSuccess",
            "Kennismakingsverzoek verstuurd.", "Introduction request sent.",
            "Wysłano prośbę o kontakt.", "Cerere de contact trimisă.", "تم إرسال طلب التعارف.");
        Add("ProactiveMatching.Employer.Shortlist.ReadOnly",
            "Alleen lezen — geen rechten om contact te starten.", "Read-only — no permission to start contact.",
            "Tylko odczyt — brak uprawnień do kontaktu.", "Doar citire — fără drept de contact.", "قراءة فقط — لا صلاحية للتواصل.");
    }
}
