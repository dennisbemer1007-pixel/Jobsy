namespace Jobsy.Web.Localization;

/// <summary>Copy for forced 2FA enrollment / prompt / recovery-code pages.</summary>
public static class UiStringsMfa
{
    public static void MergeAll(
        Dictionary<string, string> nl, Dictionary<string, string> en,
        Dictionary<string, string> pl, Dictionary<string, string> ro,
        Dictionary<string, string> ar)
    {
        void Add(string key, string nlText, string enText)
        {
            nl[key] = nlText;
            en[key] = enText;
            pl[key] = enText;
            ro[key] = enText;
            ar[key] = enText;
        }

        Add("Mfa.SetupTitle",
            "Beveilig je account",
            "Secure your account");
        Add("Mfa.SetupLead",
            "Voor jouw rol is een extra stap nodig: een code uit een authenticator-app, zoals Microsoft Authenticator.",
            "Your role needs an extra step: a code from an authenticator app, such as Microsoft Authenticator.");
        Add("Mfa.StepOf",
            "Stap {0} van {1}",
            "Step {0} of {1}");
        Add("Mfa.ScanQr",
            "Scan de QR-code met je app",
            "Scan the QR code with your app");
        Add("Mfa.QrAlt",
            "QR-code voor je authenticator-app",
            "QR code for your authenticator app");
        Add("Mfa.ManualKeySummary",
            "Lukt scannen niet? Vul deze sleutel in",
            "Can't scan? Enter this key instead");
        Add("Mfa.CopyKey",
            "Sleutel kopiëren",
            "Copy key");
        Add("Mfa.Instructions",
            "Open Microsoft Authenticator of Google Authenticator, kies account toevoegen, en scan de QR-code of vul de sleutel in.",
            "Open Microsoft Authenticator or Google Authenticator, add an account, then scan the QR code or enter the key.");
        Add("Mfa.EnterCode",
            "Vul de 6 cijfers uit je app in",
            "Enter the 6 digits from your app");
        Add("Mfa.CodeLabel",
            "Code uit authenticator",
            "Authenticator code");
        Add("Mfa.Confirm",
            "Bevestigen",
            "Confirm");
        Add("Mfa.PromptTitle",
            "Bevestig je aanmelding",
            "Confirm your sign-in");
        Add("Mfa.PromptLead",
            "Vul de code uit je authenticator-app in",
            "Enter the code from your authenticator app");
        Add("Mfa.RecoverySummary",
            "Geen toegang tot je app? Gebruik een herstelcode",
            "No access to your app? Use a recovery code");
        Add("Mfa.RecoveryLabel",
            "Herstelcode",
            "Recovery code");
        Add("Mfa.Login",
            "Inloggen",
            "Sign in");
        Add("Mfa.AdminHelp",
            "Hulp nodig? Mail support. Een beheerder kan je 2FA opnieuw instellen.",
            "Need help? Mail support. An admin can reset your 2FA.");
        Add("Mfa.KeyCopied",
            "Sleutel gekopieerd",
            "Key copied");
        Add("Mfa.CodesCopied",
            "Gekopieerd",
            "Copied");
        Add("Mfa.ErrorLocked",
            "Even pauze. Er is te vaak een verkeerde code ingevuld. Probeer het om {0} opnieuw.",
            "A short pause. A wrong code was entered too often. Please try again at {0}.");
        Add("Mfa.ErrorTooManyUntil",
            "Te veel pogingen. Probeer het om {0} opnieuw.",
            "Too many attempts. Please try again at {0}.");
        Add("Mfa.ErrorLockedFallback",
            "Probeer het over een kwartier opnieuw.",
            "Please try again in about fifteen minutes.");
        Add("Mfa.RecoveryUsedTitle",
            "Herstelcode gebruikt",
            "Recovery code used");
        Add("Mfa.RecoveryUsedLead",
            "Je bent ingelogd met een herstelcode. Je hebt er nog {0}.",
            "You signed in with a recovery code. You have {0} left.");
        Add("Mfa.RecoveryAlmostOut",
            "Bijna op. Vraag een beheerder om je 2FA opnieuw in te stellen, of maak nieuwe codes.",
            "Almost out. Ask an admin to reset your 2FA, or create new codes.");
        Add("Mfa.DownloadHeaderEmail",
            "Lobsy herstelcodes voor {0}",
            "Lobsy recovery codes for {0}");
        Add("Mfa.DownloadHeaderDate",
            "Gemaakt op {0}",
            "Created on {0}");
        Add("Mfa.DownloadNote",
            "Elke code werkt één keer.",
            "Each code works once.");
        Add("Mfa.RecoveryTitle",
            "Bewaar je herstelcodes",
            "Save your recovery codes");
        Add("Mfa.RecoveryLead",
            "Heb je je telefoon niet bij je? Dan kun je met één van deze codes inloggen. Elke code werkt één keer.",
            "Don't have your phone? You can sign in with one of these codes. Each code works once.");
        Add("Mfa.DownloadTxt",
            "Download als .txt",
            "Download as .txt");
        Add("Mfa.CopyCodes",
            "Kopieer",
            "Copy");
        Add("Mfa.SavedCheckbox",
            "Ik heb mijn herstelcodes veilig bewaard",
            "I have stored my recovery codes safely");
        Add("Mfa.Continue",
            "Verder naar Lobsy",
            "Continue to Lobsy");
        Add("Mfa.CodesAlreadyShown",
            "Je herstelcodes zijn al getoond. Bewaar ze goed — we tonen ze niet opnieuw.",
            "Your recovery codes were already shown. Keep them safe — we will not show them again.");
        Add("Mfa.ErrorExpired",
            "Je inlogpoging is verlopen. Log opnieuw in.",
            "Your sign-in attempt expired. Please sign in again.");
        Add("Mfa.ErrorInvalid",
            "De code is onjuist. Probeer het opnieuw.",
            "That code is incorrect. Please try again.");
        Add("Mfa.ErrorTooMany",
            "Te veel pogingen. Wacht even en probeer opnieuw.",
            "Too many attempts. Please wait a moment and try again.");
        Add("Mfa.ErrorGeneric",
            "Er ging iets mis. Probeer opnieuw in te loggen.",
            "Something went wrong. Please sign in again.");
        Add("Mfa.BackToLogin",
            "Naar inloggen",
            "Back to sign in");
        Add("Mfa.AdminReset",
            "2FA resetten",
            "Reset 2FA");
        Add("Mfa.AdminResetLead",
            "Deze gebruiker moet bij de volgende inlog de authenticator opnieuw instellen. Alle sessies worden beëindigd.",
            "This user must set up their authenticator again on the next sign-in. All sessions will end.");
        Add("Mfa.AdminResetReason",
            "Reden",
            "Reason");
        Add("Mfa.AdminResetConfirm",
            "Jouw authenticatorcode",
            "Your authenticator code");
        Add("Mfa.AdminResetSuccess",
            "2FA is gereset. De gebruiker moet opnieuw inschrijven.",
            "2FA has been reset. The user must enroll again.");
        Add("Mfa.StatusEnrolled",
            "2FA aan",
            "2FA on");
        Add("Mfa.StatusNotEnrolled",
            "Geen 2FA",
            "No 2FA");
        Add("Mfa.StatusExternal",
            "Extern",
            "External");
    }
}
