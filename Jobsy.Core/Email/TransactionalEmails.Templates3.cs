using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.Time;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail UserInvite(
        string? baseUrl, string fullName, string roleLabel, string email,
        string? setPasswordUrl, bool promotedFromCandidate,
        string? inviterFirstName = null, string? companyName = null, DateTime? linkExpiresAtUtc = null,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? S(c, "Email.Common.Cta.Login") : S(c, "Email.Common.Cta.AcceptInvite");
        var inviter = string.IsNullOrWhiteSpace(inviterFirstName)
            ? (companyName ?? "Lobsy")
            : inviterFirstName!;
        var company = companyName ?? "Lobsy";
        var facts = new List<(string, string)>
        {
            (S(c, "Email.Common.Fact.Role"), roleLabel),
            (S(c, "Email.Common.Fact.Company"), company),
            (S(c, "Email.Common.Fact.YourEmail"), email)
        };
        if (setPasswordUrl is not null)
        {
            var expires = linkExpiresAtUtc ?? DateTime.UtcNow.AddDays(7);
            facts.Add((S(c, "Email.Common.Fact.LinkValidUntil"), EmailFormat.Date(expires, c)));
        }

        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.UserInvite.P1",
                EmailArg.Plain(inviter), EmailArg.Bold(roleLabel), EmailArg.Bold(company))),
            F(facts),
            setPasswordUrl is null
                ? P(S(c, "Email.UserInvite.P2HasPassword"))
                : P(S(c, "Email.UserInvite.P2SetPassword"))
        };
        if (promotedFromCandidate)
        {
            blocks.Add(P(S(c, "Email.UserInvite.P3Promoted")));
        }

        var reasonInviter = string.IsNullOrWhiteSpace(inviterFirstName) ? company : inviterFirstName!;
        return Finish(Doc("UserInvite",
            Sf(c, "Email.UserInvite.Subject", EmailBidi.Isolate(c, company)),
            S(c, "Email.UserInvite.Preheader"),
            S(c, "Email.UserInvite.Heading"),
            blocks, Button(ctaLabel, ctaUrl),
            greeting: GreetOther(c, fullName),
            eyebrow: new EmailEyebrow(S(c, "Email.UserInvite.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: Sf(c, "Email.Reason.Invited", EmailBidi.Isolate(c, reasonInviter))), baseUrl);
    }

    public static ComposedEmail SalesManagerInvite(
        string? baseUrl, string name, string email, string? setPasswordUrl, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? S(c, "Email.Common.Cta.Login") : S(c, "Email.Common.Cta.AcceptInvite");
        var blocks = new List<EmailBlock>
        {
            P(S(c, "Email.SalesManagerInvite.P1")),
            P(S(c, "Email.SalesManagerInvite.Steps"))
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
        }

        return Finish(Doc("SalesManagerInvite", S(c, "Email.SalesManagerInvite.Subject"),
            S(c, "Email.SalesManagerInvite.Preheader"), S(c, "Email.SalesManagerInvite.Heading"),
            blocks, Button(ctaLabel, ctaUrl),
            greeting: GreetOther(c, name),
            eyebrow: new EmailEyebrow(S(c, "Email.SalesManagerInvite.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail AmbassadeurInvite(
        string? baseUrl, string name, string email, string? setPasswordUrl, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? S(c, "Email.Common.Cta.Login") : S(c, "Email.Common.Cta.AcceptInvite");
        var blocks = new List<EmailBlock>
        {
            P(S(c, "Email.AmbassadeurInvite.P1")),
            P(S(c, "Email.AmbassadeurInvite.Steps"))
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
        }

        return Finish(Doc("AmbassadeurInvite", S(c, "Email.AmbassadeurInvite.Subject"),
            S(c, "Email.AmbassadeurInvite.Preheader"), S(c, "Email.AmbassadeurInvite.Heading"),
            blocks, Button(ctaLabel, ctaUrl),
            greeting: GreetOther(c, name),
            eyebrow: new EmailEyebrow(S(c, "Email.AmbassadeurInvite.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail CompanyApiKeyCredentials(
        string? baseUrl, string companyName, string apiBase, string revealUrl, DateTime expiresAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var swaggerUrl = apiBase.TrimEnd('/') + "/swagger";
        var expiresLabel = EmailFormat.DateTimeWithoutZone(expiresAtUtc, c);
        return Finish(Doc("CompanyApiKeyCredentials",
            Sf(c, "Email.CompanyApiKeyCredentials.Subject", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.CompanyApiKeyCredentials.Preheader", EmailBidi.Isolate(c, expiresLabel)),
            S(c, "Email.CompanyApiKeyCredentials.Heading"),
            [
                P(T(c, "Email.CompanyApiKeyCredentials.P1", EmailArg.Bold(companyName))),
                F([
                    (S(c, "Email.Common.Fact.Company"), companyName),
                    (S(c, "Email.Common.Fact.LinkValidUntil"), expiresLabel),
                    (S(c, "Email.CompanyApiKeyCredentials.Fact.Docs"), swaggerUrl)
                ]),
                P(S(c, "Email.CompanyApiKeyCredentials.P2"))
            ],
            Button(S(c, "Email.CompanyApiKeyCredentials.Cta"), revealUrl),
            greeting: GreetOther(c, null),
            eyebrow: new EmailEyebrow(S(c, "Email.CompanyApiKeyCredentials.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: Sf(c, "Email.Reason.ApiKeyRequested", EmailBidi.Isolate(c, companyName))), baseUrl);
    }

    public static ComposedEmail ParentalConsent(
        string? baseUrl, string? childFirstName, string confirmUrl, DateTime expiresAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var age = CandidateConsentRules.ParentalConsentAge;
        var hasName = !string.IsNullOrWhiteSpace(childFirstName);
        var child = hasName ? childFirstName!.Trim() : S(c, "Email.ParentalConsent.ChildFallback");
        var childCap = hasName ? childFirstName!.Trim() : S(c, "Email.ParentalConsent.ChildFallbackCap");
        var subject = hasName
            ? Sf(c, "Email.ParentalConsent.SubjectNamed", EmailBidi.Isolate(c, child))
            : S(c, "Email.ParentalConsent.Subject");
        var expiresLabel = EmailFormat.Date(expiresAtUtc, c);
        return Finish(Doc("ParentalConsent", subject,
            Sf(c, "Email.ParentalConsent.Preheader", EmailBidi.Isolate(c, child)),
            Sf(c, "Email.ParentalConsent.Heading", EmailBidi.Isolate(c, childCap)),
            [
                P(EmailText.Plain(S(c, "Email.Common.GreetingOtherFallback"))),
                P(Sf(c, "Email.ParentalConsent.P1", EmailBidi.Isolate(c, childCap), age)),
                F([
                    (S(c, "Email.Common.Fact.Name"), childCap),
                    (S(c, "Email.Common.Fact.LinkValidUntil"), expiresLabel)
                ]),
                P(S(c, "Email.ParentalConsent.P2")),
                N(Sf(c, "Email.ParentalConsent.Note", EmailBidi.Isolate(c, child)))
            ],
            Button(S(c, "Email.ParentalConsent.Cta"), confirmUrl),
            eyebrow: new EmailEyebrow(S(c, "Email.ParentalConsent.Eyebrow"), EmailTone.Peach),
            culture: c,
            reasonText: Sf(c, "Email.Reason.ParentAsked", EmailBidi.Isolate(c, childCap))), baseUrl);
    }

    public static ComposedEmail SupportAccessRequested(
        string? baseUrl, string adminDisplay, string reason, DateTime expiresAtUtc, string scopeLabel,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var expiresLabel = EmailFormat.DateTimeWithoutZone(expiresAtUtc, c);
        var zone = S(c, "Email.Common.TimeZoneNl");
        return Finish(Doc("SupportAccessRequested",
            Sf(c, "Email.SupportAccessRequested.Subject", EmailBidi.Isolate(c, adminDisplay)),
            Sf(c, "Email.SupportAccessRequested.Preheader", EmailBidi.Isolate(c, scopeLabel), EmailBidi.Isolate(c, expiresLabel)),
            S(c, "Email.SupportAccessRequested.Heading"),
            [
                P(Sf(c, "Email.SupportAccessRequested.P1", EmailBidi.Isolate(c, adminDisplay))),
                F([
                    (S(c, "Email.SupportAccessRequested.Fact.Reason"), reason),
                    (S(c, "Email.SupportAccessRequested.Fact.Scope"), scopeLabel),
                    (S(c, "Email.SupportAccessRequested.Fact.Expires"), Sf(c, "Email.SupportAccessRequested.ExpiresVal", expiresLabel, zone))
                ])
            ],
            Button(S(c, "Email.SupportAccessRequested.Cta"), links.AdminPersonalDataAccessLog),
            eyebrow: new EmailEyebrow(S(c, "Email.SupportAccessRequested.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }

    public static ComposedEmail AccountLockout(
        string? baseUrl,
        int failedAttempts = 5,
        DateTime? lockoutUntilUtc = null,
        TimeSpan? duration = null,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var brand = Brand(baseUrl);
        var effectiveDuration = duration ?? LoginLockoutRules.LockoutDuration(failedAttempts);
        var durationLabel = EmailFormat.Duration(effectiveDuration, c);
        var until = lockoutUntilUtc ?? DateTime.UtcNow.Add(effectiveDuration);
        return Finish(Doc("AccountLockout", S(c, "Email.AccountLockout.Subject"),
            Sf(c, "Email.AccountLockout.Preheader", durationLabel),
            S(c, "Email.AccountLockout.Heading"),
            [
                P(T(c, "Email.AccountLockout.P1",
                    EmailArg.Plain(failedAttempts.ToString(), isolate: false),
                    EmailArg.Plain(durationLabel, isolate: false))),
                F([(S(c, "Email.AccountLockout.Fact.Until"), EmailFormat.DateTimeWithoutZone(until, c))]),
                P(S(c, "Email.AccountLockout.P2"))
            ],
            Button(S(c, "Email.AccountLockout.Cta"), $"mailto:{brand.SupportAddress}"),
            eyebrow: new EmailEyebrow(S(c, "Email.AccountLockout.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }

    public static ComposedEmail AccountUnsubscribeVerification(
        string? baseUrl, string fullName, string code, int ttlMinutes, EmailCulture? culture = null,
        bool codeInSubject = true)
    {
        var c = culture ?? EmailCulture.Nl;
        var subject = codeInSubject
            ? Sf(c, "Email.AccountUnsubscribeVerification.SubjectWithCode", code)
            : S(c, "Email.AccountUnsubscribeVerification.Subject");
        return Finish(Doc("AccountUnsubscribeVerification", subject,
            S(c, "Email.AccountUnsubscribeVerification.Preheader"), S(c, "Email.AccountUnsubscribeVerification.Heading"),
            [
                P(S(c, "Email.AccountUnsubscribeVerification.P1")),
                C(code, Sf(c, "Email.Common.CodeWorksMinutes", ttlMinutes)),
                N(S(c, "Email.AccountUnsubscribeVerification.Note"))
            ],
            greeting: GreetOther(c, fullName),
            eyebrow: new EmailEyebrow(S(c, "Email.AccountUnsubscribeVerification.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail MfaResetByAdmin(string? baseUrl, string recipientName, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("MfaResetByAdmin", S(c, "Email.MfaResetByAdmin.Subject"),
            S(c, "Email.MfaResetByAdmin.Preheader"), S(c, "Email.MfaResetByAdmin.Heading"),
            [
                P(S(c, "Email.MfaResetByAdmin.P1")),
                P(S(c, "Email.MfaResetByAdmin.P2"))
            ],
            Button(S(c, "Email.MfaResetByAdmin.Cta"), links.Login),
            greeting: GreetOther(c, recipientName),
            eyebrow: new EmailEyebrow(S(c, "Email.MfaResetByAdmin.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }

    public static ComposedEmail EmailSignUpCode(string? baseUrl, string code, string? culture)
    {
        var c = EmailCulture.ForLanguage(culture);
        return Finish(Doc("EmailSignUpCode",
            Sf(c, "Email.EmailSignUpCode.Subject", code),
            S(c, "Email.EmailSignUpCode.Preheader"),
            S(c, "Email.EmailSignUpCode.Heading"),
            [P(S(c, "Email.EmailSignUpCode.P1")), C(code, Sf(c, "Email.Common.CodeValid10", ApplicationRules.EmailVerificationCodeMinutes))],
            culture: c), baseUrl);
    }

    public static ComposedEmail EmailSignInCode(string? baseUrl, string code, string? culture)
    {
        var c = EmailCulture.ForLanguage(culture);
        return Finish(Doc("EmailSignInCode",
            S(c, "Email.EmailSignInCode.Subject"),
            S(c, "Email.EmailSignInCode.Preheader"),
            S(c, "Email.EmailSignInCode.Heading"),
            [P(S(c, "Email.EmailSignInCode.P1")), C(code, Sf(c, "Email.Common.CodeValid10", ApplicationRules.EmailVerificationCodeMinutes))],
            culture: c), baseUrl);
    }

    public static ComposedEmail EmailCodeUsePassword(string? baseUrl, string? culture)
    {
        var links = Links(baseUrl);
        var c = EmailCulture.ForLanguage(culture);
        return Finish(Doc("EmailCodeUsePassword",
            S(c, "Email.EmailCodeUsePassword.Subject"),
            S(c, "Email.EmailCodeUsePassword.Preheader"),
            S(c, "Email.EmailCodeUsePassword.Heading"),
            [P(S(c, "Email.EmailCodeUsePassword.P1"))],
            Button(S(c, "Email.EmailCodeUsePassword.Cta"), links.Login),
            culture: c), baseUrl);
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
