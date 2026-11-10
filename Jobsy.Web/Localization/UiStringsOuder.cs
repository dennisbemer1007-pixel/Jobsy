namespace Jobsy.Web.Localization;

public static class UiStringsOuder
{
    public static void MergeAll(
        IDictionary<string, string> nl,
        IDictionary<string, string> en,
        IDictionary<string, string> pl,
        IDictionary<string, string> ro,
        IDictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string plText, string roText, string arText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText;
            ro[key] = roText;
            ar[key] = arText;
        }

        Add("Ouder.Seo.Title",
            "Leerlingpaspoort voor ouders",
            "Pupil passport for parents",
            "Paszport ucznia dla rodziców",
            "Pașaport elev pentru părinți",
            "جواز التلميذ للوالدين");

        Add("Ouder.Page.Title",
            "Leerlingpaspoort bekijken",
            "View the pupil passport",
            "Zobacz paszport ucznia",
            "Vezi pașaportul elevului",
            "عرض جواز التلميذ");

        Add("Ouder.Page.Lead",
            "Vul de leerlingcode in die je van school kreeg. Je ziet daarna het paspoort alleen om te lezen.",
            "Enter the pupil code you received from school. You can then read the passport.",
            "Wpisz kod ucznia od szkoły. Potem możesz tylko przeczytać paszport.",
            "Introdu codul de elev de la școală. Apoi poți doar citi pașaportul.",
            "أدخل رمز التلميذ من المدرسة. بعدها يمكنك قراءة الجواز فقط.");

        Add("Ouder.Form.Code", "Leerlingcode", "Pupil code", "Kod ucznia", "Cod elev", "رمز التلميذ");
        Add("Ouder.Form.Submit", "Paspoort openen", "Open passport", "Otwórz paszport", "Deschide pașaportul", "فتح الجواز");

        Add("Ouder.Error.Invalid",
            "Deze code klopt niet of is niet meer geldig. Vraag de leraar om een nieuwe code.",
            "This code is not valid or has expired. Ask the teacher for a new code.",
            "Ten kod jest nieprawidłowy lub wygasł. Poproś nauczyciela o nowy kod.",
            "Codul nu este valid sau a expirat. Cere profesorului un cod nou.",
            "الرمز غير صالح أو منتهٍ. اطلب من المعلّم رمزاً جديداً.");

        Add("Ouder.Error.Cooldown",
            "Even wachten en opnieuw proberen.",
            "Please wait a moment and try again.",
            "Poczekaj chwilę i spróbuj ponownie.",
            "Așteaptă puțin și încearcă din nou.",
            "انتظر لحظة وحاول مرة أخرى.");

        Add("Ouder.Error.Retry", "Probeer het opnieuw.", "Please try again.", "Spróbuj ponownie.", "Încearcă din nou.", "حاول مرة أخرى.");

        Add("Ouder.Error.Staff",
            "Log eerst uit als medewerker of kandidaat.",
            "Sign out as staff or candidate first.",
            "Wyloguj się najpierw jako pracownik lub kandydat.",
            "Deconectează-te mai întâi ca personal sau candidat.",
            "سجّل الخروج أولاً كموظف أو مرشح.");

        Add("Ouder.Error.Expired",
            "Je sessie is verlopen. Vul de code opnieuw in.",
            "Your session has expired. Enter the code again.",
            "Sesja wygasła. Wpisz kod ponownie.",
            "Sesiunea a expirat. Introdu codul din nou.",
            "انتهت الجلسة. أدخل الرمز مرة أخرى.");

        Add("Ouder.Passport.Pdf", "Paspoort (pdf)", "Passport (PDF)", "Paszport (PDF)", "Pașaport (PDF)", "الجواز (PDF)");
        Add("Ouder.Answers.Pdf", "Antwoorden (pdf)", "Answers (PDF)", "Odpowiedzi (PDF)", "Răspunsuri (PDF)", "الإجابات (PDF)");
        Add("Ouder.Downloads.Aria", "Downloads", "Downloads", "Pobrania", "Descărcări", "التنزيلات");
        Add("Ouder.Passport.OtherCode", "Andere code", "Different code", "Inny kod", "Alt cod", "رمز آخر");

        Add("Ouder.FeatureDisabled",
            "Deze pagina is nu niet beschikbaar.",
            "This page is not available right now.",
            "Ta strona nie jest teraz dostępna.",
            "Această pagină nu este disponibilă acum.",
            "هذه الصفحة غير متاحة الآن.");
    }
}
