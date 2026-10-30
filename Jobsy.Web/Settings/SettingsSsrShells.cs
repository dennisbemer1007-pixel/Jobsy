using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
namespace Jobsy.Web.Settings;

internal static class SettingsSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        SettingsSsrData.PageData data,
        string antiforgeryFieldName,
        string antiforgeryToken)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"settings-page-wrap\">");
        sb.Append("<div class=\"panel-page candidate-hub panel-page--cand-ill\" data-testid=\"candidate-settings-ssr\">");
        AppendIntro(sb, culture);
        sb.Append("<div class=\"cand-settings\">");
        AppendNav(sb, culture, data);
        AppendPanel(sb, culture, data, antiforgeryFieldName, antiforgeryToken);
        sb.Append("</div></div></div>");
        return sb.ToString();
    }

    private static void AppendIntro(StringBuilder sb, CultureState culture)
    {
        sb.Append("<header class=\"sollicitaties-intro cand-settings-intro\"><span class=\"sollicitaties-intro__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["Auth.Settings"]));
        sb.Append("</span><h1 class=\"sollicitaties-intro__title\" tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Title"]));
        sb.Append("</h1><p class=\"sollicitaties-intro__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Lead"]));
        sb.Append("</p></header>");
    }

    private static void AppendNav(StringBuilder sb, CultureState culture, SettingsSsrData.PageData data)
    {
        sb.Append("<nav class=\"cand-settings__nav\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Title"]));
        sb.Append("\"><div class=\"cand-settings__nav-group\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.NavSettings"]));
        sb.Append("</h2>");
        AppendNavLink(sb, data, "mail", culture["CandidateSettings.Mail.Title"], culture["CandidateSettings.Mail.Desc"]);
        if (data.ProactiveOn)
        {
            sb.Append("<a class=\"");
            sb.Append(NavClass(data, "proactive"));
            sb.Append("\" href=\"/candidate/proactive-matching\"><span><span class=\"cand-settings__nav-title\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Proactive.Title"]));
            sb.Append("</span><span class=\"cand-settings__nav-desc\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Proactive.Desc"]));
            sb.Append("</span></span></a>");
        }

        if (data.KandidatenkaartOn)
        {
            sb.Append("<a class=\"");
            sb.Append(NavClass(data, "kk"));
            sb.Append("\" href=\"/candidate/kandidatenkaart-instellingen\"><span><span class=\"cand-settings__nav-title\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Kk.Title"]));
            sb.Append("</span><span class=\"cand-settings__nav-desc\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Kk.Desc"]));
            sb.Append("</span></span></a>");
        }

        AppendNavLink(sb, data, "profile", culture["CandidateSettings.Profile.Title"], culture["CandidateSettings.Profile.Desc"], href: "/candidate/paspoort?tab=data");
        AppendNavLink(sb, data, "how", culture["CandidateSettings.How.Title"], culture["CandidateSettings.How.Desc"], href: "/candidate/hoe-werkt-lobsy");
        sb.Append("</div><div class=\"cand-settings__nav-group\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.NavPrivacy"]));
        sb.Append("</h2>");
        AppendNavLink(sb, data, "privacy", culture["CandidatePrivacy.Policy.Title"], culture["CandidatePrivacy.Policy.Desc"], href: "/privacy");
        AppendNavLink(sb, data, "data", culture["CandidatePrivacy.Data.Title"], culture["CandidatePrivacy.Data.Desc"], href: "/privacy/data");
        AppendNavLink(sb, data, "consent", culture["CandidatePrivacy.Consent.Title"], culture["CandidatePrivacy.Consent.Desc"], href: "/candidate/paspoort?tab=data#consent");
        AppendNavLink(sb, data, "mail", culture["CandidatePrivacy.Mail.Title"], culture["CandidatePrivacy.Mail.Desc"]);
        sb.Append("</div>");
        AppendAccountGroup(sb, culture);
        sb.Append("</nav>");
    }

    private static void AppendAccountGroup(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"cand-settings__nav-group\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.NavAccount"]));
        sb.Append("</h2>");
        AppendAccountAction(
            sb,
            culture["Profile.Reset.Title"],
            culture["Profile.Reset.Lead"],
            "reset");
        AppendAccountAction(
            sb,
            culture["Profile.UnsubscribeTitle"],
            culture["Profile.UnsubscribeLead"],
            "delete");
        sb.Append("</div>");
    }

    private static void AppendAccountAction(StringBuilder sb, string title, string desc, string dangerIntent)
    {
        sb.Append("<button type=\"button\" class=\"cand-settings__nav-action\" data-settings-danger=\"");
        sb.Append(WebUtility.HtmlEncode(dangerIntent));
        sb.Append("\"><span><span class=\"cand-settings__nav-title\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</span><span class=\"cand-settings__nav-desc\">");
        sb.Append(WebUtility.HtmlEncode(desc));
        sb.Append("</span></span></button>");
    }

    private static void AppendNavLink(
        StringBuilder sb,
        SettingsSsrData.PageData data,
        string pane,
        string title,
        string desc,
        string? href = null)
    {
        var link = href ?? NavHref(pane);
        sb.Append("<a class=\"");
        sb.Append(NavClass(data, pane));
        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(link));
        sb.Append("\"><span><span class=\"cand-settings__nav-title\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</span><span class=\"cand-settings__nav-desc\">");
        sb.Append(WebUtility.HtmlEncode(desc));
        sb.Append("</span></span></a>");
    }

    private static string NavClass(SettingsSsrData.PageData data, string pane)
        => string.Equals(data.Pane, pane, StringComparison.OrdinalIgnoreCase) ? "is-active" : "";

    private static string NavHref(string pane)
        => pane == "mail" ? "/candidate/settings?pane=mail" : $"/candidate/settings?pane={Uri.EscapeDataString(pane)}";

    private static void AppendPanel(
        StringBuilder sb,
        CultureState culture,
        SettingsSsrData.PageData data,
        string antiforgeryFieldName,
        string antiforgeryToken)
    {
        sb.Append("<div class=\"cand-settings__panel\">");
        if (string.Equals(data.Pane, "mail", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("<div class=\"cand-settings__panel-head\"><div><h2>");
            sb.Append(WebUtility.HtmlEncode(culture["MailSettings.Title"]));
            sb.Append("</h2><p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Mail.Desc"]));
            sb.Append("</p></div></div>");
            AppendMailForm(sb, culture, data.Mail, antiforgeryFieldName, antiforgeryToken);
        }
        else
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.PickPane"]));
            sb.Append("</p><p><a class=\"btn btn-secondary\" href=\"");
            sb.Append(WebUtility.HtmlEncode(NavHref("mail")));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["CandidateSettings.Mail.Title"]));
            sb.Append("</a></p>");
        }

        sb.Append("</div>");
    }

    private static void AppendMailForm(
        StringBuilder sb,
        CultureState culture,
        SettingsSsrData.MailPanelData mail,
        string antiforgeryFieldName,
        string antiforgeryToken)
    {
        if (mail.LoadError)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["MailSettings.LoadError"]));
            sb.Append("</p>");
            return;
        }

        sb.Append("<form method=\"post\" action=\"/account/mail-instellingen\" class=\"login-form\" data-enhance=\"false\">");
        sb.Append("<input type=\"hidden\" name=\"settings_return\" value=\"/candidate/settings?pane=mail\" />");
        sb.Append("<input type=\"hidden\" name=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryFieldName));
        sb.Append("\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" />");

        if (mail.Optional.Count > 0)
        {
            sb.Append("<input type=\"hidden\" name=\"reminder_emails_shown\" value=\"1\" />");
            AppendToggle(
                sb,
                culture["MailSettings.Reminders.Label"],
                culture["MailSettings.Reminders.Hint"],
                "reminder_emails",
                mail.ReminderEmails,
                reminderEmailsAttr: true);

            sb.Append("<h3 style=\"font-size:1rem;margin:1.25rem 0 0.5rem\">");
            sb.Append(WebUtility.HtmlEncode(culture["MailSettings.OptionalHeading"]));
            sb.Append("</h3>");
            foreach (var item in mail.Optional)
            {
                AppendToggle(
                    sb,
                    LabelFor(culture, item.Key),
                    HintFor(culture, item.Key),
                    $"pref_{item.Key}",
                    item.Enabled);
            }

            if (mail.WhatsAppAvailable)
            {
                sb.Append("<input type=\"hidden\" name=\"whatsapp_shown\" value=\"1\" />");
                AppendToggle(
                    sb,
                    culture["Comeback.WhatsApp.Title"],
                    culture["Comeback.WhatsApp.Hint"],
                    "whatsapp_optin",
                    mail.WhatsAppOptedIn,
                    whatsAppToggle: true);
                sb.Append("<label class=\"login-field cand-settings__whatsapp-phone\" data-whatsapp-phone");
                if (!mail.WhatsAppOptedIn)
                {
                    sb.Append(" hidden");
                }

                sb.Append("><span class=\"cand-settings__field-label\">");
                sb.Append(WebUtility.HtmlEncode(culture["Comeback.WhatsApp.Phone"]));
                sb.Append("</span><input type=\"tel\" name=\"whatsapp_phone\" value=\"");
                sb.Append(WebUtility.HtmlEncode(mail.WhatsAppPhone ?? ""));
                sb.Append("\" autocomplete=\"tel\" inputmode=\"tel\" /><span class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(culture["Comeback.WhatsApp.PhoneHint"]));
                sb.Append("</span></label>");
            }

            sb.Append("<button type=\"submit\" class=\"btn btn-primary\" style=\"margin-top:1rem\">");
            sb.Append(WebUtility.HtmlEncode(culture["MailSettings.Save"]));
            sb.Append("</button>");
        }
        else
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["MailSettings.NoOptional"]));
            sb.Append("</p>");
        }

        sb.Append("<h3 style=\"font-size:1rem;margin:2rem 0 0.5rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["MailSettings.AlwaysHeading"]));
        sb.Append("</h3><ul>");
        foreach (var key in mail.AlwaysCultureKeys)
        {
            sb.Append("<li>");
            sb.Append(WebUtility.HtmlEncode(culture[key]));
            sb.Append("</li>");
        }

        sb.Append("</ul><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture[mail.EmployersOn ? "MailSettings.AlwaysHint" : "MailSettings.AlwaysHintOff"]));
        sb.Append("</p></form>");

        if (mail.SavedBanner)
        {
            sb.Append("<p role=\"status\" style=\"margin-top:1rem;color:var(--success)\">");
            sb.Append(WebUtility.HtmlEncode(culture["MailSettings.Saved"]));
            sb.Append("</p>");
        }
    }

    private static void AppendToggle(
        StringBuilder sb,
        string label,
        string hint,
        string name,
        bool on,
        bool reminderEmailsAttr = false,
        bool whatsAppToggle = false)
    {
        sb.Append("<div class=\"cand-toggle");
        if (whatsAppToggle)
        {
            sb.Append(" cand-toggle--whatsapp");
        }

        sb.Append("\"><div><strong class=\"mail-settings__label\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</strong><span class=\"muted mail-settings__hint\" style=\"display:block\">");
        sb.Append(WebUtility.HtmlEncode(hint));
        sb.Append("</span></div><label class=\"cand-toggle__switch");
        if (on)
        {
            sb.Append(" is-on");
        }

        sb.Append("\"><input type=\"checkbox\" name=\"");
        sb.Append(WebUtility.HtmlEncode(name));
        sb.Append("\" value=\"1\"");
        if (on)
        {
            sb.Append(" checked");
        }

        if (reminderEmailsAttr)
        {
            sb.Append(" data-reminder-emails=\"1\"");
        }

        if (whatsAppToggle)
        {
            sb.Append(" data-whatsapp-optin=\"1\"");
        }

        sb.Append(" /></label></div>");
    }

    private static string LabelFor(CultureState culture, string key) => key switch
    {
        "PushBom" => culture["MailSettings.PushBom.Label"],
        "VacancyEngagementReminder" => culture["MailSettings.VacancyEngagement.Label"],
        "CompanyReEngagement" => culture["MailSettings.CompanyReEngagement.Label"],
        "ComebackReminder" => culture["MailSettings.Comeback.Label"],
        _ => key
    };

    private static string HintFor(CultureState culture, string key) => key switch
    {
        "PushBom" => culture["MailSettings.PushBom.Hint"],
        "VacancyEngagementReminder" => culture["MailSettings.VacancyEngagement.Hint"],
        "CompanyReEngagement" => culture["MailSettings.CompanyReEngagement.Hint"],
        "ComebackReminder" => culture["MailSettings.Comeback.Hint"],
        _ => ""
    };
}
