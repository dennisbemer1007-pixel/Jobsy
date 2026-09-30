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

        Add("Mfa.SetupTitle", "Beveilig je account", "Secure your account");
        Add("Mfa.SetupLead", "Je hebt een app nodig die codes maakt, zoals Microsoft Authenticator of Google Authenticator.", "You need an app that creates codes, such as Microsoft Authenticator or Google Authenticator.");
        Add("Mfa.StepOf", "Stap {0} van {1}", "Step {0} of {1}");
        Add("Mfa.ScanQr", "Scan de QR-code met je app", "Scan the QR code with your app");
        Add("Mfa.QrAlt", "QR-code om Lobsy te koppelen", "QR code to link Lobsy");
        Add("Mfa.ManualKeySummary", "Lukt scannen niet? Vul deze sleutel in", "Can't scan? Enter this key instead");
        Add("Mfa.CopyKey", "Kopieer sleutel", "Copy key");
        Add("Mfa.Instructions", "Open Microsoft Authenticator of Google Authenticator, kies account toevoegen, en scan de QR-code of vul de sleutel in.", "Open Microsoft Authenticator or Google Authenticator, add an account, then scan the QR code or enter the key.");
        Add("Mfa.EnterCode", "Vul de 6 cijfers uit je app in", "Enter the 6 digits from your app");
        Add("Mfa.CodeLabel", "Code uit je app", "Code from your app");
        Add("Mfa.Confirm", "Koppelen", "Link");
        Add("Mfa.PromptTitle", "Vul je code in", "Enter your code");
        Add("Mfa.PromptLead", "Open je authenticator-app en typ de 6 cijfers.", "Open your authenticator app and type the 6 digits.");
        Add("Mfa.RecoverySummary", "Geen toegang tot je app?", "No access to your app?");
        Add("Mfa.RecoveryLabel", "Herstelcode", "Recovery code");
        Add("Mfa.Login", "Inloggen", "Sign in");
        Add("Mfa.AdminHelp", "Nieuwe telefoon en geen herstelcodes? Mail support", "New phone and no recovery codes? Mail support");
        Add("Mfa.KeyCopied", "Sleutel gekopieerd", "Key copied");
        Add("Mfa.CodesCopied", "Gekopieerd", "Copied");
        Add("Mfa.ErrorLocked", "Even pauze. Er is te vaak een verkeerde code ingevuld. Probeer het om {0} opnieuw.", "A short pause. A wrong code was entered too often. Please try again at {0}.");
        Add("Mfa.ErrorTooManyUntil", "Te veel pogingen. Probeer het om {0} opnieuw.", "Too many attempts. Please try again at {0}.");
        Add("Mfa.ErrorLockedFallback", "Probeer het over een kwartier opnieuw.", "Please try again in about fifteen minutes.");
        Add("Mfa.RecoveryUsedTitle", "Herstelcode gebruikt", "Recovery code used");
        Add("Mfa.RecoveryUsedLead", "Je bent ingelogd met een herstelcode. Je hebt er nog {0}.", "You signed in with a recovery code. You have {0} left.");
        Add("Mfa.RecoveryAlmostOut", "Bijna op. Maak nieuwe herstelcodes of vraag een beheerder om je 2FA opnieuw in te stellen.", "Almost out. Create new recovery codes or ask an admin to reset your 2FA.");
        Add("Mfa.DownloadHeaderEmail", "Lobsy herstelcodes voor {0}", "Lobsy recovery codes for {0}");
        Add("Mfa.DownloadHeaderDate", "Gemaakt op {0}", "Created on {0}");
        Add("Mfa.DownloadNote", "Elke code werkt één keer.", "Each code works once.");
        Add("Mfa.RecoveryTitle", "Bewaar je herstelcodes", "Save your recovery codes");
        Add("Mfa.RecoveryLead", "Kwijt je telefoon? Met één van deze codes kom je toch binnen. Elke code werkt één keer.", "Lost your phone? With one of these codes you can still get in. Each code works once.");
        Add("Mfa.DownloadTxt", "Download (.txt)", "Download (.txt)");
        Add("Mfa.CopyCodes", "Kopieer alles", "Copy all");
        Add("Mfa.SavedCheckbox", "Ik heb mijn codes veilig bewaard", "I have stored my codes safely");
        Add("Mfa.Continue", "Verder naar Lobsy", "Continue to Lobsy");
        Add("Mfa.CodesAlreadyShown", "Je herstelcodes zijn al getoond. Bewaar ze goed — we tonen ze niet opnieuw.", "Your recovery codes were already shown. Keep them safe — we will not show them again.");
        Add("Mfa.ErrorExpired", "Je inlogpoging is verlopen. Log opnieuw in.", "Your sign-in attempt expired. Please sign in again.");
        Add("Mfa.ErrorInvalid", "De code is onjuist. Probeer het opnieuw.", "That code is incorrect. Please try again.");
        Add("Mfa.ErrorTooMany", "Te veel pogingen. Wacht even en probeer opnieuw.", "Too many attempts. Please wait a moment and try again.");
        Add("Mfa.ErrorGeneric", "Er ging iets mis. Probeer opnieuw in te loggen.", "Something went wrong. Please sign in again.");
        Add("Mfa.BackToLogin", "Naar inloggen", "Back to sign in");
        Add("Mfa.AdminReset", "2FA resetten", "Reset 2FA");
        Add("Mfa.AdminResetLead", "Deze gebruiker moet bij de volgende inlog de authenticator opnieuw instellen. Alle sessies worden beëindigd.", "This user must set up their authenticator again on the next sign-in. All sessions will end.");
        Add("Mfa.AdminResetReason", "Reden", "Reason");
        Add("Mfa.AdminResetConfirm", "Jouw authenticatorcode", "Your authenticator code");
        Add("Mfa.AdminResetSuccess", "2FA is gereset. De gebruiker moet opnieuw inschrijven.", "2FA has been reset. The user must enroll again.");
        Add("Mfa.StatusEnrolled", "2FA aan", "2FA on");
        Add("Mfa.StatusNotEnrolled", "Geen 2FA", "No 2FA");
        Add("Mfa.StatusExternal", "Extern", "External");
        Add("Mfa.TrustDevice", "Vertrouw dit apparaat 30 dagen", "Trust this device for 30 days");
        Add("Mfa.TrustDeviceHint", "Dan vragen we de code hier niet elke keer. Alleen op je eigen apparaat.", "Then we will not ask for the code here every time. Only on your own device.");
        Add("Mfa.OtherAccount", "Ander account", "Other account");
        Add("Mfa.StepOfContext", "Stap 2 van 2", "Step 2 of 2");
        Add("Mfa.RecoveryHint", "Bijvoorbeeld 7KQ2-M9XA", "For example 7KQ2-M9XA");
        Add("Mfa.LoginRecovery", "Inloggen met herstelcode", "Sign in with recovery code");
        Add("Mfa.RegenerateTitle", "Nieuwe herstelcodes", "New recovery codes");
        Add("Mfa.RegenerateLead", "Je oude codes werken daarna niet meer.", "Your old codes will stop working.");
        Add("Mfa.RegenerateSubmit", "Maak nieuwe codes", "Create new codes");
        Add("Mfa.RegenerateLink", "Maak nieuwe herstelcodes", "Create new recovery codes");
        Add("Mfa.TrustedDevicesCount", "Vertrouwde apparaten: {0}", "Trusted devices: {0}");
        Add("Mfa.AdminResetLeadTrusted", "Hiermee zet je de extra beveiliging uit en vergeet Lobsy alle vertrouwde apparaten. De persoon stelt 2FA opnieuw in bij de volgende login.", "This turns off the extra security and Lobsy forgets all trusted devices. The person sets up 2FA again on the next sign-in.");
        Add("Mfa.SuccessOn", "Je extra beveiliging staat aan", "Your extra security is on");
        Add("Mfa.OpenAuthenticator", "Open in authenticator-app", "Open in authenticator app");
        Add("Mfa.ShowQrDetails", "Werkt dat niet? Toon de QR-code en sleutel", "Does that not work? Show the QR code and key");
        Add("Mfa.OnPhoneLink", "Op je telefoon? Open in authenticator-app", "On your phone? Open in authenticator app");
        Add("Mfa.InstallApp", "Installeer een authenticator-app", "Install an authenticator app");
        Add("Mfa.LinkApp", "Koppel de app", "Link the app");
        Add("Mfa.EnterCodeStep", "Vul de code in", "Enter the code");
        Add("Mfa.Print", "Print", "Print");
        Add("Mfa.ContinueCompany", "Verder naar je bedrijf", "Continue to your company");
    }
}
