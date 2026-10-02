using Jobsy.Core.Email.Model;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public sealed record EmailTemplateDefinition(
    string Key,
    string Category,
    string Audience,
    EmailKind Kind,
    string ReasonKey,
    bool GoodNews,
    string Title,
    string Description,
    bool RequiresEmployers = false,
    bool Parked = false);

/// <summary>Canonical registry of transactional mail templates (≥ 31 §M keys + stack joins).</summary>
public static class EmailTemplateRegistry
{
    public static IReadOnlyList<EmailTemplateDefinition> All { get; } =
    [
        // —— §M core (31) ——
        Def("ApplicationConfirmation", "ApplicationConfirmation", "Kandidaat", EmailKind.Essential, "Applied", false,
            "Sollicitatie bevestigd", "Bevestiging na versturen van een sollicitatie.", requiresEmployers: true),
        Def("ApplicationVerificationCode", "ApplicationVerificationCode", "Kandidaat", EmailKind.Security, "ApplyCode", false,
            "Verificatiecode sollicitatie", "6-cijferige code om een sollicitatie af te ronden.", requiresEmployers: true),
        Def("EmployerReactionAccepted", "EmployerReaction", "Kandidaat", EmailKind.Essential, "Applied", true,
            "Sollicitatie geaccepteerd", "Werkgever heeft de sollicitatie geaccepteerd.", requiresEmployers: true),
        Def("EmployerReactionRejected", "EmployerReaction", "Kandidaat", EmailKind.Essential, "Applied", false,
            "Sollicitatie afgewezen", "Helaas niet geselecteerd.", requiresEmployers: true),
        Def("EmployerContacting", "EmployerContacting", "Kandidaat", EmailKind.Essential, "Applied", true,
            "Werkgever neemt contact op", "Na acceptatie: werkgever gaat bellen/mailen.", requiresEmployers: true),
        Def("ApplicationHired", "ApplicationHired", "Kandidaat", EmailKind.Essential, "Applied", true,
            "Aangenomen", "Gefeliciteerd — je bent aangenomen, met optie andere sollicitaties in te trekken.", requiresEmployers: true),
        Def("ApplicationFilledElsewhere", "ApplicationFilledElsewhere", "Kandidaat", EmailKind.Essential, "Applied", false,
            "Andere kandidaat gekozen", "Vacature is vervuld door iemand anders.", requiresEmployers: true),
        Def("PushBom", "PushBom", "Kandidaat", EmailKind.Optional, "NearbyJobs", false,
            "PushBom-tip", "Nieuwe vacature in de buurt, met optie status op niet-beschikbaar.", requiresEmployers: true),
        Def("AccountUnsubscribeVerification", "AccountUnsubscribeVerification", "Account", EmailKind.Security, "AccountRequest", false,
            "Uitschrijfcode", "OTP om uitschrijving / right-to-be-forgotten te bevestigen."),
        Def("ParentalConsent", "ParentalConsent", "Ouder/voogd", EmailKind.Essential, "ParentAsked", false,
            "Ouderlijke toestemming", "Toestemming voor een minderjarige kandidaat."),
        Def("EmployerNewApplication", "EmployerNewApplication", "Werkgever", EmailKind.Essential, "ManagesVacancies", false,
            "Nieuwe sollicitatie", "Er is een nieuwe kandidaat binnengekomen.", requiresEmployers: true),
        Def("CandidateWithdrawn", "CandidateWithdrawn", "Werkgever", EmailKind.Essential, "ManagesVacancies", false,
            "Sollicitatie ingetrokken", "Kandidaat trok de sollicitatie in.", requiresEmployers: true),
        Def("CandidateWithdrawnOtherJob", "CandidateWithdrawnOtherJob", "Werkgever", EmailKind.Essential, "ManagesVacancies", false,
            "Ingetrokken na andere baan", "Kandidaat vond elders werk.", requiresEmployers: true),
        Def("PendingApproval", "PendingApproval", "Bedrijfsmanager", EmailKind.Essential, "ManagesCompany", false,
            "Publicatieaanvraag", "Vacature wacht op goedkeuring (tokens).", requiresEmployers: true),
        Def("VacancyEngagementReminder", "VacancyEngagementReminder", "Werkgever", EmailKind.Optional, "ManagesVacancies", false,
            "Engagement-check", "Vacature staat 14 dagen open — KPI’s en verbeter-CTA’s.", requiresEmployers: true),
        Def("DraftVacancyCleanupWarning", DraftVacancyCleanupRules.WarningEmailCategory, "Werkgever", EmailKind.Essential, "ManagesVacancies", false,
            "Concept opruimen", "Ongepubliceerd concept wordt over 14 dagen verwijderd.", requiresEmployers: true),
        Def("CompanyReEngagement", DraftVacancyCleanupRules.ReengagementEmailCategory, "Werkgever", EmailKind.Optional, "ManagesCompany", false,
            "We missen je", "Inactief bedrijf — tools staan nog klaar.", requiresEmployers: true),
        Def("CompanyApiKeyCredentials", "CompanyApiKeyCredentials", "Technisch contact", EmailKind.Essential, "ApiKeyRequested", false,
            "API-credentials", "Eenmalige link om de API-sleutel op te halen."),
        Def("UserInvite", "UserInvite", "Werkgever", EmailKind.Essential, "Invited", false,
            "Uitnodiging teammate", "Uitnodiging als manager/intermediair met wachtwoord-link."),
        Def("RegistrationActivation", "RegistrationActivation", "Registratie", EmailKind.Security, "Registering", false,
            "Bevestigingscode registratie", "OTP om bedrijfsregistratie te activeren.", requiresEmployers: true),
        Def("RegistrationCredentials", "RegistrationCredentials", "Registratie", EmailKind.Essential, "Registered", true,
            "Account actief", "Welkomstmail na activatie, met inlogknop.", requiresEmployers: true),
        Def("TakeoverEmailVerification", "TakeoverEmailVerification", "Registratie", EmailKind.Security, "TakeoverRequested", false,
            "Bevestigingscode overname", "OTP voordat een overnameverzoek de eigenaar bereikt.", requiresEmployers: true),
        Def("TakeoverRequest", "TakeoverRequest", "Werkgever", EmailKind.Essential, "YouManage", false,
            "Overnameverzoek (eigenaar)", "Inbox-mail voor de huidige vestigingseigenaar.", requiresEmployers: true),
        Def("TakeoverSubmitted", "TakeoverSubmitted", "Registratie", EmailKind.Essential, "TakeoverRequested", false,
            "Overnameverzoek ingediend", "Bevestiging aan de aanvrager."),
        Def("TakeoverApproved", "TakeoverApproved", "Registratie", EmailKind.Essential, "TakeoverRequested", true,
            "Overname goedgekeurd", "Aanvrager mag inloggen op de overgenomen vestiging.", requiresEmployers: true),
        Def("TakeoverRejected", "TakeoverRejected", "Registratie", EmailKind.Essential, "TakeoverRequested", false,
            "Overname afgewezen", "Aanvrager krijgt te horen dat het verzoek is afgewezen.", requiresEmployers: true),
        Def("SalesManagerInvite", "SalesManagerInvite", "Sales", EmailKind.Essential, "Invited", false,
            "Uitnodiging salesmanager", "Uitnodiging + wachtwoord-link."),
        Def("AmbassadeurInvite", "AmbassadeurInvite", "Ambassadeur", EmailKind.Essential, "Invited", false,
            "Uitnodiging ambassadeur", "Uitnodiging + wachtwoord-link.", parked: true),
        Def("AccountLockout", "AccountLockout", "Account", EmailKind.Essential, "Security", false,
            "Account tijdelijk geblokkeerd", "Melding na te veel mislukte inlogpogingen."),
        Def("MfaLockout", "MfaLockout", "Account", EmailKind.Essential, "Security", false,
            "2FA pauze", "Melding na te veel verkeerde 2FA-codes."),
        Def("RecoveryCodeUsed", "RecoveryCodeUsed", "Account", EmailKind.Essential, "Security", false,
            "Herstelcode gebruikt", "Melding wanneer een herstelcode is gebruikt."),
        Def("RecoveryCodesRegenerated", "RecoveryCodesRegenerated", "Account", EmailKind.Essential, "Security", false,
            "Herstelcodes vernieuwd", "Melding wanneer nieuwe herstelcodes zijn aangemaakt."),
        Def("PasswordReset", "PasswordReset", "Account", EmailKind.Essential, "Security", false,
            "Wachtwoord vergeten", "Link om een nieuw wachtwoord te kiezen (30 min, één keer)."),
        Def("PasswordResetExternalOnly", "PasswordResetExternalOnly", "Account", EmailKind.Essential, "Security", false,
            "Wachtwoord vergeten (extern)", "Geen Lobsy-wachtwoord; log in via Microsoft/Google."),
        Def("PasswordChanged", "PasswordChanged", "Account", EmailKind.Essential, "Security", false,
            "Wachtwoord gewijzigd", "Bevestiging na een geslaagde wachtwoordwijziging."),
        Def("SupportAccessRequested", "SupportAccessRequested", "Beheer", EmailKind.Essential, "AdminNotice", false,
            "Support-toegang aangevraagd", "Andere admins krijgen bericht over tijdelijke toegang."),
        Def("MailTest", "MailTest", "Beheer", EmailKind.Essential, "AdminNotice", false,
            "Connectiviteitstest", "Korte check of Resend/SMTP werkt."),

        // —— Dependencies E (werkgever-aanmelding) ——
        Def("CompanyVerificationReminder", "CompanyVerificationReminder", "Werkgever", EmailKind.Essential, "ManagesCompany", false,
            "Herinnering verificatie", "Day 7/21 reminder om het bedrijf te verifiëren."),
        Def("CompanyVerified", "CompanyVerified", "Werkgever", EmailKind.Essential, "ManagesCompany", true,
            "Bedrijf geverifieerd", "Bevestiging na verificatie + gepubliceerde vacatures."),
        Def("CompanyBusinessEmailVerification", "CompanyBusinessEmailVerification", "Werkgever", EmailKind.Security, "Registering", false,
            "Verificatiecode zakelijk e-mail", "6-cijferige code om bedrijf via zakelijk e-mail te verifiëren."),
        Def("CompanyVerificationRejected", "CompanyVerificationRejected", "Werkgever", EmailKind.Essential, "ManagesCompany", false,
            "Verificatie afgewezen", "Admin wees de handmatige controle af met reden.", requiresEmployers: true),
        Def("EngagementClaimRemoved", "EngagementClaimRemoved", "Werkgever", EmailKind.Essential, "ManagesCompany", false,
            "Betrokkenheid verwijderd", "Admin verwijderde een maatschappelijk kenmerk met reden."),
        Def("CompanyUnverifiedDeleted", "CompanyUnverifiedDeleted", "Werkgever", EmailKind.Essential, "ManagesCompany", false,
            "Registratie verwijderd", "Day-60 opruiming van niet-geverifieerde registratie."),
        Def("AccessRequestEmailVerification", "AccessRequestEmailVerification", "Registratie", EmailKind.Security, "TakeoverRequested", false,
            "Bevestigingscode toegangsverzoek", "OTP voor een toegangsverzoek."),
        Def("AccessRequestSubmitted", "AccessRequestSubmitted", "Registratie", EmailKind.Essential, "TakeoverRequested", false,
            "Toegangsverzoek ingediend", "Bevestiging aan de aanvrager."),
        Def("AccessRequestToManager", "AccessRequestToManager", "Werkgever", EmailKind.Essential, "YouManage", false,
            "Toegangsverzoek (manager)", "Inbox-mail voor de huidige manager.", requiresEmployers: true),
        Def("AccessRequestReminder", "AccessRequestReminder", "Werkgever", EmailKind.Essential, "YouManage", false,
            "Herinnering toegangsverzoek", "Reminder voor openstaande toegangsverzoeken.", requiresEmployers: true),
        Def("AccessRequestRejected", "AccessRequestRejected", "Registratie", EmailKind.Essential, "TakeoverRequested", false,
            "Toegangsverzoek afgewezen", "Aanvrager krijgt de afwijzing."),
        Def("AccessRequestExpired", "AccessRequestExpired", "Registratie", EmailKind.Essential, "TakeoverRequested", false,
            "Toegangsverzoek verlopen", "Aanvrager krijgt bericht dat het verzoek verlopen is."),
        Def("OwnershipTransferManagersNotify", "OwnershipTransferManagersNotify", "Werkgever", EmailKind.Essential, "YouManage", false,
            "Eigendom overgedragen", "Managers krijgen bericht over eigendomsoverdracht.", requiresEmployers: true),
        Def("IntermediaryClientSelfManaged", "IntermediaryClientSelfManaged", "Werkgever", EmailKind.Essential, "ManagesCompany", false,
            "Klant beheert zelf", "Intermediair-klant is zelfstandig geworden."),

        // —— Dependencies A (landing code mails) ——
        Def("EmailSignUpCode", "EmailSignUpCode", "Kandidaat", EmailKind.Security, "AccountRequest", false,
            "Code voor account maken", "6-cijferige code om een gratis kandidaat-account te maken."),
        Def("EmailSignInCode", "EmailSignInCode", "Kandidaat", EmailKind.Security, "AccountRequest", false,
            "Inlogcode", "6-cijferige code om in te loggen zonder wachtwoord."),
        Def("EmailCodeUsePassword", "EmailCodeUsePassword", "Account", EmailKind.Essential, "AccountRequest", false,
            "Log in met wachtwoord", "Account bestaat al als werkgever/staff — wijst naar wachtwoord-/SSO-login."),

        // —— Dependencies G ——
        Def("MfaResetByAdmin", "MfaResetByAdmin", "Account", EmailKind.Essential, "Security", false,
            "2FA gereset door support", "Authenticator ontkoppeld door Lobsy-support; opnieuw instellen bij login."),

        // —— Candidate tests (01 hotfix) ——
        Def("deep_test_receipt", "DeepTestReceipt", "Kandidaat", EmailKind.Essential, "Applied", true,
            "Uitgebreide test betaald", "Ontvangstbevestiging + factuur na betaling van de uitgebreide test."),

        // —— Meldknop / DSA notice and action (public-pages 06) ——
        Def("ReportReceived", "ReportReceived", "Melder", EmailKind.Essential, "Reported", false,
            "Melding ontvangen", "Bevestiging aan wie een vacature of bedrijfspagina meldde."),
        Def("ReportDecided", "ReportDecided", "Melder", EmailKind.Essential, "Reported", false,
            "Besluit over je melding", "Wat we met de melding deden, in B1 en zonder werkgeversgegevens."),
        Def("ContentRemoved", "ContentRemoved", "Werkgever", EmailKind.Essential, "ManagesVacancies", false,
            "Inhoud beperkt of weggehaald", "Motivering voor de werkgever na een moderatiebesluit, met bezwaarroute."),
    ];

    private static readonly HashSet<string> GoodNewsKeys = new(
        All.Where(d => d.GoodNews).Select(d => d.Key),
        StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, EmailTemplateDefinition> ByKey =
        All.ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string? key, out EmailTemplateDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(key) && ByKey.TryGetValue(key.Trim(), out definition!))
        {
            return true;
        }

        definition = null!;
        return false;
    }

    public static EmailTemplateDefinition GetRequired(string key)
        => TryGet(key, out var d)
            ? d
            : throw new ArgumentException($"Onbekend mailtype: {key}");

    public static bool AllowsMascot(string? key)
        => !string.IsNullOrWhiteSpace(key) && GoodNewsKeys.Contains(key);

    public static string ReasonText(string reasonKey)
        => Localization.EmailStrings.Reason(EmailCulture.Nl, reasonKey);

    private static EmailTemplateDefinition Def(
        string key,
        string category,
        string audience,
        EmailKind kind,
        string reasonKey,
        bool goodNews,
        string title,
        string description,
        bool requiresEmployers = false,
        bool parked = false)
        => new(key, category, audience, kind, reasonKey, goodNews, title, description, requiresEmployers, parked);
}
