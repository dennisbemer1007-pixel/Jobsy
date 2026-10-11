namespace Jobsy.Web.Localization;

public static class UiStringsExternalVacancy
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
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

        Add("ExtVac.Add.Title", "Vacature van een andere site?", "Vacancy from another site?", "Oferta z innej strony?", "Job de pe alt site?", "وظيفة من موقع آخر؟");
        Add("ExtVac.Add.Lead", "Plak een link naar een vacature op een andere website. Lobsy leest de pagina en bewaart hem voor je.", "Paste a link to a vacancy on another website. Lobsy reads the page and saves it for you.", "Wklej link do oferty na innej stronie. Lobsy odczyta stronę i ją zapisze.", "Lipește un link către un job pe alt site. Lobsy citește pagina și îl salvează.", "الصق رابطاً لوظيفة على موقع آخر. يقرأ لوبسي الصفحة ويحفظها.");
        Add("ExtVac.Add.Method1Title", "Manier 1: Deel vanuit je browser", "Method 1: Share from your browser", "Sposób 1: Udostępnij z przeglądarki", "Metoda 1: Partajează din browser", "الطريقة 1: شارك من المتصفح");
        Add("ExtVac.Add.Method1Body", "Open de vacature op je telefoon, tik op Delen en kies Lobsy. We vullen de link automatisch in.", "Open the vacancy on your phone, tap Share and choose Lobsy. We fill in the link automatically.", "Otwórz ofertę na telefonie, wybierz Udostępnij i Lobsy. Link uzupełnimy automatycznie.", "Deschide jobul pe telefon, apasă Partajare și alege Lobsy. Completăm linkul automat.", "افتح الوظيفة على هاتفك، اضغط مشاركة واختر لوبسي. نملأ الرابط تلقائياً.");
        Add("ExtVac.Add.Method2Title", "Manier 2: Plak een link", "Method 2: Paste a link", "Sposób 2: Wklej link", "Metoda 2: Lipește un link", "الطريقة 2: الصق رابطاً");
        Add("ExtVac.Add.UrlPlaceholder", "https://…", "https://…", "https://…", "https://…", "https://…");
        Add("ExtVac.Add.Submit", "Toevoegen aan Lobsy", "Add to Lobsy", "Dodaj do Lobsy", "Adaugă în Lobsy", "أضف إلى لوبسي");
        Add("ExtVac.Add.Coach", "Tip: alleen openbare vacaturepagina's werken. Inlogpagina's kunnen we niet lezen.", "Tip: only public vacancy pages work. We cannot read login pages.", "Wskazówka: działają tylko publiczne strony ofert. Nie odczytamy stron logowania.", "Sfat: funcționează doar paginile publice de job. Nu putem citi pagini de login.", "نصيحة: تعمل فقط صفحات الوظائف العامة. لا نستطيع قراءة صفحات تسجيل الدخول.");
        Add("ExtVac.Add.Working", "Vacature wordt gelezen…", "Reading vacancy…", "Odczytywanie oferty…", "Se citește jobul…", "جاري قراءة الوظيفة…");
        Add("ExtVac.Promo.Link", "Vacature van een andere site?", "Vacancy from another site?", "Oferta z innej strony?", "Job de pe alt site?", "وظيفة من موقع آخر؟");

        Add("ExtVac.Detail.Saved", "Bewaard", "Saved", "Zapisane", "Salvat", "محفوظ");
        Add("ExtVac.Detail.Requirements", "Wat ze vragen", "What they ask for", "Czego oczekują", "Ce cer", "ما يطلبونه");
        Add("ExtVac.Detail.Facts", "Feiten", "Facts", "Fakty", "Date", "حقائق");
        Add("ExtVac.Detail.Unknown", "Dat weet ik niet", "I don't know", "Nie wiem", "Nu știu", "لا أعرف");
        Add("ExtVac.Detail.Strengths", "Hier scoor je goed op", "Where you score well", "Tu masz mocne strony", "Aici te descurci bine", "هنا تتفوق");
        Add("ExtVac.Detail.Challenges", "Hier ligt je uitdaging", "Where your challenge is", "Tu masz wyzwanie", "Aici e provocarea ta", "هنا تحديك");
        Add("ExtVac.Detail.Privacy", "Alleen jij ziet deze match-analyse. Werkgevers zien dit niet.", "Only you see this match analysis. Employers do not.", "Tylko ty widzisz tę analizę. Pracodawcy nie.", "Doar tu vezi această analiză. Angajatorii nu.", "أنت فقط ترى هذا التحليل. أصحاب العمل لا.");
        Add("ExtVac.Detail.Apply", "Ik wil solliciteren", "I want to apply", "Chcę aplikować", "Vreau să aplic", "أريد التقديم");
        Add("ExtVac.Detail.Source", "Bron", "Source", "Źródło", "Sursă", "المصدر");

        Add("ExtVac.Apply.Title", "Solliciteren", "Apply", "Aplikuj", "Aplică", "تقديم");
        Add("ExtVac.Apply.Email", "E-mailadres werkgever", "Employer email address", "E-mail pracodawcy", "E-mail angajator", "بريد صاحب العمل");
        Add("ExtVac.Apply.Motivation", "Motivatie", "Motivation", "Motywacja", "Motivație", "الدافع");
        Add("ExtVac.Apply.SharedFacts", "Deel deze feiten mee", "Share these facts", "Udostępnij te fakty", "Partajează aceste date", "شارك هذه الحقائق");
        Add("ExtVac.Apply.OneTime", "We sturen één e-mail namens jou. Daarna kun je geen wijzigingen meer doen via Lobsy.", "We send one email on your behalf. After that you cannot change this via Lobsy.", "Wyślemy jeden e-mail w twoim imieniu. Potem nie zmienisz tego w Lobsy.", "Trimitem un e-mail în numele tău. Apoi nu mai poți modifica în Lobsy.", "نرسل بريداً واحداً نيابة عنك. بعد ذلك لا يمكنك التعديل عبر لوبسي.");
        Add("ExtVac.Apply.PlanBTitle", "Plan B", "Plan B", "Plan B", "Planul B", "الخطة B");
        Add("ExtVac.Apply.PlanBBody", "Download je sollicitatiebrief als PDF en stuur hem zelf per e-mail.", "Download your cover letter as PDF and email it yourself.", "Pobierz list motywacyjny PDF i wyślij sam.", "Descarcă scrisoarea PDF și trimite-o tu.", "حمّل خطاب التقديم PDF وأرسله بنفسك.");
        Add("ExtVac.Apply.DownloadPdf", "Download PDF", "Download PDF", "Pobierz PDF", "Descarcă PDF", "تحميل PDF");
        Add("ExtVac.Apply.Submit", "Verstuur sollicitatie", "Send application", "Wyślij aplikację", "Trimite candidatura", "إرسال الطلب");
        Add("ExtVac.Apply.Coach", "Controleer het e-mailadres. Verkeerde adressen komen niet aan.", "Check the email address. Wrong addresses won't arrive.", "Sprawdź adres e-mail. Złe adresy nie dotrą.", "Verifică e-mailul. Adrese greșite nu ajung.", "تحقق من البريد. العناوين الخاطئة لا تصل.");
        Add("ExtVac.Apply.Done", "Verstuurd! De werkgever krijgt een uitnodiging via Lobsy.", "Sent! The employer receives an invite via Lobsy.", "Wysłano! Pracodawca dostanie zaproszenie przez Lobsy.", "Trimis! Angajatorul primește invitație prin Lobsy.", "تم الإرسال! يستلم صاحب العمل دعوة عبر لوبسي.");

        Add("ExtVac.Saved.Title", "Externe vacatures", "External vacancies", "Oferty zewnętrzne", "Joburi externe", "وظائف خارجية");
        Add("ExtVac.Saved.Lead", "Vacatures die je via een link hebt toegevoegd.", "Vacancies you added via a link.", "Oferty dodane linkiem.", "Joburi adăugate prin link.", "وظائف أضفتها برابط.");
        Add("ExtVac.Saved.Empty", "Nog geen externe vacatures bewaard.", "No external vacancies saved yet.", "Brak zapisanych ofert zewnętrznych.", "Niciun job extern salvat.", "لا توجد وظائف خارجية محفوظة بعد.");
        Add("ExtVac.Saved.Add", "Voeg een vacature toe", "Add a vacancy", "Dodaj ofertę", "Adaugă un job", "أضف وظيفة");

        Add("ExtVac.Employer.Title", "Sollicitatie via Lobsy", "Application via Lobsy", "Aplikacja przez Lobsy", "Candidatură prin Lobsy", "تقديم عبر لوبسي");
        Add("ExtVac.Employer.StepEmail", "E-mail", "Email", "E-mail", "E-mail", "البريد");
        Add("ExtVac.Employer.StepKvk", "Bedrijf", "Company", "Firma", "Companie", "الشركة");
        Add("ExtVac.Employer.StepVacancy", "Vacature", "Vacancy", "Oferta", "Job", "الوظيفة");
        Add("ExtVac.Employer.StepDone", "Klaar", "Done", "Gotowe", "Gata", "تم");
        Add("ExtVac.Employer.Preview", "Sollicitatie van {0}", "Application from {0}", "Aplikacja od {0}", "Candidatură de la {0}", "تقديم من {0}");
        Add("ExtVac.Employer.AcceptHint", "Accepteren kan na registratie. Eerste vacature kan gratis; daarna betaal je tokens voor publiceren.", "Accept after registration. First vacancy may be free; then you pay tokens to publish.", "Akceptacja po rejestracji. Pierwsza oferta może być gratis; potem płacisz tokenami.", "Acceptă după înregistrare. Primul job poate fi gratuit; apoi plătești tokenuri.", "القبول بعد التسجيل. قد تكون الأولى مجانية؛ ثم تدفع رموزاً للنشر.");
        Add("ExtVac.Employer.RegisterCta", "Account aanmaken en accepteren", "Create account and accept", "Załóż konto i akceptuj", "Creează cont și acceptă", "أنشئ حساباً واقبل");
        Add("ExtVac.Employer.Invalid", "Deze uitnodiging is ongeldig of verlopen.", "This invite is invalid or expired.", "Zaproszenie jest nieprawidłowe lub wygasło.", "Invitația este invalidă sau expirată.", "هذه الدعوة غير صالحة أو منتهية.");
        Add("ExtVac.Employer.EmployersOff", "Werkgevers staan uit. Je kunt de uitnodiging bekijken; registreren kan zodra werkgevers weer aan staan.", "Employers are off. You can view the invite; registration opens when employers are on again.", "Pracodawcy wyłączeni. Możesz zobaczyć zaproszenie; rejestracja wróci, gdy włączą.", "Angajatorii sunt opriți. Poți vedea invitația; înregistrarea revine când sunt porniți.", "أصحاب العمل متوقفون. يمكنك عرض الدعوة؛ التسجيل يعود عند التفعيل.");

        Add("ExtVac.Fact.bedrijf", "Bedrijf", "Company", "Firma", "Companie", "الشركة");
        Add("ExtVac.Fact.plaats", "Plaats", "Place", "Miejsce", "Locație", "المكان");
        Add("ExtVac.Fact.uren", "Uren", "Hours", "Godziny", "Ore", "الساعات");
        Add("ExtVac.Fact.salaris", "Salaris", "Pay", "Wynagrodzenie", "Salariu", "الأجر");
        Add("ExtVac.Fact.start", "Start", "Start", "Start", "Start", "البداية");
        Add("ExtVac.Fact.opleiding", "Opleiding", "Training", "Szkolenie", "Formare", "التدريب");
    }
}
