namespace Jobsy.Web.Localization;

/// <summary>Auth stack login copy (file 03). Overwrites Login.* keys in all 5 languages.</summary>
public static class UiStringsAuth
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

        Add("Login.Eyebrow",
            "Welkom terug",
            "Welcome back",
            "Witamy ponownie",
            "Bine ai revenit",
            "مرحباً بعودتك");
        Add("Login.Title",
            "Inloggen bij Lobsy",
            "Sign in to Lobsy",
            "Zaloguj się do Lobsy",
            "Autentificare pe Lobsy",
            "تسجيل الدخول إلى Lobsy");
        Add("Login.Lead",
            "Kies hoe je wilt inloggen.",
            "Choose how you want to sign in.",
            "Wybierz, jak chcesz się zalogować.",
            "Alege cum vrei să te autentifici.",
            "اختر كيف تريد تسجيل الدخول.");
        Add("Login.OrEmail",
            "of met je e-mailadres",
            "or with your e-mail address",
            "lub swoim e-mailem",
            "sau cu adresa ta de e-mail",
            "أو بعنوان بريدك");
        Add("Login.Email",
            "E-mailadres",
            "E-mail address",
            "Adres e-mail",
            "Adresă de e-mail",
            "عنوان البريد");
        Add("Login.ForgotPassword",
            "Wachtwoord vergeten?",
            "Forgot password?",
            "Nie pamiętasz hasła?",
            "Ai uitat parola?",
            "نسيت كلمة المرور؟");
        Add("Login.ShowPassword",
            "Toon",
            "Show",
            "Pokaż",
            "Arată",
            "إظهار");
        Add("Login.HidePassword",
            "Verberg",
            "Hide",
            "Ukryj",
            "Ascunde",
            "إخفاء");
        Add("Login.EmployersLead",
            "Voor werkgevers:",
            "For employers:",
            "Dla pracodawców:",
            "Pentru angajatori:",
            "لأصحاب العمل:");
        Add("Login.RegisterLead",
            "Voor werkgevers:",
            "For employers:",
            "Dla pracodawców:",
            "Pentru angajatori:",
            "لأصحاب العمل:");
        Add("Login.RegisterCta",
            "Bedrijf registreren (KvK)",
            "Register company (KvK)",
            "Zarejestruj firmę (KvK)",
            "Înregistrează compania (KvK)",
            "تسجيل الشركة (KvK)");
        Add("Login.CreateAccountLead",
            "Nieuw bij Lobsy?",
            "New to Lobsy?",
            "Nowy w Lobsy?",
            "Nou pe Lobsy?",
            "جديد على Lobsy؟");
        Add("Login.CreateAccountCta",
            "Maak gratis een account",
            "Create a free account",
            "Utwórz darmowe konto",
            "Creează un cont gratuit",
            "أنشئ حسابًا مجانيًا");
        Add("Login.CreateAccountProvidersOnly",
            "Nieuw bij Lobsy? Log in met Microsoft of Google. Dan maken we je account.",
            "New to Lobsy? Sign in with Microsoft or Google. We will create your account.",
            "Nowy w Lobsy? Zaloguj się przez Microsoft lub Google. Utworzymy konto.",
            "Nou pe Lobsy? Autentifică-te cu Microsoft sau Google. Îți creăm contul.",
            "جديد على Lobsy؟ سجّل الدخول عبر Microsoft أو Google. سننشئ حسابك.");
        Add("Login.ErrorInvalidTitle",
            "Dat klopt niet helemaal",
            "That is not quite right",
            "To nie jest do końca poprawne",
            "Asta nu e chiar corect",
            "هذا غير صحيح تمامًا");
        Add("Login.ErrorInvalid",
            "Het e-mailadres of wachtwoord is niet goed. Probeer het nog eens.",
            "The e-mail address or password is wrong. Please try again.",
            "Adres e-mail lub hasło jest nieprawidłowe. Spróbuj ponownie.",
            "Adresa de e-mail sau parola este greșită. Încearcă din nou.",
            "عنوان البريد أو كلمة المرور غير صحيحة. حاول مرة أخرى.");
        Add("Login.PauseTitle",
            "Even pauze",
            "A short pause",
            "Krótka przerwa",
            "O scurtă pauză",
            "استراحة قصيرة");
        Add("Login.ErrorLocked",
            "Er is te vaak een verkeerd wachtwoord ingevuld. Daarom kun je nu niet inloggen met je wachtwoord. Zo houden we je account veilig.",
            "A wrong password was entered too often. You cannot sign in with your password right now. This keeps your account safe.",
            "Zbyt często wpisano złe hasło. Dlatego nie możesz teraz zalogować się hasłem. Tak chronimy konto.",
            "Parola greșită a fost introdusă prea des. De aceea nu te poți autentifica acum cu parola. Așa îți protejăm contul.",
            "أُدخلت كلمة مرور خاطئة كثيرًا. لذلك لا يمكنك تسجيل الدخول بكلمة المرور الآن. هكذا نحمي حسابك.");
        Add("Login.PauseRetry",
            "Probeer het weer om {0}",
            "Try again at {0}",
            "Spróbuj ponownie o {0}",
            "Încearcă din nou la {0}",
            "حاول مرة أخرى الساعة {0}");
        Add("Login.PauseNotYou",
            "Was jij dit niet? Dan kan iemand anders je wachtwoord hebben geprobeerd. Kies een nieuw wachtwoord of mail support.",
            "Was that not you? Someone else may have tried your password. Choose a new password or mail support.",
            "To nie ty? Ktoś inny mógł próbować twojego hasła. Wybierz nowe hasło lub napisz do supportu.",
            "Nu ai fost tu? Altcineva poate ți-a încercat parola. Alege o parolă nouă sau scrie la support.",
            "أكنت أنت؟ ربما حاول شخص آخر كلمة مرورك. اختر كلمة مرور جديدة أو راسل الدعم.");
        Add("Login.BackToLogin",
            "Terug naar inloggen",
            "Back to sign-in",
            "Wróć do logowania",
            "Înapoi la autentificare",
            "العودة لتسجيل الدخول");
        Add("Login.ChooseNewPassword",
            "Nieuw wachtwoord kiezen",
            "Choose a new password",
            "Wybierz nowe hasło",
            "Alege o parolă nouă",
            "اختر كلمة مرور جديدة");
        Add("Login.SetupDone",
            "Je wachtwoord is opgeslagen. Log nu in.",
            "Your password is saved. Sign in now.",
            "Hasło zostało zapisane. Zaloguj się teraz.",
            "Parola a fost salvată. Autentifică-te acum.",
            "تم حفظ كلمة المرور. سجّل الدخول الآن.");
        Add("Login.ErrorSessionExpired",
            "Je sessie is verlopen. Log opnieuw in. Je komt terug waar je was.",
            "Your session expired. Sign in again. You will return where you were.",
            "Sesja wygasła. Zaloguj się ponownie. Wrócisz tam, gdzie byłeś.",
            "Sesiunea a expirat. Autentifică-te din nou. Revii unde erai.",
            "انتهت جلستك. سجّل الدخول مجددًا. ستعود إلى حيث كنت.");
        Add("Login.ErrorTooMany",
            "Te veel pogingen vanaf dit apparaat. Probeer het om {0} opnieuw.",
            "Too many attempts from this device. Please try again at {0}.",
            "Zbyt wiele prób z tego urządzenia. Spróbuj ponownie o {0}.",
            "Prea multe încercări de pe acest dispozitiv. Încearcă din nou la {0}.",
            "محاولات كثيرة من هذا الجهاز. حاول مجددًا الساعة {0}.");
    }
}
