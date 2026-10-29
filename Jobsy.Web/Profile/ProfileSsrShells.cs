using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;

namespace Jobsy.Web.Profile;

internal static class ProfileSsrShells
{
    private static readonly CultureInfo Dutch = new("nl-NL");

    internal static string BuildBody(CultureState culture, ProfileSsrData.PageData data)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"profile-page-wrap\">");
        sb.Append("<div class=\"panel-page profile-page profile-page--candidate profile-page--illustrated profile-page--hub-ssr\" data-testid=\"candidate-profile-ssr\">");
        AppendIntro(sb, culture);
        if (data.LoadError || data.Profile is null)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
            sb.Append("</p></div></div>");
            return sb.ToString();
        }

        var profile = data.Profile;
        sb.Append("<div class=\"prof-hub\"><aside class=\"prof-hub__summary detail-card\"><h2 class=\"prof-hub__name\">");
        sb.Append(WebUtility.HtmlEncode(profile.FullName));
        sb.Append("</h2><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Lead"]));
        sb.Append("</p><nav class=\"prof-hub__jump\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Title"]));
        sb.Append("\"><a href=\"#prof-personal\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Section.Personal"]));
        sb.Append("</a><a href=\"#prof-preferences\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Section.Preferences"]));
        sb.Append("</a><a href=\"#prof-availability\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Section.Availability"]));
        sb.Append("</a><a href=\"#prof-experience\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Section.Experience"]));
        sb.Append("</a></nav><p class=\"prof-hub__paspoort\"><a class=\"btn btn-secondary\" href=\"/candidate/paspoort\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Hub.ViewPassport"]));
        sb.Append("</a></p></aside><div class=\"prof-hub__sections\">");

        AppendSectionCard(sb, culture, "prof-personal", culture["Profile.Section.Personal"], data.Personal,
            EditHref("personal"),
            BuildPersonalRows(culture, profile));
        AppendSectionCard(sb, culture, "prof-preferences", culture["Profile.Section.Preferences"], data.Preferences,
            EditHref("preferences"),
            BuildPreferencesRows(culture, profile));
        AppendSectionCard(sb, culture, "prof-availability", culture["Profile.Section.Availability"], data.Availability,
            EditHref("availability"),
            BuildAvailabilityRows(culture, profile));
        AppendSectionCard(sb, culture, "prof-experience", culture["Profile.Section.Experience"], data.Experience,
            EditHref("experience"),
            BuildExperienceRows(culture, profile));

        sb.Append("</div></div></div></div>");
        return sb.ToString();
    }

    private static void AppendIntro(StringBuilder sb, CultureState culture)
    {
        sb.Append("<header class=\"sollicitaties-intro prof-hub-intro\"><span class=\"sollicitaties-intro__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Profile"]));
        sb.Append("</span><h1 class=\"sollicitaties-intro__title\" tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Profile.Title"]));
        sb.Append("</h1></header>");
    }

    private static void AppendSectionCard(
        StringBuilder sb,
        CultureState culture,
        string id,
        string title,
        ProfileSsrData.SectionStatus status,
        string editHref,
        IReadOnlyList<(string Label, string Value)> rows)
    {
        sb.Append("<section class=\"prof-hub__card detail-card\" id=\"");
        sb.Append(WebUtility.HtmlEncode(id));
        sb.Append("\"><div class=\"prof-hub__card-head\"><h2>");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h2><span class=\"prof-hub__pill");
        if (status.Complete)
        {
            sb.Append(" prof-hub__pill--ok");
        }

        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(status.Complete
            ? culture["Profile.Hub.StatusComplete"]
            : culture["Passport.Fill"]));
        sb.Append("</span><a class=\"btn btn-ghost prof-hub__edit\" href=\"");
        sb.Append(WebUtility.HtmlEncode(editHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Edit"]));
        sb.Append("</a></div><dl class=\"prof-hub__kv\">");
        foreach (var (label, value) in rows)
        {
            sb.Append("<div><dt>");
            sb.Append(WebUtility.HtmlEncode(label));
            sb.Append("</dt><dd>");
            sb.Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "—" : value));
            sb.Append("</dd></div>");
        }

        sb.Append("</dl></section>");
    }

    private static string EditHref(string section)
        => $"/candidate/profile/bewerken?section={Uri.EscapeDataString(section)}";

    private static List<(string, string)> BuildPersonalRows(CultureState culture, MeProfile profile)
    {
        var rows = new List<(string, string)>
        {
            (culture["Profile.FirstName"], profile.FirstName ?? ""),
            (culture["Profile.LastName"], profile.LastName ?? ""),
            (culture["Profile.Phone"], profile.PhoneNumber ?? ""),
            (culture["Profile.HomeAddress"], profile.Preferences.HomeAddress ?? "")
        };
        if (profile.HasDateOfBirth && profile.DateOfBirth is { } dob)
        {
            rows.Add((culture["Profile.DateOfBirth"], dob.ToString("d MMMM yyyy", Dutch)));
        }

        return rows;
    }

    private static List<(string, string)> BuildPreferencesRows(CultureState culture, MeProfile profile)
    {
        var prefs = profile.Preferences;
        var roles = prefs.Roles.Count > 0 ? string.Join(", ", prefs.Roles) : "";
        var travel = prefs.MaxTravelMinutes is > 0
            ? prefs.MaxTravelMinutes.Value.ToString(CultureInfo.InvariantCulture)
            : "";
        return
        [
            (culture["Profile.Role"], roles),
            (culture["Profile.MaxTravel"], travel),
            (culture["Profile.PreferredTransport"], prefs.PreferredTransport ?? "")
        ];
    }

    private static List<(string, string)> BuildAvailabilityRows(CultureState culture, MeProfile profile)
    {
        var prefs = profile.Preferences;
        var from = profile.AvailableFromDate?.ToString("d MMMM yyyy", Dutch)
                   ?? (profile.OpenForWork ? culture["Passport.Facts.Available"] : "");
        var hours = prefs.MinHoursPerWeek is not null || prefs.MaxHoursPerWeek is not null
            ? $"{prefs.MinHoursPerWeek?.ToString(CultureInfo.InvariantCulture) ?? "?"}–{prefs.MaxHoursPerWeek?.ToString(CultureInfo.InvariantCulture) ?? "?"}"
            : "";
        return
        [
            (culture["Profile.AvailableFromDateLabel"], from),
            (culture["Passport.Facts.Hours"], hours)
        ];
    }

    private static List<(string, string)> BuildExperienceRows(CultureState culture, MeProfile profile)
    {
        var prefs = profile.Preferences;
        var cv = profile.UploadedCv?.FileName ?? "";
        var latest = prefs.Employers.FirstOrDefault()?.EmployerName ?? "";
        var edu = prefs.Educations.FirstOrDefault() ?? "";
        return
        [
            ("CV", cv),
            (culture["Profile.CurrentJob"], latest),
            (culture["Profile.Education"], edu)
        ];
    }
}
