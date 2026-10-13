using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

/// <summary>
/// Meldknop / DSA notice and action mails (public-pages 06). The reporter mails never name the
/// employer behind the content; the employer mail never contains the reporter's e-mail address.
/// Every value is passed as data — the renderer escapes it.
/// </summary>
public static partial class TransactionalEmails
{
    public static ComposedEmail ReportReceived(
        string? baseUrl,
        string targetLabel,
        string reasonLabel,
        DateTime reportedAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        return Finish(Doc("ReportReceived",
            S(c, "Email.ReportReceived.Subject"),
            S(c, "Email.ReportReceived.Preheader"),
            S(c, "Email.ReportReceived.Heading"),
            [
                P(S(c, "Email.ReportReceived.P1")),
                F(new[]
                {
                    (S(c, "Email.Report.Fact.What"), targetLabel),
                    (S(c, "Email.Report.Fact.Reason"), reasonLabel),
                    (S(c, "Email.Report.Fact.Date"), EmailFormat.DateTimeWithoutZone(reportedAtUtc, c))
                }),
                N(S(c, "Email.ReportReceived.Note"))
            ],
            Button(S(c, "Email.Report.Cta.Map"), links.Map),
            eyebrow: new EmailEyebrow(S(c, "Email.Report.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail ReportDecided(
        string? baseUrl,
        string targetLabel,
        string decisionLabel,
        string? decisionReason,
        DateTime decidedAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var facts = new List<(string Label, string Value)>
        {
            (S(c, "Email.Report.Fact.What"), targetLabel),
            (S(c, "Email.Report.Fact.Decision"), decisionLabel),
            (S(c, "Email.Report.Fact.Date"), EmailFormat.DateTimeWithoutZone(decidedAtUtc, c))
        };

        var blocks = new List<EmailBlock>
        {
            P(S(c, "Email.ReportDecided.P1")),
            F(facts)
        };
        if (!string.IsNullOrWhiteSpace(decisionReason))
        {
            blocks.Add(N(T(c, "Email.ReportDecided.Reason", EmailArg.Plain(decisionReason!))));
        }

        blocks.Add(P(S(c, "Email.ReportDecided.Thanks")));

        return Finish(Doc("ReportDecided",
            S(c, "Email.ReportDecided.Subject"),
            S(c, "Email.ReportDecided.Preheader"),
            S(c, "Email.ReportDecided.Heading"),
            blocks,
            Button(S(c, "Email.Report.Cta.Map"), links.Map),
            eyebrow: new EmailEyebrow(S(c, "Email.Report.Eyebrow"), EmailTone.Sky),
            culture: c), baseUrl);
    }

    public static ComposedEmail ContentRemoved(
        string? baseUrl,
        string companyName,
        string whatLabel,
        string decisionLabel,
        string decisionReason,
        DateTime decidedAtUtc,
        EmailCulture? culture = null)
    {
        var c = culture ?? EmailCulture.Nl;
        var links = Links(baseUrl);
        var brand = Brand(baseUrl);
        return Finish(Doc("ContentRemoved",
            S(c, "Email.ContentRemoved.Subject"),
            S(c, "Email.ContentRemoved.Preheader"),
            S(c, "Email.ContentRemoved.Heading"),
            [
                P(T(c, "Email.ContentRemoved.P1", EmailArg.Bold(companyName))),
                F(new[]
                {
                    (S(c, "Email.Report.Fact.What"), whatLabel),
                    (S(c, "Email.Report.Fact.Decision"), decisionLabel),
                    (S(c, "Email.Report.Fact.Reason"), decisionReason),
                    (S(c, "Email.Report.Fact.Date"), EmailFormat.DateTimeWithoutZone(decidedAtUtc, c))
                }),
                N(T(c, "Email.ContentRemoved.Appeal", EmailArg.Plain(brand.SupportAddress))),
                P(S(c, "Email.ContentRemoved.Rules"))
            ],
            Button(S(c, "Email.ContentRemoved.Cta"), links.EmployerVacancies),
            eyebrow: new EmailEyebrow(S(c, "Email.ContentRemoved.Eyebrow"), EmailTone.Peach),
            culture: c), baseUrl);
    }
}
