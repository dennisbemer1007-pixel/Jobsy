using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail UserInvite(
        string? baseUrl, string fullName, string roleLabel, string email,
        string? setPasswordUrl, bool promotedFromCandidate)
    {
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? "Inloggen" : "Uitnodiging accepteren";
        var blocks = new List<EmailBlock>
        {
            P(Fmt("Je bent uitgenodigd als {0} op Lobsy.", EmailArg.Bold(roleLabel))),
            setPasswordUrl is null
                ? P(Fmt("Log in met {0} via Google, Microsoft Entra of je bestaande wachtwoord.", EmailArg.Plain(email)))
                : P(Fmt("Accepteer de uitnodiging via de knop hieronder om een wachtwoord te kiezen voor {0}. Of log in met Google of Microsoft Entra met hetzelfde e-mailadres.", EmailArg.Plain(email)))
        };
        if (promotedFromCandidate)
        {
            blocks.Add(P("Je eerdere sollicitaties blijven zichtbaar (alleen-lezen) in Lobsy."));
        }

        if (setPasswordUrl is not null)
        {
            blocks.Add(N($"De link werkt tot {Jobsy.Core.Time.AmsterdamTime.FormatDate(DateTime.UtcNow.AddDays(7))}. Daarna vraag je een nieuwe uitnodiging."));
        }

        return Finish(Doc("UserInvite", $"Uitnodiging voor Lobsy ({roleLabel})", $"Uitnodiging voor Lobsy ({roleLabel})",
            $"Uitnodiging — {roleLabel}",
            blocks, Button(ctaLabel, ctaUrl), greeting: $"Hoi {fullName},"), baseUrl);
    }

    public static ComposedEmail SalesManagerInvite(
        string? baseUrl, string name, string email, string? setPasswordUrl)
    {
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? "Inloggen" : "Uitnodiging accepteren";
        var blocks = new List<EmailBlock>
        {
            P("Je bent uitgenodigd als salesmanager op Lobsy."),
            setPasswordUrl is null
                ? P(Fmt("Log in met {0} om verder te gaan.", EmailArg.Bold(email)))
                : P(Fmt("Accepteer de uitnodiging voor {0} via de knop hieronder.", EmailArg.Bold(email))),
            P("Na je eerste login vul je je KvK/BTW/NAW-gegevens in en onderteken je de bemiddelingsovereenkomst om je trackingcode te ontvangen.")
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N($"De link werkt tot {Jobsy.Core.Time.AmsterdamTime.FormatDate(DateTime.UtcNow.AddDays(7))}. Daarna vraag je een nieuwe uitnodiging."));
            blocks.Add(N("Na het instellen van je wachtwoord ga je verder met onboarding.",
                new EmailLink("Start onboarding", links.SalesOnboarding)));
        }

        return Finish(Doc("SalesManagerInvite", "Uitnodiging Lobsy salesmanager", "Uitnodiging Lobsy salesmanager", "Uitnodiging salesmanager",
            blocks, Button(ctaLabel, ctaUrl), greeting: $"Hallo {name},"), baseUrl);
    }

    public static ComposedEmail AmbassadeurInvite(
        string? baseUrl, string name, string email, string? setPasswordUrl)
    {
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? "Inloggen" : "Uitnodiging accepteren";
        var blocks = new List<EmailBlock>
        {
            P("Je bent uitgenodigd als ambassadeur op Lobsy."),
            setPasswordUrl is null
                ? P(Fmt("Log in met {0} om verder te gaan.", EmailArg.Bold(email)))
                : P(Fmt("Accepteer de uitnodiging voor {0} via de knop hieronder.", EmailArg.Bold(email))),
            P("Na je eerste login vul je je KvK/BTW/NAW-gegevens in en onderteken je de bemiddelingsovereenkomst om je trackingcode te ontvangen.")
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N($"De link werkt tot {Jobsy.Core.Time.AmsterdamTime.FormatDate(DateTime.UtcNow.AddDays(7))}. Daarna vraag je een nieuwe uitnodiging."));
            blocks.Add(N("Na het instellen van je wachtwoord ga je verder met onboarding.",
                new EmailLink("Start onboarding", links.AmbassadeurOnboarding)));
        }

        return Finish(Doc("AmbassadeurInvite", "Uitnodiging Lobsy ambassadeur", "Uitnodiging Lobsy ambassadeur", "Uitnodiging ambassadeur",
            blocks, Button(ctaLabel, ctaUrl), greeting: $"Hallo {name},"), baseUrl);
    }

    public static ComposedEmail CompanyApiKeyCredentials(
        string? baseUrl, string companyName, string apiBase, string revealUrl, DateTime expiresAtUtc)
    {
        var endpoint = apiBase.TrimEnd('/') + "/api/external/vacancies";
        var swaggerUrl = apiBase.TrimEnd('/') + "/swagger";
        var expiresLabel = Jobsy.Core.Time.AmsterdamTime.FormatDateTime(expiresAtUtc);
        return Finish(Doc("CompanyApiKeyCredentials", $"Lobsy API-credentials voor {companyName}",
            $"API-credentials voor {companyName}", "API-credentials",
            [
                P(Fmt("Hierbij een link om de API-sleutel voor {0} één keer op te halen.", EmailArg.Bold(companyName))),
                F([
                    ("Endpoint", endpoint),
                    ("Header", "X-API-Key: <jouw-api-key>"),
                    ("Swagger", swaggerUrl),
                    ("Geldig tot", expiresLabel)
                ]),
                P("De link werkt 72 uur en maar één keer. Je huidige sleutel blijft werken tot je de nieuwe ophaalt.")
            ],
            Button("API-sleutel ophalen", revealUrl),
            greeting: "Hallo,"), baseUrl);
    }

    public static ComposedEmail ParentalConsent(
        string? baseUrl, string? childFirstName, string confirmUrl, DateTime expiresAtUtc)
    {
        var age = CandidateConsentRules.ParentalConsentAge;
        var hasName = !string.IsNullOrWhiteSpace(childFirstName);
        var subject = hasName
            ? $"{childFirstName!.Trim()} vraagt je toestemming voor Lobsy"
            : "Je kind vraagt je toestemming voor Lobsy";
        var bodyName = hasName ? childFirstName!.Trim() : "je kind";
        var bodyNameCap = hasName ? childFirstName!.Trim() : "Je kind";
        var expiresLabel = Jobsy.Core.Time.AmsterdamTime.FormatDate(expiresAtUtc);
        return Finish(Doc("ParentalConsent", subject, "Geef je toestemming voor Lobsy.", "Geef je toestemming?",
            [
                P($"{bodyNameCap} wil Lobsy gebruiken: tests doen en een analyse met AI krijgen. Omdat {bodyName} jonger is dan {age} jaar, hebben we toestemming nodig van een ouder of voogd."),
                N($"Ben je geen ouder of voogd, of weet je hier niets van? Dan hoef je niets te doen. De link werkt tot {expiresLabel}.")
            ],
            Button("Toestemming bekijken", confirmUrl)), baseUrl);
    }

    public static ComposedEmail SupportAccessRequested(
        string? baseUrl, string adminDisplay, string reason, DateTime expiresAtUtc, string scopeLabel)
    {
        var links = Links(baseUrl);
        var expiresLabel = Jobsy.Core.Time.AmsterdamTime.FormatDateTime(expiresAtUtc);
        return Finish(Doc("SupportAccessRequested", "Support-toegang aangevraagd door een admin",
            "Support-toegang aangevraagd door een admin", "Support-toegang aangevraagd",
            [
                P($"{adminDisplay} vroeg tijdelijk toegang tot persoonsgegevens aan."),
                F([
                    ("Reden", reason),
                    ("Toegang tot", scopeLabel),
                    ("Geldig tot", $"{expiresLabel} (Nederlandse tijd)")
                ])
            ],
            Button("Bekijk de toegang", links.AdminPersonalDataAccessLog)), baseUrl);
    }

    public static ComposedEmail AccountLockout(string? baseUrl)
    {
        var brand = Brand(baseUrl);
        return Finish(Doc("AccountLockout", "Je Lobsy-account is tijdelijk geblokkeerd",
            "Er zijn meerdere mislukte inlogpogingen gedaan.", "Account tijdelijk geblokkeerd",
            [
                P("Er zijn meerdere mislukte inlogpogingen gedaan. Je account is tijdelijk geblokkeerd. Was je dit niet zelf? Kies dan na de blokkade een nieuw wachtwoord.")
            ],
            Button("Neem contact op", $"mailto:{brand.SupportAddress}")), baseUrl);
    }

    public static ComposedEmail AccountUnsubscribeVerification(
        string? baseUrl, string fullName, string code, int ttlMinutes)
    {
        return Finish(Doc("AccountUnsubscribeVerification", "Verificatiecode voor uitschrijving bij Lobsy",
            "Je verificatiecode voor uitschrijving", "Bevestig je uitschrijving",
            [
                P("Je hebt gevraagd om je Lobsy-account af te melden."),
                P("Gebruik deze 6-cijferige code om de uitschrijving te bevestigen:"),
                C(code, $"De code is {ttlMinutes} minuten geldig. Heb je dit niet zelf aangevraagd? Negeer deze mail dan.")
            ],
            greeting: $"Hoi {fullName},"), baseUrl);
    }

    public static ComposedEmail MfaResetByAdmin(string? baseUrl, string recipientName)
    {
        var links = Links(baseUrl);
        var subject = "Je tweestapsverificatie is gereset";
        return Finish(Doc("MfaResetByAdmin", subject, "Je tweestapsverificatie is gereset door support.", "Tweestapsverificatie gereset",
            [
                P("Je tweestapsverificatie is gereset door Lobsy-support. Log opnieuw in om 2FA in te stellen."),
                P("Was jij dit niet? Neem contact op.")
            ],
            Button("Opnieuw inloggen", links.Login),
            greeting: $"Hoi {recipientName},"), baseUrl);
    }

    public static ComposedEmail EmailSignUpCode(string? baseUrl, string code, string? culture)
    {
        var lang = Jobsy.Core.Localization.JobsyLanguages.Normalize(culture);
        var (subject, heading, body, ttl, pre) = lang switch
        {
            "en" => ("Your Lobsy code: " + code, "Your code for Lobsy",
                "Use this 6-digit code to create your free account:",
                "The code is valid for 10 minutes.", "Your Lobsy sign-up code"),
            "pl" => ("Twój kod Lobsy: " + code, "Twój kod do Lobsy",
                "Użyj tego 6-cyfrowego kodu, aby utworzyć darmowe konto:",
                "Kod jest ważny przez 10 minut.", "Twój kod rejestracji Lobsy"),
            "ro" => ("Codul tău Lobsy: " + code, "Codul tău pentru Lobsy",
                "Folosește acest cod de 6 cifre pentru a-ți crea contul gratuit:",
                "Codul este valabil 10 minute.", "Codul tău de înregistrare Lobsy"),
            "ar" => ("رمز لوبسي: " + code, "رمزك لـ Lobsy",
                "استخدم هذا الرمز المكوّن من 6 أرقام لإنشاء حسابك المجاني:",
                "الرمز صالح لمدة 10 دقائق.", "رمز إنشاء حساب Lobsy"),
            _ => ("Je code voor Lobsy: " + code, "Je code voor Lobsy",
                "Gebruik deze 6-cijferige code om je gratis account te maken:",
                "De code is 10 minuten geldig.", "Je Lobsy-aanmeldcode")
        };
        return Finish(Doc("EmailSignUpCode", subject, pre, heading,
            [P(body), C(code, ttl)],
            culture: EmailCulture.ForLanguage(lang)), baseUrl);
    }

    public static ComposedEmail EmailSignInCode(string? baseUrl, string code, string? culture)
    {
        var lang = Jobsy.Core.Localization.JobsyLanguages.Normalize(culture);
        var (subject, heading, body, ttl, pre) = lang switch
        {
            "en" => ("Your Lobsy sign-in code", "Your sign-in code",
                "Use this 6-digit code to sign in:",
                "The code is valid for 10 minutes.", "Enter this code to sign in."),
            "pl" => ("Twój kod logowania Lobsy", "Twój kod logowania",
                "Użyj tego 6-cyfrowego kodu, aby się zalogować:",
                "Kod jest ważny przez 10 minut.", "Wpisz ten kod, aby się zalogować."),
            "ro" => ("Codul tău de autentificare Lobsy", "Codul tău de autentificare",
                "Folosește acest cod de 6 cifre pentru a te autentifica:",
                "Codul este valabil 10 minute.", "Introdu acest cod pentru autentificare."),
            "ar" => ("رمز تسجيل الدخول إلى Lobsy", "رمز تسجيل الدخول",
                "استخدم هذا الرمز المكوّن من 6 أرقام لتسجيل الدخول:",
                "الرمز صالح لمدة 10 دقائق.", "أدخل هذا الرمز لتسجيل الدخول."),
            _ => ("Je inlogcode voor Lobsy", "Je inlogcode",
                "Gebruik deze 6-cijferige code om in te loggen:",
                "De code is 10 minuten geldig.", "Voer deze code in om in te loggen.")
        };
        // Fix preheader != subject for nl/en defaults that previously matched.
        if (string.Equals(pre, subject, StringComparison.Ordinal))
        {
            pre = lang == "en" ? "Enter this code to sign in." : "Voer deze code in om in te loggen.";
        }

        return Finish(Doc("EmailSignInCode", subject, pre, heading,
            [P(body), C(code, ttl)],
            culture: EmailCulture.ForLanguage(lang)), baseUrl);
    }

    public static ComposedEmail EmailCodeUsePassword(string? baseUrl, string? culture)
    {
        var links = Links(baseUrl);
        var lang = Jobsy.Core.Localization.JobsyLanguages.Normalize(culture);
        var (subject, heading, body, cta, pre) = lang switch
        {
            "en" => ("Sign in with your password", "Use your password or SSO",
                "This e-mail belongs to a Lobsy account that signs in with a password or Microsoft/Google. We did not send a one-time code.",
                "Go to login", "Use your password or Microsoft/Google."),
            "pl" => ("Zaloguj się hasłem", "Użyj hasła lub SSO",
                "Ten e-mail należy do konta Lobsy, które loguje się hasłem lub przez Microsoft/Google. Nie wysłaliśmy jednorazowego kodu.",
                "Przejdź do logowania", "Użyj hasła lub Microsoft/Google."),
            "ro" => ("Autentifică-te cu parola", "Folosește parola sau SSO",
                "Acest e-mail aparține unui cont Lobsy care se autentifică cu parolă sau Microsoft/Google. Nu am trimis un cod unic.",
                "Mergi la autentificare", "Folosește parola sau Microsoft/Google."),
            "ar" => ("سجّل الدخول بكلمة المرور", "استخدم كلمة المرور أو SSO",
                "هذا البريد يخص حساب Lobsy يسجّل الدخول بكلمة مرور أو Microsoft/Google. لم نُرسل رمزًا لمرة واحدة.",
                "الانتقال لتسجيل الدخول", "استخدم كلمة المرور أو Microsoft/Google."),
            _ => ("Log in met je wachtwoord", "Log in met wachtwoord of SSO",
                "Dit e-mailadres hoort bij een Lobsy-account dat inlogt met een wachtwoord of Microsoft/Google. We hebben geen eenmalige code gestuurd.",
                "Naar inloggen", "Gebruik je wachtwoord of Microsoft/Google.")
        };
        return Finish(Doc("EmailCodeUsePassword", subject, pre, heading,
            [P(body)],
            Button(cta, links.Login),
            culture: EmailCulture.ForLanguage(lang)), baseUrl);
    }
}

