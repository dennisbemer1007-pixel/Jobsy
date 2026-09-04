using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Email;

/// <summary>
/// Shared branded HTML shell for transactional Lobsy e-mails (table-based for clients).
/// </summary>
public static class EmailLayout
{
    public const string BrandNavy = "#0f2d5c";
    public const string BrandDeep = "#0a2044";
    public const string AccentTeal = "#1a7a6d";
    public const string AccentCoral = "#c45c3e";
    public const string Pearl = "#f5f2ee";
    public const string SoftSky = "#e8eef7";
    public const string Text = "#142033";
    public const string Muted = "#5a6a7d";

    public static string Escape(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    public static string Absolute(string? publicWebBaseUrl, string relativePath)
    {
        var origin = JobsyPublicUrl.NormalizeOrigin(
            string.IsNullOrWhiteSpace(publicWebBaseUrl) ? "https://lobsy.nl" : publicWebBaseUrl);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return origin;
        }

        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        var path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return origin.TrimEnd('/') + path;
    }

    /// <summary>
    /// Small PNG hosted on the public site (Gmail/Apple Mail). SMTP additionally
    /// inlines the same bytes as CID so Outlook desktop shows the mark without a
    /// remote fetch.
    /// </summary>
    public const string LogoRelativePath = "/images/brand/lobsy-email.png?v=20260904-mail";

