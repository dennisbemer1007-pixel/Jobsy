using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail RegistrationActivation(
        string? baseUrl, string contactName, string establishmentName, string code,
        EmailCulture? culture = null, bool codeInSubject = true)
    {
        var c = culture ?? EmailCulture.Nl;
        var minutes = PrivacyConstants.UnconfirmedRegistrationRetentionMinutes;
        var subject = codeInSubject
            ? Sf(c, "Email.RegistrationActivation.SubjectWithCode", code)
            : S(c, "Email.RegistrationActivation.Subject");
        return Finish(Doc("RegistrationActivation", subject,
            S(c, "Email.RegistrationActivation.Preheader"), S(c, "Email.RegistrationActivation.Heading"),
            [
                P(T(c, "Email.RegistrationActivation.P1", EmailArg.Bold(establishmentName))),
                C(code, Sf(c, "Email.Common.CodeWorksMinutes", minutes)),
                N(S(c, "Email.RegistrationActivation.Note"))
            ],
            greeting: GreetOther(c, contactName),
            eyebrow: new EmailEyebrow(S(c, "Email.RegistrationActivation.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: S(c, "Email.Reason.Registering")), baseUrl);
    }

    public static ComposedEmail RegistrationCredentials(
        string? baseUrl, string contactName, string establishmentName, string contactEmail, string? setPasswordUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.EmployerHome;
        var ctaLabel = setPasswordUrl is null
            ? S(c, "Email.RegistrationCredentials.CtaDashboard")
            : S(c, "Email.Common.Cta.SetPassword");
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.RegistrationCredentials.P1", EmailArg.Bold(establishmentName))),
            F([
                (S(c, "Email.Common.Fact.Company"), establishmentName),
                (S(c, "Email.Common.Fact.YourEmail"), contactEmail)
            ]),
            P(S(c, "Email.RegistrationCredentials.P2Oauth"))
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
        }

        return Finish(Doc("RegistrationCredentials", S(c, "Email.RegistrationCredentials.Subject"),
            S(c, "Email.RegistrationCredentials.Preheader"), S(c, "Email.RegistrationCredentials.Heading"),
            blocks, Button(ctaLabel, ctaUrl),
            greeting: GreetOther(c, contactName),
            eyebrow: new EmailEyebrow(S(c, "Email.RegistrationCredentials.Eyebrow"), EmailTone.Mint),
            showMascot: true, culture: c,
            reasonText: S(c, "Email.Reason.Registered")), baseUrl);
    }

    public static ComposedEmail CompanyVerificationReminder(
        string? baseUrl, string contactName, string companyName, int day, string? deletionDateLabel,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var dayBit = day == 21
            ? S(c, "Email.CompanyVerificationReminder.P1Day21")
            : S(c, "Email.CompanyVerificationReminder.P1Day7");
        var blocks = new List<EmailBlock>
        {
            P(dayBit),
            P(T(c, "Email.CompanyVerificationReminder.P2", EmailArg.Bold(companyName)))
        };
        if (!string.IsNullOrWhiteSpace(deletionDateLabel))
        {
            blocks.Add(P(T(c, "Email.CompanyVerificationReminder.P3", EmailArg.Bold(deletionDateLabel!), EmailArg.Plain("60", isolate: false))));
        }

        var subject = day == 21
            ? S(c, "Email.CompanyVerificationReminder.Subject21")
            : S(c, "Email.CompanyVerificationReminder.Subject7");
        return Finish(Doc("CompanyVerificationReminder", subject, S(c, "Email.CompanyVerificationReminder.Preheader"),
            S(c, "Email.CompanyVerificationReminder.Heading"),
            blocks, Button(S(c, "Email.CompanyVerificationReminder.Cta"), links.RegisterVerify),
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail CompanyVerified(
        string? baseUrl, string contactName, string companyName, bool welcomeTokenGranted, IReadOnlyList<string> publishedTitles,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.CompanyVerified.P1", EmailArg.Bold(companyName)))
        };
        if (publishedTitles.Count == 0)
        {
            blocks.Add(P(S(c, "Email.CompanyVerified.P2Empty")));
        }
        else
        {
            blocks.Add(P(S(c, "Email.CompanyVerified.P2Published")));
            blocks.Add(F(publishedTitles.Select((t, i) => (Sf(c, "Email.CompanyVerified.VacancyN", i + 1), t))));
        }

        blocks.Add(welcomeTokenGranted
            ? P(S(c, "Email.CompanyVerified.WelcomeToken"))
            : P(S(c, "Email.CompanyVerified.NoWelcomeToken")));

        return Finish(Doc("CompanyVerified", S(c, "Email.CompanyVerified.Subject"),
            S(c, "Email.CompanyVerified.Preheader"), S(c, "Email.CompanyVerified.Heading"),
            blocks, Button(S(c, "Email.CompanyVerified.Cta"), links.EmployerVacancies),
            greeting: GreetOther(c, contactName), showMascot: true, culture: c), baseUrl);
    }

    public static ComposedEmail CompanyBusinessEmailVerification(
        string companyName, string code, string? baseUrl, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        return Finish(Doc("CompanyBusinessEmailVerification", S(c, "Email.CompanyBusinessEmailVerification.Subject"),
            S(c, "Email.CompanyBusinessEmailVerification.Preheader"), S(c, "Email.CompanyBusinessEmailVerification.Heading"),
            [
                P(T(c, "Email.CompanyBusinessEmailVerification.P1", EmailArg.Bold(companyName))),
                C(code, Sf(c, "Email.Common.CodeValid10", ApplicationRules.EmailVerificationCodeMinutes))
            ], culture: c), baseUrl);
    }

    public static ComposedEmail CompanyVerificationRejected(
        string? baseUrl, string contactName, string companyName, string reason, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("CompanyVerificationRejected", S(c, "Email.CompanyVerificationRejected.Subject"),
            S(c, "Email.CompanyVerificationRejected.Preheader"), S(c, "Email.CompanyVerificationRejected.Heading"),
            [
                P(T(c, "Email.CompanyVerificationRejected.P1", EmailArg.Bold(companyName))),
                P(Sf(c, "Email.Common.ReasonLabel", EmailBidi.Isolate(c, reason))),
                P(S(c, "Email.CompanyVerificationRejected.P3"))
            ],
            Button(S(c, "Email.CompanyVerificationRejected.Cta"), links.RegisterVerify),
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail EngagementClaimRemoved(
        string? baseUrl, string companyName, string itemLabel, string reason, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("EngagementClaimRemoved", S(c, "Email.EngagementClaimRemoved.Subject"),
            S(c, "Email.EngagementClaimRemoved.Preheader"), S(c, "Email.EngagementClaimRemoved.Heading"),
            [
                P(T(c, "Email.EngagementClaimRemoved.P1", EmailArg.Bold(itemLabel), EmailArg.Bold(companyName))),
                P(Sf(c, "Email.Common.ReasonLabel", EmailBidi.Isolate(c, reason))),
                P(Sf(c, "Email.EngagementClaimRemoved.P3", 30))
            ],
            Button(S(c, "Email.EngagementClaimRemoved.Cta"), links.EmployerHome), culture: c), baseUrl);
    }

    public static ComposedEmail CompanyUnverifiedDeleted(
        string? baseUrl, string contactName, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("CompanyUnverifiedDeleted", S(c, "Email.CompanyUnverifiedDeleted.Subject"),
            S(c, "Email.CompanyUnverifiedDeleted.Preheader"), S(c, "Email.CompanyUnverifiedDeleted.Heading"),
            [
                P(T(c, "Email.CompanyUnverifiedDeleted.P1", EmailArg.Bold(companyName), EmailArg.Plain("60", isolate: false)))
            ],
            Button(S(c, "Email.CompanyUnverifiedDeleted.Cta"), links.Register),
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail TakeoverEmailVerification(
        string? baseUrl, string contactName, string companyName, string code,
        EmailCulture? culture = null, bool codeInSubject = true)
    {
        var c = culture ?? EmailCulture.Nl;
        var minutes = PrivacyConstants.UnconfirmedRegistrationRetentionMinutes;
        var subject = codeInSubject
            ? Sf(c, "Email.TakeoverEmailVerification.SubjectWithCode", code)
            : S(c, "Email.TakeoverEmailVerification.Subject");
        return Finish(Doc("TakeoverEmailVerification", subject,
            S(c, "Email.TakeoverEmailVerification.Preheader"), S(c, "Email.TakeoverEmailVerification.Heading"),
            [
                P(T(c, "Email.TakeoverEmailVerification.P1", EmailArg.Bold(companyName))),
                C(code, Sf(c, "Email.Common.CodeWorksMinutes", minutes)),
                N(S(c, "Email.TakeoverEmailVerification.Note"))
            ],
            greeting: GreetOther(c, contactName),
            eyebrow: new EmailEyebrow(S(c, "Email.TakeoverEmailVerification.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: Sf(c, "Email.Reason.TakeoverRequested", EmailBidi.Isolate(c, companyName))), baseUrl);
    }

    public static ComposedEmail TakeoverRequest(
        string? baseUrl, string companyName, string? kvkEstablishmentId, string applicantName, string applicantEmail,
        DateTime? requestedAtUtc = null, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var facts = new List<(string, string)>
        {
            (S(c, "Email.Common.Fact.Name"), applicantName),
            (S(c, "Email.Common.Fact.Email"), applicantEmail),
            (S(c, "Email.Common.Fact.Branch"), companyName)
        };
        if (!string.IsNullOrWhiteSpace(kvkEstablishmentId))
        {
            facts.Add((S(c, "Email.Common.Fact.KvkEstablishment"), kvkEstablishmentId!));
        }

        if (requestedAtUtc is DateTime when)
        {
            facts.Add((S(c, "Email.Common.Fact.RequestedOn"), EmailFormat.DateTimeWithoutZone(when, c)));
        }

        return Finish(Doc("TakeoverRequest",
            Sf(c, "Email.TakeoverRequest.Subject", EmailBidi.Isolate(c, applicantName), EmailBidi.Isolate(c, companyName)),
            S(c, "Email.TakeoverRequest.Preheader"),
            Sf(c, "Email.TakeoverRequest.Heading", EmailBidi.Isolate(c, companyName)),
            [
                P(T(c, "Email.TakeoverRequest.P1",
                    EmailArg.Bold(applicantName), EmailArg.Plain(applicantEmail), EmailArg.Bold(companyName))),
                F(facts),
                P(S(c, "Email.TakeoverRequest.P2"))
            ],
            Button(S(c, "Email.TakeoverRequest.Cta"), links.EmployerTakeovers),
            eyebrow: new EmailEyebrow(S(c, "Email.TakeoverRequest.Eyebrow"), EmailTone.Sun),
            culture: c,
            reasonText: Sf(c, "Email.Reason.YouManage", EmailBidi.Isolate(c, companyName))), baseUrl);
    }

    public static ComposedEmail TakeoverSubmitted(
        string? baseUrl, string contactName, string companyName, string? withdrawUrl = null,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = string.IsNullOrWhiteSpace(withdrawUrl) ? links.HowLobsyWorks : withdrawUrl!;
        var ctaLabel = string.IsNullOrWhiteSpace(withdrawUrl)
            ? S(c, "Email.TakeoverSubmitted.CtaHow")
            : S(c, "Email.TakeoverSubmitted.CtaWithdraw");
        return Finish(Doc("TakeoverSubmitted",
            Sf(c, "Email.TakeoverSubmitted.Subject", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.TakeoverSubmitted.Preheader"), S(c, "Email.TakeoverSubmitted.Heading"),
            [
                P(T(c, "Email.TakeoverSubmitted.P1", EmailArg.Bold(companyName)))
            ],
            Button(ctaLabel, ctaUrl),
            greeting: GreetOther(c, contactName),
            eyebrow: new EmailEyebrow(S(c, "Email.TakeoverSubmitted.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: Sf(c, "Email.Reason.TakeoverRequested", EmailBidi.Isolate(c, companyName))), baseUrl);
    }

    public static ComposedEmail AccessRequestEmailVerification(
        string? baseUrl, string contactName, string companyName, string code, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        return Finish(Doc("AccessRequestEmailVerification", S(c, "Email.AccessRequestEmailVerification.Subject"),
            S(c, "Email.AccessRequestEmailVerification.Preheader"), S(c, "Email.AccessRequestEmailVerification.Heading"),
            [
                P(T(c, "Email.AccessRequestEmailVerification.P1", EmailArg.Bold(companyName), EmailArg.Plain(ApplicationRules.EmailVerificationCodeMinutes.ToString(), isolate: false))),
                C(code, "")
            ],
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail AccessRequestSubmitted(string? baseUrl, string contactName, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("AccessRequestSubmitted", S(c, "Email.AccessRequestSubmitted.Subject"),
            S(c, "Email.AccessRequestSubmitted.Preheader"), S(c, "Email.AccessRequestSubmitted.Heading"),
            [
                P(T(c, "Email.AccessRequestSubmitted.P1", EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.AccessRequestSubmitted.Cta"), links.Login),
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail AccessRequestToManager(
        string? baseUrl, string companyName, string requesterName, string? requesterFunction,
        string requesterEmail, string roleLabel, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var functionBit = string.IsNullOrWhiteSpace(requesterFunction) ? "" : $" ({requesterFunction})";
        return Finish(Doc("AccessRequestToManager", S(c, "Email.AccessRequestToManager.Subject"),
            S(c, "Email.AccessRequestToManager.Preheader"), S(c, "Email.AccessRequestToManager.Heading"),
            [
                P(T(c, "Email.AccessRequestToManager.P1", EmailArg.Bold(companyName))),
                P(Sf(c, "Email.AccessRequestToManager.P2",
                    EmailBidi.Isolate(c, requesterName), functionBit, EmailBidi.Isolate(c, requesterEmail),
                    EmailBidi.Isolate(c, roleLabel)))
            ],
            Button(S(c, "Email.AccessRequestToManager.Cta"), links.EmployerTakeovers), culture: c), baseUrl);
    }

    public static ComposedEmail AccessRequestReminder(string? baseUrl, string companyName, string requesterName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("AccessRequestReminder", S(c, "Email.AccessRequestReminder.Subject"),
            S(c, "Email.AccessRequestReminder.Preheader"), S(c, "Email.AccessRequestReminder.Heading"),
            [
                P(T(c, "Email.AccessRequestReminder.P1", EmailArg.Plain(requesterName), EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.AccessRequestReminder.Cta"), links.EmployerTakeovers), culture: c), baseUrl);
    }

    public static ComposedEmail AccessRequestRejected(
        string? baseUrl, string contactName, string companyName, string? reason, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.AccessRequestRejected.P1", EmailArg.Bold(companyName)))
        };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            blocks.Add(P(Sf(c, "Email.Common.ReasonLabel", EmailBidi.Isolate(c, reason!))));
        }

        return Finish(Doc("AccessRequestRejected", S(c, "Email.AccessRequestRejected.Subject"),
            S(c, "Email.AccessRequestRejected.Preheader"), S(c, "Email.AccessRequestRejected.Heading"),
            blocks, Button(S(c, "Email.AccessRequestRejected.Cta"), links.RegisterAccess),
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail AccessRequestExpired(string? baseUrl, string contactName, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("AccessRequestExpired", S(c, "Email.AccessRequestExpired.Subject"),
            S(c, "Email.AccessRequestExpired.Preheader"), S(c, "Email.AccessRequestExpired.Heading"),
            [
                P(T(c, "Email.AccessRequestExpired.P1", EmailArg.Bold(companyName), EmailArg.Plain("30", isolate: false)))
            ],
            Button(S(c, "Email.AccessRequestExpired.Cta"), links.RegisterAccess),
            greeting: GreetOther(c, contactName), culture: c), baseUrl);
    }

    public static ComposedEmail OwnershipTransferManagersNotify(string? baseUrl, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("OwnershipTransferManagersNotify", S(c, "Email.OwnershipTransferManagersNotify.Subject"),
            S(c, "Email.OwnershipTransferManagersNotify.Preheader"), S(c, "Email.OwnershipTransferManagersNotify.Heading"),
            [
                P(T(c, "Email.OwnershipTransferManagersNotify.P1", EmailArg.Bold(companyName), EmailArg.Plain("7", isolate: false)))
            ],
            Button(S(c, "Email.OwnershipTransferManagersNotify.Cta"), links.EmployerTakeovers), culture: c), baseUrl);
    }

    public static ComposedEmail IntermediaryClientSelfManaged(string? baseUrl, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("IntermediaryClientSelfManaged", S(c, "Email.IntermediaryClientSelfManaged.Subject"),
            S(c, "Email.IntermediaryClientSelfManaged.Preheader"), S(c, "Email.IntermediaryClientSelfManaged.Heading"),
            [
                P(T(c, "Email.IntermediaryClientSelfManaged.P1", EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.IntermediaryClientSelfManaged.Cta"), links.Login), culture: c), baseUrl);
    }

    public static ComposedEmail TakeoverApproved(
        string? baseUrl, string contactName, string companyName, string contactEmail,
        string? setPasswordUrl, bool hasOrganization, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? S(c, "Email.Common.Cta.Login") : S(c, "Email.Common.Cta.SetPassword");
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.TakeoverApproved.P1", EmailArg.Bold(companyName)))
        };
        if (hasOrganization)
        {
            blocks.Add(P(S(c, "Email.TakeoverApproved.POrg")));
        }

        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
        }

        return Finish(Doc("TakeoverApproved",
            Sf(c, "Email.TakeoverApproved.Subject", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.TakeoverApproved.Preheader"), S(c, "Email.TakeoverApproved.Heading"),
            blocks, Button(ctaLabel, ctaUrl),
            greeting: GreetOther(c, contactName),
            eyebrow: new EmailEyebrow(S(c, "Email.TakeoverApproved.Eyebrow"), EmailTone.Mint),
            showMascot: true, culture: c,
            reasonText: Sf(c, "Email.Reason.TakeoverRequested", EmailBidi.Isolate(c, companyName))), baseUrl);
    }

    public static ComposedEmail TakeoverRejected(string? baseUrl, string contactName, string companyName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var brand = Brand(baseUrl);
        return Finish(Doc("TakeoverRejected",
            Sf(c, "Email.TakeoverRejected.Subject", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.TakeoverRejected.Preheader"), S(c, "Email.TakeoverRejected.Heading"),
            [
                P(T(c, "Email.TakeoverRejected.P1", EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.TakeoverRejected.Cta"), $"mailto:{brand.SupportAddress}"),
            greeting: GreetOther(c, contactName),
            eyebrow: new EmailEyebrow(S(c, "Email.TakeoverRejected.Eyebrow"), EmailTone.Peach),
            culture: c,
            reasonText: Sf(c, "Email.Reason.TakeoverRequested", EmailBidi.Isolate(c, companyName))), baseUrl);
    }
}
