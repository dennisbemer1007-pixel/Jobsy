using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail AfterMatchInterviewScheduled(
        string? baseUrl,
        string? recipientName,
        string companyName,
        string vacancyTitle,
        DateTime startLocal,
        string rescheduleUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var formatCulture = System.Globalization.CultureInfo.GetCultureInfo(
            string.Equals(c.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en-GB" : "nl-NL");
        var when = startLocal.ToString("dddd d MMMM yyyy HH:mm", formatCulture);
        var body = T(
            c,
            "Email.AfterMatchScheduled.P1",
            EmailArg.Bold(companyName),
            EmailArg.Bold(vacancyTitle),
            EmailArg.Plain(when, isolate: false));

        return Finish(Doc(
            "AfterMatchScheduled",
            Sf(c, "Email.AfterMatchScheduled.Subject", companyName),
            S(c, "Email.AfterMatchScheduled.Preheader"),
            S(c, "Email.AfterMatchScheduled.Heading"),
            [P(body), P(S(c, "Email.AfterMatchScheduled.P2"))],
            Button(S(c, "Email.AfterMatchScheduled.Cta"), rescheduleUrl),
            greeting: GreetCandidate(c, recipientName),
            eyebrow: new EmailEyebrow(S(c, "Email.AfterMatchScheduled.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }
}
