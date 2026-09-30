namespace Jobsy.Web.Localization;

/// <summary>Candidate self sign-up / passwordless e-mail code. Prefix: Signup.*</summary>
public static class UiStringsCandidateSignup
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText, string? plText = null, string? roText = null, string? arText = null)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = plText ?? enText;
            ro[key] = roText ?? enText;
            ar[key] = arText ?? enText;
        }

        Add("Signup.Title", "Maak je gratis account", "Create your free account",
            "Utwórz darmowe konto", "Creează-ți contul gratuit", "أنشئ حسابك المجاني");
        Add("Signup.Lead", "Bewaar je resultaat en ga verder waar je was.",
            "Save your result and continue where you left off.",
            "Zapisz wynik i kontynuuj od miejsca, w którym skończyłeś.",
            "Salvează rezultatul și continuă de unde ai rămas.",
            "احفظ نتيجتك وتابع من حيث توقفت.");
        Add("Signup.Google", "Doorgaan met Google", "Continue with Google",
            "Kontynuuj z Google", "Continuă cu Google", "المتابعة مع Google");
        Add("Signup.Microsoft", "Doorgaan met Microsoft", "Continue with Microsoft",
            "Kontynuuj z Microsoft", "Continuă cu Microsoft", "المتابعة مع Microsoft");
        Add("Signup.Email", "E-mailadres", "E-mail address", "Adres e-mail", "Adresă de e-mail", "البريد الإلكتروني");
        Add("Signup.FirstName", "Hoe mogen we je noemen?", "What may we call you?",
            "Jak możemy się do Ciebie zwracać?", "Cum să te numim?", "بماذا نناديك؟");
        Add("Signup.FirstNameOptional", "Optioneel", "Optional", "Opcjonalne", "Opțional", "اختياري");
        Add("Signup.SendCode", "Stuur mijn code", "Send my code",
            "Wyślij mój kod", "Trimite codul meu", "أرسل رمزي");
        Add("Signup.Consent",
            "Door verder te gaan ga je akkoord met de {0} en de {1}.",
            "By continuing you agree to the {0} and the {1}.",
            "Kontynuując, akceptujesz {0} i {1}.",
            "Continuând, ești de acord cu {0} și {1}.",
            "بالمتابعة فإنك توافق على {0} و{1}.");
        Add("Signup.ConsentTerms", "voorwaarden", "terms", "warunki", "termeni", "الشروط");
        Add("Signup.ConsentPrivacy", "privacyverklaring", "privacy policy", "politykę prywatności", "politica de confidențialitate", "سياسة الخصوصية");
        Add("Signup.HaveAccount", "Heb je al een account?", "Already have an account?",
            "Masz już konto?", "Ai deja un cont?", "هل لديك حساب بالفعل؟");
        Add("Signup.Login", "Inloggen", "Log in", "Zaloguj się", "Autentificare", "تسجيل الدخول");
        Add("Signup.EmployerLead", "Ben je werkgever?", "Are you an employer?",
            "Jesteś pracodawcą?", "Ești angajator?", "هل أنت صاحب عمل؟");
        Add("Signup.EmployerCta", "Bedrijf registreren", "Register your company",
            "Zarejestruj firmę", "Înregistrează compania", "تسجيل الشركة");
        Add("Signup.Under16",
            "Ben je jonger dan 16? Na het aanmaken vragen we toestemming aan je ouder of voogd.",
            "Are you under 16? After sign-up we ask your parent or guardian for consent.",
            "Masz mniej niż 16 lat? Po utworzeniu konta poprosimy rodzica lub opiekuna o zgodę.",
            "Ai sub 16 ani? După creare cerem acordul părintelui sau tutorelui.",
            "هل عمرك أقل من 16؟ بعد إنشاء الحساب نطلب موافقة ولي أمرك.");
        Add("Signup.ErrorRetry", "Er ging iets mis. Probeer het opnieuw.",
            "Something went wrong. Please try again.",
            "Coś poszło nie tak. Spróbuj ponownie.",
            "A apărut o eroare. Încearcă din nou.",
            "حدث خطأ. حاول مرة أخرى.");
        Add("Signup.ErrorEmail", "Vul een geldig e-mailadres in.",
            "Enter a valid e-mail address.",
            "Podaj prawidłowy adres e-mail.",
            "Introdu o adresă de e-mail validă.",
            "أدخل بريدًا إلكترونيًا صالحًا.");
        Add("Signup.ErrorTooMany", "Probeer het over een kwartier opnieuw.",
            "Please try again in about fifteen minutes.",
            "Spróbuj ponownie za około piętnaście minut.",
            "Încearcă din nou peste un sfert de oră.",
            "حاول مرة أخرى بعد ربع ساعة تقريبًا.");
        Add("Signup.ErrorExpired", "Vraag een nieuwe code aan.",
            "Request a new code.",
            "Poproś o nowy kod.",
            "Cere un cod nou.",
            "اطلب رمزًا جديدًا.");

        Add("Signup.Code.Title", "Vul je code in", "Enter your code",
            "Wpisz swój kod", "Introdu codul", "أدخل رمزك");
        Add("Signup.Code.Lead", "We hebben een code gestuurd naar {0}.",
            "We sent a code to {0}.",
            "Wysłaliśmy kod na {0}.",
            "Am trimis un cod la {0}.",
            "أرسلنا رمزًا إلى {0}.");
        Add("Signup.Code.Label", "6-cijferige code", "6-digit code",
            "6-cyfrowy kod", "Cod de 6 cifre", "رمز من 6 أرقام");
        Add("Signup.Code.Submit", "Bevestigen", "Confirm", "Potwierdź", "Confirmă", "تأكيد");
        Add("Signup.Code.Resend", "Nieuwe code sturen", "Send a new code",
            "Wyślij nowy kod", "Trimite un cod nou", "إرسال رمز جديد");
        Add("Signup.Code.OtherEmail", "Ander e-mailadres", "Use another e-mail",
            "Inny adres e-mail", "Altă adresă de e-mail", "بريد إلكتروني آخر");
        Add("Signup.Code.Invalid", "Die code klopt niet. Je hebt nog {0} pogingen.",
            "That code is incorrect. You have {0} attempts left.",
            "Ten kod jest nieprawidłowy. Pozostało Ci {0} prób.",
            "Codul este greșit. Mai ai {0} încercări.",
            "الرمز غير صحيح. لديك {0} محاولات متبقية.");
        Add("Signup.Code.InvalidGeneric", "Die code klopt niet.",
            "That code is incorrect.",
            "Ten kod jest nieprawidłowy.",
            "Codul este greșit.",
            "الرمز غير صحيح.");

        Add("Signup.Seo.Title", "Account maken", "Create account",
            "Utwórz konto", "Creează cont", "إنشاء حساب");
        Add("Signup.Seo.Description", "Maak gratis een Lobsy-account met Google, Microsoft of e-mail.",
            "Create a free Lobsy account with Google, Microsoft or e-mail.",
            "Utwórz darmowe konto Lobsy przez Google, Microsoft lub e-mail.",
            "Creează un cont Lobsy gratuit cu Google, Microsoft sau e-mail.",
            "أنشئ حساب Lobsy مجانًا عبر Google أو Microsoft أو البريد.");

        Add("Login.CreateAccountLead", "Nieuw bij Lobsy?", "New to Lobsy?",
            "Nowy w Lobsy?", "Nou pe Lobsy?", "جديد على Lobsy؟");
        Add("Login.CreateAccountCta", "Maak gratis account", "Create a free account",
            "Utwórz darmowe konto", "Creează un cont gratuit", "أنشئ حسابًا مجانيًا");
    }
}
