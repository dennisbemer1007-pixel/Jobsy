using Jobsy.Core.Email.Model;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail UserInvite(
        string? baseUrl, string fullName, string roleLabel, string email,
        string? setPasswordUrl, bool promotedFromCandidate, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var ctaUrl = setPasswordUrl ?? links.Login;
        var ctaLabel = setPasswordUrl is null ? S(c, "Email.Common.Cta.Login") : S(c, "Email.Common.Cta.AcceptInvite");
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.UserInvite.P1", EmailArg.Bold(roleLabel))),
            setPasswordUrl is null
                ? P(T(c, "Email.UserInvite.P2HasPassword", EmailArg.Plain(email)))
                : P(T(c, "Email.UserInvite.P2SetPassword", EmailArg.Plain(email)))
        };
        if (promotedFromCandidate)
        {
            blocks.Add(P(S(c, "Email.UserInvite.P3Promoted")));
        }

        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
        }

        return Finish(Doc("UserInvite",
            Sf(c, "Email.UserInvite.Subject", EmailBidi.Isolate(c, roleLabel)),
            Sf(c, "Email.UserInvite.Preheader", EmailBidi.Isolate(c, roleLabel)),
            Sf(c, "Email.UserInvite.Heading", EmailBidi.Isolate(c, roleLabel)),
            blocks, Button(ctaLabel, ctaUrl), greeting: GreetOther(c, fullName), culture: c), baseUrl);
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
            setPasswordUrl is null
                ? P(T(c, "Email.SalesManagerInvite.P2HasPassword", EmailArg.Bold(email)))
                : P(T(c, "Email.SalesManagerInvite.P2SetPassword", EmailArg.Bold(email))),
            P(S(c, "Email.SalesManagerInvite.P3"))
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
            blocks.Add(N(S(c, "Email.SalesManagerInvite.NoteOnboarding"),
                new EmailLink(S(c, "Email.SalesManagerInvite.NoteLink"), links.SalesOnboarding)));
        }

        return Finish(Doc("SalesManagerInvite", S(c, "Email.SalesManagerInvite.Subject"),
            S(c, "Email.SalesManagerInvite.Preheader"), S(c, "Email.SalesManagerInvite.Heading"),
            blocks, Button(ctaLabel, ctaUrl), greeting: GreetOther(c, name), culture: c), baseUrl);
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
            setPasswordUrl is null
                ? P(T(c, "Email.AmbassadeurInvite.P2HasPassword", EmailArg.Bold(email)))
                : P(T(c, "Email.AmbassadeurInvite.P2SetPassword", EmailArg.Bold(email))),
            P(S(c, "Email.AmbassadeurInvite.P3"))
        };
        if (setPasswordUrl is not null)
        {
            blocks.Add(N(Sf(c, "Email.Common.LinkValidUntil", EmailFormat.Date(DateTime.UtcNow.AddDays(7), c))));
            blocks.Add(N(S(c, "Email.AmbassadeurInvite.NoteOnboarding"),
                new EmailLink(S(c, "Email.AmbassadeurInvite.NoteLink"), links.AmbassadeurOnboarding)));
        }

        return Finish(Doc("AmbassadeurInvite", S(c, "Email.AmbassadeurInvite.Subject"),
            S(c, "Email.AmbassadeurInvite.Preheader"), S(c, "Email.AmbassadeurInvite.Heading"),
            blocks, Button(ctaLabel, ctaUrl), greeting: GreetOther(c, name), culture: c), baseUrl);
    }

    public static ComposedEmail CompanyApiKeyCredentials(
        string? baseUrl, string companyName, string apiBase, string revealUrl, DateTime expiresAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var endpoint = apiBase.TrimEnd('/') + "/api/external/vacancies";
        var swaggerUrl = apiBase.TrimEnd('/') + "/swagger";
        var expiresLabel = EmailFormat.DateTimeWithoutZone(expiresAtUtc, c);
        return Finish(Doc("CompanyApiKeyCredentials",
            Sf(c, "Email.CompanyApiKeyCredentials.Subject", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.CompanyApiKeyCredentials.Preheader", EmailBidi.Isolate(c, companyName)),
            S(c, "Email.CompanyApiKeyCredentials.Heading"),
            [
                P(T(c, "Email.CompanyApiKeyCredentials.P1", EmailArg.Bold(companyName))),
                F([
                    (S(c, "Email.CompanyApiKeyCredentials.Fact.Endpoint"), endpoint),
                    (S(c, "Email.CompanyApiKeyCredentials.Fact.Header"), S(c, "Email.CompanyApiKeyCredentials.Fact.HeaderVal")),
                    (S(c, "Email.CompanyApiKeyCredentials.Fact.Swagger"), swaggerUrl),
                    (S(c, "Email.CompanyApiKeyCredentials.Fact.Expires"), expiresLabel)
                ]),
                P(S(c, "Email.CompanyApiKeyCredentials.P2"))
            ],
            Button(S(c, "Email.CompanyApiKeyCredentials.Cta"), revealUrl),
            greeting: GreetOther(c, null), culture: c), baseUrl);
    }

    public static ComposedEmail ParentalConsent(
        string? baseUrl, string? childFirstName, string confirmUrl, DateTime expiresAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var age = CandidateConsentRules.ParentalConsentAge;
        var hasName = !string.IsNullOrWhiteSpace(childFirstName);
        var subject = hasName
            ? Sf(c, "Email.ParentalConsent.SubjectNamed", EmailBidi.Isolate(c, childFirstName!.Trim()))
            : S(c, "Email.ParentalConsent.Subject");
        var bodyName = hasName ? childFirstName!.Trim() : S(c, "Email.ParentalConsent.ChildFallback");
        var bodyNameCap = hasName ? childFirstName!.Trim() : S(c, "Email.ParentalConsent.ChildFallbackCap");
        var expiresLabel = EmailFormat.Date(expiresAtUtc, c);
        return Finish(Doc("ParentalConsent", subject, S(c, "Email.ParentalConsent.Preheader"),
            S(c, "Email.ParentalConsent.Heading"),
            [
                P(Sf(c, "Email.ParentalConsent.P1",
                    EmailBidi.Isolate(c, bodyNameCap), EmailBidi.Isolate(c, bodyName), age)),
                N(Sf(c, "Email.ParentalConsent.Note", expiresLabel))
            ],
            Button(S(c, "Email.ParentalConsent.Cta"), confirmUrl), culture: c), baseUrl);
    }

    public static ComposedEmail SupportAccessRequested(
        string? baseUrl, string adminDisplay, string reason, DateTime expiresAtUtc, string scopeLabel,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var expiresLabel = EmailFormat.DateTimeWithoutZone(expiresAtUtc, c);
        var zone = S(c, "Email.Common.TimeZoneNl");
        return Finish(Doc("SupportAccessRequested", S(c, "Email.SupportAccessRequested.Subject"),
            S(c, "Email.SupportAccessRequested.Preheader"), S(c, "Email.SupportAccessRequested.Heading"),
            [
                P(Sf(c, "Email.SupportAccessRequested.P1", EmailBidi.Isolate(c, adminDisplay))),
                F([
                    (S(c, "Email.SupportAccessRequested.Fact.Reason"), reason),
                    (S(c, "Email.SupportAccessRequested.Fact.Scope"), scopeLabel),
                    (S(c, "Email.SupportAccessRequested.Fact.Expires"), Sf(c, "Email.SupportAccessRequested.ExpiresVal", expiresLabel, zone))
                ])
            ],
            Button(S(c, "Email.SupportAccessRequested.Cta"), links.AdminPersonalDataAccessLog), culture: c), baseUrl);
    }

    public static ComposedEmail AccountLockout(string? baseUrl, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var brand = Brand(baseUrl);
        return Finish(Doc("AccountLockout", S(c, "Email.AccountLockout.Subject"),
            S(c, "Email.AccountLockout.Preheader"), S(c, "Email.AccountLockout.Heading"),
            [
                P(S(c, "Email.AccountLockout.P1"))
            ],
            Button(S(c, "Email.AccountLockout.Cta"), $"mailto:{brand.SupportAddress}"), culture: c), baseUrl);
    }

    public static ComposedEmail AccountUnsubscribeVerification(
        string? baseUrl, string fullName, string code, int ttlMinutes, EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        return Finish(Doc("AccountUnsubscribeVerification", S(c, "Email.AccountUnsubscribeVerification.Subject"),
            S(c, "Email.AccountUnsubscribeVerification.Preheader"), S(c, "Email.AccountUnsubscribeVerification.Heading"),
            [
                P(S(c, "Email.AccountUnsubscribeVerification.P1")),
                P(S(c, "Email.AccountUnsubscribeVerification.P2")),
                C(code, Sf(c, "Email.Common.CodeValidMinutes", ttlMinutes))
            ],
            greeting: GreetOther(c, fullName), culture: c), baseUrl);
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
            greeting: GreetOther(c, recipientName), culture: c), baseUrl);
    }

    public static ComposedEmail EmailSignUpCode(string? baseUrl, string code, string? culture)
    {
        var c = EmailCulture.ForLanguage(culture);
        return Finish(Doc("EmailSignUpCode",
            Sf(c, "Email.EmailSignUpCode.Subject", code),
            S(c, "Email.EmailSignUpCode.Preheader"),
            S(c, "Email.EmailSignUpCode.Heading"),
            [P(S(c, "Email.EmailSignUpCode.P1")), C(code, S(c, "Email.Common.CodeValid10"))],
            culture: c), baseUrl);
    }

    public static ComposedEmail EmailSignInCode(string? baseUrl, string code, string? culture)
    {
        var c = EmailCulture.ForLanguage(culture);
        return Finish(Doc("EmailSignInCode",
            S(c, "Email.EmailSignInCode.Subject"),
            S(c, "Email.EmailSignInCode.Preheader"),
            S(c, "Email.EmailSignInCode.Heading"),
            [P(S(c, "Email.EmailSignInCode.P1")), C(code, S(c, "Email.Common.CodeValid10"))],
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
