using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Passport;

internal static class PassportCoachToneSsrHtml
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        CandidatePreferences prefs,
        string antiforgeryToken,
        bool openSection,
        bool forceQuiz)
    {
        var coach = prefs.CoachTonePreferences;
        var hasAnswers = CoachTonePreferenceRules.HasAnyAnswers(ToDto(coach));
        var showSummary = hasAnswers && !forceQuiz;

        sb.Append("<section class=\"coach-prefs-card");
        if (openSection)
        {
            sb.Append(" is-open");
        }

        sb.Append("\" id=\"passport-data-coach-voorkeuren\" data-testid=\"passport-coach-tone\" ");
        sb.Append("data-coach-prefs ");
        sb.Append("data-mode=\"").Append(showSummary ? "summary" : "quiz").Append("\" ");
        sb.Append("data-question-count=\"").Append(CoachToneCatalog.QuestionCount).Append("\">");

        sb.Append("<h3 class=\"passport-data__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Card.Title"]));
        sb.Append("</h3><p class=\"passport-data__sub\">");
        sb.Append(WebUtility.HtmlEncode(showSummary
            ? culture["CoachTone.Card.Sub.Summary"]
            : culture["CoachTone.Card.Sub.Quiz"]));
        sb.Append("</p>");

        AppendPrivacyNote(sb, culture, showSummary);

        if (showSummary)
        {
            AppendSummary(sb, culture, coach, antiforgeryToken);
        }
        else
        {
            AppendQuiz(sb, culture, coach, antiforgeryToken);
        }

        sb.Append("</section>");
    }

    private static void AppendPrivacyNote(StringBuilder sb, CultureState culture, bool summary)
    {
        sb.Append("<div class=\"coach-prefs__why\" role=\"note\"><span class=\"coach-prefs__why-icon\" aria-hidden=\"true\">");
        sb.Append(PassportCoachToneIcons.Svg("ok"));
        sb.Append("</span><p>");
        sb.Append(WebUtility.HtmlEncode(summary
            ? culture["CoachTone.Privacy.Summary"]
            : culture["CoachTone.Privacy.Quiz"]));
        sb.Append("</p></div>");
    }

    private static void AppendQuiz(
        StringBuilder sb,
        CultureState culture,
        CoachTonePreferences? coach,
        string token)
    {
        var answers = coach?.Answers ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var state = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in answers)
        {
            if (kv.Value is { Count: > 0 })
            {
                state[kv.Key] = kv.Value.ToArray();
            }
        }

        var startIndex = 0;
        for (var i = 0; i < CoachToneCatalog.Questions.Count; i++)
        {
            var q = CoachToneCatalog.Questions[i];
            if (!state.TryGetValue(q.Key, out var codes) || codes.Length == 0)
            {
                startIndex = i;
                break;
            }

            if (i == CoachToneCatalog.Questions.Count - 1)
            {
                startIndex = i;
            }
        }

        sb.Append("<form method=\"post\" action=\"/api/passport-client/save-coach-tone\" class=\"coach-prefs__form\" data-coach-prefs-form>");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(token));
        sb.Append("\" /><input type=\"hidden\" name=\"returnTab\" value=\"data\" />");
        sb.Append("<input type=\"hidden\" name=\"openSection\" value=\"coach-voorkeuren\" />");
        sb.Append("<input type=\"hidden\" name=\"coachAction\" value=\"save\" data-coach-action />");
        sb.Append("<script type=\"application/json\" data-coach-prefs-json>");
        sb.Append(WebUtility.HtmlEncode(JsonSerializer.Serialize(new
        {
            startIndex,
            answers = state
        }, JsonOptions)));
        sb.Append("</script>");

        foreach (var (qIndex, q) in CoachToneCatalog.Questions.Select((q, i) => (i, q)))
        {
            sb.Append("<div class=\"coach-prefs__step\" data-coach-step=\"");
            sb.Append(qIndex);
            sb.Append("\" data-question-key=\"");
            sb.Append(WebUtility.HtmlEncode(q.Key));
            sb.Append('"');
            if (qIndex != startIndex)
            {
                sb.Append(" hidden");
            }

            sb.Append('>');

            var stepNum = qIndex + 1;
            var pct = (int)Math.Round(100.0 * stepNum / CoachToneCatalog.QuestionCount);
            sb.Append("<div class=\"coach-prefs__progress-row\"><p class=\"coach-prefs__progress-label\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("CoachTone.Progress", stepNum, CoachToneCatalog.QuestionCount)));
            sb.Append("</p><p class=\"coach-prefs__progress-pct\" aria-hidden=\"true\">");
            sb.Append(pct).Append("%</p></div>");
            sb.Append("<div class=\"coach-prefs__progress-track\" role=\"progressbar\" aria-valuenow=\"");
            sb.Append(stepNum);
            sb.Append("\" aria-valuemin=\"0\" aria-valuemax=\"");
            sb.Append(CoachToneCatalog.QuestionCount);
            sb.Append("\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture.Format("CoachTone.Progress", stepNum, CoachToneCatalog.QuestionCount)));
            sb.Append("\"><div class=\"coach-prefs__progress-fill\" style=\"width:");
            sb.Append(pct).Append("%\"></div></div>");

            if (!string.IsNullOrWhiteSpace(q.SectionKey))
            {
                sb.Append("<p class=\"coach-prefs__hint coach-prefs__hint--section\">");
                sb.Append(WebUtility.HtmlEncode(culture[q.SectionKey]));
                sb.Append("</p>");
            }

            sb.Append("<h4 class=\"coach-prefs__question\">");
            sb.Append(WebUtility.HtmlEncode(culture[q.TitleKey]));
            sb.Append("</h4><p class=\"coach-prefs__hint\">");
            sb.Append(WebUtility.HtmlEncode(culture[q.HintKey ?? "CoachTone.Multi.Hint"]));
            sb.Append("</p>");

            var selected = state.TryGetValue(q.Key, out var sel) ? sel.ToHashSet(StringComparer.OrdinalIgnoreCase) : [];
            sb.Append("<div class=\"lobsy-choice-grid\" role=\"group\">");
            foreach (var opt in q.Options)
            {
                var pressed = selected.Contains(opt.Code);
                sb.Append("<button type=\"button\" class=\"lobsy-choice-tile coach-prefs__tile");
                if (pressed)
                {
                    sb.Append(" is-selected");
                }

                sb.Append("\" data-coach-tile data-code=\"");
                sb.Append(WebUtility.HtmlEncode(opt.Code));
                sb.Append("\" aria-pressed=\"").Append(pressed ? "true" : "false").Append("\" ");
                sb.Append(q.MultiSelect ? "data-multi=\"1\"" : "data-multi=\"0\"");
                sb.Append("><span class=\"coach-prefs__tile-icon\" aria-hidden=\"true\">");
                sb.Append(PassportCoachToneIcons.Svg(opt.Icon));
                sb.Append("</span><span class=\"coach-prefs__tile-text\">");
                sb.Append(WebUtility.HtmlEncode(culture[opt.LabelKey]));
                sb.Append("</span></button>");
            }

            sb.Append("</div>");

            foreach (var code in selected)
            {
                sb.Append("<input type=\"hidden\" name=\"answer_");
                sb.Append(WebUtility.HtmlEncode(q.Key));
                sb.Append("\" value=\"");
                sb.Append(WebUtility.HtmlEncode(code));
                sb.Append("\" data-coach-answer-field />");
            }

            sb.Append("<div class=\"coach-prefs__actions\">");
            sb.Append("<button type=\"button\" class=\"btn btn-ghost\" data-coach-btn=\"skip\">");
            sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Action.Skip"]));
            sb.Append("</button><button type=\"button\" class=\"btn btn-secondary\" data-coach-btn=\"later\">");
            sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Action.Later"]));
            sb.Append("</button><span class=\"coach-prefs__actions-spacer\"></span>");
            sb.Append("<button type=\"button\" class=\"btn btn-primary\" data-coach-btn=\"next\">");
            sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Action.Next"]));
            sb.Append("</button></div></div>");
        }

        sb.Append("</form>");
    }

    private static void AppendSummary(
        StringBuilder sb,
        CultureState culture,
        CoachTonePreferences? coach,
        string token)
    {
        var answers = coach?.Answers ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        sb.Append("<h4 class=\"coach-prefs__summary-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Summary.Title"]));
        sb.Append("</h4><dl class=\"coach-prefs__summary-list\">");

        var thinkingLabels = new List<string>();
        foreach (var q in CoachToneCatalog.Questions)
        {
            if (q.Key is "thinking_learn" or "thinking_new_task")
            {
                if (answers.TryGetValue(q.Key, out var tCodes) && tCodes.Count > 0)
                {
                    thinkingLabels.AddRange(tCodes
                        .Select(c => CoachToneCatalog.TryOptionLabelKey(q.Key, c) is string lk ? culture[lk] : c));
                }

                continue;
            }

            if (!answers.TryGetValue(q.Key, out var codes) || codes.Count == 0)
            {
                continue;
            }

            var labels = codes
                .Select(c => CoachToneCatalog.TryOptionLabelKey(q.Key, c) is string lk ? culture[lk] : c)
                .ToList();
            sb.Append("<div><dt>");
            sb.Append(WebUtility.HtmlEncode(culture[CoachToneCatalog.SummaryGroupKey(q.Key)]));
            sb.Append("</dt><dd>");
            sb.Append(WebUtility.HtmlEncode(string.Join(" · ", labels)));
            sb.Append("</dd></div>");
        }

        if (thinkingLabels.Count > 0)
        {
            sb.Append("<div><dt>");
            sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Summary.Thinking"]));
            sb.Append("</dt><dd>");
            sb.Append(WebUtility.HtmlEncode(string.Join(" · ", thinkingLabels.Distinct(StringComparer.OrdinalIgnoreCase))));
            sb.Append("</dd></div>");
        }

        sb.Append("</dl><form method=\"post\" action=\"/api/passport-client/save-coach-tone\" class=\"coach-prefs__form-summary\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(token));
        sb.Append("\" /><input type=\"hidden\" name=\"returnTab\" value=\"data\" />");
        sb.Append("<input type=\"hidden\" name=\"openSection\" value=\"coach-voorkeuren\" />");
        sb.Append("<div class=\"coach-prefs__actions\">");
        sb.Append("<button type=\"submit\" name=\"coachAction\" value=\"edit\" class=\"btn btn-secondary\">");
        sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Action.Edit"]));
        sb.Append("</button><button type=\"submit\" name=\"coachAction\" value=\"wipe\" class=\"btn btn-ghost\">");
        sb.Append(WebUtility.HtmlEncode(culture["CoachTone.Action.Wipe"]));
        sb.Append("</button></div></form>");
    }

    private static Jobsy.Core.Contracts.CoachTonePreferencesDto? ToDto(CoachTonePreferences? coach)
    {
        if (coach is null)
        {
            return null;
        }

        var answers = coach.Answers?.ToDictionary(
            kv => kv.Key,
            kv => kv.Value?.ToArray() ?? [],
            StringComparer.OrdinalIgnoreCase);

        return new Jobsy.Core.Contracts.CoachTonePreferencesDto(
            coach.Version,
            coach.UpdatedAtUtc,
            coach.Skipped,
            answers);
    }
}
