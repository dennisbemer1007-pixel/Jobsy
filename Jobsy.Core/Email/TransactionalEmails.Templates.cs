using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail MailTest(string? baseUrl, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("MailTest", S(c, "Email.MailTest.Subject"), S(c, "Email.MailTest.Preheader"), S(c, "Email.MailTest.Heading"),
            [P(S(c, "Email.MailTest.P1")), P(S(c, "Email.MailTest.P2"))],
            Button(S(c, "Email.MailTest.Cta"), links.AdminEmails), culture: c), baseUrl);
    }

    public static ComposedEmail ApplicationConfirmation(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName,
        EmailCulture? culture = null, string? placeLabel = null, DateTime? sentAtUtc = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var subject = Sf(c, "Email.ApplicationConfirmation.Subject", EmailBidi.Isolate(c, vacancyTitle));
        var facts = new List<(string, string)>
        {
            (S(c, "Email.Common.Fact.Vacancy"), vacancyTitle),
            (S(c, "Email.Common.Fact.Company"), companyName)
        };
        if (!string.IsNullOrWhiteSpace(placeLabel))
        {
            facts.Add((S(c, "Email.Common.Fact.Place"), placeLabel!));
        }

        facts.Add((S(c, "Email.Common.Fact.SentOn"), EmailFormat.Date(sentAtUtc ?? DateTime.UtcNow, c)));
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.ApplicationConfirmation.P1", EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName))),
            F(facts),
            new StepsBlock(S(c, "Email.ApplicationConfirmation.StepsTitle"),
            [
                T(c, "Email.ApplicationConfirmation.Step1", EmailArg.Plain(companyName)),
                EmailText.Plain(S(c, "Email.ApplicationConfirmation.Step2")),
                EmailText.Plain(S(c, "Email.ApplicationConfirmation.Step3"))
            ])
        };
        return Finish(Doc("ApplicationConfirmation", subject,
            Sf(c, "Email.ApplicationConfirmation.Preheader", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.ApplicationConfirmation.Heading"),
            blocks, Button(S(c, "Email.ApplicationConfirmation.Cta"), links.CandidateApplications),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.ApplicationConfirmation.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }

    public static ComposedEmail ApplicationVerificationCode(
        string? baseUrl, string candidateName, string vacancyTitle, Guid vacancyId, string code,
        EmailCulture? culture = null, bool codeInSubject = true)
    {
        var c = culture ?? EmailCulture.Nl;
        var minutes = ApplicationRules.EmailVerificationCodeMinutes;
        var subject = codeInSubject
            ? Sf(c, "Email.ApplicationVerificationCode.SubjectWithCode", code)
            : S(c, "Email.ApplicationVerificationCode.Subject");
        return Finish(Doc("ApplicationVerificationCode", subject, S(c, "Email.ApplicationVerificationCode.Preheader"),
            S(c, "Email.ApplicationVerificationCode.Heading"),
            [
                P(T(c, "Email.ApplicationVerificationCode.P1", EmailArg.Bold(vacancyTitle))),
                C(code, Sf(c, "Email.Common.CodeWorksMinutes", minutes)),
                N(S(c, "Email.ApplicationVerificationCode.Note"))
            ],
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.ApplicationVerificationCode.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail EmployerReactionAccepted(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("EmployerReactionAccepted",
            Sf(c, "Email.EmployerReactionAccepted.Subject", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.EmployerReactionAccepted.Preheader", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.EmployerReactionAccepted.Heading", EmailBidi.Isolate(c, companyName)),
            [
                P(T(c, "Email.EmployerReactionAccepted.P1", EmailArg.Plain(companyName), EmailArg.Bold(vacancyTitle)))
            ],
            Button(S(c, "Email.EmployerReactionAccepted.Cta"), links.CandidateApplications),
            greeting: GreetCandidate(c, candidateName), showMascot: true,
            eyebrow: new EmailEyebrow(S(c, "Email.EmployerReactionAccepted.Eyebrow"), EmailTone.Mint),
            culture: c), baseUrl);
    }

    public static ComposedEmail EmployerReactionRejected(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("EmployerReactionRejected",
            Sf(c, "Email.EmployerReactionRejected.Subject", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.EmployerReactionRejected.Preheader"),
            S(c, "Email.EmployerReactionRejected.Heading"),
            [
                P(T(c, "Email.EmployerReactionRejected.P1", EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.EmployerReactionRejected.Cta"), links.Map),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.EmployerReactionRejected.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }

    public static ComposedEmail EmployerContacting(
        string? baseUrl, string vacancyTitle, string? candidateName = null, string? companyName = null,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var company = companyName ?? "";
        return Finish(Doc("EmployerContacting",
            Sf(c, "Email.EmployerContacting.Subject", EmailBidi.Isolate(c, company)),
            S(c, "Email.EmployerContacting.Preheader"),
            Sf(c, "Email.EmployerContacting.Heading", EmailBidi.Isolate(c, company)),
            [
                P(T(c, "Email.EmployerContacting.P1", EmailArg.Plain(company), EmailArg.Bold(vacancyTitle)))
            ],
            Button(S(c, "Email.EmployerContacting.Cta"), links.CandidateApplications),
            greeting: GreetCandidate(c, candidateName), showMascot: true,
            eyebrow: new EmailEyebrow(S(c, "Email.EmployerContacting.Eyebrow"), EmailTone.Mint),
            culture: c), baseUrl);
    }

    public static ComposedEmail ApplicationHired(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName,
        Guid hiredApplicationId, string? withdrawAbsoluteUrl = null, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.ApplicationHired.P1", EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName)))
        };
        EmailCta cta;
        if (!string.IsNullOrWhiteSpace(withdrawAbsoluteUrl))
        {
            cta = Button(S(c, "Email.ApplicationHired.CtaWithdraw"), withdrawAbsoluteUrl!);
            blocks.Add(N(
                S(c, "Email.ApplicationHired.NoteWithdraw"),
                new EmailLink(S(c, "Email.ApplicationHired.NoteLink"), links.CandidateApplications)));
        }
        else
        {
            cta = Button(S(c, "Email.ApplicationHired.Cta"), links.CandidateApplications);
        }

        return Finish(Doc("ApplicationHired",
            Sf(c, "Email.ApplicationHired.Subject", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.ApplicationHired.Preheader", EmailBidi.Isolate(c, vacancyTitle)),
            S(c, "Email.ApplicationHired.Heading"),
            blocks, cta, greeting: GreetCandidate(c, candidateName), showMascot: true,
            eyebrow: new EmailEyebrow(S(c, "Email.ApplicationHired.Eyebrow"), EmailTone.Mint),
            culture: c), baseUrl);
    }

    public static ComposedEmail ApplicationFilledElsewhere(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("ApplicationFilledElsewhere",
            Sf(c, "Email.ApplicationFilledElsewhere.Subject", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.ApplicationFilledElsewhere.Preheader"),
            S(c, "Email.ApplicationFilledElsewhere.Heading"),
            [
                P(T(c, "Email.ApplicationFilledElsewhere.P1", EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.ApplicationFilledElsewhere.Cta"), links.Map),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.ApplicationFilledElsewhere.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }

    public static ComposedEmail EmployerNewApplication(string? baseUrl, string vacancyTitle, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var subject = Sf(c, "Email.EmployerNewApplication.Subject", EmailBidi.Isolate(c, vacancyTitle));
        return Finish(Doc("EmployerNewApplication", subject, S(c, "Email.EmployerNewApplication.Preheader"),
            S(c, "Email.EmployerNewApplication.Heading"),
            [
                P(T(c, "Email.EmployerNewApplication.P1", EmailArg.Bold(vacancyTitle))),
                P(S(c, "Email.EmployerNewApplication.P2"))
            ],
            Button(S(c, "Email.EmployerNewApplication.Cta"), links.EmployerApplications()), culture: c), baseUrl);
    }

    public static ComposedEmail CandidateWithdrawn(string? baseUrl, string vacancyTitle, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var subject = Sf(c, "Email.CandidateWithdrawn.Subject", EmailBidi.Isolate(c, vacancyTitle));
        return Finish(Doc("CandidateWithdrawn", subject, S(c, "Email.CandidateWithdrawn.Preheader"),
            S(c, "Email.CandidateWithdrawn.Heading"),
            [P(T(c, "Email.CandidateWithdrawn.P1", EmailArg.Bold(vacancyTitle)))],
            Button(S(c, "Email.CandidateWithdrawn.Cta"), links.EmployerApplications()), culture: c), baseUrl);
    }

    public static ComposedEmail CandidateWithdrawnOtherJob(string? baseUrl, string vacancyTitle, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var subject = Sf(c, "Email.CandidateWithdrawnOtherJob.Subject", EmailBidi.Isolate(c, vacancyTitle));
        return Finish(Doc("CandidateWithdrawnOtherJob", subject, S(c, "Email.CandidateWithdrawnOtherJob.Preheader"),
            S(c, "Email.CandidateWithdrawnOtherJob.Heading"),
            [
                P(T(c, "Email.CandidateWithdrawnOtherJob.P1", EmailArg.Bold(vacancyTitle))),
                P(S(c, "Email.CandidateWithdrawnOtherJob.P2"))
            ],
            Button(S(c, "Email.CandidateWithdrawnOtherJob.Cta"), links.EmployerApplications()),
            greeting: GreetOther(c, null), culture: c), baseUrl);
    }

    public static ComposedEmail PushBom(
        string? baseUrl, string candidateName, string vacancyTitle, string companyName, Guid vacancyId,
        string? locationLabel, double distanceKm, int travelMinutes, decimal? hourlyWage, string wageNote,
        string? setUnavailableAbsoluteUrl = null, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var facts = new List<(string, string)>
        {
            (S(c, "Email.Common.Fact.Vacancy"), vacancyTitle),
            (S(c, "Email.Common.Fact.Company"), companyName)
        };
        if (!string.IsNullOrWhiteSpace(locationLabel))
        {
            facts.Add((S(c, "Email.Common.Fact.Place"), locationLabel!));
        }

        facts.Add((S(c, "Email.Common.Fact.Distance"), EmailFormat.Km(distanceKm, c)));
        facts.Add((S(c, "Email.Common.Fact.TravelTime"), Sf(c, "Email.Common.TravelMinutesShort", travelMinutes)));
        if (hourlyWage is decimal w)
        {
            var label = string.IsNullOrWhiteSpace(wageNote) ? S(c, "Email.PushBom.WageNote") : wageNote;
            facts.Add((label, EmailFormat.Money(w, c)));
        }

        var subject = Sf(c, "Email.PushBom.Subject", EmailBidi.Isolate(c, vacancyTitle));
        var setUnavailable = string.IsNullOrWhiteSpace(setUnavailableAbsoluteUrl)
            ? links.SetUnavailable
            : setUnavailableAbsoluteUrl!;
        var preheader = Sf(c, "Email.PushBom.Preheader",
            EmailBidi.Isolate(c, companyName), EmailFormat.Km(distanceKm, c));
        return Finish(Doc("PushBom", subject, preheader, S(c, "Email.PushBom.Heading"),
            [
                P(S(c, "Email.PushBom.P1")),
                F(facts),
                N(S(c, "Email.PushBom.Note"), new EmailLink(S(c, "Email.PushBom.NoteLink"), setUnavailable))
            ],
            Button(S(c, "Email.PushBom.Cta"), links.Vacancy(vacancyId)),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.PushBom.Eyebrow"), EmailTone.Sun),
            culture: c), baseUrl);
    }

    public static ComposedEmail PendingApproval(string? baseUrl, string vacancyTitle, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var subject = Sf(c, "Email.PendingApproval.Subject", EmailBidi.Isolate(c, vacancyTitle));
        return Finish(Doc("PendingApproval", subject, S(c, "Email.PendingApproval.Preheader"),
            S(c, "Email.PendingApproval.Heading"),
            [
                P(T(c, "Email.PendingApproval.P1", EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyName))),
                P(S(c, "Email.PendingApproval.P2"))
            ],
            Button(S(c, "Email.PendingApproval.Cta"), links.EmployerTokens), culture: c), baseUrl);
    }

    public static ComposedEmail VacancyEngagementReminder(
        string? baseUrl, string vacancyTitle, Guid vacancyId, int impressions, int views, int shares,
        int saved, int applications, string tip, string? companyName = null, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var subject = Sf(c, "Email.VacancyEngagementReminder.Subject",
            EmailBidi.Isolate(c, vacancyTitle), VacancyEngagementReminderRules.OpenDaysBeforeReminder);
        var companyBit = string.IsNullOrWhiteSpace(companyName)
            ? ""
            : Sf(c, "Email.VacancyEngagementReminder.CompanyBit", EmailBidi.Isolate(c, companyName));
        var preheader = Sf(c, "Email.VacancyEngagementReminder.Preheader",
            EmailBidi.Isolate(c, vacancyTitle), impressions, views, applications);
        return Finish(Doc("VacancyEngagementReminder", subject, preheader,
            Sf(c, "Email.VacancyEngagementReminder.Heading", VacancyEngagementReminderRules.OpenDaysBeforeReminder),
            [
                P(T(c, "Email.VacancyEngagementReminder.P1",
                    EmailArg.Bold(vacancyTitle), EmailArg.Plain(companyBit),
                    EmailArg.Plain(VacancyEngagementReminderRules.OpenDaysBeforeReminder.ToString(), isolate: false))),
                P(Bold(S(c, "Email.VacancyEngagementReminder.StatsLead"))),
                F([
                    (S(c, "Email.VacancyEngagementReminder.Fact.Impressions"), impressions.ToString()),
                    (S(c, "Email.VacancyEngagementReminder.Fact.Views"), views.ToString()),
                    (S(c, "Email.VacancyEngagementReminder.Fact.Shares"), shares.ToString()),
                    (S(c, "Email.VacancyEngagementReminder.Fact.Saved"), saved.ToString()),
                    (S(c, "Email.VacancyEngagementReminder.Fact.Applications"), applications.ToString())
                ]),
                P(EmailText.Join(Bold(S(c, "Email.VacancyEngagementReminder.TipLead")), Plain(tip))),
                P(Sf(c, "Email.VacancyEngagementReminder.P3", VacancyEngagementReminderRules.GoodwillExtendDays)),
                N(S(c, "Email.VacancyEngagementReminder.Note"),
                    new EmailLink(S(c, "Email.VacancyEngagementReminder.NoteLink"), links.EmployerVacancyBoostHighlight(vacancyId)))
            ],
            Button(S(c, "Email.VacancyEngagementReminder.Cta"), links.EmployerVacancyEdit(vacancyId)),
            greeting: GreetOther(c, null), culture: c), baseUrl);
    }

    public static ComposedEmail DraftVacancyCleanupWarning(
        string? baseUrl, string vacancyTitle, string companyName, Guid vacancyId, DateTime deleteOnUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var deleteLabel = EmailFormat.Date(deleteOnUtc, c);
        var subject = Sf(c, "Email.DraftVacancyCleanupWarning.Subject", EmailBidi.Isolate(c, vacancyTitle), 14);
        return Finish(Doc("DraftVacancyCleanupWarning", subject,
            Sf(c, "Email.DraftVacancyCleanupWarning.Preheader", EmailBidi.Isolate(c, vacancyTitle), 14),
            S(c, "Email.DraftVacancyCleanupWarning.Heading"),
            [
                P(T(c, "Email.DraftVacancyCleanupWarning.P1",
                    EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName),
                    EmailArg.Plain(DraftVacancyCleanupRules.WarningAfterDays.ToString(), isolate: false))),
                P(T(c, "Email.DraftVacancyCleanupWarning.P2", EmailArg.Bold(deleteLabel), EmailArg.Plain("14", isolate: false))),
                P(S(c, "Email.DraftVacancyCleanupWarning.P3")),
                N(S(c, "Email.DraftVacancyCleanupWarning.Note"))
            ],
            Button(S(c, "Email.DraftVacancyCleanupWarning.Cta"), links.EmployerVacancyEdit(vacancyId)),
            greeting: GreetOther(c, null), culture: c), baseUrl);
    }

    public static ComposedEmail CompanyReEngagement(string? baseUrl, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("CompanyReEngagement", S(c, "Email.CompanyReEngagement.Subject"),
            S(c, "Email.CompanyReEngagement.Preheader"), S(c, "Email.CompanyReEngagement.Heading"),
            [
                P(T(c, "Email.CompanyReEngagement.P1", EmailArg.Bold(companyName))),
                P(S(c, "Email.CompanyReEngagement.P2")),
                P(S(c, "Email.CompanyReEngagement.P3")),
                F([
                    (S(c, "Email.CompanyReEngagement.Fact.Csv"), S(c, "Email.CompanyReEngagement.Fact.CsvVal")),
                    (S(c, "Email.CompanyReEngagement.Fact.Api"), S(c, "Email.CompanyReEngagement.Fact.ApiVal")),
                    (S(c, "Email.CompanyReEngagement.Fact.Publish"), S(c, "Email.CompanyReEngagement.Fact.PublishVal"))
                ]),
                N(S(c, "Email.CompanyReEngagement.Note"))
            ],
            Button(S(c, "Email.CompanyReEngagement.Cta"), links.EmployerHome), culture: c), baseUrl);
    }
}
