using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail ApplicationCvViewed(
        string? baseUrl,
        string candidateName,
        string vacancyTitle,
        string companyName,
        Guid applicationId,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("ApplicationCvViewed",
            Sf(c, "Email.ApplicationCvViewed.Subject", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.ApplicationCvViewed.Preheader", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.ApplicationCvViewed.Heading", EmailBidi.Isolate(c, companyName)),
            [
                P(T(c, "Email.ApplicationCvViewed.P1", EmailArg.Plain(companyName), EmailArg.Bold(vacancyTitle)))
            ],
            Button(S(c, "Email.ApplicationCvViewed.Cta"), links.CandidateApplicationDetail(applicationId)),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.ApplicationCvViewed.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail ApplicationMutualYes(
        string? baseUrl,
        string candidateName,
        string vacancyTitle,
        string companyName,
        Guid applicationId,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("ApplicationMutualYes",
            Sf(c, "Email.ApplicationMutualYes.Subject", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.ApplicationMutualYes.Preheader", EmailBidi.Isolate(c, companyName)),
            Sf(c, "Email.ApplicationMutualYes.Heading", EmailBidi.Isolate(c, companyName)),
            [
                P(T(c, "Email.ApplicationMutualYes.P1", EmailArg.Plain(companyName), EmailArg.Bold(vacancyTitle)))
            ],
            Button(S(c, "Email.ApplicationMutualYes.Cta"), links.CandidateAfterMatch(applicationId)),
            greeting: GreetCandidate(c, candidateName),
            showMascot: true,
            eyebrow: new EmailEyebrow(S(c, "Email.ApplicationMutualYes.Eyebrow"), EmailTone.Mint),
            culture: c), baseUrl);
    }
}
