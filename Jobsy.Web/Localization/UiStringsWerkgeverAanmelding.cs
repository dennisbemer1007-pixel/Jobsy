namespace Jobsy.Web.Localization;

/// <summary>UI strings for werkgever-aanmelding (verification, wizard, banners).</summary>
internal static class UiStringsWerkgeverAanmelding
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

        Add("WaBanner.PreviewLine",
            "Voorbeeld · nog niet zichtbaar voor kandidaten",
            "Preview · not yet visible to candidates",
            "Podgląd · jeszcze niewidoczny dla kandydatów",
            "Previzualizare · încă nevăzut de candidați",
            "معاينة · غير مرئي للمرشحين بعد");

        Add("WaBanner.Blocked.Publish",
            "Je bedrijf is nog niet geverifieerd. Publiceren volgt na verificatie.",
            "Your company is not verified yet. Publishing unlocks after verification.",
            "Twoja firma nie jest jeszcze zweryfikowana. Publikacja po weryfikacji.",
            "Firma ta nu este încă verificată. Publicarea urmează după verificare.",
            "شركتك غير موثّقة بعد. النشر بعد التحقق.");

        Add("WaBanner.Blocked.Tokens",
            "Je bedrijf is nog niet geverifieerd. Tokens kopen volgt na verificatie.",
            "Your company is not verified yet. Buying tokens unlocks after verification.",
            "Twoja firma nie jest jeszcze zweryfikowana. Zakup tokenów po weryfikacji.",
            "Firma ta nu este încă verificată. Cumpărarea de tokeni urmează după verificare.",
            "شركتك غير موثّقة بعد. شراء الرموز بعد التحقق.");

        Add("WaBanner.Blocked.Candidates",
            "Je bedrijf is nog niet geverifieerd. Kandidatengegevens volgen na verificatie.",
            "Your company is not verified yet. Candidate data unlocks after verification.",
            "Twoja firma nie jest jeszcze zweryfikowana. Dane kandydatów po weryfikacji.",
            "Firma ta nu este încă verificată. Datele candidaților urmează după verificare.",
            "شركتك غير موثّقة بعد. بيانات المرشحين بعد التحقق.");

        Add("WaBanner.ReadyCta",
            "Klaarzetten · gaat live na verificatie",
            "Mark ready · goes live after verification",
            "Oznacz jako gotowe · start po weryfikacji",
            "Pregătește · apare după verificare",
            "جهّز · يُنشر بعد التحقق");

        Add("WaBanner.ReadyStatus",
            "Klaar · gaat live na verificatie",
            "Ready · goes live after verification",
            "Gotowe · start po weryfikacji",
            "Gata · apare după verificare",
            "جاهز · يُنشر بعد التحقق");
    }
}
