using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail RegistrationActivation(
        string? baseUrl, string contactName, string establishmentName, string roleLabel, string? sbi, string code)
    {
        var sbiBit = string.IsNullOrEmpty(sbi) ? "" : $", SBI {sbi}";
        return Finish(Doc("RegistrationActivation", "Bevestigingscode — Lobsy", "Je Lobsy-bevestigingscode", "Welkom bij Lobsy",
            [
                P(Fmt("Bevestig je e-mailadres om je bedrijfsregistratie voor {0} te activeren (rol: {1}{2}).",
                    EmailArg.Bold(establishmentName), EmailArg.Plain(roleLabel), EmailArg.Plain(sbiBit))),
                P("Na bevestiging kun je direct je bedrijf inrichten: conceptvacatures klaarzetten, het profiel invullen en collega's uitnodigen. Publiceren volgt na verificatie."),
                P("Je bevestigingscode (geldig 10 minuten):"),
                C(code, "")
            ],
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail RegistrationCredentials(
        string? baseUrl, string contactName, string establishmentName, string contactEmail, string? setPasswordUrl)
    {
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? "Inloggen" : "Kies je wachtwoord";
        var blocks = new List<EmailBlock>
        {
            P(Fmt("Geslaagd! Je account voor {0} is geactiveerd. Je kunt direct aan de slag met concepten, het profiel en uitnodigingen.", EmailArg.Bold(establishmentName))),
            P("Zodra je bedrijf is geverifieerd, worden klaargezette vacatures gepubliceerd en ontvang je (buiten de gratis-publicatieperiode) je welkomsttoken."),
            P(Fmt("Je kunt inloggen met e-mail/wachtwoord of met Microsoft Entra / Google op {0}.", EmailArg.Plain(contactEmail))),
            setPasswordUrl is null
                ? P("Log in met het wachtwoord dat je bij registratie hebt gekozen, of via Microsoft Entra / Google met hetzelfde geverifieerde e-mailadres.")
                : P("Kies een wachtwoord via de knop hieronder. Daarna kun je inloggen met e-mail/wachtwoord of met Microsoft Entra / Google.")
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N($"De link werkt tot {Jobsy.Core.Time.AmsterdamTime.FormatDate(DateTime.UtcNow.AddDays(7))}. Daarna vraag je een nieuwe uitnodiging."));
        }

        blocks.Add(N($"Inloggen via Lobsy ({links.Login})."));
        return Finish(Doc("RegistrationCredentials", "Geslaagd — je Lobsy-account is actief!", "Je Lobsy-account is actief", "Account actief",
            blocks, Button(ctaLabel, ctaUrl), greeting: $"Hoi {contactName},", showMascot: true), baseUrl);
    }

    public static ComposedEmail CompanyVerificationReminder(
        string? baseUrl, string contactName, string companyName, int day, string? deletionDateLabel)
    {
        var links = Links(baseUrl);
        var dayBit = day == 21
            ? "Al drie weken geleden heb je je bedrijf geregistreerd, maar het is nog niet geverifieerd."
            : "Een week geleden heb je je bedrijf geregistreerd. Verifieer het zodat kandidaten je kunnen vinden.";
        var blocks = new List<EmailBlock>
        {
            P(dayBit),
            P(Fmt("Bedrijf: {0}. Tot die tijd blijft het onzichtbaar voor kandidaten.", EmailArg.Bold(companyName)))
        };
        if (!string.IsNullOrWhiteSpace(deletionDateLabel))
        {
            blocks.Add(P(Fmt("Zonder verificatie verwijderen we deze registratie op {0} (60 dagen na aanmelding).", EmailArg.Bold(deletionDateLabel!))));
        }

        var subject = day == 21
            ? "Laatste herinnering: verifieer je bedrijf — Lobsy"
            : "Herinnering: verifieer je bedrijf — Lobsy";
        return Finish(Doc("CompanyVerificationReminder", subject, "Verifieer je bedrijf op Lobsy", "Verifieer je bedrijf",
            blocks, Button("Nu verifiëren", links.RegisterVerify), greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail CompanyVerified(
        string? baseUrl, string contactName, string companyName, bool welcomeTokenGranted, IReadOnlyList<string> publishedTitles)
    {
        var links = Links(baseUrl);
        var blocks = new List<EmailBlock>
        {
            P(Fmt("Gefeliciteerd! {0} is geverifieerd en nu zichtbaar voor kandidaten.", EmailArg.Bold(companyName)))
        };
        if (publishedTitles.Count == 0)
        {
            blocks.Add(P("Er stonden geen klaargezette vacatures klaar om te publiceren."));
        }
        else
        {
            blocks.Add(P("Gepubliceerd:"));
            blocks.Add(F(publishedTitles.Select((t, i) => ($"Vacature {i + 1}", t))));
        }

        blocks.Add(welcomeTokenGranted
            ? P("Je hebt van ons je welkomsttoken gekregen — daarmee plaats je (of verleng je) een vacature.")
            : P("Tijdens de gratis-publicatieperiode ontvang je geen welkomsttoken; publiceren is nu gratis."));

        return Finish(Doc("CompanyVerified", "Je bedrijf is geverifieerd — Lobsy", "Je bedrijf is geverifieerd op Lobsy", "Je bedrijf is geverifieerd",
            blocks, Button("Naar vacatures", links.EmployerVacancies),
            greeting: $"Hoi {contactName},", showMascot: true), baseUrl);
    }

    public static ComposedEmail CompanyBusinessEmailVerification(string companyName, string code, string? baseUrl)
    {
        return Finish(Doc("CompanyBusinessEmailVerification", "Je Lobsy-verificatiecode", "Verificatiecode voor je bedrijf", "Je verificatiecode",
            [
                P(Fmt("Gebruik deze 6-cijferige code om {0} te verifiëren:", EmailArg.Bold(companyName))),
                C(code, "De code is 10 minuten geldig.")
            ]), baseUrl);
    }

    public static ComposedEmail CompanyVerificationRejected(
        string? baseUrl, string contactName, string companyName, string reason)
    {
        var links = Links(baseUrl);
        return Finish(Doc("CompanyVerificationRejected", "Verificatie afgewezen — Lobsy", "Verificatie afgewezen", "Verificatie afgewezen",
            [
                P(Fmt("We konden {0} niet verifiëren.", EmailArg.Bold(companyName))),
                P($"Reden: {reason}"),
                P("Je kunt een brief met code aanvragen of opnieuw een handmatige controle starten.")
            ],
            Button("Opnieuw verifiëren", links.RegisterVerify),
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail EngagementClaimRemoved(
        string? baseUrl, string companyName, string itemLabel, string reason)
    {
        var links = Links(baseUrl);
        return Finish(Doc("EngagementClaimRemoved", "Kenmerk verwijderd — Lobsy", "Maatschappelijk kenmerk verwijderd", "Kenmerk verwijderd",
            [
                P(Fmt("Het kenmerk {0} van {1} is verwijderd van Lobsy.", EmailArg.Bold(itemLabel), EmailArg.Bold(companyName))),
                P($"Reden: {reason}"),
                P("Je kunt het kenmerk over 30 dagen opnieuw opgeven met nieuw bewijs, of een ander kenmerk kiezen.")
            ],
            Button("Naar dashboard", links.EmployerHome)), baseUrl);
    }

    public static ComposedEmail CompanyUnverifiedDeleted(string? baseUrl, string contactName, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("CompanyUnverifiedDeleted", "Registratie verwijderd — Lobsy", "Niet-geverifieerde registratie verwijderd", "Registratie verwijderd",
            [
                P(Fmt("Je niet-geverifieerde registratie voor {0} is na 60 dagen verwijderd. Je kunt opnieuw beginnen via Bedrijf registreren.", EmailArg.Bold(companyName)))
            ],
            Button("Opnieuw registreren", links.Register),
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail TakeoverEmailVerification(
        string? baseUrl, string contactName, string companyName, string code)
    {
        return Finish(Doc("TakeoverEmailVerification", "Bevestigingscode overnameverzoek — Lobsy", "Bevestigingscode overnameverzoek", "Bevestig je e-mailadres",
            [
                P(Fmt("Vestiging {0} is al geregistreerd. Bevestig eerst je e-mailadres met deze code (geldig 10 minuten):", EmailArg.Bold(companyName))),
                C(code, ""),
                P("Daarna sturen we het overnameverzoek naar de huidige eigenaar.")
            ],
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail TakeoverRequest(
        string? baseUrl, string companyName, string? kvkEstablishmentId, string applicantName, string applicantEmail)
    {
        var links = Links(baseUrl);
        var inboxUrl = links.EmployerTakeovers;
        return Finish(Doc("TakeoverRequest", "Overnameverzoek vestiging — Lobsy", "Overnameverzoek vestiging", "Overnameverzoek",
            [
                P(Fmt("Er is een overnameverzoek voor {0} ({1}).", EmailArg.Bold(companyName), EmailArg.Plain(kvkEstablishmentId ?? ""))),
                P($"Aanvrager: {applicantName} ({applicantEmail})."),
                N($"Bekijk verzoeken in Lobsy onder Overnames ({inboxUrl}).")
            ],
            Button("Bekijk overnames", inboxUrl)), baseUrl);
    }

    public static ComposedEmail TakeoverSubmitted(string? baseUrl, string contactName, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("TakeoverSubmitted", "Overnameverzoek ingediend — Lobsy", "Overnameverzoek ingediend", "Verzoek ingediend",
            [
                P(Fmt("Vestiging {0} is al in gebruik. We hebben een overnameverzoek gestuurd naar de huidige eigenaar.", EmailArg.Bold(companyName)))
            ],
            Button("Naar Lobsy", links.HowLobsyWorks),
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail AccessRequestEmailVerification(
        string? baseUrl, string contactName, string companyName, string code)
    {
        return Finish(Doc("AccessRequestEmailVerification", "Bevestigingscode toegangsverzoek — Lobsy", "Bevestigingscode toegangsverzoek", "Bevestig je e-mailadres",
            [
                P(Fmt("Bevestig je e-mailadres om toegang aan te vragen tot {0}. Code geldig 10 minuten:", EmailArg.Bold(companyName))),
                C(code, "")
            ],
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail AccessRequestSubmitted(string? baseUrl, string contactName, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("AccessRequestSubmitted", "Toegangsverzoek verstuurd — Lobsy", "Toegangsverzoek verstuurd", "Aanvraag verstuurd",
            [
                P(Fmt("Je aanvraag is verstuurd. {0} beslist; na 5 werkdagen kijkt Lobsy mee.", EmailArg.Bold(companyName)))
            ],
            Button("Naar Lobsy", links.Login),
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail AccessRequestToManager(
        string? baseUrl, string companyName, string requesterName, string? requesterFunction,
        string requesterEmail, string roleLabel)
    {
        var links = Links(baseUrl);
        var functionBit = string.IsNullOrWhiteSpace(requesterFunction) ? "" : $" ({requesterFunction})";
        return Finish(Doc("AccessRequestToManager", "Toegangsverzoek — Lobsy", "Toegangsverzoek op Lobsy", "Nieuw toegangsverzoek",
            [
                P(Fmt("Er is een toegangsverzoek voor {0}.", EmailArg.Bold(companyName))),
                P($"Aanvrager: {requesterName}{functionBit} — {requesterEmail}. Gevraagde rol: {roleLabel}.")
            ],
            Button("Bekijk toegangsverzoeken", links.EmployerTakeovers)), baseUrl);
    }

    public static ComposedEmail AccessRequestReminder(string? baseUrl, string companyName, string requesterName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("AccessRequestReminder", "Herinnering toegangsverzoek — Lobsy", "Herinnering toegangsverzoek", "Herinnering toegangsverzoek",
            [
                P(Fmt("Er wacht nog een toegangsverzoek van {0} voor {1}.", EmailArg.Plain(requesterName), EmailArg.Bold(companyName)))
            ],
            Button("Bekijk verzoek", links.EmployerTakeovers)), baseUrl);
    }

    public static ComposedEmail AccessRequestRejected(
        string? baseUrl, string contactName, string companyName, string? reason)
    {
        var links = Links(baseUrl);
        var blocks = new List<EmailBlock>
        {
            P(Fmt("Je verzoek voor toegang tot {0} is afgewezen.", EmailArg.Bold(companyName)))
        };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            blocks.Add(P($"Reden: {reason}"));
        }

        return Finish(Doc("AccessRequestRejected", "Toegangsverzoek afgewezen — Lobsy", "Toegangsverzoek afgewezen", "Toegangsverzoek afgewezen",
            blocks, Button("Opnieuw aanvragen", links.RegisterAccess), greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail AccessRequestExpired(string? baseUrl, string contactName, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("AccessRequestExpired", "Toegangsverzoek verlopen — Lobsy", "Toegangsverzoek verlopen", "Toegangsverzoek verlopen",
            [
                P(Fmt("Je verzoek voor toegang tot {0} is na 30 dagen verlopen.", EmailArg.Bold(companyName)))
            ],
            Button("Opnieuw aanvragen", links.RegisterAccess),
            greeting: $"Hoi {contactName},"), baseUrl);
    }

    public static ComposedEmail OwnershipTransferManagersNotify(string? baseUrl, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("OwnershipTransferManagersNotify", "Eigendomsoverdracht aangevraagd — Lobsy", "Eigendomsoverdracht aangevraagd", "Eigendomsoverdracht aangevraagd",
            [
                P(Fmt("Er is een eigendomsoverdracht aangevraagd voor {0}. Klopt dit niet? Reageer binnen 7 dagen via Lobsy-support.", EmailArg.Bold(companyName)))
            ],
            Button("Naar Lobsy", links.EmployerTakeovers)), baseUrl);
    }

    public static ComposedEmail IntermediaryClientSelfManaged(string? baseUrl, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("IntermediaryClientSelfManaged", "Klant beheert zelf — Lobsy", "Klant beheert zelf — koppeling blijft", "Klant beheert nu zelf",
            [
                P(Fmt("{0} beheert nu zelf een account op Lobsy; jullie koppeling blijft bestaan.", EmailArg.Bold(companyName)))
            ],
            Button("Naar Lobsy", links.Login)), baseUrl);
    }

    public static ComposedEmail TakeoverApproved(
        string? baseUrl, string contactName, string companyName, string contactEmail,
        string? setPasswordUrl, bool hasOrganization)
    {
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? "Inloggen" : "Kies je wachtwoord";
        var orgBit = hasOrganization ? " onder de organisatie" : "";
        var blocks = new List<EmailBlock>
        {
            P(Fmt("Je overnameverzoek voor {0} is goedgekeurd.", EmailArg.Bold(companyName))),
            P($"Tokens, vacatures en geschiedenis blijven gekoppeld aan de vestiging{orgBit}."),
            setPasswordUrl is null
                ? P("Log in met het wachtwoord dat je bij registratie hebt gekozen, of via Microsoft Entra met hetzelfde geverifieerde e-mailadres.")
                : P(Fmt("Kies een wachtwoord voor {0} via de knop hieronder.", EmailArg.Plain(contactEmail)))
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N($"De link werkt tot {Jobsy.Core.Time.AmsterdamTime.FormatDate(DateTime.UtcNow.AddDays(7))}. Daarna vraag je een nieuwe uitnodiging."));
        }

        blocks.Add(N($"Inloggen via Lobsy ({links.Login})."));
        return Finish(Doc("TakeoverApproved", "Overname goedgekeurd — Lobsy", "Overname goedgekeurd", "Overname goedgekeurd",
            blocks, Button(ctaLabel, ctaUrl), greeting: $"Hoi {contactName},", showMascot: true), baseUrl);
    }

    public static ComposedEmail TakeoverRejected(string? baseUrl, string contactName, string companyName)
    {
        var brand = Brand(baseUrl);
        return Finish(Doc("TakeoverRejected", "Overname afgewezen — Lobsy", "Overname afgewezen", "Overname afgewezen",
            [
                P(Fmt("Je overnameverzoek voor {0} is afgewezen.", EmailArg.Bold(companyName)))
            ],
            Button("Neem contact op", $"mailto:{brand.SupportAddress}"),
            greeting: $"Hoi {contactName},"), baseUrl);
    }
}
