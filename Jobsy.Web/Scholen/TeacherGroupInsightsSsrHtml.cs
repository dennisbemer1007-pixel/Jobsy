using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>Chart layout for teacher group insights (groepsresultaten).</summary>
public static class TeacherGroupInsightsSsrHtml
{
    private const string Navy = "#0f2747";
    private const string Coral = "#c4552a";
    private const string Teal = "#4a90a4";

    public static void AppendFullPageCharts(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<div class=\"sch-group-charts\" data-testid=\"leraar-group-insights\">");
        sb.Append("<div class=\"sch-group-grid sch-group-grid--primary\">");
        AppendInterestsCard(sb, group, culture);
        AppendCompetenceCard(sb, group, culture);
        sb.Append("</div><div class=\"sch-group-grid\">");
        if (ShouldShowGroupSection(group, group.TopValues))
        {
            AppendDriversCard(sb, group, culture);
        }

        if (ShouldShowGroupSection(group, group.TopCultures))
        {
            AppendCulturesCard(sb, group, culture);
        }

        sb.Append("</div>");

        if (group.QuestionSet != PupilQuestionSet.MboHbo)
        {
            AppendDreamJobsCard(sb, group, culture);
        }

        if (group.DiscussionPromptKeys.Count > 0)
        {
            AppendDiscussionCard(sb, group, culture);
        }

