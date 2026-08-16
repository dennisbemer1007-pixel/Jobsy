using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public sealed record EmailTemplateInfo(
    string Key,
    string Title,
    string Audience,
    string Description,
    string Category);

public sealed record ComposedEmail(
    string Key,
    string Category,
    string Subject,
    string Html);

/// <summary>
/// Canonical HTML for every transactional Lobsy mail. Production senders and the admin
/// mail-test page both use these composers so previews stay identical to live mail.
/// </summary>
public static class TransactionalEmails
{
    public const string SampleOtp = "123456";
    public const string SamplePassword = "VoorbeeldWachtwoord12";
    public const string SampleApiKey = "lobsy_test_xxxxxxxxxxxxxxxx";

    public static IReadOnlyList<EmailTemplateInfo> Templates { get; } =
    [
        new("MailTest", "Connectiviteitstest", "Beheer", "Korte check of Resend/SMTP werkt.", "MailTest"),
        new("ApplicationConfirmation", "Sollicitatie bevestigd", "Kandidaat", "Bevestiging na versturen van een sollicitatie.", "ApplicationConfirmation"),
        new("ApplicationVerificationCode", "Verificatiecode sollicitatie", "Kandidaat", "6-cijferige code om een sollicitatie af te ronden.", "ApplicationVerificationCode"),
        new("EmployerReactionAccepted", "Sollicitatie geaccepteerd", "Kandidaat", "Werkgever heeft de sollicitatie geaccepteerd.", "EmployerReaction"),
        new("EmployerReactionRejected", "Sollicitatie afgewezen", "Kandidaat", "Helaas niet geselecteerd.", "EmployerReaction"),
        new("EmployerContacting", "Werkgever neemt contact op", "Kandidaat", "Na acceptatie: werkgever gaat bellen/mailen.", "EmployerContacting"),
        new("ApplicationHired", "Aangenomen", "Kandidaat", "Gefeliciteerd — je bent aangenomen, met optie andere sollicitaties in te trekken.", "ApplicationHired"),
        new("ApplicationFilledElsewhere", "Andere kandidaat gekozen", "Kandidaat", "Vacature is vervuld door iemand anders.", "ApplicationFilledElsewhere"),
        new("EmployerNewApplication", "Nieuwe sollicitatie", "Werkgever", "Er is een nieuwe kandidaat binnengekomen.", "EmployerNewApplication"),
        new("CandidateWithdrawn", "Sollicitatie ingetrokken", "Werkgever", "Kandidaat trok de sollicitatie in.", "CandidateWithdrawn"),
        new("CandidateWithdrawnOtherJob", "Ingetrokken na andere baan", "Werkgever", "Kandidaat vond elders werk.", "CandidateWithdrawnOtherJob"),
        new("PushBom", "PushBom-tip", "Kandidaat", "Nieuwe vacature in de buurt, met optie status op niet-beschikbaar.", "PushBom"),
        new("PendingApproval", "Publicatieaanvraag", "Bedrijfsmanager", "Vacature wacht op goedkeuring (tokens).", "PendingApproval"),
        new("VacancyEngagementReminder", "Engagement-check", "Werkgever", "Vacature staat 14 dagen open — KPI’s en verbeter-CTA’s.", "VacancyEngagementReminder"),
        new("DraftVacancyCleanupWarning", "Concept opruimen", "Werkgever", "Ongepubliceerd concept wordt over 14 dagen verwijderd.", DraftVacancyCleanupRules.WarningEmailCategory),
        new("CompanyReEngagement", "We missen je", "Werkgever", "Inactief bedrijf — tools staan nog klaar.", DraftVacancyCleanupRules.ReengagementEmailCategory),
        new("RegistrationActivation", "Bevestigingscode registratie", "Registratie", "OTP om bedrijfsregistratie te activeren.", "RegistrationActivation"),
        new("RegistrationCredentials", "Account actief", "Registratie", "Welkomstmail na activatie, met inlogknop.", "RegistrationCredentials"),
        new("TakeoverEmailVerification", "Bevestigingscode overname", "Registratie", "OTP voordat een overnameverzoek de eigenaar bereikt.", "TakeoverEmailVerification"),
        new("TakeoverRequest", "Overnameverzoek (eigenaar)", "Werkgever", "Inbox-mail voor de huidige vestigingseigenaar.", "TakeoverRequest"),
        new("TakeoverSubmitted", "Overnameverzoek ingediend", "Registratie", "Bevestiging aan de aanvrager.", "TakeoverSubmitted"),
        new("TakeoverApproved", "Overname goedgekeurd", "Registratie", "Aanvrager mag inloggen op de overgenomen vestiging.", "TakeoverApproved"),
        new("TakeoverRejected", "Overname afgewezen", "Registratie", "Aanvrager krijgt te horen dat het verzoek is afgewezen.", "TakeoverRejected"),
        new("UserInvite", "Uitnodiging teammate", "Werkgever", "Uitnodiging als manager/intermediair met tijdelijk wachtwoord.", "UserInvite"),
        new("SalesManagerInvite", "Uitnodiging salesmanager", "Sales", "Uitnodiging + tijdelijk wachtwoord + onboarding.", "SalesManagerInvite"),
        new("AmbassadeurInvite", "Uitnodiging ambassadeur", "Ambassadeur", "Uitnodiging + tijdelijk wachtwoord + onboarding.", "AmbassadeurInvite"),
        new("CompanyApiKeyCredentials", "API-credentials", "Werkgever", "Eenmalige API-key (in testmails een voorbeeldkey).", "CompanyApiKeyCredentials"),
        new("AccountUnsubscribeVerification", "Uitschrijfcode", "Account", "OTP om uitschrijving / right-to-be-forgotten te bevestigen.", "AccountUnsubscribeVerification"),
    ];

