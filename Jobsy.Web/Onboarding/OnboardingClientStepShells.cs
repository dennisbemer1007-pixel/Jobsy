using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

/// <summary>Static HTML for client onboarding steps (rendered once; JS attaches behavior).</summary>
internal static class OnboardingClientStepShells
{
    public static string Build(int step, CultureState culture, OnboardingProfileDraft? draft = null)
    {
        return step switch
        {
            1 => BuildStep1(culture, draft),
            2 => BuildStep2(culture, draft),
            3 => BuildStep3(culture),
            4 => BuildStep4(culture),
            5 => BuildStep5(culture),
            6 => BuildStep6(culture),
            7 when draft?.HasConsent == true => BuildMiniStep(culture),
            7 => BuildStep7ConsentAndMini(culture),
            >= 8 and <= 10 => BuildMiniStep(culture),
            _ => "",
        };
    }

    private static string BuildStep1(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var birthText = draft.BirthDate is { } d
            ? JobsyDateInputRules.FormatDisplayDate(d, culture.Language)
            : string.Empty;

        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        sb.Append("<div class=\"ob-wizard__field-row\">");
        sb.Append(LabelInput("onboarding-field-firstname", culture["Profile.FirstName"], "firstName", "given-name", draft.FirstName));
        sb.Append(LabelInput("onboarding-field-lastname", culture["Profile.LastName"], "lastName", "family-name", draft.LastName));
        sb.Append("</div>");
        sb.Append("<div id=\"onboarding-field-location\" class=\"ob-fields__address\">");
        sb.Append("<label>");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Address"]));
        sb.Append("<input id=\"onboarding-home-address\" type=\"text\" autocomplete=\"street-address\" data-ob-client=\"address\" value=\"");
        sb.Append(WebUtility.HtmlEncode(draft.AddressQuery ?? string.Empty));
        sb.Append("\" /></label>");
        sb.Append("<ul class=\"address-suggestions ob-client-suggest\" role=\"listbox\" hidden></ul>");
        sb.Append("</div>");
        sb.Append("<fieldset id=\"onboarding-field-birthdate\"><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.BirthDate"]));
        sb.Append("</legend><input data-ob-client=\"birthDate\" inputmode=\"numeric\" autocomplete=\"bday\" value=\"");
        sb.Append(WebUtility.HtmlEncode(birthText));
        sb.Append("\" /></fieldset>");
        sb.Append("<fieldset class=\"work-status-field\" id=\"onboarding-field-work-status\">");
        sb.Append("<legend class=\"ob-wizard__section-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["WorkStatus.Legend"]));
        sb.Append("</legend>");
        sb.Append("<p class=\"ob-wizard__hint ob-wizard__section-hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["WorkStatus.Hint"]));
        sb.Append("</p>");
        sb.Append("<div class=\"work-status-chips lobsy-choice-grid lobsy-choice-grid--desktop-3\" data-ob-client=\"workStatusGrid\" role=\"group\">");
        AppendWorkStatusChips(sb, culture, draft);
        sb.Append("</div>");
        sb.Append("</fieldset>");
        sb.Append(LabelInput(null, culture["Profile.Phone"], "phone", "tel", draft.Phone));
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep2(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var minH = (int)(draft.MinHours ?? 12);
        var maxH = (int)(draft.MaxHours ?? 40);
        var immediate = draft.Immediate;
        var fromText = draft.AvailableFrom is { } d
            ? JobsyDateInputRules.FormatDisplayDate(d, culture.Language)
            : string.Empty;
        var fromPlaceholder = JobsyDateInputRules.Placeholder(culture.Language);
        var selectedPresets = new HashSet<string>(draft.Presets, StringComparer.OrdinalIgnoreCase);
        var availability = new HashSet<string>(draft.Availability, StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated ob-wizard__presets\">");
        sb.Append("<div data-ob-client=\"presetGrid\" class=\"ob-wizard__presets\">");
        AppendPresetTiles(sb, culture, selectedPresets);
        sb.Append("</div>");
        sb.Append("<div class=\"hours-range\" data-ob-client=\"hoursRail\">");
        sb.Append("<label>");
        sb.Append(WebUtility.HtmlEncode(culture["Vacancy.HoursPerWeek"]));
        sb.Append(" <span data-ob-client=\"hoursLabel\">");
        sb.Append(minH);
        sb.Append(" – ");
        sb.Append(maxH);
        sb.Append("</span>");
        sb.Append("<input type=\"range\" min=\"1\" max=\"60\" data-ob-client=\"hoursMin\" value=\"");
        sb.Append(minH);
        sb.Append("\" />");
        sb.Append("<input type=\"range\" min=\"1\" max=\"60\" data-ob-client=\"hoursMax\" value=\"");
        sb.Append(maxH);
        sb.Append("\" />");
        sb.Append("</label></div>");
        sb.Append("<div class=\"availability-matrix availability-matrix--touch\" data-ob-client=\"matrix\">");
        AppendAvailabilityMatrix(sb, culture, availability);
        sb.Append("</div>");
        sb.Append("<fieldset><legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailabilityStartSummary"]));
        sb.Append("</legend>");
        sb.Append("<label><input type=\"radio\" name=\"ob-start\" data-ob-client=\"startImmediate\"");
        if (immediate)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailableImmediate"]));
        sb.Append("</label>");
        sb.Append("<label><input type=\"radio\" name=\"ob-start\" data-ob-client=\"startFromDate\"");
        if (!immediate)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailableFrom"]));
        sb.Append(" <input id=\"onboarding-available-from\" data-ob-client=\"availableFrom\" type=\"text\" inputmode=\"numeric\" autocomplete=\"off\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(fromPlaceholder));
        sb.Append('"');
        if (immediate)
        {
            sb.Append(" hidden");
        }

        sb.Append(" value=\"");
        sb.Append(WebUtility.HtmlEncode(fromText));
        sb.Append("\" /></label>");
        sb.Append("</fieldset>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep3(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-transport\">");
        sb.Append("<div data-ob-client=\"transportGrid\" class=\"ob-wizard__transport\"></div>");
        sb.Append("<div data-ob-client=\"travelGrid\" class=\"ob-wizard__travel-times\"></div>");
        sb.Append("<label>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Other"]));
        sb.Append("<input type=\"number\" min=\"1\" max=\"180\" data-ob-client=\"customTravel\" hidden />");
        sb.Append("</label>");
        AppendStepNav(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep4(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        sb.Append("<label class=\"ob-wizard__check\"><input type=\"checkbox\" data-ob-client=\"noWork\" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.NoWorkExperience"]));
        sb.Append("</label><div data-ob-client=\"jobsList\"></div>");
        sb.Append("<button type=\"button\" class=\"btn btn-secondary\" data-ob-client=\"addJob\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AddJob"]));
        sb.Append("</button>");
        AppendStepNav(sb, culture, optionalSkip: true);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep5(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        sb.Append("<div data-ob-client=\"educationGrid\" class=\"ob-wizard__education-grid\"></div>");
        AppendStepNav(sb, culture, optionalSkip: true);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep6(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-dream\">");
        sb.Append("<div data-ob-client=\"dreamGrid\" class=\"ob-rustig__dream-grid\"></div>");
        sb.Append("<input id=\"onboarding-rustig-dream-input\" data-ob-client=\"dreamInput\" type=\"text\" autocomplete=\"off\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.DreamSearch"]));
        sb.Append("\" />");
        AppendStepNav(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep7ConsentAndMini(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-consent\">");
        sb.Append("<label class=\"ob-wizard__consent\"><input type=\"checkbox\" data-ob-client=\"consent\" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Consent.Check"]));
        sb.Append("</label>");
        sb.Append("<div class=\"ob-client-mini\" data-ob-client=\"miniRoot\"></div>");
        AppendStepNav(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildMiniStep(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        sb.Append("<div class=\"ob-client-mini\" data-ob-client=\"miniRoot\"></div>");
        AppendStepNav(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendPresetTiles(StringBuilder sb, CultureState culture, HashSet<string> selected)
    {
        foreach (var preset in OnboardingPresetCatalog.Items)
        {
            var on = selected.Contains(preset.Code);
            sb.Append("<button type=\"button\" class=\"lobsy-choice-tile");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" data-ob-code=\"");
            sb.Append(WebUtility.HtmlEncode(preset.Code));
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\"><strong>");
            sb.Append(WebUtility.HtmlEncode(culture[preset.TitleKey]));
            sb.Append("</strong><small>");
            sb.Append(WebUtility.HtmlEncode(culture[preset.LeadKey]));
            sb.Append("</small></button>");
        }
    }

    private static void AppendAvailabilityMatrix(StringBuilder sb, CultureState culture, HashSet<string> selected)
    {
        var days = AvailabilityDayPartMatrixExport.Days;
        var slots = AvailabilityDayPartMatrixExport.Slots;
        sb.Append("<div class=\"availability-matrix__corner\"></div>");
        foreach (var slot in slots)
        {
            sb.Append("<div class=\"availability-matrix__slot\">");
            sb.Append(WebUtility.HtmlEncode(UiLabels.AvailabilitySlot(culture, slot)));
            sb.Append("</div>");
        }

        foreach (var day in days)
        {
            sb.Append("<div class=\"availability-matrix__day\">");
            sb.Append(WebUtility.HtmlEncode(UiLabels.Weekday(culture, day)));
            sb.Append("</div>");
            foreach (var slot in slots)
            {
                var key = day + ":" + slot;
                var on = selected.Contains(key);
                sb.Append("<button type=\"button\" class=\"availability-matrix__cell availability-matrix__cell--btn");
                if (on)
                {
                    sb.Append(" is-on");
                }

                sb.Append("\" data-ob-key=\"");
                sb.Append(WebUtility.HtmlEncode(key));
                sb.Append("\" aria-pressed=\"");
                sb.Append(on ? "true" : "false");
                sb.Append("\"></button>");
            }
        }
    }

    private static void AppendWorkStatusChips(StringBuilder sb, CultureState culture, OnboardingProfileDraft draft)
    {
        var selected = new HashSet<string>(draft.WorkStatuses, StringComparer.OrdinalIgnoreCase);
        foreach (var code in WorkStatusCatalog.AllCodes)
        {
            var on = selected.Contains(code);
            var label = culture[WorkStatusCatalog.LabelKey(code)];
            var icon = ChoiceTileIcons.ForWorkStatus(code);
            sb.Append("<button type=\"button\" class=\"lobsy-choice-tile work-status-chip");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" data-ob-code=\"");
            sb.Append(WebUtility.HtmlEncode(code));
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\">");
            if (!string.IsNullOrEmpty(icon))
            {
                sb.Append("<span class=\"lobsy-choice-tile__icon\" aria-hidden=\"true\">");
                sb.Append(icon);
                sb.Append("</span>");
            }

            sb.Append("<span class=\"lobsy-choice-tile__label\">");
            sb.Append(WebUtility.HtmlEncode(label));
            sb.Append("</span>");
            if (on)
            {
                sb.Append("<span class=\"lobsy-choice-tile__check\" aria-hidden=\"true\">✓</span>");
            }

            sb.Append("</button>");
        }
    }

    private static void AppendStepNav(StringBuilder sb, CultureState culture, bool optionalSkip = false, bool plainJsNav = false)
    {
        sb.Append("<div class=\"ob-step-actions\">");
        if (optionalSkip)
        {
            sb.Append("<button type=\"button\" class=\"ob-rustig__btn ob-step-skip\" data-ob-step-skip data-testid=\"onboarding-skip-optional\">");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Skip"]));
            sb.Append("</button>");
        }

        sb.Append("<button type=\"button\" class=\"ob-rustig__btn ob-rustig__btn--primary ob-step-next\" data-ob-step-next");
        if (plainJsNav)
        {
            sb.Append(" data-ob-nav-next");
        }

        sb.Append(" data-testid=\"onboarding-next\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Next"]));
        sb.Append("</button>");
        sb.Append("<p class=\"state-message error ob-step-next-error\" data-ob-step-error hidden aria-live=\"polite\"></p>");
        sb.Append("</div>");
    }

    private static string LabelInput(string? wrapId, string label, string clientKey, string autocomplete, string? value = null)
    {
        var idAttr = wrapId is null ? "" : $" id=\"{wrapId}\"";
        var valueAttr = string.IsNullOrEmpty(value)
            ? string.Empty
            : $" value=\"{WebUtility.HtmlEncode(value)}\"";
        return $"""
<label{idAttr}>{WebUtility.HtmlEncode(label)}<input autocomplete="{autocomplete}" maxlength="128" data-ob-client="{clientKey}"{valueAttr} /></label>
""";
    }
}
