using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Onboarding;

/// <summary>Static HTML for client onboarding steps (rendered once; JS attaches behavior).</summary>
internal static class OnboardingClientStepShells
{
    public static string Build(
        int step,
        CultureState culture,
        OnboardingProfileDraft? draft = null,
        IReadOnlyList<string>? dreamTitles = null,
        OnboardingClientStepSeed? seed = null)
    {
        return step switch
        {
            1 => BuildStep1(culture, draft),
            2 => BuildStep2(culture, draft),
            3 => BuildStep3(culture, draft),
            4 => BuildStep4(culture, draft),
            5 => BuildStep5(culture, draft),
            6 => BuildStep6(culture, draft, dreamTitles),
            7 => BuildStep7Ssr(culture, draft, seed),
            8 => BuildStep8Ssr(seed),
            9 => BuildStep9Ssr(seed),
            10 => BuildStep10Ssr(seed),
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

    private static string BuildStep3(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var selectedTransport = new HashSet<string>(TransportLabels.SplitMany(draft.Transport), StringComparer.OrdinalIgnoreCase);
        var maxTravel = draft.MaxTravel > 0 ? draft.MaxTravel : 30;
        var travelPresets = new[] { 10, 20, 30, 45 };
        var customTravel = draft.CustomTravel || !travelPresets.Contains(maxTravel);

        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-transport\">");
        sb.Append("<div class=\"ob-wizard__section-title ob-illustrated-step__field-label\" id=\"onboarding-transport-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Transport"]));
        sb.Append("</div>");
        sb.Append("<fieldset aria-labelledby=\"onboarding-transport-label\">");
        sb.Append("<legend class=\"visually-hidden\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Transport"]));
        sb.Append("</legend>");
        sb.Append("<p class=\"ob-wizard__hint ob-wizard__section-hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.TransportMulti"]));
        sb.Append("</p>");
        sb.Append("<div data-ob-client=\"transportGrid\" class=\"ob-wizard__transport lobsy-choice-grid\">");
        AppendTransportTiles(sb, culture, selectedTransport);
        sb.Append("</div></fieldset>");
        sb.Append("<fieldset>");
        sb.Append("<legend>");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.MaxTravel"]));
        sb.Append(" <strong data-ob-client=\"travelLegendValue\">");
        sb.Append(maxTravel);
        sb.Append("</strong> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Minutes"]));
        sb.Append("</legend>");
        sb.Append("<div data-ob-client=\"travelGrid\" class=\"ob-wizard__transport ob-wizard__travel-times lobsy-choice-grid\">");
        foreach (var minutes in travelPresets)
        {
            var on = !customTravel && maxTravel == minutes;
            sb.Append("<button type=\"button\" class=\"lobsy-choice-tile");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" data-ob-travel=\"");
            sb.Append(minutes);
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\">");
            sb.Append(minutes);
            sb.Append("</button>");
        }

        sb.Append("</div>");
        sb.Append("<button type=\"button\" class=\"ob-wizard__inline-link\" data-ob-client=\"showCustomTravel\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Other"]));
        sb.Append("</button>");
        sb.Append("<label class=\"ob-wizard__custom-travel\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Other"]));
        sb.Append(" <input type=\"number\" min=\"1\" max=\"180\" data-ob-client=\"customTravel\"");
        if (!customTravel)
        {
            sb.Append(" hidden");
        }

        sb.Append(" value=\"");
        sb.Append(maxTravel);
        sb.Append("\" /></label></fieldset></div>");
        return sb.ToString();
    }

    private static string BuildStep4(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        sb.Append("<label class=\"ob-wizard__check\"><input type=\"checkbox\" data-ob-client=\"noWork\"");
        if (draft.NoWork)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.NoWorkExperience"]));
        sb.Append("</label><div data-ob-client=\"jobsList\">");
        if (!draft.NoWork)
        {
            var jobs = draft.Jobs.Count > 0 ? draft.Jobs : [new CandidateEmployerHistory()];
            for (var i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                sb.Append("<div class=\"ob-client-job ob-wizard__job ob-job-card\" data-job-index=\"");
                sb.Append(i);
                sb.Append("\"><label>");
                sb.Append(WebUtility.HtmlEncode(culture["Profile.Employer"]));
                sb.Append("<input type=\"text\" data-field=\"employer\" data-ob-client=\"jobEmployer\" value=\"");
                sb.Append(WebUtility.HtmlEncode(job.EmployerName ?? string.Empty));
                sb.Append("\" autocomplete=\"organization\" /></label></div>");
            }
        }

        sb.Append("</div>");
        sb.Append("<button type=\"button\" class=\"ob-wizard__add-job\" data-ob-client=\"addJob\" data-testid=\"onboarding-add-job\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AddJob"]));
        sb.Append("</button></div>");
        return sb.ToString();
    }

    private static string BuildStep5(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var selected = string.IsNullOrWhiteSpace(draft.Education)
            ? null
            : draft.Education.Trim();
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-education\">");
        sb.Append("<div class=\"ob-wizard__section-title ob-illustrated-step__field-label\" id=\"onboarding-education-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.EducationPickLevel"]));
        sb.Append("</div>");
        sb.Append("<div data-ob-client=\"educationGrid\" class=\"ob-wizard__education-grid lobsy-choice-grid\" role=\"radiogroup\" aria-labelledby=\"onboarding-education-label\">");
        AppendEducationTiles(sb, culture, selected);
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string BuildStep6(CultureState culture, OnboardingProfileDraft? draft, IReadOnlyList<string>? dreamTitles)
    {
        draft ??= new OnboardingProfileDraft();
        var activeDream = string.IsNullOrWhiteSpace(draft.DreamSearch) ? draft.Dream : draft.DreamSearch;
        activeDream = activeDream?.Trim() ?? "";
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-dream\">");
        sb.Append("<div class=\"ob-rustig__dream-grid lobsy-choice-grid\" data-ob-ssr-dream-grid role=\"listbox\">");
        foreach (var title in dreamTitles ?? [])
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var trimmed = title.Trim();
            var on = !string.IsNullOrEmpty(activeDream)
                && string.Equals(trimmed, activeDream, StringComparison.OrdinalIgnoreCase);
            sb.Append("<button type=\"button\" class=\"ob-rustig__dream lobsy-choice-tile");
            if (on)
            {
                sb.Append(" is-on is-selected");
            }

            sb.Append("\" data-ob-dream-title=\"");
            sb.Append(WebUtility.HtmlEncode(trimmed));
            sb.Append("\" data-testid=\"onboarding-dream-tile\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(trimmed));
            sb.Append("</button>");
        }

        sb.Append("</div>");
        sb.Append("<div class=\"ob-rustig__field ob-rustig__field--dream\" style=\"margin-top:1rem\">");
        sb.Append("<label for=\"onboarding-rustig-dream-input\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.DreamOwnLabel"]));
        sb.Append("</label>");
        sb.Append("<input id=\"onboarding-rustig-dream-input\" name=\"dream\" type=\"text\" autocomplete=\"off\" data-testid=\"onboarding-dream-input\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.DreamSearch"]));
        sb.Append("\" value=\"");
        sb.Append(WebUtility.HtmlEncode(activeDream));
        sb.Append("\" /></div></div>");
        return sb.ToString();
    }

    private static string BuildStep7Ssr(CultureState culture, OnboardingProfileDraft? draft, OnboardingClientStepSeed? seed)
    {
        draft ??= new OnboardingProfileDraft();
        seed ??= new OnboardingClientStepSeed();
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-consent\">");
        if (!draft.HasConsent)
        {
            var consentOn = draft.ConsentRequested;
            sb.Append("<label class=\"ob-wizard__consent\"><input type=\"checkbox\" name=\"consentRequested\" value=\"true\" data-testid=\"onboarding-consent\"");
            if (consentOn)
            {
                sb.Append(" checked");
            }

            sb.Append(" /> ");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Consent.Check"]));
            sb.Append("</label>");
        }

        AppendMiniQuestionsSsr(sb, seed.MiniQuestions, seed.MiniAnswers, "onboarding-step-7-mini");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep8Ssr(OnboardingClientStepSeed? seed)
        => BuildMiniBlockSsr(seed, "onboarding-step-8-mini");

    private static string BuildStep9Ssr(OnboardingClientStepSeed? seed)
        => BuildMiniBlockSsr(seed, "onboarding-step-9-mini");

    private static string BuildStep10Ssr(OnboardingClientStepSeed? seed)
        => BuildMiniBlockSsr(seed, "onboarding-step-10-mini");

    private static string BuildMiniBlockSsr(OnboardingClientStepSeed? seed, string miniTestId)
    {
        seed ??= new OnboardingClientStepSeed();
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        AppendMiniQuestionsSsr(sb, seed.MiniQuestions, seed.MiniAnswers, miniTestId);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendMiniQuestionsSsr(
        StringBuilder sb,
        IReadOnlyList<OnboardingClientStepSeed.MiniQuestionSeed> questions,
        IReadOnlyDictionary<int, int> answers,
        string miniTestId)
    {
        sb.Append("<div class=\"ob-client-mini\" data-ob-ssr-mini data-testid=\"");
        sb.Append(WebUtility.HtmlEncode(miniTestId));
        sb.Append("\">");
        foreach (var question in questions)
        {
            answers.TryGetValue(question.Id, out var selected);
            sb.Append("<div class=\"ob-client-mini-q\" data-ob-qid=\"");
            sb.Append(question.Id);
            sb.Append("\"><p>");
            sb.Append(WebUtility.HtmlEncode(question.Text));
            sb.Append("</p><div class=\"ob-client-mini-likert lobsy-choice-grid\" role=\"group\">");
            for (var score = 1; score <= 5; score++)
            {
                var on = selected == score;
                sb.Append("<button type=\"button\" class=\"lobsy-choice-tile");
                if (on)
                {
                    sb.Append(" is-selected");
                }

                sb.Append("\" data-ob-mini-qid=\"");
                sb.Append(question.Id);
                sb.Append("\" data-ob-mini-score=\"");
                sb.Append(score);
                sb.Append("\" data-testid=\"onboarding-mini-likert\" aria-pressed=\"");
                sb.Append(on ? "true" : "false");
                sb.Append("\">");
                sb.Append(score);
                sb.Append("</button>");
            }

            sb.Append("</div>");
            if (selected is >= 1 and <= 5)
            {
                sb.Append("<input type=\"hidden\" name=\"miniAnswers[");
                sb.Append(question.Id);
                sb.Append("]\" value=\"");
                sb.Append(selected);
                sb.Append("\" />");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
    }

    private static void AppendEducationTiles(StringBuilder sb, CultureState culture, string? selected)
    {
        foreach (var level in OnboardingWizardCatalog.EducationRadioLevels)
        {
            var on = !string.IsNullOrEmpty(selected)
                && string.Equals(level, selected, StringComparison.OrdinalIgnoreCase);
            var label = level == EducationLevelLabels.None
                ? culture["Education.None"]
                : level;
            sb.Append("<button type=\"button\" class=\"lobsy-choice-tile");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" data-ob-code=\"");
            sb.Append(WebUtility.HtmlEncode(level));
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\" role=\"radio\">");
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

    private static void AppendTransportTiles(StringBuilder sb, CultureState culture, HashSet<string> selected)
    {
        foreach (var code in TransportLabels.Selectable)
        {
            var on = selected.Contains(code);
            var label = TransportTileLabel(culture, code);
            var icon = ChoiceTileIcons.ForTransport(code);
            sb.Append("<button type=\"button\" class=\"lobsy-choice-tile");
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

    private static string TransportTileLabel(CultureState culture, string code) => code switch
    {
        TransportLabels.Bike => culture["Transport.Bike"],
        TransportLabels.EBike => culture["Transport.EBike"],
        TransportLabels.Car => culture["Transport.Car"],
        TransportLabels.PublicTransport => culture["Transport.Transit"],
        TransportLabels.Walking => culture["Transport.Walk"],
        _ => code,
    };

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
