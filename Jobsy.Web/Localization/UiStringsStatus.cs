namespace Jobsy.Web.Localization;

/// <summary>
/// Copy for the status / error pages (404, 500 and the shared ErrorLayout chrome).
/// nl and en are final; pl, ro and ar are B1 drafts — see <c>docs/i18n/errors-review.md</c>.
/// Tone: B1, "je", calm, never blaming and without technical words.
/// </summary>
public static class UiStringsStatus
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

        // —— 404 ——
        Add("Status.NotFound.Chip",
            "404",
            "404",
            "404",
            "404",
            "٤٠٤");
        Add("Status.NotFound.Title",
            "Deze pagina bestaat niet",
            "This page does not exist",
            "Ta strona nie istnieje",
            "Această pagină nu există",
            "هذه الصفحة غير موجودة");
        Add("Status.NotFound.Lead",
            "Misschien is de link oud, of zit er een typfout in. Geen zorgen, we helpen je verder.",
            "The link may be old, or there is a typo in it. No worries, we will help you on your way.",
            "Link może być stary albo jest w nim literówka. Spokojnie, pomożemy ci dalej.",
            "Poate linkul este vechi sau are o greșeală de scriere. Nicio grijă, te ajutăm mai departe.",
            "ربما يكون الرابط قديماً أو فيه خطأ مطبعي. لا تقلق، سنساعدك للمتابعة.");

        // —— 500 ——
        Add("Status.Error.Eyebrow",
            "Foutje bij ons",
            "A slip on our side",
            "Potknięcie po naszej stronie",
            "O mică eroare la noi",
            "خطأ من جانبنا");
        Add("Status.Error.Title",
            "Er ging iets mis",
            "Something went wrong",
            "Coś poszło nie tak",
            "Ceva nu a mers bine",
            "حدث خطأ ما");
        Add("Status.Error.Lead",
            "Het ligt niet aan jou. Probeer het zo nog eens. Lukt het niet? Stuur ons de code hieronder.",
            "It is not your fault. Try again in a moment. Still stuck? Send us the code below.",
            "To nie twoja wina. Spróbuj za chwilę jeszcze raz. Nadal nie działa? Wyślij nam kod poniżej.",
            "Nu este vina ta. Mai încearcă o dată peste puțin timp. Tot nu merge? Trimite-ne codul de mai jos.",
            "الأمر ليس بسببك. جرّب مرة أخرى بعد قليل. ما زال لا يعمل؟ أرسل لنا الرمز أدناه.");

        // —— Generic status (codes without their own page yet) ——
        Add("Status.Generic.Title",
            "Deze pagina kan nu niet geopend worden",
            "This page cannot be opened right now",
            "Tej strony nie można teraz otworzyć",
            "Această pagină nu poate fi deschisă acum",
            "لا يمكن فتح هذه الصفحة الآن");
        Add("Status.Generic.Lead",
            "Probeer het zo nog eens, of ga terug naar het begin.",
            "Try again in a moment, or go back to the start.",
            "Spróbuj za chwilę jeszcze raz albo wróć na początek.",
            "Mai încearcă peste puțin timp sau întoarce-te la început.",
            "جرّب مرة أخرى بعد قليل أو عُد إلى البداية.");

        // —— Shared actions and small print ——
        Add("Status.Common.Banenkaart",
            "Banenkaart",
            "Job map",
            "Mapa pracy",
            "Harta joburilor",
            "خريطة الوظائف");
        Add("Status.Common.Passport",
            "Mijn Paspoort",
            "My Passport",
            "Mój Paszport",
            "Pașaportul meu",
            "جوازي");
        Add("Status.Common.FreeTest",
            "Gratis test",
            "Free test",
            "Darmowy test",
            "Test gratuit",
            "اختبار مجاني");
        Add("Status.Common.Help",
            "Hulp",
            "Help",
            "Pomoc",
            "Ajutor",
            "مساعدة");
        Add("Status.Common.TryAgain",
            "Probeer opnieuw",
            "Try again",
            "Spróbuj ponownie",
            "Încearcă din nou",
            "حاول مرة أخرى");
        Add("Status.Common.MailSupport",
            "Mail support",
            "E-mail support",
            "Napisz do wsparcia",
            "Scrie-ne la suport",
            "راسل الدعم");
        Add("Status.Common.CodeLabel",
            "Foutcode",
            "Error code",
            "Kod błędu",
            "Cod de eroare",
            "رمز الخطأ");
        Add("Status.Common.Copy",
            "Kopieer",
            "Copy",
            "Kopiuj",
            "Copiază",
            "انسخ");
        Add("Status.Common.Copied",
            "Gekopieerd",
            "Copied",
            "Skopiowano",
            "Copiat",
            "تم النسخ");
        Add("Status.Common.SupportLine",
            "Klopt er iets niet? Mail {0}.",
            "Does something look wrong? E-mail {0}.",
            "Coś się nie zgadza? Napisz na {0}.",
            "Ceva nu este în regulă? Scrie la {0}.",
            "هل هناك شيء غير صحيح؟ راسل {0}.");
        Add("Status.Common.MailSubject",
            "Foutcode {0}",
            "Error code {0}",
            "Kod błędu {0}",
            "Cod de eroare {0}",
            "رمز الخطأ {0}");

        // —— ErrorLayout chrome ——
        Add("Status.Nav.SkipToContent",
            "Naar de inhoud",
            "Skip to content",
            "Przejdź do treści",
            "Sari la conținut",
            "انتقل إلى المحتوى");
        Add("Status.Nav.Home",
            "Naar de startpagina",
            "To the home page",
            "Na stronę główną",
            "Spre pagina principală",
            "إلى الصفحة الرئيسية");
        Add("Status.Nav.ChooseLanguage",
            "Kies je taal",
            "Choose your language",
            "Wybierz język",
            "Alege limba",
            "اختر لغتك");
        Add("Status.Nav.Login",
            "Inloggen",
            "Sign in",
            "Zaloguj się",
            "Autentificare",
            "تسجيل الدخول");
        Add("Status.Nav.MyStart",
            "Naar mijn start",
            "To my start page",
            "Do mojej strony startowej",
            "Spre pagina mea de start",
            "إلى صفحتي");
        Add("Status.Nav.Privacy",
            "Privacy",
            "Privacy",
            "Prywatność",
            "Confidențialitate",
            "الخصوصية");
        Add("Status.Nav.Terms",
            "Voorwaarden",
            "Terms",
            "Warunki",
            "Termeni",
            "الشروط");
        Add("Status.Nav.Help",
            "Hulp",
            "Help",
            "Pomoc",
            "Ajutor",
            "مساعدة");
    }
}
