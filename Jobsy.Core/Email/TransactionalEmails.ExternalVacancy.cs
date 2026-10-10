using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

public static partial class TransactionalEmails
{
    public static ComposedEmail ExternalVacancyApplication(
        string? baseUrl,
        string candidateName,
        string vacancyTitle,
        string companyName,
        string motivation,
        IReadOnlyList<(string Label, string Value)> sharedFacts,
        string inviteUrl,
        string unsubscribeUrl,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var blocks = new List<EmailBlock>
        {
            P(T(c, "Email.ExternalVacancyApplication.P1", EmailArg.Bold(candidateName), EmailArg.Bold(vacancyTitle), EmailArg.Bold(companyName))),
            P(EmailText.Plain($"\"{motivation}\"")),
        };

        if (sharedFacts.Count > 0)
        {
            blocks.Add(F(sharedFacts.Select(f => (f.Label, f.Value)).ToList()));
        }

        blocks.Add(P(EmailText.Plain("Je kunt de sollicitatie bekijken en een Lobsy-account maken om verder te gaan.")));

        return Finish(
            Doc(
                "ExternalVacancyApplication",
                S(c, "Email.ExternalVacancyApplication.Subject"),
                S(c, "Email.ExternalVacancyApplication.Preheader"),
                S(c, "Email.ExternalVacancyApplication.Heading"),
                blocks,
                Button(S(c, "Email.ExternalVacancyApplication.Cta"), inviteUrl),
                eyebrow: new EmailEyebrow(S(c, "Email.ExternalVacancyApplication.Eyebrow"), EmailTone.Peach),
                culture: c,
                reasonText: $"{S(c, "Email.ExternalVacancyApplication.Unsubscribe")} {unsubscribeUrl}"),
            baseUrl);
    }
}
