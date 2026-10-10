namespace Jobsy.Web.Localization;

/// <summary>Westland Golf 2 pilot copy (B1 Dutch baseline; mirrored in other locales).</summary>
internal static class UiStringsGolf2
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

        Add("Golf2.NextStep.Title",
            "Je volgende stap",
            "Your next step",
            "Twój następny krok",
            "Următorul tău pas",
            "خطوتك التالية");
        Add("Golf2.NextStep.Lead",
            "Je hebt de vier tests afgerond. Dit past bij je droombaan in je carrièreplan.",
            "You finished the four tests. This fits your dream job in your career plan.",
            "Ukończyłeś cztery testy. To pasuje do wymarzonej pracy w planie kariery.",
            "Ai terminat cele patru teste. Se potrivește cu jobul de vis din planul tău.",
            "أنهيت الاختبارات الأربعة. هذا يناسب وظيفة أحلامك في خطة مسيرتك.");
        Add("Golf2.NextStep.Dream",
            "Droombaan: {0}",
            "Dream job: {0}",
            "Wymarzona praca: {0}",
            "Job de vis: {0}",
            "وظيفة الأحلام: {0}");
        Add("Golf2.NextStep.CareerLink",
            "Bekijk cursussen en opleiding",
            "View courses and training",
            "Zobacz kursy i szkolenia",
            "Vezi cursuri și formare",
            "عرض الدورات والتدريب");
        Add("Golf2.NextStep.SheetLink",
            "Maak je gespreksblad",
            "Create your conversation sheet",
            "Zrób arkusz rozmowy",
            "Creează foaia de discuție",
            "أنشئ ورقة الحوار");

        Add("Golf2.Feedback.Title",
            "Hoe hielpen de tests?",
            "How did the tests help?",
            "Jak pomogły testy?",
            "Cum te-au ajutat testele?",
            "كيف ساعدتك الاختبارات؟");
        Add("Golf2.Feedback.Lead",
            "Kies een smiley. Je mag ook iets typen of overslaan.",
            "Pick a smiley. You can type more or skip.",
            "Wybierz buźkę. Możesz też coś napisać lub pominąć.",
            "Alege o față. Poți scrie mai mult sau sări peste.",
            "اختر رمزًا. يمكنك الكتابة أو التخطي.");
        Add("Golf2.Feedback.Rating1",
            "Niet zo goed",
            "Not so good",
            "Nie za dobrze",
            "Nu prea bine",
            "ليس جيدًا");
        Add("Golf2.Feedback.Rating2",
            "Gaat wel",
            "Okay",
            "W porządku",
            "Acceptabil",
            "مقبول");
        Add("Golf2.Feedback.Rating3",
            "Heel goed",
            "Very good",
            "Bardzo dobrze",
            "Foarte bine",
            "جيد جدًا");
        Add("Golf2.Feedback.OpenLabel",
            "Wil je nog iets zeggen? (optioneel)",
            "Anything else? (optional)",
            "Chcesz coś dodać? (opcjonalnie)",
            "Vrei să adaugi ceva? (opțional)",
            "هل تريد إضافة شيء؟ (اختياري)");
        Add("Golf2.Feedback.SharePilot",
            "Deel mijn antwoord anoniem met de pilot",
            "Share my answer anonymously with the pilot",
            "Udostępnij moją odpowiedź anonimowo w pilocie",
            "Distribuie răspunsul anonim în pilot",
            "شارك إجابتي بشكل مجهول مع التجربة");
        Add("Golf2.Feedback.Skip",
            "Overslaan",
            "Skip",
            "Pomiń",
            "Sari peste",
            "تخطي");
        Add("Golf2.Feedback.Save",
            "Versturen",
            "Send",
            "Wyślij",
            "Trimite",
            "إرسال");
        Add("Golf2.Feedback.Thanks",
            "Bedankt voor je feedback.",
            "Thanks for your feedback.",
            "Dziękujemy za opinię.",
            "Mulțumim pentru feedback.",
            "شكرًا على ملاحظاتك.");

        Add("Golf2.Sheet.Title",
            "Gespreksblad",
            "Conversation sheet",
            "Arkusz rozmowy",
            "Foaie de discuție",
            "ورقة الحوار");
        Add("Golf2.Sheet.Lead",
            "Schrijf in je eigen woorden. Geen testscores of geboortedatum.",
            "Write in your own words. No test scores or birth date.",
            "Pisz własnymi słowami. Bez wyników testów ani daty urodzenia.",
            "Scrie cu propriile cuvinte. Fără scoruri sau dată de naștere.",
            "اكتب بكلماتك. بدون نتائج اختبارات أو تاريخ ميلاد.");
        Add("Golf2.Sheet.Strengths",
            "Waar ben ik goed in?",
            "What am I good at?",
            "W czym jestem dobry?",
            "La ce mă pricep?",
            "في ماذا أنا جيد؟");
        Add("Golf2.Sheet.Motivation",
            "Waarom wil ik dit werk?",
            "Why do I want this job?",
            "Dlaczego chcę tę pracę?",
            "De ce vreau acest job?",
            "لماذا أريد هذه الوظيفة؟");
        Add("Golf2.Sheet.Custom",
            "Eigen toevoeging",
            "Your own note",
            "Własna notatka",
            "Notă proprie",
            "ملاحظة خاصة");
        Add("Golf2.Sheet.Print",
            "Printen",
            "Print",
            "Drukuj",
            "Printează",
            "طباعة");
        Add("Golf2.Sheet.Save",
            "Opslaan",
            "Save",
            "Zapisz",
            "Salvează",
            "حفظ");
        Add("Golf2.Sheet.Back",
            "Terug naar paspoort",
            "Back to passport",
            "Wróć do paszportu",
            "Înapoi la pașaport",
            "العودة إلى الجواز");

        Add("Golf2.OutsideWork.Title",
            "Werk buiten betaald werk",
            "Work outside paid jobs",
            "Praca poza płatną pracą",
            "Activitate în afara muncii plătite",
            "عمل خارج الوظائف المدفوعة");
        Add("Golf2.OutsideWork.Lead",
            "Bijvoorbeeld vrijwilligerswerk, mantelzorg of een eigen project.",
            "For example volunteering, informal care or your own project.",
            "Np. wolontariat, opieka lub własny projekt.",
            "De ex. voluntariat, îngrijire sau proiect propriu.",
            "مثل التطوع أو الرعاية أو مشروعك الخاص.");
        Add("Golf2.OutsideWork.Activity",
            "Wat deed je?",
            "What did you do?",
            "Co robiłeś?",
            "Ce ai făcut?",
            "ماذا فعلت؟");
        Add("Golf2.OutsideWork.Description",
            "Korte uitleg",
            "Short explanation",
            "Krótki opis",
            "Scurtă explicație",
            "شرح قصير");
        Add("Golf2.OutsideWork.Hours",
            "Uren per week (optioneel)",
            "Hours per week (optional)",
            "Godzin tygodniowo (opcjonalnie)",
            "Ore pe săptămână (opțional)",
            "ساعات في الأسبوع (اختياري)");
        Add("Golf2.OutsideWork.Add",
            "Toevoegen",
            "Add",
            "Dodaj",
            "Adaugă",
            "إضافة");
        Add("Golf2.OutsideWork.Delete",
            "Verwijderen",
            "Remove",
            "Usuń",
            "Șterge",
            "إزالة");
        Add("Golf2.OutsideWork.Empty",
            "Nog niets toegevoegd.",
            "Nothing added yet.",
            "Jeszcze nic nie dodano.",
            "Nimic adăugat încă.",
            "لم تُضف شيء بعد.");

        Add("Golf2.Tasks.Title",
            "Taken kiezen",
            "Choose tasks",
            "Wybierz zadania",
            "Alege sarcini",
            "اختر المهام");
        Add("Golf2.Tasks.Save",
            "Keuzes opslaan",
            "Save choices",
            "Zapisz wybory",
            "Salvează alegerile",
            "حفظ الاختيارات");

        Add("Golf2.Admin.Title",
            "Westland pilot",
            "Westland pilot",
            "Pilot Westland",
            "Pilot Westland",
            "تجربة Westland");
        Add("Golf2.Admin.Lead",
            "Alleen totalen. Geen vrije tekst of persoonsgegevens.",
            "Totals only. No free text or personal data.",
            "Tylko sumy. Bez tekstu ani danych osobowych.",
            "Doar totaluri. Fără text liber sau date personale.",
            "مجاميع فقط. بدون نص حر أو بيانات شخصية.");
        Add("Golf2.Admin.Off",
            "Pilotrapportage staat uit in de configuratie.",
            "Pilot reporting is off in configuration.",
            "Raportowanie pilota jest wyłączone.",
            "Raportarea pilotului este oprită.",
            "تقارير التجربة متوقفة في الإعدادات.");
        Add("Golf2.Admin.Enrollments",
            "Aanmeldingen pilot",
            "Pilot enrollments",
            "Zapisy do pilota",
            "Înscrieri pilot",
            "تسجيلات التجربة");
        Add("Golf2.Admin.Feedback",
            "Feedbackreacties",
            "Feedback responses",
            "Odpowiedzi feedback",
            "Răspunsuri feedback",
            "ردود الملاحظات");
        Add("Golf2.Admin.AvgRating",
            "Gemiddelde smiley",
            "Average rating",
            "Średnia ocena",
            "Rating mediu",
            "متوسط التقييم");
        Add("Golf2.Admin.Insufficient",
            "Nog te weinig data (minimaal 10)",
            "Not enough data yet (minimum 10)",
            "Za mało danych (minimum 10)",
            "Date insuficiente (minimum 10)",
            "بيانات غير كافية (10 كحد أدنى)");
        Add("Golf2.Admin.ExportCsv",
            "Download CSV (taken)",
            "Download CSV (tasks)",
            "Pobierz CSV (zadania)",
            "Descarcă CSV (sarcini)",
            "تنزيل CSV (المهام)");

        Add("Golf2.SamenStarten.Title",
            "Samen starten",
            "Starting together",
            "Wspólny start",
            "Pornim împreună",
            "نبدأ معًا");
        Add("Golf2.SamenStarten.Lead",
            "Informatie voor coaches en begeleiders in het Westland.",
            "Information for coaches and guides in Westland.",
            "Informacje dla coachów w Westland.",
            "Informații pentru coachi în Westland.",
            "معلومات للمرافقين في Westland.");
        Add("Golf2.SamenStarten.Section1Title",
            "Wat is Lobsy?",
            "What is Lobsy?",
            "Czym jest Lobsy?",
            "Ce este Lobsy?",
            "ما هو Lobsy؟");
        Add("Golf2.SamenStarten.Section1Body",
            "Lobsy helpt mensen werk te vinden dichtbij huis. Kandidaten maken een paspoort met tests en bewijs van ervaring.",
            "Lobsy helps people find work close to home. Candidates build a passport with tests and proof of experience.",
            "Lobsy pomaga znaleźć pracę blisko domu. Kandydaci budują paszport z testami i dowodami doświadczenia.",
            "Lobsy ajută la găsirea muncii aproape de casă. Candidații au un pașaport cu teste și dovezi.",
            "يساعد Lobsy في إيجاد عمل قريب من المنزل. المرشحون يبنون جوازًا باختبارات وخبرة.");
        Add("Golf2.SamenStarten.Section2Title",
            "Gespreksblad",
            "Conversation sheet",
            "Arkusz rozmowy",
            "Foaie de discuție",
            "ورقة الحوار");
        Add("Golf2.SamenStarten.Section2Body",
            "Het gespreksblad is een korte tekst voor het intakegesprek. Geen scores, geen geboortedatum.",
            "The conversation sheet is a short text for intake. No scores, no birth date.",
            "Arkusz to krótki tekst na rozmowę. Bez wyników i daty urodzenia.",
            "Foaia este un text scurt pentru intake. Fără scoruri sau dată de naștere.",
            "ورقة قصيرة للمقابلة. بدون نتائج أو تاريخ ميلاد.");
        Add("Golf2.SamenStarten.Section3Title",
            "Privacy",
            "Privacy",
            "Prywatność",
            "Confidențialitate",
            "الخصوصية");
        Add("Golf2.SamenStarten.Section3Body",
            "Pilotcijfers zijn alleen totalen. Vrije antwoorden worden niet gedeeld met werkgevers.",
            "Pilot numbers are totals only. Free-text answers are not shared with employers.",
            "Liczby pilota to tylko sumy. Wolne odpowiedzi nie idą do pracodawców.",
            "Cifrele pilot sunt doar totaluri. Răspunsurile libere nu merg la angajatori.",
            "أرقام التجربة مجاميع فقط. الإجابات الحرة لا تُشارَك مع أصحاب العمل.");
        Add("Golf2.SamenStarten.Print",
            "Print deze pagina",
            "Print this page",
            "Drukuj stronę",
            "Printează pagina",
            "طباعة هذه الصفحة");
    }
}