public sealed record EmailSampleContext(
    string PublicWebBaseUrl,
    string RecipientName,
    string CompanyName,
    string VacancyTitle,
    Guid VacancyId,
    Guid ApplicationId,
    string LocationLabel,
    double DistanceKm,
    int TravelMinutes,
    decimal HourlyWage,
    string OtpCode,
    string SetPasswordUrl,
    string SampleRevealUrl,
    string RoleLabel,
    string EstablishmentName,
    string ContactEmail,
    string KvkEstablishmentId,
    string ApiBaseUrl)
{
    public static EmailSampleContext ForPreview(string publicWebBaseUrl, string? contactEmail = null)
    {
        var baseUrl = string.IsNullOrWhiteSpace(publicWebBaseUrl) ? "https://lobsy.nl" : publicWebBaseUrl.TrimEnd('/');
        return new(
            PublicWebBaseUrl: baseUrl,
            RecipientName: "Alex de Tester",
            CompanyName: "Bakkerij De Gouden Korrel",
            VacancyTitle: "Weekendhulp verkoop",
            VacancyId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ApplicationId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            LocationLabel: "Delft",
            DistanceKm: 3.4,
            TravelMinutes: 12,
            HourlyWage: 14.50m,
            OtpCode: TransactionalEmails.SampleOtp,
            SetPasswordUrl: baseUrl + TransactionalEmails.SampleSetPasswordPath,
            SampleRevealUrl: baseUrl + TransactionalEmails.SampleRevealPath,
            RoleLabel: "Filiaalmanager",
            EstablishmentName: "Bakkerij De Gouden Korrel — Delft",
            ContactEmail: string.IsNullOrWhiteSpace(contactEmail) ? "tester@example.com" : contactEmail.Trim(),
            KvkEstablishmentId: "000012345678",
            ApiBaseUrl: "https://api.lobsy.nl");
    }
}

// Note: EmailSampleContext is in this file already — AdHoc goes in main partial