        sb.Append("</div>");
    }

    public static void AppendOverviewInsights(StringBuilder sb, TeacherGroupInsightsDto gi, PupilQuestionSet questionSet, CultureState culture)
    {
        sb.Append("<div class=\"sch-overview-insights\" data-testid=\"leraar-overview-group-insights\">");
        if (!gi.Visible)
        {
            sb.Append("<section class=\"sch-card\"><p class=\"muted\">");
            sb.Append(E(string.Format(CultureInfo.CurrentCulture, culture["Leraar.Group.Hidden"], gi.CompletedCount)));
            sb.Append("</p></section></div>");
            return;
        }

        if (ShouldShowGroupSection(gi, gi.TopValues))
        {
            AppendDriversCard(sb, gi, culture);
        }

        if (questionSet != PupilQuestionSet.MboHbo)
        {
            AppendDreamJobsCard(sb, gi, culture, compact: true);
        }

        if (ShouldShowGroupSection(gi, gi.TopCultures))
        {
            AppendCulturesCard(sb, gi, culture);
        }

        sb.Append("</div>");
    }

    private static void AppendInterestsCard(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-chart-card\"><h2>");
        sb.Append(E(culture["Leraar.Group.Interests"]));
        sb.Append("</h2><p class=\"sch-chart-hint\">");
        sb.Append(E(culture["Leraar.Group.InterestsHint"]));
        sb.Append("</p>");
        var bars = BuildRiasecBarRows(group, culture);
        if (bars.Count == 0)
        {
            sb.Append("<p class=\"sch-chart-empty\">");
            sb.Append(E(culture["Leraar.Overview.Chart.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            AppendHorizontalBarChart(sb, culture["Leraar.Group.Interests"], bars);
        }

        sb.Append("</section>");
    }

    private static void AppendCompetenceCard(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-chart-card sch-chart-donut\"><h2>");
        sb.Append(E(culture["Leraar.Group.Competence"]));
        sb.Append("</h2><p class=\"sch-chart-hint\">");
        sb.Append(E(culture["Leraar.Group.CompetenceHint"]));
        sb.Append("</p>");
        if (group.CompetenceBands.Count == 0)
        {
            sb.Append("<p class=\"sch-chart-empty\">");
            sb.Append(E(culture["Leraar.Overview.Chart.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            var segments = CompetenceSegments(group.CompetenceBands);
            sb.Append("<div class=\"sch-chart-donut__wrap\" role=\"img\" aria-label=\"");
            sb.Append(E(CompetenceAriaLabel(segments)));
            sb.Append("\">");
            sb.Append(TeacherOverviewChartSvg.Donut(segments, culture["Leraar.Overview.Chart.Empty"], size: 160, stroke: 26));
            sb.Append("<ul class=\"sch-chart-donut__legend\">");
            foreach (var seg in segments)
            {
                sb.Append("<li><span class=\"sch-chart-donut__swatch\" style=\"background:");
                sb.Append(seg.Color);
                sb.Append("\"></span><span>");
                sb.Append(E(seg.Label));
                sb.Append("</span><strong class=\"tabular\">");
                sb.Append(seg.Value.ToString(CultureInfo.InvariantCulture));
                sb.Append("</strong></li>");
            }

            sb.Append("</ul></div>");
        }

        sb.Append("</section>");
    }

    private static void AppendDriversCard(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-chart-card\"><h2>");
        sb.Append(E(culture["Leraar.Group.Drivers"]));
        sb.Append("</h2><p class=\"sch-chart-hint\">");
        sb.Append(E(culture["Leraar.Group.DriversHint"]));
        sb.Append("</p>");
        var bars = BuildNamedBarRows(group, group.TopValues, GroupDimensionLabel, culture);
        if (bars.Count == 0)
        {
            sb.Append("<p class=\"sch-chart-empty\">");
            sb.Append(E(culture["Leraar.Overview.Chart.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            AppendHorizontalBarChart(sb, culture["Leraar.Group.Drivers"], bars);
        }

        sb.Append("</section>");
    }

    private static void AppendCulturesCard(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-card sch-chart-card\"><h2>");
        sb.Append(E(culture["Leraar.Group.Cultures"]));
        sb.Append("</h2><p class=\"sch-chart-hint\">");
        sb.Append(E(culture["Leraar.Group.CulturesHint"]));
        sb.Append("</p>");
        var bars = BuildNamedBarRows(group, group.TopCultures, GroupCultureLabel, culture);
        if (bars.Count == 0)
        {
            sb.Append("<p class=\"sch-chart-empty\">");
            sb.Append(E(culture["Leraar.Overview.Chart.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            AppendHorizontalBarChart(sb, culture["Leraar.Group.Cultures"], bars);
        }

        sb.Append("</section>");
    }

    private static void AppendDreamJobsCard(
        StringBuilder sb,
        TeacherGroupInsightsDto group,
        CultureState culture,
        bool compact = false)
    {
        sb.Append("<section class=\"sch-card sch-chart-card sch-group-dreamjobs");
        if (!compact)
        {
            sb.Append(" sch-group-grid__full");
        }

        sb.Append("\"><h2>");
        sb.Append(E(culture["Leraar.Group.DreamJobs"]));
        sb.Append("</h2><p class=\"sch-chart-hint\">");
        sb.Append(E(culture["Leraar.Group.DreamJobsHint"]));
        sb.Append("</p>");
        var jobBars = group.DreamJobs
            .Select(j => new BarRow(PupilDreamJobText.Label(culture, j.Key), j.Count))
            .Where(b => b.Count > 0)
            .ToList();
        if (jobBars.Count == 0)
        {
            sb.Append("<p class=\"sch-chart-empty\">");
            sb.Append(E(culture["Leraar.Overview.Chart.Empty"]));
            sb.Append("</p>");
        }
        else
        {
            var items = jobBars
                .Select(b => new TeacherOverviewChartSvg.BarItem(b.Label, b.Count))
                .ToList();
            sb.Append("<div role=\"img\" aria-label=\"");
            sb.Append(E(NamedBarsAriaLabel(culture["Leraar.Group.DreamJobs"], jobBars)));
            sb.Append("\">");
            sb.Append(TeacherOverviewChartSvg.VerticalColumns(items, culture["Leraar.Overview.Chart.Empty"]));
            sb.Append("</div>");
        }

        sb.Append("<p class=\"muted sch-group-dreamjobs__meta\">");
        sb.Append(E(culture["Leraar.DreamJobs.Undecided"]));
        sb.Append(": ");
        sb.Append(group.UndecidedDreamJobCount.ToString(CultureInfo.InvariantCulture));
        if (group.NotFilledDreamJobCount > 0)
        {
            sb.Append(" · ");
            sb.Append(E(culture["Leraar.DreamJobs.NotFilled"]));
            sb.Append(": ");
            sb.Append(group.NotFilledDreamJobCount.ToString(CultureInfo.InvariantCulture));
        }

        sb.Append("</p></section>");
    }

    private static void AppendDiscussionCard(StringBuilder sb, TeacherGroupInsightsDto group, CultureState culture)
    {
        sb.Append("<section class=\"sch-group-discussion sch-group-grid__full\"><h2>");
        sb.Append(E(culture["Leraar.Group.Discussion"]));
        sb.Append("</h2><ul class=\"sch-prompt-list\">");
        foreach (var line in group.DiscussionPromptKeys)
        {
            sb.Append("<li>");
            sb.Append(E(line));
            sb.Append("</li>");
        }

        sb.Append("</ul></section>");
    }

    private static void AppendHorizontalBarChart(StringBuilder sb, string chartTitle, IReadOnlyList<BarRow> bars)
    {
        var max = Math.Max(1, bars.Max(b => b.Count));
        sb.Append("<div class=\"sch-group-bars\" role=\"img\" aria-label=\"");
        sb.Append(E(NamedBarsAriaLabel(chartTitle, bars)));
        sb.Append("\">");
        foreach (var bar in bars)
        {
            var pct = 100d * bar.Count / max;
            sb.Append("<div class=\"sch-group-bar\"><span class=\"sch-group-bar__label\">");
            sb.Append(E(bar.Label));
            sb.Append("</span><div class=\"sch-group-bar__track\"><div class=\"sch-group-bar__fill ");
            sb.Append(BarToneClass(bar.Count, max));
            sb.Append("\" style=\"width:");
            sb.Append(pct.ToString("0.##", CultureInfo.InvariantCulture));
            sb.Append("%\"></div></div><span class=\"sch-group-bar__value tabular\">");
            sb.Append(bar.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("</span></div>");
        }

        sb.Append("</div>");
    }

    private static List<BarRow> BuildRiasecBarRows(TeacherGroupInsightsDto group, CultureState culture)
        => group.RiasecBars
            .Where(b => b.Count > 0)
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Letter, StringComparer.OrdinalIgnoreCase)
            .Select(b => new BarRow($"{culture[b.KidLabelKey]} ({b.Letter})", b.Count))
            .ToList();

    private static List<BarRow> BuildNamedBarRows(
        TeacherGroupInsightsDto group,
        IReadOnlyList<NamedCountDto> items,
        Func<TeacherGroupInsightsDto, CultureState, string, string> labelFn,
        CultureState culture)
        => items
            .Where(i => i.Count > 0)
            .Select(i => new BarRow(labelFn(group, culture, i.Key), i.Count))
            .ToList();

    private static List<TeacherOverviewChartSvg.Segment> CompetenceSegments(IReadOnlyList<NamedCountDto> bands)
    {
        var order = new[] { "Laag", "Midden", "Hoog" };
        var byKey = bands.ToDictionary(b => b.Key, b => b.Count, StringComparer.OrdinalIgnoreCase);
        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Laag"] = Navy,
            ["Midden"] = Coral,
            ["Hoog"] = Teal
        };
        return order
            .Where(k => byKey.GetValueOrDefault(k) > 0)
            .Select(k => new TeacherOverviewChartSvg.Segment(k, byKey[k], colors[k]))
            .ToList();
    }

    private static string CompetenceAriaLabel(IReadOnlyList<TeacherOverviewChartSvg.Segment> segments)
        => string.Join(", ", segments.Select(s => $"{s.Label} {s.Value}"));

    private static string NamedBarsAriaLabel(string title, IReadOnlyList<BarRow> bars)
        => $"{title}: {string.Join(", ", bars.Select(b => $"{b.Label} {b.Count}"))}";

    private static string BarToneClass(int value, int max)
    {
        if (value >= max)
        {
            return "is-navy";
        }

        return value >= max / 2d ? "is-teal" : "is-coral";
    }

    private static bool ShouldShowGroupSection(TeacherGroupInsightsDto group, IReadOnlyList<NamedCountDto> items)
        => group.QuestionSet != PupilQuestionSet.MboHbo || items.Count > 0;

    private static string GroupDimensionLabel(TeacherGroupInsightsDto group, CultureState culture, string key)
        => group.QuestionSet == PupilQuestionSet.MboHbo
            ? key
            : PupilDimensionText.Value(culture, key);

    private static string GroupCultureLabel(TeacherGroupInsightsDto group, CultureState culture, string key)
        => group.QuestionSet == PupilQuestionSet.MboHbo
            ? key
            : PupilDimensionText.Culture(culture, key);

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    private sealed record BarRow(string Label, int Count);
}
