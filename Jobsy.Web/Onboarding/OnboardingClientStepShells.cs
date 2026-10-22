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
    private static readonly int[] TravelPresetMinutes = [15, 30, 45, 60];

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
            8 => BuildStep8Ssr(culture, seed),
            9 => BuildStep9Ssr(culture, seed),
            10 => BuildStep10Ssr(culture, seed),
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
        var immediate = draft.Immediate;
        var fromIso = draft.AvailableFrom is { } d ? d.ToString("yyyy-MM-dd") : string.Empty;
        var selectedPresets = new HashSet<string>(draft.Presets, StringComparer.OrdinalIgnoreCase);
        var availability = draft.Availability;
        const string radioLabelStyle =
            "display:flex;flex-direction:row;justify-content:flex-start;align-items:center;gap:8px;width:auto;text-align:left;font-weight:600";
        const string radioInputStyle = "width:18px;height:18px;margin:0;flex:0 0 auto";
        const string saveBtnStyle =
            "white-space:nowrap;padding:0 20px;min-width:max-content;text-align:center;height:44px;min-height:44px;max-height:44px;display:inline-flex;align-items:center;justify-content:center;line-height:1;flex:0 0 auto;border-radius:8px;border:1px solid var(--brand);background:var(--brand);color:#fff;font:inherit;font-weight:600;cursor:pointer;box-sizing:border-box;overflow:hidden";
        const string step2StackStyle =
            "display:flex;flex-direction:column;align-items:stretch;width:100%;max-width:none;box-sizing:border-box;gap:12px";
        const string step2GridStyle =
            "display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;width:100%;max-width:none;box-sizing:border-box";

        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated ob-wizard__presets ob-rustig__step2-card\" style=\"");
        sb.Append(step2StackStyle);
        sb.Append("\">");
        sb.Append("<div class=\"ob-wizard__section-title\" id=\"ob-presets\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailabilityPresetsLegend"]));
        sb.Append("</div>");
        sb.Append("<div data-ob-client=\"presetGrid\" class=\"ob-wizard__presets lobsy-choice-grid ob-rustig__step2-presets\" style=\"");
        sb.Append(step2GridStyle);
        sb.Append("\">");
        AppendPresetTiles(sb, culture, selectedPresets);
        sb.Append("</div>");
        sb.Append("<div class=\"ob-wizard__section-title ob-rustig__step2-matrix-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Preset.Custom.GridLegend"]));
        sb.Append("</div>");
        sb.Append("<p class=\"ob-wizard__hint ob-wizard__section-hint ob-rustig__step2-matrix-hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Preset.Custom.GridHint"]));
        sb.Append("</p>");
        sb.Append("<div id=\"ob-matrix\" class=\"availability-matrix availability-matrix--touch ob-rustig__step2-matrix\" data-ob-client=\"matrix\" style=\"width:100%;max-width:none;box-sizing:border-box\">");
        AppendAvailabilityMatrix(sb, culture, availability);
        sb.Append("</div>");
        sb.Append("<fieldset id=\"ob-start\" class=\"ob-rustig__step2-start\" style=\"width:100%;max-width:none;border:0;padding:0;margin:0 0 24px 0;box-sizing:border-box\"><legend class=\"ob-wizard__section-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailabilityStartSummary"]));
        sb.Append("</legend>");
        sb.Append("<div style=\"display:flex;flex-direction:column;gap:10px;width:100%\">");
        sb.Append("<label style=\"");
        sb.Append(radioLabelStyle);
        sb.Append("\"><input type=\"radio\" name=\"obStartMode\" value=\"now\" style=\"");
        sb.Append(radioInputStyle);
        sb.Append("\" data-testid=\"onboarding-start-now\"");
        if (immediate)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailableImmediate"]));
        sb.Append("</label>");
        sb.Append("<div style=\"display:flex;flex-direction:column;align-items:stretch;gap:8px;width:100%;max-width:100%;box-sizing:border-box\">");
        sb.Append("<label style=\"");
        sb.Append(radioLabelStyle);
        sb.Append("\"><input type=\"radio\" name=\"obStartMode\" value=\"from\" style=\"");
        sb.Append(radioInputStyle);
        sb.Append("\" data-testid=\"onboarding-start-from\"");
        if (!immediate)
        {
            sb.Append(" checked");
        }

        sb.Append(" /> ");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.AvailableFrom"]));
        sb.Append("</label>");
        sb.Append("<div style=\"display:grid;grid-template-columns:minmax(0,1fr) max-content;align-items:center;gap:8px;width:100%;max-width:100%;box-sizing:border-box\">");
        sb.Append("<input id=\"onboarding-available-from\" name=\"obAvailableFrom\" type=\"date\" autocomplete=\"off\" value=\"");
        sb.Append(WebUtility.HtmlEncode(fromIso));
        sb.Append("\" style=\"height:44px;min-height:44px;flex:1 1 auto;min-width:0;box-sizing:border-box\" data-testid=\"onboarding-available-from\" onkeydown=\"if(event.key==='Enter'){event.preventDefault();var b=this.form.querySelector('[data-testid=&quot;onboarding-start-save&quot;]');if(b)b.click();}\" />");
        sb.Append("<button type=\"submit\" name=\"obAction\" value=\"saveStart\" style=\"");
        sb.Append(saveBtnStyle);
        sb.Append("\" data-testid=\"onboarding-start-save\">");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
        sb.Append("</button></div></div></div></fieldset>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep3(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var selectedTransport = new HashSet<string>(TransportLabels.SplitMany(draft.Transport), StringComparer.OrdinalIgnoreCase);
        var maxTravel = draft.MaxTravel > 0 ? draft.MaxTravel : 30;
        var travelPresets = TravelPresetMinutes;
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
            sb.Append("<button type=\"submit\" name=\"obTravel\" class=\"lobsy-choice-tile");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" value=\"");
            sb.Append(minutes);
            sb.Append("\" data-ob-travel=\"");
            sb.Append(minutes);
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\">");
            sb.Append(minutes);
            sb.Append(' ');
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Minutes"]));
            sb.Append("</button>");
        }

        sb.Append("</div></fieldset></div>");
        return sb.ToString();
    }

    private static string BuildStep4(CultureState culture, OnboardingProfileDraft? draft)
    {
        draft ??= new OnboardingProfileDraft();
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        sb.Append("<label class=\"ob-wizard__check\" style=\"display:flex;flex-direction:row;justify-content:flex-start;align-items:center;gap:8px;width:auto;text-align:left\"><input type=\"checkbox\" data-ob-client=\"noWork\" style=\"width:auto;margin:0;flex:0 0 auto\" data-testid=\"onboarding-no-work\"");
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
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-education\" style=\"width:100%;max-width:none;box-sizing:border-box\">");
        sb.Append("<div class=\"ob-wizard__section-title ob-illustrated-step__field-label\" id=\"onboarding-education-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.EducationPickLevel"]));
        sb.Append("</div>");
        sb.Append("<div data-ob-client=\"educationGrid\" class=\"ob-wizard__education-grid lobsy-choice-grid\" role=\"radiogroup\" aria-labelledby=\"onboarding-education-label\" style=\"display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;width:100%;max-width:none;box-sizing:border-box\">");
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
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\" id=\"onboarding-field-dream\" style=\"width:100%;max-width:none;box-sizing:border-box\">");
        sb.Append("<div class=\"ob-rustig__dream-grid\" data-ob-ssr-dream-grid role=\"listbox\" style=\"display:flex;flex-wrap:wrap;gap:8px;width:100%;max-width:100%;box-sizing:border-box\">");
        foreach (var title in dreamTitles ?? [])
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var trimmed = title.Trim();
            var on = !string.IsNullOrEmpty(activeDream)
                && string.Equals(trimmed, activeDream, StringComparison.OrdinalIgnoreCase);
            sb.Append("<button type=\"button\" class=\"ob-rustig__dream");
            if (on)
            {
                sb.Append(" is-on is-selected");
            }

            sb.Append("\" style=\"width:auto;display:inline-flex;align-items:center;justify-content:center;flex:0 0 auto;max-width:none;min-height:48px;padding:10px 14px;border-radius:8px;border:1px solid var(--border);background:var(--surface);font:inherit;font-weight:600;text-align:center;box-sizing:border-box");
            if (on)
            {
                sb.Append(";border-color:var(--brand);background:color-mix(in srgb, var(--brand) 12%, var(--surface));color:var(--brand)");
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
        sb.Append("<div class=\"ob-rustig__field ob-rustig__field--dream\" style=\"margin-top:1rem;width:100%;max-width:none;box-sizing:border-box\">");
        sb.Append("<label for=\"onboarding-rustig-dream-input\">");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Rustig.DreamOwnLabel"]));
        sb.Append("</label>");
        sb.Append("<input id=\"onboarding-rustig-dream-input\" name=\"dream\" type=\"text\" autocomplete=\"off\" data-testid=\"onboarding-dream-input\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Onboarding.DreamSearch"]));
        sb.Append("\" value=\"");
        sb.Append(WebUtility.HtmlEncode(activeDream));
        sb.Append("\" style=\"width:100%;min-height:44px;box-sizing:border-box\" /></div></div>");
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

        AppendMiniQuestionsSsr(sb, culture, 7, seed.MiniQuestions, seed.MiniAnswers, "onboarding-step-7-mini", seed.MiniActiveQuestion);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildStep8Ssr(CultureState culture, OnboardingClientStepSeed? seed)
        => BuildMiniBlockSsr(culture, 8, seed, "onboarding-step-8-mini");

    private static string BuildStep9Ssr(CultureState culture, OnboardingClientStepSeed? seed)
        => BuildMiniBlockSsr(culture, 9, seed, "onboarding-step-9-mini");

    private static string BuildStep10Ssr(CultureState culture, OnboardingClientStepSeed? seed)
        => BuildMiniBlockSsr(culture, 10, seed, "onboarding-step-10-mini");

    /// <summary>Mini test block for De ontdekkingsreis SSR (steps 7–10).</summary>
    public static string BuildDiscoveryMiniBlock(
        CultureState culture,
        int step,
        OnboardingClientStepSeed seed,
        int activeQuestion1Based,
        string titleKey,
        string hintKey)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"journey-body\">");
        AppendMiniQuestionsSsr(sb, culture, step, seed.MiniQuestions, seed.MiniAnswers, $"discovery-step-{step}-mini", activeQuestion1Based);
        sb.Append("</div>");
        _ = titleKey;
        _ = hintKey;
        return sb.ToString();
    }

    private static string BuildMiniBlockSsr(CultureState culture, int wizardStep, OnboardingClientStepSeed? seed, string miniTestId)
    {
        seed ??= new OnboardingClientStepSeed();
        var sb = new StringBuilder();
        sb.Append("<div class=\"ob-wizard__card ob-wizard__card--illustrated\">");
        AppendMiniQuestionsSsr(sb, culture, wizardStep, seed.MiniQuestions, seed.MiniAnswers, miniTestId, seed.MiniActiveQuestion);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static readonly (int Step, string TitleKey)[] MiniWizardTabs =
    [
        (7, "Onboarding.Test.Competency"),
        (8, "Onboarding.Test.Career"),
        (9, "Onboarding.Test.Culture"),
        (10, "Onboarding.Test.Values"),
    ];

    private static void AppendMiniQuestionsSsr(
        StringBuilder sb,
        CultureState culture,
        int wizardStep,
        IReadOnlyList<OnboardingClientStepSeed.MiniQuestionSeed> questions,
        IReadOnlyDictionary<int, int> answers,
        string miniTestId,
        int activeQuestion1Based)
    {
        const string tabRowStyle =
            "display:flex;flex-wrap:wrap;gap:0.45rem;margin:0 0 0.85rem;width:100%;box-sizing:border-box";
        const string tabBaseStyle =
            "padding:0.35rem 0.75rem;border-radius:999px;font-size:0.78rem;font-weight:600;border:1px solid color-mix(in srgb,var(--border) 80%,var(--brand) 20%);color:var(--muted);background:var(--surface);box-sizing:border-box;cursor:default;pointer-events:none;user-select:none";
        const string tabDoneStyle =
            ";border-color:color-mix(in srgb,var(--success,#2d6a4f) 35%,var(--border));color:var(--success,#2d6a4f)";
        const string tabCurrentStyle = ";background:var(--brand);border-color:var(--brand);color:#fff";
        const string leadStyle =
            "font-size:0.8125rem;color:var(--muted);margin:0 0 0.75rem;line-height:1.35";
        const string counterStyle =
            "font-size:0.8125rem;color:var(--muted);margin:0 0 0.35rem;font-weight:600";
        const string statementStyle =
            "font-family:var(--font-display,Georgia,serif);font-size:clamp(1.25rem,2.2vw,1.55rem);line-height:1.25;color:var(--brand);border:none;padding:0;margin:0 0 1rem;overflow-wrap:break-word;hyphens:none";
        const string scaleLabelsStyle =
            "display:flex;justify-content:space-between;margin:0 0 0.35rem;color:var(--muted);font-size:0.85rem;width:100%";
        const string likertRowStyle =
            "display:flex;flex-wrap:nowrap;gap:8px;width:100%;max-width:100%;box-sizing:border-box;margin:0 0 1.5rem";
        const string likertBtnStyle =
            "flex:1 1 0;min-width:0;min-height:56px;display:inline-flex;align-items:center;justify-content:center;border:2px solid var(--pearl,#f7f4f0);border-radius:8px;background:var(--surface);font:inherit;font-weight:600;cursor:pointer;box-sizing:border-box;width:100%";
        const string likertBtnOnStyle =
            ";border-color:var(--brand);background:var(--accent-soft);color:var(--brand-deep,var(--brand))";

        sb.Append("<div class=\"ob-client-mini\" data-testid=\"");
        sb.Append(WebUtility.HtmlEncode(miniTestId));
        sb.Append("\" style=\"width:100%;max-width:min(52rem,100%);box-sizing:border-box\">");
        if (wizardStep is >= 7 and <= 10)
        {
            sb.Append("<div class=\"ob-rustig__test-tabs\" style=\"");
            sb.Append(tabRowStyle);
            sb.Append("\">");
            foreach (var (step, titleKey) in MiniWizardTabs)
            {
                var isCurrent = step == wizardStep;
                var isDone = step < wizardStep;
                sb.Append("<span class=\"ob-rustig__test-tab");
                if (isDone)
                {
                    sb.Append(" is-done");
                }

                if (isCurrent)
                {
                    sb.Append(" is-current");
                }

                sb.Append("\" style=\"");
                sb.Append(tabBaseStyle);
                if (isDone)
                {
                    sb.Append(tabDoneStyle);
                }

                if (isCurrent)
                {
                    sb.Append(tabCurrentStyle);
                }

                sb.Append("\">");
                if (isDone)
                {
                    sb.Append("✓ ");
                }

                sb.Append(WebUtility.HtmlEncode(culture[titleKey]));
                sb.Append("</span>");
            }

            sb.Append("</div>");
            sb.Append("<p class=\"test-flow__lead\" style=\"");
            sb.Append(leadStyle);
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.Test.Lead"]));
            sb.Append("</p>");
        }

        var total = questions.Count;
        if (total == 0)
        {
            sb.Append("</div>");
            return;
        }

        var activeIndex = Math.Clamp(activeQuestion1Based - 1, 0, total - 1);

        var question = questions[activeIndex];
        answers.TryGetValue(question.Id, out var selected);
        var q1 = activeIndex + 1;

        sb.Append("<input type=\"hidden\" name=\"obMiniQ\" value=\"");
        sb.Append(q1);
        sb.Append("\" />");

        sb.Append("<div class=\"ob-client-mini-q\" data-ob-qid=\"");
        sb.Append(question.Id);
        sb.Append("\" style=\"width:100%;box-sizing:border-box\">");
        sb.Append("<p class=\"test-flow__counter\" style=\"");
        sb.Append(counterStyle);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("TestFlow.QuestionOf", q1, total)));
        sb.Append("</p>");
        sb.Append("<div class=\"test-flow__bar\" role=\"group\" aria-hidden=\"true\" style=\"display:flex;gap:4px;margin:0 0 0.75rem;width:100%;max-width:100%\">");
        for (var seg = 0; seg < total; seg++)
        {
            var segQuestion = questions[seg];
            answers.TryGetValue(segQuestion.Id, out var segScore);
            var segAnswered = segScore is >= 1 and <= 5;
            var segCurrent = seg == activeIndex;
            var segStyle = segAnswered
                ? "background:var(--brand)"
                : segCurrent
                    ? "background:color-mix(in srgb,var(--brand) 45%,var(--border))"
                    : "background:color-mix(in srgb,var(--border) 70%,transparent)";
            sb.Append("<span style=\"flex:1 1 0;height:4px;border-radius:2px;");
            sb.Append(segStyle);
            sb.Append("\"></span>");
        }

        sb.Append("</div>");

        sb.Append("<blockquote class=\"test-flow__statement\" style=\"");
        sb.Append(statementStyle);
        sb.Append("\">“");
        sb.Append(WebUtility.HtmlEncode(question.Text));
        sb.Append("”</blockquote>");
        sb.Append("<div class=\"test-likert__labels\" style=\"");
        sb.Append(scaleLabelsStyle);
        sb.Append("\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["TestFlow.Scale.Low"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["TestFlow.Scale.High"]));
        sb.Append("</span></div>");
        sb.Append("<div class=\"ob-client-mini-likert\" role=\"group\" style=\"");
        sb.Append(likertRowStyle);
        sb.Append("\">");
        for (var score = 1; score <= 5; score++)
        {
            var on = selected == score;
            sb.Append("<button type=\"submit\" name=\"obMiniPick\" value=\"");
            sb.Append(question.Id);
            sb.Append(':');
            sb.Append(score);
            sb.Append('"');
            if (on)
            {
                sb.Append(" class=\"is-selected\"");
            }

            sb.Append(" style=\"");
            sb.Append(likertBtnStyle);
            if (on)
            {
                sb.Append(likertBtnOnStyle);
            }

            sb.Append("\" data-testid=\"onboarding-mini-likert\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\">");
            sb.Append(score);
            sb.Append("</button>");
        }

        sb.Append("</div></div></div>");
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

            sb.Append("\" style=\"min-height:52px;width:100%;box-sizing:border-box;justify-content:flex-start;text-align:start;padding:12px 14px;border-radius:8px");
            if (on)
            {
                sb.Append(";border-color:var(--brand);background:color-mix(in srgb, var(--brand) 12%, var(--surface));color:var(--brand);font-weight:600");
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
            sb.Append("<button type=\"submit\" name=\"obTransport\" class=\"lobsy-choice-tile");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(code));
            sb.Append("\" data-ob-code=\"");
            sb.Append(WebUtility.HtmlEncode(code));
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\"><span class=\"lobsy-choice-tile__label\">");
            sb.Append(WebUtility.HtmlEncode(label));
            sb.Append("</span></button>");
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
            sb.Append("<button type=\"submit\" name=\"obPreset\" class=\"lobsy-choice-tile lobsy-choice-tile--tall");
            if (on)
            {
                sb.Append(" is-selected");
            }

            sb.Append("\" value=\"");
            sb.Append(WebUtility.HtmlEncode(preset.Code));
            sb.Append("\" data-ob-code=\"");
            sb.Append(WebUtility.HtmlEncode(preset.Code));
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\">");
            sb.Append("<span class=\"lobsy-choice-tile__stack\">");
            sb.Append("<strong>");
            sb.Append(WebUtility.HtmlEncode(culture[preset.TitleKey]));
            sb.Append("</strong><small>");
            sb.Append(WebUtility.HtmlEncode(culture[preset.LeadKey]));
            sb.Append("</small></span>");
            if (on)
            {
                sb.Append("<span class=\"lobsy-choice-tile__check\" aria-hidden=\"true\">✓</span>");
            }

            sb.Append("</button>");
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
                sb.Append("<button type=\"submit\" name=\"obMatrix\" class=\"availability-matrix__cell availability-matrix__cell--btn");
                if (on)
                {
                    sb.Append(" is-on");
                }

                sb.Append("\" style=\"");
                sb.Append(on
                    ? "background:color-mix(in srgb, var(--brand) 20%, var(--surface));border:2px solid var(--brand);min-height:44px;box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:var(--brand)"
                    : "background:var(--surface);border:1px solid var(--border);min-height:44px;box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:transparent");
                sb.Append("\" value=\"");
                sb.Append(WebUtility.HtmlEncode(key));
                sb.Append("\" data-ob-key=\"");
                sb.Append(WebUtility.HtmlEncode(key));
                sb.Append("\" aria-pressed=\"");
                sb.Append(on ? "true" : "false");
                sb.Append("\" aria-label=\"");
                sb.Append(WebUtility.HtmlEncode(UiLabels.Weekday(culture, day)));
                sb.Append(' ');
                sb.Append(WebUtility.HtmlEncode(UiLabels.AvailabilitySlot(culture, slot)));
                sb.Append("\">");
                if (on)
                {
                    sb.Append("<span class=\"availability-matrix__mark\" style=\"font-weight:700;font-size:1.125rem;line-height:1\" aria-hidden=\"true\">✓</span>");
                }

                sb.Append("</button>");
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
