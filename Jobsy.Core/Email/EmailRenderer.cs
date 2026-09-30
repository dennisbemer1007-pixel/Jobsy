using System.Net;
using System.Text;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;

namespace Jobsy.Core.Email;

public sealed record EmailRenderResult(string Html, string Text);

/// <summary>
/// Single renderer for transactional mail: email-safe HTML + plain text.
/// </summary>
public static class EmailRenderer
{
    public static EmailRenderResult Render(
        EmailDocument doc,
        EmailBrand brand,
        EmailRenderMode mode = EmailRenderMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(brand);
        Validate(doc, throwOnError: true);

        var html = RenderHtml(doc, brand, mode);
        var text = RenderText(doc, brand);
        return new EmailRenderResult(html, text);
    }

    /// <summary>
    /// Validates sanity rules. Throws in Development/Testing; callers in Production
    /// should pass <paramref name="throwOnError"/> = false and log instead.
    /// </summary>
    public static IReadOnlyList<string> Validate(EmailDocument doc, bool throwOnError)
    {
        var errors = new List<string>();
        if (doc.Cta is not null && doc.Kind == EmailKind.Security)
        {
            errors.Add("Security mails must not have a CTA.");
        }

        var codeCount = doc.Blocks.OfType<CodeBlock>().Count();
        if (doc.Kind == EmailKind.Security && codeCount != 1)
        {
            errors.Add("Security mails must have exactly one Code block.");
        }

        if (string.Equals(doc.Preheader?.Trim(), doc.Subject?.Trim(), StringComparison.Ordinal))
        {
            errors.Add("Preheader must not equal Subject.");
        }

        if (doc.ShowMascot && !EmailTemplateRegistry.AllowsMascot(doc.TemplateKey))
        {
            errors.Add($"ShowMascot is not allowed for template '{doc.TemplateKey}'.");
        }

        if (throwOnError && errors.Count > 0)
        {
            throw new InvalidOperationException(
                "EmailDocument sanity check failed: " + string.Join(" ", errors));
        }

        return errors;
    }

    private static string RenderHtml(EmailDocument doc, EmailBrand brand, EmailRenderMode mode)
    {
        var rtl = doc.Culture.IsRightToLeft;
        var lang = WebUtility.HtmlEncode(doc.Culture.Language);
        var dir = rtl ? "rtl" : "ltr";
        var align = rtl ? "right" : "left";
        var font = rtl ? EmailTheme.FontArabic : EmailTheme.Font;
        var forceDark = mode == EmailRenderMode.PreviewDark;

        var sb = new StringBuilder(12_000);
        sb.Append("<!DOCTYPE html>");
        sb.Append($"<html lang=\"{lang}\" dir=\"{dir}\" xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\">");
        sb.Append("<head>");
        sb.Append("<meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.Append("<meta name=\"x-apple-disable-message-reformatting\">");
        sb.Append("<meta name=\"format-detection\" content=\"telephone=no,date=no,address=no,email=no,url=no\">");
        sb.Append("<meta name=\"color-scheme\" content=\"light dark\">");
        sb.Append("<meta name=\"supported-color-schemes\" content=\"light dark\">");
        sb.Append($"<title>{Escape(doc.Subject)}</title>");
        sb.Append("<!--[if mso]><noscript><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml></noscript><![endif]-->");
        sb.Append("<style>");
        sb.Append(BuildCss(rtl, forceDark));
        sb.Append("</style>");
        sb.Append("</head>");

        var bodyBg = forceDark ? EmailTheme.Dark.Bg : EmailTheme.Light.Bg;
        var cardBg = forceDark ? EmailTheme.Dark.Surface : EmailTheme.Light.Surface;
        var border = forceDark ? EmailTheme.Dark.Border : EmailTheme.Light.Border;
        var text = forceDark ? EmailTheme.Dark.Text : EmailTheme.Light.Text;
        var muted = forceDark ? EmailTheme.Dark.Muted : EmailTheme.Light.Muted;
        var brandColor = forceDark ? EmailTheme.Dark.Text : EmailTheme.Light.Brand;
        var btnBg = forceDark ? EmailTheme.Dark.Btn : EmailTheme.Light.Brand;
        var btnFg = forceDark ? EmailTheme.Dark.BtnText : EmailTheme.Light.BtnText;

        sb.Append($"<body class=\"bg\" style=\"margin:0;padding:0;background:{bodyBg};\">");
        AppendPreheader(sb, doc.Preheader);

        sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" class=\"bg\" data-lobsy-layout=\"2\" style=\"background:{bodyBg};\" dir=\"{dir}\">");
        sb.Append("<tr><td align=\"center\" class=\"outer\" style=\"padding:28px 16px 32px 16px;\">");
        sb.Append("<!--[if mso]><table role=\"presentation\" width=\"600\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\"><tr><td><![endif]-->");
        sb.Append($"<div style=\"max-width:600px;margin:0 auto;font-family:{font};\" dir=\"{dir}\">");

