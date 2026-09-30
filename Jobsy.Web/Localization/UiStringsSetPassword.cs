namespace Jobsy.Web.Localization;

internal static class UiStringsSetPassword
{
    public static void MergeAll(
        Dictionary<string, string> nl,
        Dictionary<string, string> en,
        Dictionary<string, string> pl,
        Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        foreach (var (k, v) in Nl) nl[k] = v;
        foreach (var (k, v) in En) en[k] = v;
        foreach (var (k, v) in Pl) pl[k] = v;
        foreach (var (k, v) in Ro) ro[k] = v;
        foreach (var (k, v) in Ar) ar[k] = v;
    }

    private static readonly Dictionary<string, string> Nl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SetPassword.Title"] = "Wachtwoord instellen",
        ["SetPassword.Heading"] = "Kies je wachtwoord",
        ["SetPassword.Hint"] = "Minimaal 12 en maximaal 128 tekens.",
        ["SetPassword.Password"] = "Wachtwoord",
        ["SetPassword.Repeat"] = "Herhaal wachtwoord",
        ["SetPassword.Submit"] = "Wachtwoord opslaan",
        ["SetPassword.OrExternal"] = "Of log in met Google of Microsoft",
        ["SetPassword.Invalid"] = "Deze link werkt niet meer. Vraag degene die je uitnodigde om een nieuwe uitnodiging.",
        ["SetPassword.Mismatch"] = "De wachtwoorden komen niet overeen.",
        ["SetPassword.Login"] = "Naar inloggen",
        ["SetPassword.Done"] = "Je wachtwoord is opgeslagen. Log nu in.",
        ["SetPassword.Seo.Title"] = "Wachtwoord instellen",
        ["SetPassword.Seo.Description"] = "Kies een wachtwoord voor je Lobsy-account.",
        ["ApiKeyReveal.Title"] = "API-sleutel ophalen",
        ["ApiKeyReveal.Heading"] = "API-sleutel voor {0}",
        ["ApiKeyReveal.Warn"] = "Je ziet de sleutel maar één keer. Bewaar hem meteen in je wachtwoordkluis of je systeem.",
        ["ApiKeyReveal.Submit"] = "Toon de sleutel",
        ["ApiKeyReveal.Invalid"] = "Deze link werkt niet meer. Vraag een nieuwe aan via Lobsy (Bedrijfsgegevens → API).",
        ["ApiKeyReveal.Copy"] = "Kopiëren",
        ["ApiKeyReveal.Endpoint"] = "Endpoint",
        ["ApiKeyReveal.Header"] = "Header",
        ["ApiKeyReveal.Docs"] = "API-documentatie",
        ["ApiKeyReveal.Seo.Title"] = "API-sleutel",
        ["ApiKeyReveal.Seo.Description"] = "Haal je Lobsy API-sleutel eenmalig op."
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SetPassword.Title"] = "Set password",
        ["SetPassword.Heading"] = "Choose your password",
        ["SetPassword.Hint"] = "At least 12 and at most 128 characters.",
        ["SetPassword.Password"] = "Password",
        ["SetPassword.Repeat"] = "Repeat password",
        ["SetPassword.Submit"] = "Save password",
        ["SetPassword.OrExternal"] = "Or sign in with Google or Microsoft",
        ["SetPassword.Invalid"] = "This link no longer works. Ask the person who invited you for a new invitation.",
        ["SetPassword.Mismatch"] = "The passwords do not match.",
        ["SetPassword.Login"] = "Go to login",
        ["SetPassword.Done"] = "Your password was saved. Sign in now.",
        ["SetPassword.Seo.Title"] = "Set password",
        ["SetPassword.Seo.Description"] = "Choose a password for your Lobsy account.",
        ["ApiKeyReveal.Title"] = "Retrieve API key",
        ["ApiKeyReveal.Heading"] = "API key for {0}",
        ["ApiKeyReveal.Warn"] = "You will see the key only once. Store it in your password manager or system immediately.",
        ["ApiKeyReveal.Submit"] = "Show the key",
        ["ApiKeyReveal.Invalid"] = "This link no longer works. Request a new one via Lobsy (Company details → API).",
        ["ApiKeyReveal.Copy"] = "Copy",
        ["ApiKeyReveal.Endpoint"] = "Endpoint",
        ["ApiKeyReveal.Header"] = "Header",
        ["ApiKeyReveal.Docs"] = "API documentation",
        ["ApiKeyReveal.Seo.Title"] = "API key",
        ["ApiKeyReveal.Seo.Description"] = "Retrieve your Lobsy API key once."
    };

    private static readonly Dictionary<string, string> Pl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SetPassword.Title"] = "Ustaw hasło",
        ["SetPassword.Heading"] = "Wybierz hasło",
        ["SetPassword.Hint"] = "Minimum 12 i maksimum 128 znaków.",
        ["SetPassword.Password"] = "Hasło",
        ["SetPassword.Repeat"] = "Powtórz hasło",
        ["SetPassword.Submit"] = "Zapisz hasło",
        ["SetPassword.OrExternal"] = "Lub zaloguj się przez Google albo Microsoft",
        ["SetPassword.Invalid"] = "Ten link już nie działa. Poproś o nowe zaproszenie.",
        ["SetPassword.Mismatch"] = "Hasła nie są takie same.",
        ["SetPassword.Login"] = "Przejdź do logowania",
        ["SetPassword.Done"] = "Hasło zapisane. Zaloguj się.",
        ["SetPassword.Seo.Title"] = "Ustaw hasło",
        ["SetPassword.Seo.Description"] = "Wybierz hasło do konta Lobsy.",
        ["ApiKeyReveal.Title"] = "Pobierz klucz API",
        ["ApiKeyReveal.Heading"] = "Klucz API dla {0}",
        ["ApiKeyReveal.Warn"] = "Klucz zobaczysz tylko raz. Zapisz go od razu.",
        ["ApiKeyReveal.Submit"] = "Pokaż klucz",
        ["ApiKeyReveal.Invalid"] = "Ten link już nie działa. Poproś o nowy w Lobsy.",
        ["ApiKeyReveal.Copy"] = "Kopiuj",
        ["ApiKeyReveal.Endpoint"] = "Endpoint",
        ["ApiKeyReveal.Header"] = "Nagłówek",
        ["ApiKeyReveal.Docs"] = "Dokumentacja API",
        ["ApiKeyReveal.Seo.Title"] = "Klucz API",
        ["ApiKeyReveal.Seo.Description"] = "Pobierz klucz API Lobsy raz."
    };

    private static readonly Dictionary<string, string> Ro = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SetPassword.Title"] = "Setează parola",
        ["SetPassword.Heading"] = "Alege parola",
        ["SetPassword.Hint"] = "Minim 12 și maxim 128 de caractere.",
        ["SetPassword.Password"] = "Parolă",
        ["SetPassword.Repeat"] = "Repetă parola",
        ["SetPassword.Submit"] = "Salvează parola",
        ["SetPassword.OrExternal"] = "Sau autentifică-te cu Google sau Microsoft",
        ["SetPassword.Invalid"] = "Acest link nu mai funcționează. Cere o invitație nouă.",
        ["SetPassword.Mismatch"] = "Parolele nu coincid.",
        ["SetPassword.Login"] = "Mergi la autentificare",
        ["SetPassword.Done"] = "Parola a fost salvată. Autentifică-te acum.",
        ["SetPassword.Seo.Title"] = "Setează parola",
        ["SetPassword.Seo.Description"] = "Alege o parolă pentru contul Lobsy.",
        ["ApiKeyReveal.Title"] = "Preia cheia API",
        ["ApiKeyReveal.Heading"] = "Cheie API pentru {0}",
        ["ApiKeyReveal.Warn"] = "Vezi cheia o singură dată. Salveaz-o imediat.",
        ["ApiKeyReveal.Submit"] = "Arată cheia",
        ["ApiKeyReveal.Invalid"] = "Acest link nu mai funcționează. Cere unul nou în Lobsy.",
        ["ApiKeyReveal.Copy"] = "Copiază",
        ["ApiKeyReveal.Endpoint"] = "Endpoint",
        ["ApiKeyReveal.Header"] = "Header",
        ["ApiKeyReveal.Docs"] = "Documentație API",
        ["ApiKeyReveal.Seo.Title"] = "Cheie API",
        ["ApiKeyReveal.Seo.Description"] = "Preia o dată cheia API Lobsy."
    };

    private static readonly Dictionary<string, string> Ar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SetPassword.Title"] = "تعيين كلمة المرور",
        ["SetPassword.Heading"] = "اختر كلمة المرور",
        ["SetPassword.Hint"] = "١٢ حرفًا على الأقل و١٢٨ كحد أقصى.",
        ["SetPassword.Password"] = "كلمة المرور",
        ["SetPassword.Repeat"] = "أعد كلمة المرور",
        ["SetPassword.Submit"] = "حفظ كلمة المرور",
        ["SetPassword.OrExternal"] = "أو سجّل الدخول عبر Google أو Microsoft",
        ["SetPassword.Invalid"] = "هذا الرابط لم يعد يعمل. اطلب دعوة جديدة.",
        ["SetPassword.Mismatch"] = "كلمتا المرور غير متطابقتين.",
        ["SetPassword.Login"] = "الانتقال لتسجيل الدخول",
        ["SetPassword.Done"] = "تم حفظ كلمة المرور. سجّل الدخول الآن.",
        ["SetPassword.Seo.Title"] = "تعيين كلمة المرور",
        ["SetPassword.Seo.Description"] = "اختر كلمة مرور لحساب Lobsy.",
        ["ApiKeyReveal.Title"] = "استرداد مفتاح API",
        ["ApiKeyReveal.Heading"] = "مفتاح API لـ {0}",
        ["ApiKeyReveal.Warn"] = "سترى المفتاح مرة واحدة فقط. احفظه فورًا.",
        ["ApiKeyReveal.Submit"] = "أظهر المفتاح",
        ["ApiKeyReveal.Invalid"] = "هذا الرابط لم يعد يعمل. اطلب رابطًا جديدًا عبر Lobsy.",
        ["ApiKeyReveal.Copy"] = "نسخ",
        ["ApiKeyReveal.Endpoint"] = "Endpoint",
        ["ApiKeyReveal.Header"] = "Header",
        ["ApiKeyReveal.Docs"] = "وثائق API",
        ["ApiKeyReveal.Seo.Title"] = "مفتاح API",
        ["ApiKeyReveal.Seo.Description"] = "استرد مفتاح API لـ Lobsy مرة واحدة."
    };
}
