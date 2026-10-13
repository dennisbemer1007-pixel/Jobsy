namespace Jobsy.Web.Localization;

/// <summary>Copy for forced 2FA enrollment / prompt / recovery-code pages.</summary>
public static class UiStringsMfa
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

        Add("Mfa.SetupTitle",
            "Beveilig je account",
            "Secure your account",
            "Zabezpiecz swoje konto",
            "Securizează-ți contul",
            "أمّن حسابك");
        Add("Mfa.SetupLead",
            "Je hebt een app nodig die codes maakt, zoals Microsoft Authenticator of Google Authenticator.",
            "You need an app that creates codes, such as Microsoft Authenticator or Google Authenticator.",
            "Potrzebujesz aplikacji, która tworzy kody, np. Microsoft Authenticator lub Google Authenticator.",
            "Ai nevoie de o aplicație care creează coduri, cum ar fi Microsoft Authenticator sau Google Authenticator.",
            "تحتاج إلى تطبيق ينشئ رموزًا مثل Microsoft Authenticator أو Google Authenticator.");
        Add("Mfa.StepOf",
            "Stap {0} van {1}",
            "Step {0} of {1}",
            "Krok {0} z {1}",
            "Pasul {0} din {1}",
            "الخطوة {0} من {1}");
        Add("Mfa.ScanQr",
            "Scan de QR-code met je app",
            "Scan the QR code with your app",
            "Zeskanuj kod QR aplikacją",
            "Scanează codul QR cu aplicația",
            "امسح رمز QR بتطبيقك");
        Add("Mfa.QrAlt",
            "QR-code om Lobsy te koppelen",
            "QR code to link Lobsy",
            "Kod QR do połączenia Lobsy",
            "Cod QR pentru conectarea Lobsy",
            "رمز QR لربط Lobsy");
        Add("Mfa.ManualKeySummary",
            "Lukt scannen niet? Vul deze sleutel in",
            "Can't scan? Enter this key instead",
            "Nie możesz skanować? Wpisz ten klucz",
            "Nu poți scana? Introdu această cheie",
            "تعذر المسح؟ أدخل هذا المفتاح");
        Add("Mfa.CopyKey",
            "Kopieer sleutel",
            "Copy key",
            "Kopiuj klucz",
            "Copiază cheia",
            "انسخ المفتاح");
        Add("Mfa.Instructions",
            "Open Microsoft Authenticator of Google Authenticator, kies account toevoegen, en scan de QR-code of vul de sleutel in.",
            "Open Microsoft Authenticator or Google Authenticator, add an account, then scan the QR code or enter the key.",
            "Otwórz Microsoft Authenticator lub Google Authenticator, dodaj konto i zeskanuj kod QR albo wpisz klucz.",
            "Deschide Microsoft Authenticator sau Google Authenticator, adaugă un cont și scanează codul QR sau introdu cheia.",
            "افتح Microsoft Authenticator أو Google Authenticator، أضف حسابًا، ثم امسح رمز QR أو أدخل المفتاح.");
        Add("Mfa.EnterCode",
            "Vul de 6 cijfers uit je app in",
            "Enter the 6 digits from your app",
            "Wpisz 6 cyfr z aplikacji",
            "Introdu cele 6 cifre din aplicație",
            "أدخل الأرقام الستة من تطبيقك");
        Add("Mfa.CodeLabel",
            "Code uit je app",
            "Code from your app",
            "Kod z aplikacji",
            "Codul din aplicație",
            "الرمز من تطبيقك");
        Add("Mfa.Confirm",
            "Koppelen",
            "Link",
            "Połącz",
            "Conectează",
            "ربط");
        Add("Mfa.PromptTitle",
            "Vul je code in",
            "Enter your code",
            "Wpisz kod",
            "Introdu codul",
            "أدخل رمزك");
        Add("Mfa.PromptLead",
            "Open je authenticator-app en typ de 6 cijfers.",
            "Open your authenticator app and type the 6 digits.",
            "Otwórz aplikację authenticator i wpisz 6 cyfr.",
            "Deschide aplicația authenticator și tastează cele 6 cifre.",
            "افتح تطبيق المصادقة واكتب الأرقام الستة.");
        Add("Mfa.RecoverySummary",
            "Geen toegang tot je app?",
            "No access to your app?",
            "Brak dostępu do aplikacji?",
            "Nu ai acces la aplicație?",
            "لا يمكنك الوصول إلى تطبيقك؟");
        Add("Mfa.RecoveryLabel",
            "Herstelcode",
            "Recovery code",
            "Kod odzyskiwania",
            "Cod de recuperare",
            "رمز الاسترداد");
        Add("Mfa.Login",
            "Inloggen",
            "Sign in",
            "Zaloguj się",
            "Autentificare",
            "تسجيل الدخول");
        Add("Mfa.AdminHelp",
            "Nieuwe telefoon en geen herstelcodes? Mail support",
            "New phone and no recovery codes? Mail support",
            "Nowy telefon i brak kodów? Napisz do supportu",
            "Telefon nou și fără coduri? Scrie la support",
            "هاتف جديد بلا رموز؟ راسل الدعم");
        Add("Mfa.KeyCopied",
            "Sleutel gekopieerd",
            "Key copied",
            "Klucz skopiowany",
            "Cheie copiată",
            "تم نسخ المفتاح");
        Add("Mfa.CodesCopied",
            "Gekopieerd",
            "Copied",
            "Skopiowano",
            "Copiat",
            "تم النسخ");
        Add("Mfa.ErrorLocked",
            "Even pauze. Er is te vaak een verkeerde code ingevuld. Probeer het om {0} opnieuw.",
            "A short pause. A wrong code was entered too often. Please try again at {0}.",
            "Krótka przerwa. Zbyt często wpisano zły kod. Spróbuj ponownie o {0}.",
            "O scurtă pauză. Cod greșit introdus prea des. Încearcă din nou la {0}.",
            "استراحة قصيرة. أُدخل رمز خاطئ كثيرًا. حاول مرة أخرى الساعة {0}.");
        Add("Mfa.ErrorTooManyUntil",
            "Te veel pogingen. Probeer het om {0} opnieuw.",
            "Too many attempts. Please try again at {0}.",
            "Zbyt wiele prób. Spróbuj ponownie o {0}.",
            "Prea multe încercări. Încearcă din nou la {0}.",
            "محاولات كثيرة جدًا. حاول مرة أخرى الساعة {0}.");
        Add("Mfa.ErrorLockedFallback",
            "Probeer het over een kwartier opnieuw.",
            "Please try again in about fifteen minutes.",
            "Spróbuj ponownie za około kwadrans.",
            "Încearcă din nou peste aproximativ cincisprezece minute.",
            "حاول مرة أخرى بعد حوالي ربع ساعة.");
        Add("Mfa.RecoveryUsedTitle",
            "Herstelcode gebruikt",
            "Recovery code used",
            "Użyto kodu odzyskiwania",
            "Cod de recuperare folosit",
            "تم استخدام رمز الاسترداد");
        Add("Mfa.RecoveryUsedLead",
            "Je bent ingelogd met een herstelcode. Je hebt er nog {0}.",
            "You signed in with a recovery code. You have {0} left.",
            "Zalogowałeś się kodem odzyskiwania. Pozostało ci {0}.",
            "Te-ai autentificat cu un cod de recuperare. Mai ai {0}.",
            "سجّلت الدخول برمز استرداد. تبقى لديك {0}.");
        Add("Mfa.RecoveryAlmostOut",
            "Bijna op. Maak nieuwe herstelcodes of vraag een beheerder om je 2FA opnieuw in te stellen.",
            "Almost out. Create new recovery codes or ask an admin to reset your 2FA.",
            "Prawie wyczerpane. Utwórz nowe kody odzyskiwania albo poproś administratora o ponowne ustawienie 2FA.",
            "Aproape epuizate. Creează coduri noi sau cere unui admin să reseteze 2FA.",
            "أوشكت على النفاد. أنشئ رموز استرداد جديدة أو اطلب من مشرف إعادة ضبط 2FA.");
        Add("Mfa.DownloadHeaderEmail",
            "Lobsy herstelcodes voor {0}",
            "Lobsy recovery codes for {0}",
            "Kody odzyskiwania Lobsy dla {0}",
            "Coduri de recuperare Lobsy pentru {0}",
            "رموز استرداد Lobsy لـ {0}");
        Add("Mfa.DownloadHeaderDate",
            "Gemaakt op {0}",
            "Created on {0}",
            "Utworzono {0}",
            "Create pe {0}",
            "أُنشئت في {0}");
        Add("Mfa.DownloadNote",
            "Elke code werkt één keer.",
            "Each code works once.",
            "Każdy kod działa raz.",
            "Fiecare cod funcționează o dată.",
            "كل رمز يعمل مرة واحدة.");
        Add("Mfa.RecoveryTitle",
            "Bewaar je herstelcodes",
            "Save your recovery codes",
            "Zachowaj kody odzyskiwania",
            "Păstrează codurile de recuperare",
            "احفظ رموز الاسترداد");
        Add("Mfa.RecoveryLead",
            "Kwijt je telefoon? Met één van deze codes kom je toch binnen. Elke code werkt één keer.",
            "Lost your phone? With one of these codes you can still get in. Each code works once.",
            "Zgubiłeś telefon? Jednym z tych kodów i tak wejdziesz. Każdy kod działa raz.",
            "Ți-ai pierdut telefonul? Cu unul din aceste coduri poți intra. Fiecare cod funcționează o dată.",
            "فقدت هاتفك؟ بأحد هذه الرموز تدخل أيضًا. كل رمز يعمل مرة واحدة.");
        Add("Mfa.DownloadTxt",
            "Download (.txt)",
            "Download (.txt)",
            "Pobierz (.txt)",
            "Descarcă (.txt)",
            "تنزيل (.txt)");
        Add("Mfa.CopyCodes",
            "Kopieer alles",
            "Copy all",
            "Kopiuj wszystko",
            "Copiază tot",
            "انسخ الكل");
        Add("Mfa.SavedCheckbox",
            "Ik heb mijn codes veilig bewaard",
            "I have stored my codes safely",
            "Zachowałem kody w bezpiecznym miejscu",
            "Am păstrat codurile în siguranță",
            "حفظت رموزي بأمان");
        Add("Mfa.Continue",
            "Verder naar Lobsy",
            "Continue to Lobsy",
            "Dalej do Lobsy",
            "Continuă către Lobsy",
            "المتابعة إلى Lobsy");
        Add("Mfa.AlreadySignedIn",
            "Je bent al ingelogd. Tweestapsverificatie vraag je alleen tijdens het inloggen.",
            "You are already signed in. Two-step verification is only asked while signing in.",
            "Jesteś już zalogowany. Weryfikacji dwuetapowej pytamy tylko przy logowaniu.",
            "Ești deja autentificat. Verificarea în doi pași se cere doar la autentificare.",
            "أنت مسجّل الدخول بالفعل. نطلب التحقق بخطوتين فقط أثناء تسجيل الدخول.");
        Add("Mfa.CodesAlreadyShown",
            "Je herstelcodes zijn al getoond. Bewaar ze goed — we tonen ze niet opnieuw.",
            "Your recovery codes were already shown. Keep them safe — we will not show them again.",
            "Twoje kody odzyskiwania już były pokazane. Zachowaj je — nie pokażemy ich ponownie.",
            "Codurile tale au fost deja afișate. Păstrează-le — nu le mai arătăm.",
            "عُرضت رموز الاسترداد من قبل. احفظها جيدًا — لن نعرضها مجددًا.");
        Add("Mfa.ErrorExpired",
            "Je inlogpoging is verlopen. Log opnieuw in.",
            "Your sign-in attempt expired. Please sign in again.",
            "Próba logowania wygasła. Zaloguj się ponownie.",
            "Încercarea de autentificare a expirat. Autentifică-te din nou.",
            "انتهت محاولة الدخول. سجّل الدخول مجددًا.");
        Add("Mfa.ErrorInvalid",
            "De code is onjuist. Probeer het opnieuw.",
            "That code is incorrect. Please try again.",
            "Kod jest nieprawidłowy. Spróbuj ponownie.",
            "Codul este greșit. Încearcă din nou.",
            "الرمز غير صحيح. حاول مرة أخرى.");
        Add("Mfa.ErrorTooMany",
            "Te veel pogingen. Wacht even en probeer opnieuw.",
            "Too many attempts. Please wait a moment and try again.",
            "Zbyt wiele prób. Poczekaj chwilę i spróbuj ponownie.",
            "Prea multe încercări. Așteaptă puțin și încearcă din nou.",
            "محاولات كثيرة. انتظر قليلًا وحاول مجددًا.");
        Add("Mfa.ErrorGeneric",
            "Er ging iets mis. Probeer opnieuw in te loggen.",
            "Something went wrong. Please sign in again.",
            "Coś poszło nie tak. Zaloguj się ponownie.",
            "Ceva nu a mers. Autentifică-te din nou.",
            "حدث خطأ. سجّل الدخول مجددًا.");
        Add("Mfa.BackToLogin",
            "Naar inloggen",
            "Back to sign in",
            "Do logowania",
            "Înapoi la autentificare",
            "إلى تسجيل الدخول");
        Add("Mfa.AdminReset",
            "2FA resetten",
            "Reset 2FA",
            "Resetuj 2FA",
            "Resetează 2FA",
            "إعادة ضبط 2FA");
        Add("Mfa.AdminResetLead",
            "Deze gebruiker moet bij de volgende inlog de authenticator opnieuw instellen. Alle sessies worden beëindigd.",
            "This user must set up their authenticator again on the next sign-in. All sessions will end.",
            "Ta osoba musi ponownie ustawić authenticator przy następnym logowaniu. Wszystkie sesje zostaną zakończone.",
            "Această persoană trebuie să configureze din nou authenticatorul la următoarea autentificare. Toate sesiunile se încheie.",
            "يجب على هذا المستخدم إعداد المصادقة مجددًا عند الدخول التالي. تُنهى كل الجلسات.");
        Add("Mfa.AdminResetReason",
            "Reden",
            "Reason",
            "Powód",
            "Motiv",
            "السبب");
        Add("Mfa.AdminResetConfirm",
            "Jouw authenticatorcode",
            "Your authenticator code",
            "Twój kod authenticator",
            "Codul tău authenticator",
            "رمز المصادقة الخاص بك");
        Add("Mfa.AdminResetSuccess",
            "2FA is gereset. De gebruiker moet opnieuw inschrijven.",
            "2FA has been reset. The user must enroll again.",
            "2FA zresetowane. Użytkownik musi się ponownie zapisać.",
            "2FA a fost resetat. Utilizatorul trebuie să se înscrie din nou.",
            "أُعيد ضبط 2FA. يجب على المستخدم التسجيل مجددًا.");
        Add("Mfa.StatusEnrolled",
            "2FA aan",
            "2FA on",
            "2FA włączone",
            "2FA activ",
            "2FA مفعّل");
        Add("Mfa.StatusNotEnrolled",
            "Geen 2FA",
            "No 2FA",
            "Bez 2FA",
            "Fără 2FA",
            "بلا 2FA");
        Add("Mfa.StatusExternal",
            "Extern",
            "External",
            "Zewnętrzne",
            "Extern",
            "خارجي");
        Add("Mfa.TrustDevice",
            "Vertrouw dit apparaat 30 dagen",
            "Trust this device for 30 days",
            "Zaufaj temu urządzeniu przez 30 dni",
            "Încrede-te în acest dispozitiv 30 de zile",
            "ثق بهذا الجهاز لمدة 30 يومًا");
        Add("Mfa.TrustDeviceHint",
            "Dan vragen we de code hier niet elke keer. Alleen op je eigen apparaat.",
            "Then we will not ask for the code here every time. Only on your own device.",
            "Wtedy nie będziemy tu pytać o kod za każdym razem. Tylko na własnym urządzeniu.",
            "Atunci nu vom cere codul aici de fiecare dată. Doar pe dispozitivul tău.",
            "عندها لن نطلب الرمز هنا في كل مرة. فقط على جهازك.");
        Add("Mfa.OtherAccount",
            "Ander account",
            "Other account",
            "Inne konto",
            "Alt cont",
            "حساب آخر");
        Add("Mfa.StepOfContext",
            "Stap 2 van 2",
            "Step 2 of 2",
            "Krok 2 z 2",
            "Pasul 2 din 2",
            "الخطوة 2 من 2");
        Add("Mfa.RecoveryHint",
            "Bijvoorbeeld 7KQ2-M9XA",
            "For example 7KQ2-M9XA",
            "Na przykład 7KQ2-M9XA",
            "De exemplu 7KQ2-M9XA",
            "مثلًا 7KQ2-M9XA");
        Add("Mfa.LoginRecovery",
            "Inloggen met herstelcode",
            "Sign in with recovery code",
            "Zaloguj się kodem odzyskiwania",
            "Autentificare cu cod de recuperare",
            "تسجيل الدخول برمز الاسترداد");
        Add("Mfa.RegenerateTitle",
            "Nieuwe herstelcodes",
            "New recovery codes",
            "Nowe kody odzyskiwania",
            "Coduri de recuperare noi",
            "رموز استرداد جديدة");
        Add("Mfa.RegenerateLead",
            "Je oude codes werken daarna niet meer.",
            "Your old codes will stop working.",
            "Stare kody potem już nie działają.",
            "Codurile vechi nu vor mai funcționa.",
            "رموزك القديمة لن تعمل بعدها.");
        Add("Mfa.RegenerateSubmit",
            "Maak nieuwe codes",
            "Create new codes",
            "Utwórz nowe kody",
            "Creează coduri noi",
            "أنشئ رموزًا جديدة");
        Add("Mfa.RegenerateLink",
            "Maak nieuwe herstelcodes",
            "Create new recovery codes",
            "Utwórz nowe kody odzyskiwania",
            "Creează coduri de recuperare noi",
            "أنشئ رموز استرداد جديدة");
        Add("Mfa.TrustedDevicesCount",
            "Vertrouwde apparaten: {0}",
            "Trusted devices: {0}",
            "Zaufane urządzenia: {0}",
            "Dispozitive de încredere: {0}",
            "الأجهزة الموثوقة: {0}");
        Add("Mfa.AdminResetLeadTrusted",
            "Hiermee zet je de extra beveiliging uit en vergeet Lobsy alle vertrouwde apparaten. De persoon stelt 2FA opnieuw in bij de volgende login.",
            "This turns off the extra security and Lobsy forgets all trusted devices. The person sets up 2FA again on the next sign-in.",
            "To wyłącza dodatkową ochronę i Lobsy zapomina wszystkie zaufane urządzenia. Osoba ustawi 2FA ponownie przy następnym logowaniu.",
            "Asta oprește securitatea extra și Lobsy uită toate dispozitivele de încredere. Persoana configurează 2FA din nou la următoarea autentificare.",
            "هذا يوقف الحماية الإضافية وتنسى Lobsy كل الأجهزة الموثوقة. يعيد الشخص إعداد 2FA عند الدخول التالي.");
        Add("Mfa.SuccessOn",
            "Je extra beveiliging staat aan",
            "Your extra security is on",
            "Twoja dodatkowa ochrona jest włączona",
            "Securitatea ta extra este activă",
            "حمايتك الإضافية مفعّلة");
        Add("Mfa.OpenAuthenticator",
            "Open in authenticator-app",
            "Open in authenticator app",
            "Otwórz w aplikacji authenticator",
            "Deschide în aplicația authenticator",
            "افتح في تطبيق المصادقة");
        Add("Mfa.ShowQrDetails",
            "Werkt dat niet? Toon de QR-code en sleutel",
            "Does that not work? Show the QR code and key",
            "Nie działa? Pokaż kod QR i klucz",
            "Nu merge? Arată codul QR și cheia",
            "لا يعمل؟ اعرض رمز QR والمفتاح");
        Add("Mfa.OnPhoneLink",
            "Op je telefoon? Open in authenticator-app",
            "On your phone? Open in authenticator app",
            "Na telefonie? Otwórz w aplikacji authenticator",
            "Pe telefon? Deschide în aplicația authenticator",
            "على هاتفك؟ افتح في تطبيق المصادقة");
        Add("Mfa.InstallApp",
            "Installeer een authenticator-app",
            "Install an authenticator app",
            "Zainstaluj aplikację authenticator",
            "Instalează o aplicație authenticator",
            "ثبّت تطبيق مصادقة");
        Add("Mfa.LinkApp",
            "Koppel de app",
            "Link the app",
            "Połącz aplikację",
            "Conectează aplicația",
            "اربط التطبيق");
        Add("Mfa.EnterCodeStep",
            "Vul de code in",
            "Enter the code",
            "Wpisz kod",
            "Introdu codul",
            "أدخل الرمز");
        Add("Mfa.Print",
            "Print",
            "Print",
            "Drukuj",
            "Printează",
            "طباعة");
        Add("Mfa.ContinueCompany",
            "Verder naar je bedrijf",
            "Continue to your company",
            "Dalej do firmy",
            "Continuă către companie",
            "المتابعة إلى شركتك");
    }
}