    public static string LogoUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, LogoRelativePath);

    public static string VacancyUrl(string? publicWebBaseUrl, Guid vacancyId)
        => Absolute(publicWebBaseUrl, $"/vacancies/{vacancyId}");

    public static string CandidateApplicationsUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/candidate/applications");

    public static string EditVacancyUrl(string? publicWebBaseUrl, Guid vacancyId)
        => Absolute(publicWebBaseUrl, $"/branch/vacancies/new?edit={vacancyId}");

    public static string HighlightVacancyUrl(string? publicWebBaseUrl, Guid vacancyId)
        => Absolute(publicWebBaseUrl, $"/employer/vacancies?boost=highlight&id={vacancyId}");

    public static string PushBomVacancyUrl(string? publicWebBaseUrl, Guid vacancyId)
        => Absolute(publicWebBaseUrl, $"/employer/vacancies?boost=pushbom&id={vacancyId}");

    public static string BranchApplicantsUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/branch/applicants");

    public static string LoginUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/login");

    public static string RegisterActivateUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/register/activate");

    public static string RegisterUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/register");

    public static string PrivacyDataUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/privacy/data");

    public static string TakeoversUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/employer/takeovers");

    public static string EmployerVacanciesUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/employer/vacancies");

    public static string JobMapUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/");

    public static string SetUnavailableUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, CandidateActionPurposes.SetUnavailableInAppPath);

    public static string WithdrawOthersUrl(string? publicWebBaseUrl, Guid hiredApplicationId)
        => Absolute(publicWebBaseUrl, CandidateActionPurposes.WithdrawOthersInAppPath(hiredApplicationId));

    public static string SalesOnboardingUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/salesmanager/onboarding");

    public static string AmbassadeurOnboardingUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/ambassadeur/onboarding");

    public static string EmployerApiKeysUrl(string? publicWebBaseUrl)
        => Absolute(publicWebBaseUrl, "/employer/company");

    /// <summary>Full HTML document with logo header, content, and footer.</summary>
    public static string Wrap(
        string innerHtml,
        string? publicWebBaseUrl,
        string? preheader = null,
        string brandName = "Lobsy")
    {
        var logo = Escape(LogoUrl(publicWebBaseUrl));
        var brand = Escape(brandName);
        var pre = string.IsNullOrWhiteSpace(preheader)
            ? ""
            : $"""
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">
                {Escape(preheader)}
              </div>
              """;

        return $"""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{brand}</title>
            </head>
            <body style="margin:0;padding:0;background:{Pearl};font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:{Text};">
              {pre}
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:{Pearl};padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e0d8;">
                      <tr>
                        <td style="background:{BrandNavy};padding:22px 28px;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                            <tr>
                              <td width="64" valign="middle" style="width:64px;">
                                <table role="presentation" cellspacing="0" cellpadding="0">
                                  <tr>
                                    <td style="background:#ffffff;border-radius:12px;padding:6px;line-height:0;">
                                      <img src="{logo}" width="48" height="48" alt="{brand}" border="0" style="display:block;border:0;outline:none;text-decoration:none;background:transparent;" />
                                    </td>
                                  </tr>
                                </table>
                              </td>
                              <td valign="middle" style="padding-left:12px;">
                                <div style="font-size:20px;font-weight:700;color:#ffffff;letter-spacing:0.02em;">{brand}</div>
                                <div style="font-size:12px;color:{SoftSky};margin-top:2px;">Hyper-lokaal matchen</div>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="height:4px;background:{AccentTeal};font-size:0;line-height:0;">&nbsp;</td>
                      </tr>
                      <tr>
                        <td style="padding:28px 28px 8px 28px;font-size:15px;line-height:1.55;color:{Text};">
                          {innerHtml}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 28px 28px 28px;font-size:12px;line-height:1.45;color:{Muted};">
                          Met vriendelijke groet,<br/>
                          <strong style="color:{BrandNavy};">Team {brand}</strong>
                        </td>
                      </tr>
                    </table>
                    <p style="margin:16px 0 0;font-size:11px;color:{Muted};">
                      Je ontvangt deze e-mail omdat je een Lobsy-account hebt of een actie op het platform hebt gedaan.
                    </p>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string Paragraph(string htmlInner)
        => $"<p style=\"margin:0 0 14px 0;\">{htmlInner}</p>";

    public static string Heading(string text)
        => $"<h1 style=\"margin:0 0 14px 0;font-size:22px;line-height:1.3;color:{BrandNavy};font-weight:700;\">{Escape(text)}</h1>";

    /// <summary>Primary CTA button — full width on mobile clients that honor max-width.</summary>
    public static string PrimaryButton(string absoluteUrl, string label)
    {
        var href = Escape(absoluteUrl);
        var text = Escape(label);
        return $"""
            <table role="presentation" cellspacing="0" cellpadding="0" style="margin:18px 0 8px 0;">
              <tr>
                <td style="border-radius:10px;background:{BrandNavy};">
                  <a href="{href}" style="display:inline-block;padding:12px 22px;font-size:14px;font-weight:700;color:#ffffff;text-decoration:none;border-radius:10px;">
                    {text}
                  </a>
                </td>
              </tr>
            </table>
            """;
    }

    public static string SecondaryButton(string absoluteUrl, string label)
    {
        var href = Escape(absoluteUrl);
        var text = Escape(label);
        return $"""
            <table role="presentation" cellspacing="0" cellpadding="0" style="margin:8px 0;">
              <tr>
                <td style="border-radius:10px;background:{SoftSky};border:1px solid #c5d4e8;">
                  <a href="{href}" style="display:inline-block;padding:11px 20px;font-size:13px;font-weight:650;color:{BrandNavy};text-decoration:none;border-radius:10px;">
                    {text}
                  </a>
                </td>
              </tr>
            </table>
            """;
    }

    public static string MutedNote(string htmlInner)
        => $"<p style=\"margin:18px 0 0 0;font-size:12px;line-height:1.45;color:{Muted};\">{htmlInner}</p>";

    public static string FactCard(IEnumerable<(string Label, string Value)> facts)
    {
        var sb = new StringBuilder();
        sb.Append($"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:4px 0 16px 0;background:{SoftSky};border-radius:12px;">
              <tr><td style="padding:14px 16px;">
            """);
        var first = true;
        foreach (var (label, value) in facts)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!first)
            {
                sb.Append("""<div style="height:8px;"></div>""");
            }

            first = false;
            sb.Append($"""
                <div style="font-size:11px;text-transform:uppercase;letter-spacing:0.04em;color:{Muted};">{Escape(label)}</div>
                <div style="font-size:15px;font-weight:650;color:{BrandDeep};margin-top:2px;">{Escape(value)}</div>
                """);
        }

        sb.Append("</td></tr></table>");
        return sb.ToString();
    }

    public static string KpiList(IEnumerable<(string Label, string Value)> items)
    {
        var sb = new StringBuilder();
        sb.Append($"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:8px 0 16px 0;border:1px solid #e4e0d8;border-radius:12px;overflow:hidden;">
            """);
        var i = 0;
        foreach (var (label, value) in items)
        {
            var bg = i % 2 == 0 ? "#ffffff" : Pearl;
            sb.Append($"""
                <tr>
                  <td style="padding:10px 14px;background:{bg};font-size:14px;color:{Text};">{Escape(label)}</td>
                  <td align="right" style="padding:10px 14px;background:{bg};font-size:14px;font-weight:700;color:{BrandNavy};">{Escape(value)}</td>
                </tr>
                """);
            i++;
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>Large OTP / temporary secret display (tests extract via data-lobsy-otp).</summary>
    public static string OtpBlock(string plaintextCode)
    {
        var code = Escape(plaintextCode);
        return $"""
            <p data-lobsy-otp="{code}" style="margin:16px 0;font-size:28px;letter-spacing:0.18em;font-weight:700;color:{BrandNavy};text-align:center;"><code>{code}</code></p>
            """;
    }

    public static string FormatEuro(decimal amount)
        => amount.ToString("C", CultureInfo.GetCultureInfo("nl-NL"));

    public static string FormatKm(double km)
        => km.ToString("0.0", CultureInfo.GetCultureInfo("nl-NL")) + " km";
}