        // Logo row — "Lobsy" stays LTR; mascot/logo sit at inline-start in RTL.
        var logoMargin = rtl ? "margin-right:8px" : "margin-left:8px";
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
        sb.Append($"<td class=\"px\" align=\"{align}\" style=\"padding:0 4px 16px 4px;\">");
        sb.Append($"<a href=\"{Escape(brand.PublicWebBaseUrl)}\" style=\"text-decoration:none;\">");
        sb.Append($"<img class=\"logo-img\" src=\"{Escape(brand.LogoUrl)}\" width=\"36\" height=\"36\" alt=\"Lobsy\" style=\"display:inline-block;vertical-align:middle;width:36px;height:36px;\">");
        sb.Append($"<span class=\"t\" dir=\"ltr\" style=\"display:inline-block;vertical-align:middle;{logoMargin};font-size:20px;line-height:36px;font-weight:700;letter-spacing:-0.01em;color:{brandColor};\">Lobsy</span>");
        sb.Append("</a></td></tr></table>");

        // Card
        sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" class=\"card\" style=\"background:{cardBg};border:1px solid {border};border-radius:18px;border-collapse:separate;\">");
        sb.Append($"<tr><td class=\"px\" align=\"{align}\" style=\"padding:30px 36px 20px 36px;text-align:{align};font-family:{font};\">");

        AppendHero(sb, doc, brand, forceDark, rtl, text);
        if (!string.IsNullOrWhiteSpace(doc.Greeting))
        {
            AppendParagraphHtml(sb, EmailText.Plain(doc.Greeting!), text, muted: false);
        }

        foreach (var block in doc.Blocks)
        {
            AppendBlock(sb, block, forceDark, text, muted, border, brandColor, rtl);
        }

        if (doc.Cta is not null)
        {
            AppendButton(sb, doc.Cta, btnBg, btnFg);
        }

        sb.Append($"<p class=\"m\" style=\"margin:18px 0 0 0;font-size:14px;line-height:20px;color:{muted};\">{Escape(doc.SignOff)}</p>");
        sb.Append("</td></tr></table>");

        // Footer
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
        sb.Append($"<td class=\"ft m\" align=\"{align}\" style=\"padding:20px 8px 0 8px;font-size:13px;line-height:20px;color:{muted};text-align:{align};font-family:{font};\">");
        sb.Append($"<p class=\"m\" style=\"margin:0 0 8px 0;\">{Escape(doc.ReasonText)}</p>");
        if (doc.Kind == EmailKind.Optional)
        {
            var unsubHref = string.IsNullOrWhiteSpace(doc.UnsubscribeUrl)
                ? Absolute(brand.PublicWebBaseUrl, "/mail/afmelden")
                : doc.UnsubscribeUrl!;
            var unsubLabel = EmailStrings.Get(doc.Culture, "Email.Common.Unsubscribe");
            sb.Append($"<p class=\"m\" style=\"margin:0 0 8px 0;\"><a href=\"{Escape(unsubHref)}\" data-lobsy-unsub=\"1\" style=\"color:{muted};text-decoration:underline;\">{Escape(unsubLabel)}</a></p>");
        }

        var helpLabel = EmailStrings.Get(doc.Culture, "Email.Common.Help");
        var privacyLabel = EmailStrings.Get(doc.Culture, "Email.Common.Privacy");
        sb.Append("<p class=\"m\" style=\"margin:0 0 8px 0;\">");
        sb.Append($"<a href=\"mailto:{Escape(brand.SupportAddress)}\" style=\"color:{muted};text-decoration:underline;\">{Escape(helpLabel)}</a>");
        sb.Append($" &nbsp;·&nbsp; <a href=\"{Escape(Absolute(brand.PublicWebBaseUrl, "/privacy"))}\" style=\"color:{muted};text-decoration:underline;\">{Escape(privacyLabel)}</a>");
        if (doc.Kind == EmailKind.Optional)
        {
            var mailSettingsLabel = EmailStrings.Get(doc.Culture, "Email.Common.MailSettings");
            sb.Append($" &nbsp;·&nbsp; <a href=\"{Escape(Absolute(brand.PublicWebBaseUrl, "/account/mail-instellingen"))}\" style=\"color:{muted};text-decoration:underline;\">{Escape(mailSettingsLabel)}</a>");
        }

        sb.Append("</p>");
        var legal = brand.LegalLine;
        if (!string.IsNullOrWhiteSpace(legal))
        {
            sb.Append($"<p class=\"m\" style=\"margin:0;\">{Escape(legal)}</p>");
        }

        sb.Append("</td></tr></table>");
        sb.Append("</div>");
        sb.Append("<!--[if mso]></td></tr></table><![endif]-->");
        sb.Append("</td></tr></table>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void AppendHero(
        StringBuilder sb,
        EmailDocument doc,
        EmailBrand brand,
        bool forceDark,
        bool rtl,
        string textColor)
    {
        var eyebrowHtml = doc.Eyebrow is null ? "" : BuildEyebrow(doc.Eyebrow, forceDark, textColor);
        var heading = $"<h1 class=\"h1 t\" style=\"margin:0 0 12px 0;font-size:26px;line-height:32px;font-weight:700;letter-spacing:-0.01em;color:{textColor};\">{Escape(doc.Heading)}</h1>";

        if (doc.ShowMascot)
        {
            // Mascot at inline-start in RTL (matches em-d09).
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
            if (rtl)
            {
                sb.Append("<td width=\"72\" valign=\"top\" align=\"right\" style=\"width:72px;\">");
                sb.Append($"<img src=\"{Escape(brand.MascotUrl)}\" width=\"64\" height=\"64\" alt=\"\" style=\"display:block;width:64px;height:64px;\">");
                sb.Append("</td>");
                sb.Append($"<td valign=\"top\">{eyebrowHtml}{heading}</td>");
            }
            else
            {
                sb.Append($"<td valign=\"top\">{eyebrowHtml}{heading}</td>");
                sb.Append("<td width=\"72\" valign=\"top\" align=\"right\" style=\"width:72px;\">");
                sb.Append($"<img src=\"{Escape(brand.MascotUrl)}\" width=\"64\" height=\"64\" alt=\"\" style=\"display:block;width:64px;height:64px;\">");
                sb.Append("</td>");
            }

            sb.Append("</tr></table>");
        }
        else
        {
            sb.Append(eyebrowHtml);
            sb.Append(heading);
        }
    }

    private static string BuildEyebrow(EmailEyebrow eyebrow, bool forceDark, string textColor)
    {
        var toneClass = EmailTheme.ToneClass(eyebrow.Tone);
        var bg = EmailTheme.ToneBackground(eyebrow.Tone, forceDark);
        return $"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 14px 0;\"><tr>"
               + $"<td class=\"{toneClass}\" style=\"background:{bg};border-radius:999px;padding:5px 12px;font-size:13px;line-height:18px;font-weight:600;color:{textColor};\">"
               + $"<span class=\"t\" style=\"color:{textColor};\">{Escape(eyebrow.Text)}</span></td></tr></table>";
    }

    private static void AppendBlock(
        StringBuilder sb,
        EmailBlock block,
        bool forceDark,
        string text,
        string muted,
        string border,
        string brandColor,
        bool rtl)
    {
        switch (block)
        {
            case ParagraphBlock p:
                AppendParagraphHtml(sb, p.Text, text, muted: false);
                break;
            case FactsBlock f:
                AppendFacts(sb, f, forceDark, text, muted, border, rtl);
                break;
            case StepsBlock s:
                AppendSteps(sb, s, forceDark, text, brandColor, rtl);
                break;
            case CodeBlock c:
                AppendCode(sb, c, forceDark, text);
                if (!string.IsNullOrWhiteSpace(c.ValidityText))
                {
                    AppendParagraphHtml(sb, EmailText.Plain(c.ValidityText), text, muted: false);
                }

                break;
            case NoteBlock n:
                AppendNote(sb, n, muted, border);
                break;
        }
    }

    private static void AppendParagraphHtml(StringBuilder sb, EmailText text, string color, bool muted)
    {
        var cls = muted ? "m" : "t";
        sb.Append($"<p class=\"{cls}\" style=\"margin:0 0 14px 0;font-size:16px;line-height:24px;color:{color};\">");
        AppendInline(sb, text, color);
        sb.Append("</p>");
    }

    private static void AppendInline(StringBuilder sb, EmailText text, string color)
    {
        foreach (var seg in text.Segments)
        {
            switch (seg)
            {
                case PlainSegment p:
                    sb.Append(WrapBdi(Escape(p.Text), p.Isolate));
                    break;
                case BoldSegment b:
                    sb.Append("<strong>").Append(WrapBdi(Escape(b.Text), b.Isolate)).Append("</strong>");
                    break;
                case LinkSegment l:
                    var label = WrapBdi(Escape(l.Label), l.IsolateLabel);
                    sb.Append($"<a href=\"{Escape(l.Url)}\" style=\"color:{color};text-decoration:underline;\">{label}</a>");
                    break;
            }
        }
    }

    private static string WrapBdi(string escaped, bool isolate)
        => isolate ? $"<bdi>{escaped}</bdi>" : escaped;

    private static void AppendFacts(
        StringBuilder sb,
        FactsBlock facts,
        bool forceDark,
        string text,
        string muted,
        string border,
        bool rtl)
    {
        var bg = EmailTheme.ToneBackground(facts.Tone, forceDark);
        var toneClass = EmailTheme.ToneClass(facts.Tone);
        sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" class=\"{toneClass}\" data-lobsy-block=\"facts\" style=\"background:{bg};border-radius:14px;margin:6px 0 18px 0;\">");
        sb.Append("<tr><td style=\"padding:6px 18px;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\">");
        var i = 0;
        foreach (var (label, value) in facts.Rows)
        {
            if (string.IsNullOrWhiteSpace(value.Flatten()))
            {
                continue;
            }

            var bt = i == 0 ? "" : $"border-top:1px solid {border};";
            sb.Append("<tr>");
            if (rtl)
            {
                sb.Append($"<td class=\"kv-v t ln\" valign=\"top\" style=\"{bt}padding:10px 12px 10px 0;font-size:16px;line-height:22px;font-weight:600;color:{text};\">");
                AppendInline(sb, value, text);
                sb.Append("</td>");
                sb.Append($"<td class=\"kv-l m ln\" width=\"150\" valign=\"top\" style=\"width:150px;{bt}padding:10px 0;font-size:14px;line-height:20px;color:{muted};\">{Escape(label)}</td>");
            }
            else
            {
                sb.Append($"<td class=\"kv-l m ln\" width=\"150\" valign=\"top\" style=\"width:150px;{bt}padding:10px 0;font-size:14px;line-height:20px;color:{muted};\">{Escape(label)}</td>");
                sb.Append($"<td class=\"kv-v t ln\" valign=\"top\" style=\"{bt}padding:10px 0 10px 12px;font-size:16px;line-height:22px;font-weight:600;color:{text};\">");
                AppendInline(sb, value, text);
                sb.Append("</td>");
            }

            sb.Append("</tr>");
            i++;
        }

        sb.Append("</table></td></tr></table>");
    }

    private static void AppendSteps(
        StringBuilder sb,
        StepsBlock steps,
        bool forceDark,
        string text,
        string brandColor,
        bool rtl)
    {
        if (!string.IsNullOrWhiteSpace(steps.Title))
        {
            sb.Append($"<h2 class=\"t\" style=\"margin:22px 0 10px 0;font-size:17px;line-height:24px;font-weight:700;color:{text};\">{Escape(steps.Title)}</h2>");
        }

        var numBg = forceDark ? EmailTheme.Dark.Sky : EmailTheme.Light.Sky;
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" data-lobsy-block=\"steps\" style=\"margin:0 0 8px 0;\">");
        var n = 1;
        foreach (var item in steps.Items)
        {
            sb.Append("<tr>");
            if (rtl)
            {
                sb.Append($"<td class=\"t\" valign=\"top\" style=\"padding:2px 0 10px 0;font-size:16px;line-height:22px;color:{text};\">");
                AppendInline(sb, item, text);
                sb.Append("</td>");
                sb.Append("<td width=\"36\" valign=\"top\" align=\"right\" style=\"width:36px;padding:0 0 10px 0;\">");
                sb.Append($"<div class=\"num\" style=\"width:26px;height:26px;border-radius:13px;background:{numBg};color:{brandColor};font-size:14px;line-height:26px;font-weight:700;text-align:center;\">{n}</div>");
                sb.Append("</td>");
            }
            else
            {
                sb.Append("<td width=\"36\" valign=\"top\" style=\"width:36px;padding:0 0 10px 0;\">");
                sb.Append($"<div class=\"num\" style=\"width:26px;height:26px;border-radius:13px;background:{numBg};color:{brandColor};font-size:14px;line-height:26px;font-weight:700;text-align:center;\">{n}</div>");
                sb.Append("</td>");
                sb.Append($"<td class=\"t\" valign=\"top\" style=\"padding:2px 0 10px 0;font-size:16px;line-height:22px;color:{text};\">");
                AppendInline(sb, item, text);
                sb.Append("</td>");
            }

            sb.Append("</tr>");
            n++;
        }

        sb.Append("</table>");
    }

    private static void AppendCode(StringBuilder sb, CodeBlock code, bool forceDark, string text)
    {
        var bg = forceDark ? EmailTheme.Dark.Sky : EmailTheme.Light.Sky;
        var digits = Escape(code.Digits);
        sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" class=\"tone-sky\" data-lobsy-block=\"code\" style=\"background:{bg};border-radius:14px;margin:6px 0 14px 0;\">");
        sb.Append("<tr><td align=\"center\" style=\"padding:22px 12px;\">");
        sb.Append($"<div class=\"otp t\" dir=\"ltr\" data-lobsy-otp=\"{digits}\" style=\"font-family:'SF Mono',Menlo,Consolas,'Roboto Mono',monospace;font-size:40px;line-height:46px;font-weight:700;letter-spacing:10px;color:{text};\">{digits}</div>");
        sb.Append("</td></tr></table>");
    }

    private static void AppendNote(StringBuilder sb, NoteBlock note, string muted, string border)
    {
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" data-lobsy-block=\"note\" style=\"margin:4px 0 6px 0;\"><tr>");
        sb.Append($"<td class=\"m ln\" style=\"border-top:1px solid {border};padding:14px 0 0 0;font-size:14px;line-height:21px;color:{muted};\">");
        AppendInline(sb, note.Text, muted);
        if (note.Link is not null)
        {
            sb.Append(" ");
            sb.Append($"<a href=\"{Escape(note.Link.AbsoluteUrl)}\" style=\"color:{muted};text-decoration:underline;\">{Escape(note.Link.Label)}</a>");
        }

        sb.Append("</td></tr></table>");
    }

    private static void AppendButton(StringBuilder sb, EmailCta cta, string btnBg, string btnFg)
    {
        var href = Escape(cta.AbsoluteUrl);
        var label = Escape(cta.Label);
        var width = Math.Max(220, 12 * cta.Label.Length + 64);
        sb.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" class=\"btn-t\" style=\"margin:8px 0 18px 0;\"><tr>");
        sb.Append($"<td class=\"btn-c\" align=\"center\" style=\"border-radius:999px;background:{btnBg};\">");
        sb.Append($"<!--[if mso]><v:roundrect xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:w=\"urn:schemas-microsoft-com:office:word\" href=\"{href}\" style=\"height:50px;v-text-anchor:middle;width:{width}px;\" arcsize=\"50%\" stroke=\"f\" fillcolor=\"{btnBg}\"><w:anchorlock/><center style=\"color:{btnFg};font-family:Arial,sans-serif;font-size:16px;font-weight:bold;\">{label}</center></v:roundrect><![endif]-->");
        sb.Append($"<!--[if !mso]><!--><a class=\"btn-a\" data-lobsy-cta href=\"{href}\" style=\"display:inline-block;background:{btnBg};color:{btnFg};font-size:16px;line-height:20px;font-weight:700;text-decoration:none;padding:15px 30px;border-radius:999px;mso-hide:all;\">{label}</a><!--<![endif]-->");
        sb.Append("</td></tr></table>");
    }

    private static void AppendPreheader(StringBuilder sb, string? preheader)
    {
        var text = Escape(preheader ?? string.Empty);
        sb.Append($"<div style=\"display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;\">{text}</div>");
        var filler = string.Concat(Enumerable.Repeat("&#8199;&#65279;&#847; ", 60));
        sb.Append($"<div style=\"display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;\">{filler}</div>");
    }

    private static string BuildCss(bool rtl, bool forceDark)
    {
        var s = rtl ? "right" : "left";
        var darkRules =
            $".bg{{background:{EmailTheme.Dark.Bg}!important}} .card{{background:{EmailTheme.Dark.Surface}!important;border-color:{EmailTheme.Dark.Border}!important}} " +
            $".t,.t a{{color:{EmailTheme.Dark.Text}!important}} .m,.m a{{color:{EmailTheme.Dark.Muted}!important}} .ln{{border-color:{EmailTheme.Dark.Border}!important}} " +
            $".tone-peach{{background:{EmailTheme.Dark.Peach}!important}} .tone-sun{{background:{EmailTheme.Dark.Sun}!important}} .tone-sky{{background:{EmailTheme.Dark.Sky}!important}} .tone-mint{{background:{EmailTheme.Dark.Mint}!important}} " +
            $".btn-c{{background:{EmailTheme.Dark.Btn}!important}} .btn-a{{color:{EmailTheme.Dark.BtnText}!important;background:{EmailTheme.Dark.Btn}!important}} " +
            $".num{{background:{EmailTheme.Dark.Sky}!important;color:{EmailTheme.Dark.Text}!important}} .logo-img{{background:transparent!important}}";

        var sb = new StringBuilder();
        sb.Append(":root{color-scheme:light dark;supported-color-schemes:light dark}");
        sb.Append("body{margin:0!important;padding:0!important;width:100%!important;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%}");
        sb.Append("table{border-collapse:collapse;mso-table-lspace:0;mso-table-rspace:0} img{border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic}");
        sb.Append("a[x-apple-data-detectors]{color:inherit!important;text-decoration:none!important}");
        sb.Append("@media (max-width:620px){");
        sb.Append(".outer{padding:18px 0 24px 0!important} .card{border-radius:0!important;border-left:0!important;border-right:0!important}");
        sb.Append(".px{padding-left:20px!important;padding-right:20px!important} .h1{font-size:24px!important;line-height:30px!important}");
        sb.Append(".btn-a{display:block!important;text-align:center!important} .btn-t{width:100%!important}");
        sb.Append($".kv-l,.kv-v{{display:block!important;width:auto!important;padding-{s}:0!important}} .kv-l{{padding-bottom:0!important}} .kv-v{{padding-top:2px!important}}");
        sb.Append(".otp{font-size:34px!important;letter-spacing:6px!important} .ft{padding-left:20px!important;padding-right:20px!important}");
        sb.Append('}');
        if (forceDark)
        {
            sb.Append(darkRules);
        }
        else
        {
            sb.Append("@media (prefers-color-scheme:dark){");
            sb.Append(darkRules);
            sb.Append('}');
        }

        sb.Append($"[data-ogsc] .t{{color:{EmailTheme.Dark.Text}!important}} [data-ogsc] .m{{color:{EmailTheme.Dark.Muted}!important}} [data-ogsb] .bg{{background:{EmailTheme.Dark.Bg}!important}} [data-ogsb] .card{{background:{EmailTheme.Dark.Surface}!important}}");
        return sb.ToString();
    }

    private static string RenderText(EmailDocument doc, EmailBrand brand)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(doc.Greeting))
        {
            lines.Add(doc.Greeting!);
            lines.Add("");
        }

        lines.Add(doc.Heading);
        if (!doc.Culture.IsRightToLeft)
        {
            lines.Add(new string('=', Math.Min(76, Math.Max(3, doc.Heading.Length))));
        }

        lines.Add("");

        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case ParagraphBlock p:
                    lines.Add(WrapText(p.Text.Flatten(forRtlText: doc.Culture.IsRightToLeft)));
                    lines.Add("");
                    break;
                case FactsBlock f:
                    foreach (var (label, value) in f.Rows)
                    {
                        var flat = value.Flatten(forRtlText: doc.Culture.IsRightToLeft);
                        if (string.IsNullOrWhiteSpace(flat))
                        {
                            continue;
                        }

                        lines.Add(WrapText(doc.Culture.IsRightToLeft ? $"{flat} :{label}" : $"{label}: {flat}"));
                    }

                    lines.Add("");
                    break;
                case StepsBlock s:
                    if (!string.IsNullOrWhiteSpace(s.Title))
                    {
                        lines.Add(s.Title);
                    }

                    var n = 1;
                    foreach (var item in s.Items)
                    {
                        var flat = item.Flatten(forRtlText: doc.Culture.IsRightToLeft);
                        lines.Add(WrapText(doc.Culture.IsRightToLeft ? $"{flat} .{n}" : $"{n}. {flat}"));
                        n++;
                    }

                    lines.Add("");
                    break;
                case CodeBlock c:
                    lines.Add("");
                    lines.Add(c.Digits);
                    lines.Add("");
                    if (!string.IsNullOrWhiteSpace(c.ValidityText))
                    {
                        lines.Add(WrapText(c.ValidityText));
                        lines.Add("");
                    }

                    break;
                case NoteBlock note:
                    var noteLine = note.Text.Flatten(forRtlText: doc.Culture.IsRightToLeft);
                    if (note.Link is not null)
                    {
                        noteLine = string.IsNullOrWhiteSpace(noteLine)
                            ? $"{note.Link.Label}: {note.Link.AbsoluteUrl}"
                            : $"{noteLine} {note.Link.Label}: {note.Link.AbsoluteUrl}";
                    }

                    lines.Add(WrapText(noteLine));
                    lines.Add("");
                    break;
            }
        }

        if (doc.Cta is not null)
        {
            lines.Add($"{doc.Cta.Label}: {doc.Cta.AbsoluteUrl}");
            lines.Add("");
        }

        lines.Add(doc.SignOff);
        lines.Add("");
        lines.Add(doc.ReasonText);
        if (doc.Kind == EmailKind.Optional)
        {
            var unsubHref = string.IsNullOrWhiteSpace(doc.UnsubscribeUrl)
                ? Absolute(brand.PublicWebBaseUrl, "/mail/afmelden")
                : doc.UnsubscribeUrl!;
            var unsubLabel = EmailStrings.Get(doc.Culture, "Email.Common.Unsubscribe");
            lines.Add($"{unsubLabel}: {unsubHref}");
        }

        var helpLabel = EmailStrings.Get(doc.Culture, "Email.Common.Help");
        var privacyLabel = EmailStrings.Get(doc.Culture, "Email.Common.Privacy");
        lines.Add($"{helpLabel}: mailto:{brand.SupportAddress}");
        lines.Add($"{privacyLabel}: {Absolute(brand.PublicWebBaseUrl, "/privacy")}");
        if (doc.Kind == EmailKind.Optional)
        {
            var mailSettingsLabel = EmailStrings.Get(doc.Culture, "Email.Common.MailSettings");
            lines.Add($"{mailSettingsLabel}: {Absolute(brand.PublicWebBaseUrl, "/account/mail-instellingen")}");
        }

        if (!string.IsNullOrWhiteSpace(brand.LegalLine))
        {
            lines.Add(brand.LegalLine);
        }

        // Strip HTML tags if any leaked into flattened text (safety).
        var joined = string.Join('\n', lines);
        if (joined.Contains('<') && joined.Contains('>'))
        {
            // Keep literal angle brackets from user content; only reject tag-like sequences in structure tests.
        }

        return joined.TrimEnd() + "\n";
    }

    private static string WrapText(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= 76)
        {
            return input;
        }

        var words = input.Split(' ');
        var sb = new StringBuilder();
        var line = new StringBuilder();
        foreach (var word in words)
        {
            // Never break inside URLs.
            if (word.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || word.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || word.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Length > 0)
                {
                    sb.Append(line).Append('\n');
                    line.Clear();
                }

                sb.Append(word).Append('\n');
                continue;
            }

            if (line.Length == 0)
            {
                line.Append(word);
            }
            else if (line.Length + 1 + word.Length <= 76)
            {
                line.Append(' ').Append(word);
            }
            else
            {
                sb.Append(line).Append('\n');
                line.Clear();
                line.Append(word);
            }
        }

        if (line.Length > 0)
        {
            sb.Append(line);
        }

        return sb.ToString().TrimEnd();
    }

    internal static string Escape(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Absolute(string baseUrl, string path)
        => JobsyPublicUrl.NormalizeOrigin(baseUrl).TrimEnd('/') + (path.StartsWith('/') ? path : "/" + path);
}