    public static bool TryGet(string? key, out EmailTemplateInfo info)
    {
        info = Templates.FirstOrDefault(t =>
            string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase))!;
        return info is not null;
    }

    public static ComposedEmail Compose(string key, EmailSampleContext ctx)
    {
        if (!TryGet(key, out var info))
        {
            throw new ArgumentException($"Onbekend mailtype: {key}");
        }

        return key.ToLowerInvariant() switch
        {
            "mailtest" => MailTest(ctx.PublicWebBaseUrl),
            "applicationconfirmation" => ApplicationConfirmation(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, authenticatorStubUsed: false),
            "applicationverificationcode" => ApplicationVerificationCode(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.VacancyId, ctx.OtpCode),
            "employerreactionaccepted" => EmployerReactionAccepted(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName),
            "employerreactionrejected" => EmployerReactionRejected(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName),
            "employercontacting" => EmployerContacting(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle),
            "applicationhired" => ApplicationHired(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName, ctx.ApplicationId),
            "applicationfilledelsewhere" => ApplicationFilledElsewhere(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName),
            "employernewapplication" => EmployerNewApplication(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle),
            "candidatewithdrawn" => CandidateWithdrawn(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle),
            "candidatewithdrawnotherjob" => CandidateWithdrawnOtherJob(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle),
            "pushbom" => PushBom(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.VacancyTitle, ctx.CompanyName,
                ctx.VacancyId, ctx.LocationLabel, ctx.DistanceKm, ctx.TravelMinutes, ctx.HourlyWage, "Uurloon"),
            "pendingapproval" => PendingApproval(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle, ctx.CompanyName),
            "vacancyengagementreminder" => VacancyEngagementReminder(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle, ctx.VacancyId, 42, 18, 3, 5, 2,
                VacancyEngagementReminderRules.BuildHeuristicTip(42, 18, 3, 5, 2)),
            "draftvacancycleanupwarning" => DraftVacancyCleanupWarning(
                ctx.PublicWebBaseUrl, ctx.VacancyTitle, ctx.CompanyName, ctx.VacancyId,
                DateTime.UtcNow.AddDays(DraftVacancyCleanupRules.DeleteAfterWarningDays)),
            "companyreengagement" => CompanyReEngagement(ctx.PublicWebBaseUrl, ctx.CompanyName),
            "registrationactivation" => RegistrationActivation(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.EstablishmentName, ctx.RoleLabel, "5610", ctx.OtpCode),
            "registrationcredentials" => RegistrationCredentials(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.EstablishmentName, ctx.ContactEmail, temporaryPassword: null),
            "takeoveremailverification" => TakeoverEmailVerification(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, ctx.OtpCode),
            "takeoverrequest" => TakeoverRequest(
                ctx.PublicWebBaseUrl, ctx.CompanyName, ctx.KvkEstablishmentId, ctx.RecipientName, ctx.ContactEmail),
            "takeoversubmitted" => TakeoverSubmitted(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName),
            "takeoverapproved" => TakeoverApproved(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName, ctx.ContactEmail, temporaryPassword: null, hasOrganization: true),
            "takeoverrejected" => TakeoverRejected(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.CompanyName),
            "userinvite" => UserInvite(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.RoleLabel, ctx.ContactEmail, ctx.TemporaryPassword, promotedFromCandidate: false),
            "salesmanagerinvite" => SalesManagerInvite(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.ContactEmail, ctx.TemporaryPassword),
            "ambassadeurinvite" => AmbassadeurInvite(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.ContactEmail, ctx.TemporaryPassword),
            "companyapikeycredentials" => CompanyApiKeyCredentials(
                ctx.PublicWebBaseUrl, ctx.CompanyName, ctx.ApiBaseUrl, ctx.SampleApiKey, "lobsy_test"),
            "accountunsubscribeverification" => AccountUnsubscribeVerification(
                ctx.PublicWebBaseUrl, ctx.RecipientName, ctx.OtpCode, ttlMinutes: 10),
            _ => throw new ArgumentException($"Onbekend mailtype: {key}")
        };
    }

    public static ComposedEmail MailTest(string? baseUrl)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Testmail")}
             {EmailLayout.Paragraph("Dit is een testmail van Lobsy.")}
             {EmailLayout.Paragraph("Als je dit bericht ziet, werkt de uitgaande mailconfiguratie.")}
             {EmailLayout.PrimaryButton(EmailLayout.JobMapUrl(baseUrl), "Open de banenkaart")}
             """,
            baseUrl,
            preheader: "Lobsy testmail");
        return new("MailTest", "MailTest", "Lobsy testmail", html);
    }

    public static ComposedEmail ApplicationConfirmation(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName, bool authenticatorStubUsed)
    {
        var subject = $"Sollicitatie bevestigd: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Sollicitatie verstuurd!")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},")}
             {EmailLayout.Paragraph(
                 $"Je sollicitatie op <strong>{EmailLayout.Escape(vacancyTitle)}</strong> bij {EmailLayout.Escape(companyName)} is ontvangen. Top!")}
             {EmailLayout.Paragraph("Je kunt de status volgen onder Mijn sollicitaties.")}
             {EmailLayout.PrimaryButton(EmailLayout.CandidateApplicationsUrl(baseUrl), "Bekijk mijn sollicitaties")}
             {(authenticatorStubUsed
                 ? EmailLayout.MutedNote("<em>Authenticator stub: verificatie gesimuleerd.</em>")
                 : "")}
             """,
            baseUrl,
            preheader: subject);
        return new("ApplicationConfirmation", "ApplicationConfirmation", subject, html);
    }

    public static ComposedEmail ApplicationVerificationCode(
        string? baseUrl, string candidateName, string vacancyTitle, Guid vacancyId, string code)
    {
        var subject = $"Verificatiecode voor sollicitatie: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Je verificatiecode")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},")}
             {EmailLayout.Paragraph("Gebruik deze 6-cijferige code om je sollicitatie af te ronden:")}
             {EmailLayout.OtpBlock(code)}
             {EmailLayout.Paragraph("De code is 10 minuten geldig.")}
             {EmailLayout.PrimaryButton(EmailLayout.VacancyUrl(baseUrl, vacancyId), "Terug naar de vacature")}
             """,
            baseUrl,
            preheader: "Je Lobsy-verificatiecode");
        return new("ApplicationVerificationCode", "ApplicationVerificationCode", subject, html);
    }

    public static ComposedEmail EmployerReactionAccepted(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName)
    {
        var subject = $"Je sollicitatie is geaccepteerd: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Goed nieuws!")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},")}
             {EmailLayout.Paragraph(
                 $"Het bedrijf heeft je sollicitatie voor <strong>{EmailLayout.Escape(vacancyTitle)}</strong> bij {EmailLayout.Escape(companyName)} geaccepteerd.")}
             {EmailLayout.Paragraph(
                 "Wellicht nemen ze binnenkort contact met je op. Houd je telefoon en mail in de gaten.")}
             {EmailLayout.PrimaryButton(EmailLayout.CandidateApplicationsUrl(baseUrl), "Bekijk mijn sollicitaties")}
             """,
            baseUrl,
            preheader: subject);
        return new("EmployerReactionAccepted", "EmployerReaction", subject, html);
    }

    public static ComposedEmail EmployerReactionRejected(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName)
    {
        var subject = $"Update op je sollicitatie: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Update op je sollicitatie")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},")}
             {EmailLayout.Paragraph(
                 $"Bedankt voor je interesse in <strong>{EmailLayout.Escape(vacancyTitle)}</strong> bij {EmailLayout.Escape(companyName)}.")}
             {EmailLayout.Paragraph(
                 "Helaas is de keuze dit keer niet op jou gevallen. We wensen je veel succes met je verdere zoektocht!")}
             {EmailLayout.PrimaryButton(EmailLayout.JobMapUrl(baseUrl), "Bekijk andere vacatures")}
             """,
            baseUrl,
            preheader: subject);
        return new("EmployerReactionRejected", "EmployerReaction", subject, html);
    }

    public static ComposedEmail EmployerContacting(string? baseUrl, string vacancyTitle)
    {
        var subject = $"Werkgever neemt contact op: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("De werkgever neemt contact op")}
             {EmailLayout.Paragraph(
                 $"Goed nieuws! De werkgever van <strong>{EmailLayout.Escape(vacancyTitle)}</strong> neemt contact met je op.")}
             {EmailLayout.Paragraph("Houd je telefoon, mail of WhatsApp in de gaten.")}
             {EmailLayout.PrimaryButton(EmailLayout.CandidateApplicationsUrl(baseUrl), "Bekijk mijn sollicitaties")}
             """,
            baseUrl,
            preheader: subject);
        return new("EmployerContacting", "EmployerContacting", subject, html);
    }

    public static ComposedEmail ApplicationHired(
        string? baseUrl,
        string candidateName,
        string vacancyTitle,
        string companyName,
        Guid hiredApplicationId,
        string? withdrawAbsoluteUrl = null)
    {
        var subject = $"Gefeliciteerd! Je bent aangenomen voor {vacancyTitle}";
        var withdraw = string.IsNullOrWhiteSpace(withdrawAbsoluteUrl)
            ? EmailLayout.WithdrawOthersUrl(baseUrl, hiredApplicationId)
            : withdrawAbsoluteUrl;
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Gefeliciteerd — je bent aangenomen!")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},")}
             {EmailLayout.Paragraph(
                 $"<strong>Wat een feest!</strong> Je bent aangenomen voor <strong>{EmailLayout.Escape(vacancyTitle)}</strong> bij {EmailLayout.Escape(companyName)}.")}
             {EmailLayout.Paragraph("Heel veel succes — en geniet van deze stap.")}
             {EmailLayout.PrimaryButton(EmailLayout.CandidateApplicationsUrl(baseUrl), "Bekijk mijn sollicitaties")}
             {EmailLayout.SecondaryButton(withdraw, "Andere sollicitaties netjes intrekken")}
             {EmailLayout.MutedNote(
                 $"Heb je nog andere sollicitaties lopen? Trek ze in, zodat die werkgevers weten dat je al bent voorzien. " +
                 $"<a href=\"{EmailLayout.Escape(withdraw)}\" style=\"color:{EmailLayout.AccentTeal};font-weight:650;\">Andere sollicitaties netjes intrekken</a>")}
             """,
            baseUrl,
            preheader: subject);
        return new("ApplicationHired", "ApplicationHired", subject, html);
    }

    public static ComposedEmail ApplicationFilledElsewhere(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName)
    {
        var subject = $"Update sollicitatie: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Update op je sollicitatie")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},")}
             {EmailLayout.Paragraph(
                 $"Bedankt voor je sollicitatie op <strong>{EmailLayout.Escape(vacancyTitle)}</strong> bij {EmailLayout.Escape(companyName)}.")}
             {EmailLayout.Paragraph(
                 "Helaas is de keuze op een andere kandidaat gevallen. We wensen je veel succes!")}
             {EmailLayout.PrimaryButton(EmailLayout.JobMapUrl(baseUrl), "Bekijk andere vacatures")}
             """,
            baseUrl,
            preheader: subject);
        return new("ApplicationFilledElsewhere", "ApplicationFilledElsewhere", subject, html);
    }

    public static ComposedEmail EmployerNewApplication(string? baseUrl, string vacancyTitle)
    {
        var subject = $"Nieuwe sollicitatie: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Nieuwe sollicitatie")}
             {EmailLayout.Paragraph(
                 $"Er is een nieuwe sollicitatie ontvangen voor <strong>{EmailLayout.Escape(vacancyTitle)}</strong>.")}
             {EmailLayout.Paragraph("Log in op Lobsy om de kandidaat te bekijken en te reageren.")}
             {EmailLayout.PrimaryButton(EmailLayout.BranchApplicantsUrl(baseUrl), "Bekijk sollicitaties")}
             """,
            baseUrl,
            preheader: subject);
        return new("EmployerNewApplication", "EmployerNewApplication", subject, html);
    }

    public static ComposedEmail CandidateWithdrawn(string? baseUrl, string vacancyTitle)
    {
        var subject = $"Sollicitatie ingetrokken: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Sollicitatie ingetrokken")}
             {EmailLayout.Paragraph(
                 $"Een kandidaat heeft de sollicitatie op <strong>{EmailLayout.Escape(vacancyTitle)}</strong> ingetrokken.")}
             {EmailLayout.PrimaryButton(EmailLayout.BranchApplicantsUrl(baseUrl), "Open sollicitaties")}
             """,
            baseUrl,
            preheader: subject);
        return new("CandidateWithdrawn", "CandidateWithdrawn", subject, html);
    }

    public static ComposedEmail CandidateWithdrawnOtherJob(string? baseUrl, string vacancyTitle)
    {
        var subject = $"Sollicitatie ingetrokken: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Sollicitatie ingetrokken")}
             {EmailLayout.Paragraph("Hoi,")}
             {EmailLayout.Paragraph(
                 $"Goed om te weten: de kandidaat heeft de sollicitatie op " +
                 $"<strong>{EmailLayout.Escape(vacancyTitle)}</strong> ingetrokken.")}
             {EmailLayout.Paragraph("Reden: de kandidaat heeft inmiddels een andere baan gevonden.")}
             {EmailLayout.PrimaryButton(EmailLayout.BranchApplicantsUrl(baseUrl), "Open sollicitaties")}
             """,
            baseUrl,
            preheader: subject);
        return new("CandidateWithdrawnOtherJob", "CandidateWithdrawnOtherJob", subject, html);
    }

    public static ComposedEmail PushBom(
        string? baseUrl,
        string candidateName,
        string vacancyTitle,
        string companyName,
        Guid vacancyId,
        string? locationLabel,
        double distanceKm,
        int travelMinutes,
        decimal? hourlyWage,
        string wageNote)
    {
        var facts = new List<(string, string)>
        {
            ("Functie", vacancyTitle),
            ("Bedrijf", companyName)
        };
        if (!string.IsNullOrWhiteSpace(locationLabel))
        {
            facts.Add(("Locatie", locationLabel));
        }

        facts.Add(("Afstand", EmailLayout.FormatKm(distanceKm)));
        facts.Add(("Reistijd", $"{travelMinutes} min"));
        if (hourlyWage is decimal w && !string.IsNullOrWhiteSpace(wageNote))
        {
            facts.Add((wageNote, EmailLayout.FormatEuro(w)));
        }

        var subject = $"Nieuwe vacature bij jou in de buurt: {vacancyTitle}";
        var setUnavailable = EmailLayout.SetUnavailableUrl(baseUrl);
        var deepLink = EmailLayout.VacancyUrl(baseUrl, vacancyId);
        var inner = new System.Text.StringBuilder();
        inner.Append(EmailLayout.Heading("Iets moois bij jou in de buurt"));
        inner.Append(EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(candidateName)},"));
        inner.Append(EmailLayout.Paragraph(
            "Er staat een passende vacature open — op fiets- of reistijd-afstand van jou."));
        inner.Append(EmailLayout.FactCard(facts));
        inner.Append(EmailLayout.PrimaryButton(deepLink, "Klik hier"));
        inner.Append(EmailLayout.MutedNote(
            $"Niet meer op zoek naar werk? " +
            $"<a href=\"{EmailLayout.Escape(setUnavailable)}\" style=\"color:{EmailLayout.AccentTeal};\">Zet je status op Niet beschikbaar</a> " +
            "— dan sturen we je geen PushBom-tips meer."));

        var html = EmailLayout.Wrap(
            inner.ToString(),
            baseUrl,
            preheader: $"{vacancyTitle} bij {companyName} — {EmailLayout.FormatKm(distanceKm)} van jou");
        return new("PushBom", "PushBom", subject, html);
    }

    public static ComposedEmail PendingApproval(string? baseUrl, string vacancyTitle, string companyName)
    {
        var subject = $"Publicatieaanvraag: {vacancyTitle}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Publicatieaanvraag")}
             {EmailLayout.Paragraph(
                 $"Vacature <strong>{EmailLayout.Escape(vacancyTitle)}</strong> bij {EmailLayout.Escape(companyName)} " +
                 "wacht op goedkeuring (onvoldoende tokens).")}
             {EmailLayout.Paragraph("Log in op Lobsy om de aanvraag te beoordelen onder Vacatures.")}
             {EmailLayout.PrimaryButton(EmailLayout.EmployerVacanciesUrl(baseUrl), "Beoordeel de aanvraag")}
             """,
            baseUrl,
            preheader: subject);
        return new("PendingApproval", "PendingApproval", subject, html);
    }

    public static ComposedEmail VacancyEngagementReminder(
        string? baseUrl,
        string vacancyTitle,
        Guid vacancyId,
        int impressions,
        int views,
        int shares,
        int saved,
        int applications,
        string tip)
    {
        var subject = $"Even checken: {vacancyTitle} staat {VacancyEngagementReminderRules.OpenDaysBeforeReminder} dagen open";
        var inner = $"""
            {EmailLayout.Heading("Even checken")}
            {EmailLayout.Paragraph(
                $"Je vacature <strong>{EmailLayout.Escape(vacancyTitle)}</strong> " +
                $"staat al {VacancyEngagementReminderRules.OpenDaysBeforeReminder} dagen open. Tijd voor een korte check-in.")}
            {EmailLayout.Paragraph("<strong>Dit zien we tot nu toe:</strong>")}
            {EmailLayout.KpiList([
                ("In zoekresultaten", impressions.ToString()),
                ("Bekeken", views.ToString()),
                ("Gedeeld", shares.ToString()),
                ("Bewaard", saved.ToString()),
                ("Sollicitaties", applications.ToString())
            ])}
            {EmailLayout.Paragraph($"<strong>Tip van Lobsy:</strong> {EmailLayout.Escape(tip)}")}
            {EmailLayout.Paragraph(
                $"Pas de vacature aan vóór de einddatum. Bij een update verlengen we de deadline als goodwill met " +
                $"{VacancyEngagementReminderRules.GoodwillExtendDays} dagen — zo geef je je tekst nog even de ruimte.")}
            {EmailLayout.PrimaryButton(EmailLayout.EditVacancyUrl(baseUrl, vacancyId), "Vacature nu verbeteren")}
            {EmailLayout.SecondaryButton(EmailLayout.HighlightVacancyUrl(baseUrl, vacancyId), "Highlight deze vacature")}
            {EmailLayout.SecondaryButton(EmailLayout.PushBomVacancyUrl(baseUrl, vacancyId), "PushBom versturen")}
            {EmailLayout.MutedNote(
                "Highlight en PushBom openen je vacatureoverzicht, waar je de actie met één klik kunt afronden (tokens vereist).")}
            """;
        var html = EmailLayout.Wrap(
            inner,
            baseUrl,
            preheader: $"{vacancyTitle}: {impressions} zoek · {views} bekeken · {applications} sollicitaties");
        return new("VacancyEngagementReminder", "VacancyEngagementReminder", subject, html);
    }

    public static ComposedEmail DraftVacancyCleanupWarning(
        string? baseUrl, string vacancyTitle, string companyName, Guid vacancyId, DateTime deleteOnUtc)
    {
        var deleteLabel = deleteOnUtc.ToString("dd-MM-yyyy");
        var subject = $"Concept-vacature '{vacancyTitle}' wordt over 14 dagen verwijderd";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Concept wordt binnenkort opgeruimd")}
             {EmailLayout.Paragraph("Hallo,")}
             {EmailLayout.Paragraph(
                 $"Je concept-vacature <strong>{EmailLayout.Escape(vacancyTitle)}</strong> " +
                 $"voor <strong>{EmailLayout.Escape(companyName)}</strong> staat al " +
                 $"{DraftVacancyCleanupRules.WarningAfterDays} dagen als concept en is nog nooit gepubliceerd.")}
             {EmailLayout.Paragraph(
                 $"Als je niets doet, ruimt Lobsy dit concept automatisch op op " +
                 $"<strong>{EmailLayout.Escape(deleteLabel)}</strong> (14 dagen vanaf deze mail).")}
             {EmailLayout.Paragraph(
                 "Vacatures die je wél hebt gepubliceerd blijven altijd bewaard — ook na de deadline.")}
             {EmailLayout.PrimaryButton(EmailLayout.EditVacancyUrl(baseUrl, vacancyId), "Open dit concept")}
             {EmailLayout.MutedNote("Log in op Lobsy → Vacatures om dit concept te publiceren of te verwijderen.")}
             """,
            baseUrl,
            preheader: $"Concept '{vacancyTitle}' wordt over 14 dagen verwijderd");
        return new("DraftVacancyCleanupWarning", DraftVacancyCleanupRules.WarningEmailCategory, subject, html);
    }

    public static ComposedEmail CompanyReEngagement(string? baseUrl, string companyName)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("We missen je")}
             {EmailLayout.Paragraph(
                 $"Hallo team <strong>{EmailLayout.Escape(companyName)}</strong>,")}
             {EmailLayout.Paragraph(
                 "Het is al een tijdje stil op Lobsy — geen actieve vacatures, geen inlog, " +
                 "geen API-call en geen CSV-upload.")}
             {EmailLayout.Paragraph("Goed nieuws: jullie tools staan nog klaar:")}
             {EmailLayout.KpiList([
                 ("CSV Batch Import", "Veel vacatures in één keer als concept"),
                 ("Externe API", "Koppel je ATS met een API-key"),
                 ("Publiceren", "Tokens pas bij publicatie in Lobsy")
             ])}
             {EmailLayout.PrimaryButton(EmailLayout.LoginUrl(baseUrl), "Inloggen op Lobsy")}
             {EmailLayout.MutedNote("Log in op Lobsy wanneer je weer wilt starten.")}
             """,
            baseUrl,
            preheader: "We missen je bij Lobsy");
        return new("CompanyReEngagement", DraftVacancyCleanupRules.ReengagementEmailCategory, "We missen je bij Lobsy", html);
    }

    public static ComposedEmail RegistrationActivation(
        string? baseUrl, string contactName, string establishmentName, string roleLabel, string? sbi, string code)
    {
        var sbiBit = string.IsNullOrEmpty(sbi) ? "" : $", SBI {EmailLayout.Escape(sbi)}";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Welkom bij Lobsy")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(contactName)},")}
             {EmailLayout.Paragraph(
                 $"Bevestig je e-mailadres om je bedrijfsregistratie voor " +
                 $"<strong>{EmailLayout.Escape(establishmentName)}</strong> te activeren " +
                 $"(rol: {EmailLayout.Escape(roleLabel)}{sbiBit}).")}
             {EmailLayout.Paragraph(
                 "Na bevestiging kun je direct aan de slag — je eerste token is helemaal gratis, " +
                 "zodat je meteen een vacature kunt plaatsen.")}
             {EmailLayout.Paragraph("Je bevestigingscode (geldig 10 minuten):")}
             {EmailLayout.OtpBlock(code)}
             {EmailLayout.PrimaryButton(EmailLayout.RegisterActivateUrl(baseUrl), "Code invoeren")}
             """,
            baseUrl,
            preheader: "Je Lobsy-bevestigingscode");
        return new("RegistrationActivation", "RegistrationActivation", "Bevestigingscode — Lobsy", html);
    }

    public static ComposedEmail RegistrationCredentials(
        string? baseUrl, string contactName, string establishmentName, string contactEmail, string? temporaryPassword)
    {
        var loginUrl = EmailLayout.LoginUrl(baseUrl);
        var passwordBlock = temporaryPassword is null
            ? EmailLayout.Paragraph(
                "Log in met het wachtwoord dat je bij registratie hebt gekozen, of via " +
                "<strong>Microsoft Entra</strong> / Google met hetzelfde geverifieerde e-mailadres.")
            : $"""
               {EmailLayout.Paragraph("Gebruik dit eenmalige tijdelijke wachtwoord (niet opnieuw zichtbaar in de app):")}
               <p style="margin:16px 0;font-size:20px;letter-spacing:0.06em;font-weight:700;color:{EmailLayout.BrandNavy};text-align:center;"><code>{EmailLayout.Escape(temporaryPassword)}</code></p>
               {EmailLayout.MutedNote("Wijzig dit wachtwoord zo snel mogelijk.")}
               """;
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Account actief")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(contactName)},")}
             {EmailLayout.Paragraph(
                 $"Geslaagd! Je account voor <strong>{EmailLayout.Escape(establishmentName)}</strong> " +
                 "is geactiveerd. Je kunt direct aan de slag.")}
             {EmailLayout.Paragraph(
                 "Je hebt van ons je eerste token helemaal gratis gekregen — daarmee plaats je meteen je eerste vacature.")}
             {EmailLayout.Paragraph(
                 "Je kunt inloggen met e-mail/wachtwoord of met <strong>Microsoft Entra</strong> / " +
                 $"Google op <code>{EmailLayout.Escape(contactEmail)}</code>.")}
             {passwordBlock}
             {EmailLayout.PrimaryButton(loginUrl, "Inloggen")}
             {EmailLayout.MutedNote($"Inloggen via Lobsy ({EmailLayout.Escape(loginUrl)}).")}
             """,
            baseUrl,
            preheader: "Je Lobsy-account is actief");
        return new("RegistrationCredentials", "RegistrationCredentials", "Geslaagd — je Lobsy-account is actief!", html);
    }

    public static ComposedEmail TakeoverEmailVerification(
        string? baseUrl, string contactName, string companyName, string code)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Bevestig je e-mailadres")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(contactName)},")}
             {EmailLayout.Paragraph(
                 $"Vestiging <strong>{EmailLayout.Escape(companyName)}</strong> is al geregistreerd. " +
                 "Bevestig eerst je e-mailadres met deze code (geldig 10 minuten):")}
             {EmailLayout.OtpBlock(code)}
             {EmailLayout.Paragraph("Daarna sturen we het overnameverzoek naar de huidige eigenaar.")}
             {EmailLayout.PrimaryButton(EmailLayout.RegisterActivateUrl(baseUrl), "Code invoeren")}
             """,
            baseUrl,
            preheader: "Bevestigingscode overnameverzoek");
        return new("TakeoverEmailVerification", "TakeoverEmailVerification", "Bevestigingscode overnameverzoek — Lobsy", html);
    }

    public static ComposedEmail TakeoverRequest(
        string? baseUrl, string companyName, string? kvkEstablishmentId, string applicantName, string applicantEmail)
    {
        var inboxUrl = EmailLayout.TakeoversUrl(baseUrl);
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Overnameverzoek")}
             {EmailLayout.Paragraph(
                 $"Er is een overnameverzoek voor <strong>{EmailLayout.Escape(companyName)}</strong> " +
                 $"({EmailLayout.Escape(kvkEstablishmentId ?? "")}).")}
             {EmailLayout.Paragraph(
                 $"Aanvrager: {EmailLayout.Escape(applicantName)} ({EmailLayout.Escape(applicantEmail)}).")}
             {EmailLayout.PrimaryButton(inboxUrl, "Bekijk overnames")}
             {EmailLayout.MutedNote(
                 $"Bekijk verzoeken in Lobsy onder Overnames ({EmailLayout.Escape(inboxUrl)}).")}
             """,
            baseUrl,
            preheader: "Overnameverzoek vestiging");
        return new("TakeoverRequest", "TakeoverRequest", "Overnameverzoek vestiging — Lobsy", html);
    }

    public static ComposedEmail TakeoverSubmitted(string? baseUrl, string contactName, string companyName)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Verzoek ingediend")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(contactName)},")}
             {EmailLayout.Paragraph(
                 $"Vestiging <strong>{EmailLayout.Escape(companyName)}</strong> is al in gebruik. " +
                 "We hebben een overnameverzoek gestuurd naar de huidige eigenaar.")}
             {EmailLayout.PrimaryButton(EmailLayout.LoginUrl(baseUrl), "Naar Lobsy")}
             """,
            baseUrl,
            preheader: "Overnameverzoek ingediend");
        return new("TakeoverSubmitted", "TakeoverSubmitted", "Overnameverzoek ingediend — Lobsy", html);
    }

    public static ComposedEmail TakeoverApproved(
        string? baseUrl,
        string contactName,
        string companyName,
        string contactEmail,
        string? temporaryPassword,
        bool hasOrganization)
    {
        var loginUrl = EmailLayout.LoginUrl(baseUrl);
        var passwordBlock = temporaryPassword is null
            ? EmailLayout.Paragraph(
                "Log in met het wachtwoord dat je bij registratie hebt gekozen, of via " +
                "<strong>Microsoft Entra</strong> met hetzelfde geverifieerde e-mailadres.")
            : $"""
               {EmailLayout.Paragraph(
                   $"Log in met <code>{EmailLayout.Escape(contactEmail)}</code>. " +
                   "Je eenmalige tijdelijke wachtwoord (bewaar dit veilig; het wordt niet opnieuw getoond):")}
               <p style="margin:16px 0;font-size:20px;letter-spacing:0.06em;font-weight:700;color:{EmailLayout.BrandNavy};text-align:center;"><code>{EmailLayout.Escape(temporaryPassword)}</code></p>
               {EmailLayout.MutedNote("Wijzig dit wachtwoord zo snel mogelijk.")}
               """;
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Overname goedgekeurd")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(contactName)},")}
             {EmailLayout.Paragraph(
                 $"Je overnameverzoek voor <strong>{EmailLayout.Escape(companyName)}</strong> is goedgekeurd.")}
             {EmailLayout.Paragraph(
                 "Tokens, vacatures en geschiedenis blijven gekoppeld aan de vestiging" +
                 $"{(hasOrganization ? " onder de organisatie" : "")}.")}
             {passwordBlock}
             {EmailLayout.PrimaryButton(loginUrl, "Inloggen")}
             {EmailLayout.MutedNote($"Inloggen via Lobsy ({EmailLayout.Escape(loginUrl)}).")}
             """,
            baseUrl,
            preheader: "Overname goedgekeurd");
        return new("TakeoverApproved", "TakeoverApproved", "Overname goedgekeurd — Lobsy", html);
    }

    public static ComposedEmail TakeoverRejected(string? baseUrl, string contactName, string companyName)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Overname afgewezen")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(contactName)},")}
             {EmailLayout.Paragraph(
                 $"Je overnameverzoek voor <strong>{EmailLayout.Escape(companyName)}</strong> is afgewezen.")}
             {EmailLayout.PrimaryButton(EmailLayout.RegisterUrl(baseUrl), "Opnieuw registreren")}
             """,
            baseUrl,
            preheader: "Overname afgewezen");
        return new("TakeoverRejected", "TakeoverRejected", "Overname afgewezen — Lobsy", html);
    }

    public static ComposedEmail UserInvite(
        string? baseUrl,
        string fullName,
        string roleLabel,
        string email,
        string temporaryPassword,
        bool promotedFromCandidate)
    {
        var loginUrl = EmailLayout.LoginUrl(baseUrl);
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading($"Uitnodiging — {EmailLayout.Escape(roleLabel)}")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(fullName)},")}
             {EmailLayout.Paragraph(
                 $"Je bent uitgenodigd als <strong>{EmailLayout.Escape(roleLabel)}</strong> op Lobsy.")}
             {EmailLayout.Paragraph(
                 "<strong>Aanbevolen:</strong> log in met <strong>Google</strong> of <strong>Microsoft Entra</strong> " +
                 $"op <code>{EmailLayout.Escape(email)}</code> — dan krijg je automatisch je managerrol.")}
             {EmailLayout.Paragraph(
                 "Alternatief: lokaal inloggen via het inlogscherm van Lobsy met dit eenmalige tijdelijke wachtwoord " +
                 "(niet opnieuw zichtbaar in de app):")}
             <p style="margin:16px 0;font-size:20px;letter-spacing:0.06em;font-weight:700;color:{EmailLayout.BrandNavy};text-align:center;"><code>{EmailLayout.Escape(temporaryPassword)}</code></p>
             {(promotedFromCandidate
                 ? EmailLayout.Paragraph("Je eerdere sollicitaties blijven zichtbaar (alleen-lezen) in Lobsy.")
                 : "")}
             {EmailLayout.PrimaryButton(loginUrl, "Inloggen")}
             {EmailLayout.MutedNote("Wijzig het wachtwoord zo snel mogelijk na je eerste login.")}
             """,
            baseUrl,
            preheader: $"Uitnodiging voor Lobsy ({roleLabel})");
        return new("UserInvite", "UserInvite", $"Uitnodiging voor Lobsy ({roleLabel})", html);
    }

    public static ComposedEmail SalesManagerInvite(
        string? baseUrl, string name, string email, string temporaryPassword)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Uitnodiging salesmanager")}
             {EmailLayout.Paragraph($"Hallo {EmailLayout.Escape(name)},")}
             {EmailLayout.Paragraph("Je bent uitgenodigd als salesmanager op Lobsy.")}
             {EmailLayout.Paragraph(
                 $"Log in met <strong>{EmailLayout.Escape(email)}</strong> " +
                 "en dit tijdelijke wachtwoord:")}
             <p style="margin:16px 0;font-size:20px;letter-spacing:0.06em;font-weight:700;color:{EmailLayout.BrandNavy};text-align:center;"><code>{EmailLayout.Escape(temporaryPassword)}</code></p>
             {EmailLayout.Paragraph(
                 "Vul daarna je KvK/BTW/NAW-gegevens in en onderteken de bemiddelingsovereenkomst om je trackingcode te ontvangen.")}
             {EmailLayout.PrimaryButton(EmailLayout.LoginUrl(baseUrl), "Inloggen")}
             {EmailLayout.SecondaryButton(EmailLayout.SalesOnboardingUrl(baseUrl), "Start onboarding")}
             {EmailLayout.MutedNote("Wijzig het wachtwoord zo snel mogelijk na je eerste login.")}
             """,
            baseUrl,
            preheader: "Uitnodiging Lobsy salesmanager");
        return new("SalesManagerInvite", "SalesManagerInvite", "Uitnodiging Lobsy salesmanager", html);
    }

    public static ComposedEmail AmbassadeurInvite(
        string? baseUrl, string name, string email, string temporaryPassword)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Uitnodiging ambassadeur")}
             {EmailLayout.Paragraph($"Hallo {EmailLayout.Escape(name)},")}
             {EmailLayout.Paragraph("Je bent uitgenodigd als ambassadeur op Lobsy.")}
             {EmailLayout.Paragraph(
                 $"Log in met <strong>{EmailLayout.Escape(email)}</strong> " +
                 "en dit tijdelijke wachtwoord:")}
             <p style="margin:16px 0;font-size:20px;letter-spacing:0.06em;font-weight:700;color:{EmailLayout.BrandNavy};text-align:center;"><code>{EmailLayout.Escape(temporaryPassword)}</code></p>
             {EmailLayout.Paragraph(
                 "Vul daarna je KvK/BTW/NAW-gegevens in en onderteken de bemiddelingsovereenkomst om je trackingcode te ontvangen.")}
             {EmailLayout.PrimaryButton(EmailLayout.LoginUrl(baseUrl), "Inloggen")}
             {EmailLayout.SecondaryButton(EmailLayout.AmbassadeurOnboardingUrl(baseUrl), "Start onboarding")}
             {EmailLayout.MutedNote("Wijzig het wachtwoord zo snel mogelijk na je eerste login.")}
             """,
            baseUrl,
            preheader: "Uitnodiging Lobsy ambassadeur");
        return new("AmbassadeurInvite", "AmbassadeurInvite", "Uitnodiging Lobsy ambassadeur", html);
    }

    public static ComposedEmail CompanyApiKeyCredentials(
        string? baseUrl, string companyName, string apiBase, string plaintextKey, string keyPrefix)
    {
        var endpoint = apiBase.TrimEnd('/') + "/api/external/vacancies";
        var swaggerUrl = apiBase.TrimEnd('/') + "/swagger";
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("API-credentials")}
             {EmailLayout.Paragraph("Hallo,")}
             {EmailLayout.Paragraph(
                 $"Hierbij de API-credentials voor <strong>{EmailLayout.Escape(companyName)}</strong>.")}
             {EmailLayout.FactCard([
                 ("Endpoint", endpoint),
                 ("Header", "X-API-Key: <jouw-api-key>"),
                 ("API-key", plaintextKey),
                 ("Prefix", keyPrefix),
                 ("Swagger", swaggerUrl)
             ])}
             {EmailLayout.Paragraph(
                 "<strong>Let op:</strong> deze sleutel wordt slechts één keer getoond en " +
                 "vervangt eventuele eerdere actieve keys. Bewaar hem veilig.")}
             {EmailLayout.PrimaryButton(swaggerUrl, "Open API-documentatie")}
             {EmailLayout.SecondaryButton(EmailLayout.EmployerApiKeysUrl(baseUrl), "Bedrijfsgegevens in Lobsy")}
             """,
            baseUrl,
            preheader: $"API-credentials voor {companyName}");
        return new("CompanyApiKeyCredentials", "CompanyApiKeyCredentials", $"Lobsy API-credentials voor {companyName}", html);
    }

    public static ComposedEmail AccountUnsubscribeVerification(
        string? baseUrl, string fullName, string code, int ttlMinutes)
    {
        var html = EmailLayout.Wrap(
            $"""
             {EmailLayout.Heading("Bevestig je uitschrijving")}
             {EmailLayout.Paragraph($"Hoi {EmailLayout.Escape(fullName)},")}
             {EmailLayout.Paragraph("Je hebt gevraagd om je Lobsy-account af te melden.")}
             {EmailLayout.Paragraph("Gebruik deze 6-cijferige code om de uitschrijving te bevestigen:")}
             {EmailLayout.OtpBlock(code)}
             {EmailLayout.Paragraph(
                 $"De code is {ttlMinutes} minuten geldig. Heb je dit niet zelf aangevraagd? Negeer deze mail dan.")}
             {EmailLayout.PrimaryButton(EmailLayout.PrivacyDataUrl(baseUrl), "Code invoeren")}
             """,
            baseUrl,
            preheader: "Je verificatiecode voor uitschrijving");
        return new("AccountUnsubscribeVerification", "AccountUnsubscribeVerification",
            "Verificatiecode voor uitschrijving bij Lobsy", html);
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
    string TemporaryPassword,
    string SampleApiKey,
    string RoleLabel,
    string EstablishmentName,
    string ContactEmail,
    string KvkEstablishmentId,
    string ApiBaseUrl)
{
    public static EmailSampleContext ForPreview(string publicWebBaseUrl, string? contactEmail = null)
        => new(
            PublicWebBaseUrl: string.IsNullOrWhiteSpace(publicWebBaseUrl) ? "https://lobsy.nl" : publicWebBaseUrl,
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
            TemporaryPassword: TransactionalEmails.SamplePassword,
            SampleApiKey: TransactionalEmails.SampleApiKey,
            RoleLabel: "Filiaalmanager",
            EstablishmentName: "Bakkerij De Gouden Korrel — Delft",
            ContactEmail: string.IsNullOrWhiteSpace(contactEmail) ? "tester@example.com" : contactEmail.Trim(),
            KvkEstablishmentId: "000012345678",
            ApiBaseUrl: "https://api.lobsy.nl");
}
