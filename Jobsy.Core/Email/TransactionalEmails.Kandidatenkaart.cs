using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail KandidatenkaartInviteBundled(
        string? baseUrl,
        string? candidateName,
        string companyName,
        string vacancyTitle,
        int matchPercent,
        string vacancyUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var subject = Sf(c, "Email.KkInviteBundled.Subject", EmailBidi.Isolate(c, vacancyTitle));
        return Finish(Doc(
            "KandidatenkaartInviteBundled",
            subject,
            Sf(c, "Email.KkInviteBundled.Preheader", matchPercent.ToString()),
            S(c, "Email.KkInviteBundled.Heading"),
            [
                P(T(c, "Email.KkInviteBundled.P1",
                    EmailArg.Bold(companyName),
                    EmailArg.Bold(vacancyTitle),
                    EmailArg.Plain(matchPercent.ToString(), isolate: false))),
                P(S(c, "Email.KkInviteBundled.P2"))
            ],
            Button(S(c, "Email.KkInviteBundled.Cta"), vacancyUrl),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.KkInviteBundled.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: S(c, "Email.Reason.NearbyJobs")), baseUrl);
    }

    public static ComposedEmail KandidatenkaartInviteReminder(
        string? baseUrl,
        string? candidateName,
        string vacancyTitle,
        string companyName,
        string vacancyUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        return Finish(Doc(
            "KandidatenkaartInviteReminder",
            Sf(c, "Email.KkInviteReminder.Subject", EmailBidi.Isolate(c, vacancyTitle)),
            S(c, "Email.KkInviteReminder.Preheader"),
            S(c, "Email.KkInviteReminder.Heading"),
            [
                P(T(c, "Email.KkInviteReminder.P1",
                    EmailArg.Bold(vacancyTitle),
                    EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.KkInviteReminder.Cta"), vacancyUrl),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.KkInviteReminder.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: S(c, "Email.Reason.NearbyJobs")), baseUrl);
    }

    public static ComposedEmail KandidatenkaartInactivityCheck(
        string? baseUrl,
        string? candidateName,
        string settingsUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        return Finish(Doc(
            "KandidatenkaartInactivityCheck",
            S(c, "Email.KkInactivity.Subject"),
            S(c, "Email.KkInactivity.Preheader"),
            S(c, "Email.KkInactivity.Heading"),
            [P(S(c, "Email.KkInactivity.P1"))],
            Button(S(c, "Email.KkInactivity.Cta"), settingsUrl),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.KkInactivity.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: S(c, "Email.Reason.NearbyJobs")), baseUrl);
    }

    public static ComposedEmail KandidatenkaartMissedOut(
        string? baseUrl,
        string? candidateName,
        string vacancyTitle,
        string companyName,
        IReadOnlyList<(string Title, string Company)> similar,
        string mapUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.KkMissedOut.P1", EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName)))
        };
        foreach (var row in similar.Take(3))
        {
            blocks.Add(P(T(c, "Email.KkMissedOut.Item", EmailArg.Plain(row.Title), EmailArg.Plain(row.Company))));
        }

        return Finish(Doc(
            "KandidatenkaartMissedOut",
            Sf(c, "Email.KkMissedOut.Subject", EmailBidi.Isolate(c, vacancyTitle)),
            S(c, "Email.KkMissedOut.Preheader"),
            S(c, "Email.KkMissedOut.Heading"),
            blocks,
            Button(S(c, "Email.KkMissedOut.Cta"), mapUrl),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.KkMissedOut.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: S(c, "Email.Reason.NearbyJobs")), baseUrl);
    }

    public static ComposedEmail KandidatenkaartInviteWithdrawn(
        string? baseUrl,
        string? candidateName,
        string vacancyTitle,
        string companyName,
        string mapUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        return Finish(Doc(
            "KandidatenkaartInviteWithdrawn",
            Sf(c, "Email.KkWithdrawn.Subject", EmailBidi.Isolate(c, vacancyTitle)),
            S(c, "Email.KkWithdrawn.Preheader"),
            S(c, "Email.KkWithdrawn.Heading"),
            [
                P(T(c, "Email.KkWithdrawn.P1", EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName)))
            ],
            Button(S(c, "Email.KkWithdrawn.Cta"), mapUrl),
            greeting: GreetCandidate(c, candidateName),
            eyebrow: new EmailEyebrow(S(c, "Email.KkWithdrawn.Eyebrow"), EmailTone.Sky),
            culture: c,
            reasonText: S(c, "Email.Reason.NearbyJobs")), baseUrl);
    }
}
