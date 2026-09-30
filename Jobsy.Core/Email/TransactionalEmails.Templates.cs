using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail MailTest(string? baseUrl)
    {
        var links = Links(baseUrl);
        return Finish(Doc("MailTest", "Lobsy testmail", "Dit is een testmail van Lobsy.", "Testmail",
            [P("Dit is een testmail van Lobsy."), P("Als je dit bericht ziet, werkt de uitgaande mailconfiguratie.")],
            Button("Naar e-mails", links.AdminEmails)), baseUrl);
    }

    public static ComposedEmail ApplicationConfirmation(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName, bool authenticatorStubUsed)
    {
        var links = Links(baseUrl);
        var subject = $"Sollicitatie bevestigd: {vacancyTitle}";
        var blocks = new List<EmailBlock>
        {
            P(Fmt("Je sollicitatie op {0} bij {1} is ontvangen. Top!", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName))),
            P("Je kunt de status volgen onder Mijn sollicitaties.")
        };
        if (authenticatorStubUsed)
        {
            blocks.Add(N("Authenticator stub: verificatie gesimuleerd."));
        }

        return Finish(Doc("ApplicationConfirmation", subject, "Je sollicitatie is ontvangen.", "Sollicitatie verstuurd!",
            blocks, Button("Bekijk mijn sollicitaties", links.CandidateApplications),
            greeting: $"Hoi {candidateName},"), baseUrl);
    }

    public static ComposedEmail ApplicationVerificationCode(
        string? baseUrl, string candidateName, string vacancyTitle, Guid vacancyId, string code)
    {
        var subject = $"Verificatiecode voor sollicitatie: {vacancyTitle}";
        return Finish(Doc("ApplicationVerificationCode", subject, "Je Lobsy-verificatiecode", "Je verificatiecode",
            [
                P("Gebruik deze 6-cijferige code om je sollicitatie af te ronden:"),
                C(code, "De code is 10 minuten geldig.")
            ],
            greeting: $"Hoi {candidateName},"), baseUrl);
    }

    public static ComposedEmail EmployerReactionAccepted(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName)
    {
        var links = Links(baseUrl);
        var subject = $"Je sollicitatie is geaccepteerd: {vacancyTitle}";
        return Finish(Doc("EmployerReactionAccepted", subject, "Je sollicitatie is geaccepteerd.", "Goed nieuws!",
            [
                P(Fmt("Het bedrijf heeft je sollicitatie voor {0} bij {1} geaccepteerd.", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName))),
                P("Wellicht nemen ze binnenkort contact met je op. Houd je telefoon en mail in de gaten.")
            ],
            Button("Bekijk mijn sollicitaties", links.CandidateApplications),
            greeting: $"Hoi {candidateName},", showMascot: true), baseUrl);
    }

    public static ComposedEmail EmployerReactionRejected(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName)
    {
        var links = Links(baseUrl);
        var subject = $"Update op je sollicitatie: {vacancyTitle}";
        return Finish(Doc("EmployerReactionRejected", subject, "Update op je sollicitatie.", "Update op je sollicitatie",
            [
                P(Fmt("Bedankt voor je interesse in {0} bij {1}.", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName))),
                P("Helaas is de keuze dit keer niet op jou gevallen. We wensen je veel succes met je verdere zoektocht!")
            ],
            Button("Bekijk andere vacatures", links.Map),
            greeting: $"Hoi {candidateName},"), baseUrl);
    }

    public static ComposedEmail EmployerContacting(string? baseUrl, string vacancyTitle)
    {
        var links = Links(baseUrl);
        var subject = $"Werkgever neemt contact op: {vacancyTitle}";
        return Finish(Doc("EmployerContacting", subject, "De werkgever neemt contact op.", "De werkgever neemt contact op",
            [
                P(Fmt("Goed nieuws! De werkgever van {0} neemt contact met je op.", EmailArg.Bold(vacancyTitle))),
                P("Houd je telefoon, mail of WhatsApp in de gaten.")
            ],
            Button("Bekijk mijn sollicitaties", links.CandidateApplications),
            showMascot: true), baseUrl);
    }

    public static ComposedEmail ApplicationHired(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName,
        Guid hiredApplicationId, string? withdrawAbsoluteUrl = null)
    {
        var links = Links(baseUrl);
        var subject = $"Gefeliciteerd! Je bent aangenomen voor {vacancyTitle}";
        var blocks = new List<EmailBlock>
        {
            P(EmailText.Join(Bold("Wat een feest! "), Fmt("Je bent aangenomen voor {0} bij {1}.", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName)))),
            P("Heel veel succes — en geniet van deze stap.")
        };
        EmailCta cta;
        if (!string.IsNullOrWhiteSpace(withdrawAbsoluteUrl))
        {
            cta = Button("Andere sollicitaties netjes intrekken", withdrawAbsoluteUrl!);
            blocks.Add(N(
                "Heb je nog andere sollicitaties lopen? Trek ze in, zodat die werkgevers weten dat je al bent voorzien.",
                new EmailLink("Bekijk mijn sollicitaties", links.CandidateApplications)));
        }
        else
        {
            cta = Button("Bekijk mijn sollicitaties", links.CandidateApplications);
        }

        return Finish(Doc("ApplicationHired", subject, "Je bent aangenomen — gefeliciteerd!", "Gefeliciteerd — je bent aangenomen!",
            blocks, cta, greeting: $"Hoi {candidateName},", showMascot: true), baseUrl);
    }

    public static ComposedEmail ApplicationFilledElsewhere(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName)
    {
        var links = Links(baseUrl);
        var subject = $"Update sollicitatie: {vacancyTitle}";
        return Finish(Doc("ApplicationFilledElsewhere", subject, "Update op je sollicitatie.", "Update op je sollicitatie",
            [
                P(Fmt("Bedankt voor je sollicitatie op {0} bij {1}.", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName))),
                P("Helaas is de keuze op een andere kandidaat gevallen. We wensen je veel succes!")
            ],
            Button("Bekijk andere vacatures", links.Map),
            greeting: $"Hoi {candidateName},"), baseUrl);
    }

    public static ComposedEmail EmployerNewApplication(string? baseUrl, string vacancyTitle)
    {
        var links = Links(baseUrl);
        var subject = $"Nieuwe sollicitatie: {vacancyTitle}";
        return Finish(Doc("EmployerNewApplication", subject, "Er is een nieuwe sollicitatie.", "Nieuwe sollicitatie",
            [
                P(Fmt("Er is een nieuwe sollicitatie ontvangen voor {0}.", EmailArg.Bold(vacancyTitle))),
                P("Log in op Lobsy om de kandidaat te bekijken en te reageren.")
            ],
            Button("Bekijk sollicitaties", links.EmployerApplications())), baseUrl);
    }

    public static ComposedEmail CandidateWithdrawn(string? baseUrl, string vacancyTitle)
    {
        var links = Links(baseUrl);
        var subject = $"Sollicitatie ingetrokken: {vacancyTitle}";
        return Finish(Doc("CandidateWithdrawn", subject, "Een sollicitatie is ingetrokken.", "Sollicitatie ingetrokken",
            [P(Fmt("Een kandidaat heeft de sollicitatie op {0} ingetrokken.", EmailArg.Bold(vacancyTitle)))],
            Button("Open sollicitaties", links.EmployerApplications())), baseUrl);
    }

    public static ComposedEmail CandidateWithdrawnOtherJob(string? baseUrl, string vacancyTitle)
    {
        var links = Links(baseUrl);
        var subject = $"Sollicitatie ingetrokken: {vacancyTitle}";
        return Finish(Doc("CandidateWithdrawnOtherJob", subject, "Een sollicitatie is ingetrokken.", "Sollicitatie ingetrokken",
            [
                P(Fmt("Goed om te weten: de kandidaat heeft de sollicitatie op {0} ingetrokken.", EmailArg.Bold(vacancyTitle))),
                P("Reden: de kandidaat heeft inmiddels een andere baan gevonden.")
            ],
            Button("Open sollicitaties", links.EmployerApplications()),
            greeting: "Hoi,"), baseUrl);
    }

    public static ComposedEmail PushBom(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName, Guid vacancyId,
        string? locationLabel, double distanceKm, int travelMinutes, decimal? hourlyWage, string wageNote,
        string? setUnavailableAbsoluteUrl = null)
    {
        var links = Links(baseUrl);
        var facts = new List<(string, string)> { ("Functie", vacancyTitle), ("Bedrijf", companyName) };
        if (!string.IsNullOrWhiteSpace(locationLabel))
        {
            facts.Add(("Locatie", locationLabel!));
        }

        facts.Add(("Afstand", EmailFormat.FormatKm(distanceKm)));
        facts.Add(("Reistijd", $"{travelMinutes} min"));
        if (hourlyWage is decimal w && !string.IsNullOrWhiteSpace(wageNote))
        {
            facts.Add((wageNote, EmailFormat.FormatEuro(w)));
        }

        var subject = $"Nieuwe vacature bij jou in de buurt: {vacancyTitle}";
        var setUnavailable = string.IsNullOrWhiteSpace(setUnavailableAbsoluteUrl)
            ? links.SetUnavailable
            : setUnavailableAbsoluteUrl!;
        return Finish(Doc("PushBom", subject,
            $"{vacancyTitle} bij {companyName} — {EmailFormat.FormatKm(distanceKm)} van jou",
            "Iets moois bij jou in de buurt",
            [
                P("Er staat een passende vacature open — op fiets- of reistijd-afstand van jou."),
                F(facts),
                N("Niet meer op zoek naar werk?", new EmailLink("Zet je status op Niet beschikbaar", setUnavailable))
            ],
            Button("Klik hier", links.Vacancy(vacancyId)),
            greeting: $"Hoi {candidateName},"), baseUrl);
    }

    public static ComposedEmail PendingApproval(string? baseUrl, string vacancyTitle, string companyName)
    {
        var links = Links(baseUrl);
        var subject = $"Publicatieaanvraag: {vacancyTitle}";
        return Finish(Doc("PendingApproval", subject, "Er wacht een publicatieaanvraag.", "Publicatieaanvraag",
            [
                P(Fmt("Vacature {0} bij {1} wacht op goedkeuring (onvoldoende tokens).", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName))),
                P("Log in op Lobsy om de aanvraag te beoordelen onder Vacatures.")
            ],
            Button("Beoordeel de aanvraag", links.EmployerTokens)), baseUrl);
    }

    public static ComposedEmail VacancyEngagementReminder(
        string? baseUrl, string vacancyTitle, Guid vacancyId, int impressions, int views, int shares,
        int saved, int applications, string tip, string? companyName = null)
    {
        var links = Links(baseUrl);
        var subject = $"Even checken: {vacancyTitle} staat {VacancyEngagementReminderRules.OpenDaysBeforeReminder} dagen open";
        var companyBit = string.IsNullOrWhiteSpace(companyName) ? "" : $" bij {companyName}";
        return Finish(Doc("VacancyEngagementReminder", subject,
            $"{vacancyTitle}: {impressions} zoek · {views} bekeken · {applications} sollicitaties",
            "Even checken — je vacature staat 14 dagen open",
            [
                P(Fmt("Je vacature {0}{1} staat al {2} dagen open. Tijd voor een korte check-in.",
                    EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyBit),
                    EmailArg.Plain(VacancyEngagementReminderRules.OpenDaysBeforeReminder.ToString()))),
                P(Bold("Dit zien we tot nu toe:")),
                F([
                    ("In zoekresultaten", impressions.ToString()),
                    ("Bekeken", views.ToString()),
                    ("Gedeeld", shares.ToString()),
                    ("Bewaard", saved.ToString()),
                    ("Sollicitaties", applications.ToString())
                ]),
                P(EmailText.Join(Bold("Tip van Lobsy: "), Plain(tip))),
                P($"Pas de vacature aan vóór de einddatum. Bij een update verlengen we de deadline als goodwill met {VacancyEngagementReminderRules.GoodwillExtendDays} dagen — zo geef je je tekst nog even de ruimte."),
                N("Highlight en PushBom openen je vacatureoverzicht, waar je de actie met één klik kunt afronden (tokens vereist).",
                    new EmailLink("Highlight deze vacature", links.EmployerVacancyBoostHighlight(vacancyId)))
            ],
            Button("Vacature nu verbeteren", links.EmployerVacancyEdit(vacancyId)),
            greeting: "Hoi,"), baseUrl);
    }

    public static ComposedEmail DraftVacancyCleanupWarning(
        string? baseUrl, string vacancyTitle, string companyName, Guid vacancyId, DateTime deleteOnUtc)
    {
        var links = Links(baseUrl);
        var deleteLabel = deleteOnUtc.ToString("dd-MM-yyyy");
        var subject = $"Concept-vacature '{vacancyTitle}' wordt over 14 dagen verwijderd";
        return Finish(Doc("DraftVacancyCleanupWarning", subject,
            $"Concept '{vacancyTitle}' wordt over 14 dagen verwijderd",
            "Concept wordt binnenkort opgeruimd",
            [
                P(Fmt("Je concept-vacature {0} voor {1} staat al {2} dagen als concept en is nog nooit gepubliceerd.",
                    EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName),
                    EmailArg.Plain(DraftVacancyCleanupRules.WarningAfterDays.ToString()))),
                P(Fmt("Als je niets doet, ruimt Lobsy dit concept automatisch op op {0} (14 dagen vanaf deze mail).", EmailArg.Bold(deleteLabel))),
                P("Vacatures die je wél hebt gepubliceerd blijven altijd bewaard — ook na de deadline."),
                N("Log in op Lobsy → Vacatures om dit concept te publiceren of te verwijderen.")
            ],
            Button("Open dit concept", links.EmployerVacancyEdit(vacancyId)),
            greeting: "Hallo,"), baseUrl);
    }

    public static ComposedEmail CompanyReEngagement(string? baseUrl, string companyName)
    {
        var links = Links(baseUrl);
        return Finish(Doc("CompanyReEngagement", "We missen je bij Lobsy", "Je tools staan nog klaar op Lobsy.", "We missen je",
            [
                P(Fmt("Hallo team {0},", EmailArg.Bold(companyName))),
                P("Het is al een tijdje stil op Lobsy — geen actieve vacatures, geen inlog, geen API-call en geen CSV-upload."),
                P("Goed nieuws: jullie tools staan nog klaar:"),
                F([
                    ("CSV Batch Import", "Veel vacatures in één keer als concept"),
                    ("Externe API", "Koppel je ATS met een API-key"),
                    ("Publiceren", "Tokens pas bij publicatie in Lobsy")
                ]),
                N("Log in op Lobsy wanneer je weer wilt starten.")
            ],
            Button("Inloggen op Lobsy", links.EmployerHome)), baseUrl);
    }
}
