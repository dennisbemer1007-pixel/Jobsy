using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Onboarding;
namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrShells
{
    internal sealed record OverviewStop(
        string PhaseId,
        string TitleKey,
        string VignetteUrl,
        string StatusText,
        string StatusClass,
        bool ShowCheck,
        bool ShowContinue);

    internal sealed record OverviewPanelSection(
        string Title,
        string Summary,
        string? AdjustHref,
        bool ShowAdjust);

    internal sealed record OverviewTestRow(
        int Step,
        string TitleKey,
        string AccentUrl,
        bool IsComplete,
        string? ProgressText,
        string ReportHref,
        string? StartHref,
        string? ShedHref);

    internal sealed record OverviewModel(
        IReadOnlyList<OverviewStop> Stops,
        string ActivePhaseId,
        string YouAreHerePhaseId,
        double YouAreHereProgressFrac,
        string? PanelTitleKey,
        string? PanelLeadKey,
        string? PanelVignette,
        IReadOnlyList<OverviewPanelSection> PanelSections,
        IReadOnlyList<OverviewTestRow> TestRows,
        string? ErrorMessage);

    private const string ContainerStyle =
        "max-width:72rem;margin-inline:auto;padding-inline:clamp(1rem,4vw,2rem);box-sizing:border-box;width:100%;";

    internal sealed record ShedModel(
        int ShedStep,
        string TestName,
        IReadOnlyList<string> ImpressionLines,
        IReadOnlyList<JourneyTestFlow.DepthOptionSpec> Options,
        int SelectedLevel,
        bool FullyDone,
        string PrimaryLabel,
        bool ShowDiveDeeper);

    internal sealed record TestModel(
        int Step,
        int TargetLevel,
        int ActiveQuestion1Based,
        OnboardingClientStepSeed Seed,
        bool BandComplete,
        string TitleKey,
        string HintKey);

    public static string BuildPage(
        CultureState culture,
        string antiforgeryToken,
        string screen,
        OverviewModel? overview,
        ShedModel? shed,
        TestModel? test,
        string? errorMessage)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"journey journey-root--ssr");
        if (string.Equals(screen, "overview", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(" journey--overview-route");
        }

        sb.Append("\" data-discovery-ssr=\"1\">");

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            sb.Append("<p class=\"state-message state-message--error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(errorMessage));
            sb.Append("</p>");
        }

        if (overview is not null)
        {
            AppendOverview(sb, culture, overview, antiforgeryToken);
        }
        else if (shed is not null)
        {
            AppendShed(sb, culture, shed, antiforgeryToken);
        }
        else if (test is not null)
        {
            AppendTest(sb, culture, test, antiforgeryToken);
        }
        else if (string.Equals(screen, "klaar", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("<section class=\"journey-step\"><h1>");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.End.Title"]));
            sb.Append("</h1><p class=\"journey-step__lead\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.End.Lead"]));
            sb.Append("</p><p><a class=\"btn btn-primary\" href=\"");
            sb.Append(DiscoverySsrUrls.Overview());
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Back"]));
            sb.Append("</a></p></section>");
        }
        else
        {
            sb.Append("<section class=\"journey-step\"><p class=\"journey-step__lead\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Start.Lead"]));
            sb.Append("</p><p><a class=\"btn btn-primary\" href=\"");
            sb.Append(DiscoverySsrUrls.Overview());
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Title"]));
            sb.Append("</a></p></section>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendOverview(StringBuilder sb, CultureState culture, OverviewModel model, string _)
    {
        sb.Append("<div class=\"journey-overview discovery-ssr-container\" data-testid=\"discovery-overview-route\" style=\"");
        sb.Append(ContainerStyle);
        sb.Append("\">");
        sb.Append("<p class=\"journey-step__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Eyebrow"]));
        sb.Append("</p><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Title"]));
        sb.Append("</h1><p class=\"journey-step__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Lead"]));
        sb.Append("</p><p class=\"journey-route__intro\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.RouteLead"]));
        sb.Append("</p><div class=\"journey-route journey-route--ssr\">");

        sb.Append("<div class=\"journey-route__desktop-only\"><div class=\"journey-route__canvas\" id=\"discovery-route-canvas\" data-route-layout=\"desktop\">");
        sb.Append("<svg class=\"journey-route__svg\" aria-hidden=\"true\" preserveAspectRatio=\"none\" viewBox=\"0 0 1000 320\">");
        sb.Append("<defs><linearGradient id=\"discoveryRouteStroke\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#c4a574\" /><stop offset=\"45%\" stop-color=\"#8fb8a8\" /><stop offset=\"100%\" stop-color=\"#d4b878\" />");
        sb.Append("</linearGradient></defs><path id=\"discovery-route-path\" d=\"");
        sb.Append(DiscoveryRouteGeometry.DesktopPathD);
        sb.Append("\" fill=\"none\" stroke=\"url(#discoveryRouteStroke)\" stroke-width=\"12\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.62\" /></svg>");
        sb.Append("<div class=\"journey-route__stops\" role=\"list\">");
        foreach (var stop in model.Stops)
        {
            var isActive = stop.PhaseId == model.ActivePhaseId;
            var href = DiscoverySsrUrls.Overview(stop.PhaseId);
            sb.Append("<a class=\"journey-route__stop journey-route__stop-link");
            if (isActive)
            {
                sb.Append(" is-current");
            }

            sb.Append("\" role=\"listitem\" data-phase=\"");
            sb.Append(WebUtility.HtmlEncode(stop.PhaseId));
            sb.Append("\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" data-enhance-nav=\"false\"><span class=\"journey-route__stop-stack\"><span class=\"journey-route__vignette");
            if (stop.ShowCheck)
            {
                sb.Append(" is-done");
            }

            if (isActive)
            {
                sb.Append(" is-current");
            }

            sb.Append("\" data-discovery-vignette><img src=\"");
            sb.Append(WebUtility.HtmlEncode(stop.VignetteUrl));
            sb.Append("\" alt=\"\" width=\"120\" height=\"120\" loading=\"eager\" decoding=\"async\" /></span><span class=\"journey-route__label\"><span class=\"journey-route__title\">");
            sb.Append(WebUtility.HtmlEncode(culture[stop.TitleKey]));
            sb.Append("</span><span class=\"journey-route__status ");
            sb.Append(WebUtility.HtmlEncode(stop.StatusClass));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(stop.StatusText));
            sb.Append("</span>");
            if (isActive && stop.ShowContinue)
            {
                sb.Append("<span class=\"btn btn-primary journey-route__cta\">");
                sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Continue"]));
                sb.Append("</span>");
            }

            sb.Append("</span></span></a>");
        }

        sb.Append("</div></div>");

        if (model.PanelTitleKey is not null)
        {
            sb.Append("<div class=\"journey-route__panel-wrap\"><div class=\"journey-route__connector\" aria-hidden=\"true\"></div>");
            sb.Append("<article class=\"journey-route__panel\" aria-live=\"polite\"><header class=\"journey-route__panel-head\">");
            if (model.PanelVignette is not null)
            {
                sb.Append("<img class=\"journey-route__panel-vig\" src=\"");
                sb.Append(WebUtility.HtmlEncode(model.PanelVignette));
                sb.Append("\" alt=\"\" width=\"48\" height=\"48\" />");
            }

            sb.Append("<div><h3>");
            sb.Append(WebUtility.HtmlEncode(culture[model.PanelTitleKey]));
            sb.Append("</h3>");
            if (model.PanelLeadKey is not null)
            {
                sb.Append("<p>");
                sb.Append(WebUtility.HtmlEncode(culture[model.PanelLeadKey]));
                sb.Append("</p>");
            }

            sb.Append("</div></header><ul class=\"journey-route__sections\">");
            foreach (var section in model.PanelSections)
            {
                sb.Append("<li><div><h4>");
                sb.Append(WebUtility.HtmlEncode(section.Title));
                sb.Append("</h4><p>");
                sb.Append(WebUtility.HtmlEncode(section.Summary));
                sb.Append("</p></div>");
                if (section.ShowAdjust && !string.IsNullOrWhiteSpace(section.AdjustHref))
                {
                    sb.Append("<a class=\"journey-route__adjust\" href=\"");
                    sb.Append(WebUtility.HtmlEncode(section.AdjustHref));
                    sb.Append("\" data-enhance-nav=\"false\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Adjust"]));
                    sb.Append("</a>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul></article></div>");
        }

        sb.Append("</div>");

        sb.Append("<div class=\"journey-route__mobile-only discovery-mobile-route\" id=\"discovery-route-canvas-mobile\" data-route-layout=\"mobile-list\"><ol class=\"discovery-mobile-stops\" role=\"list\">");
        for (var i = 0; i < model.Stops.Count; i++)
        {
            var stop = model.Stops[i];
            var isActive = stop.PhaseId == model.ActivePhaseId;
            var isLast = i == model.Stops.Count - 1;
            var href = DiscoverySsrUrls.Overview(stop.PhaseId);
            sb.Append("<li class=\"discovery-mobile-stop");
            if (isActive)
            {
                sb.Append(" is-open");
            }

            if (stop.PhaseId == model.YouAreHerePhaseId)
            {
                sb.Append(" is-you");
            }

            sb.Append("\" data-phase=\"");
            sb.Append(WebUtility.HtmlEncode(stop.PhaseId));
            sb.Append("\"><div class=\"discovery-mobile-stop__row\"><div class=\"discovery-mobile-stop__rail\" aria-hidden=\"true\">");
            if (!isLast)
            {
                sb.Append("<span class=\"discovery-mobile-stop__line\"></span>");
            }

            sb.Append("<a class=\"discovery-mobile-stop__circle journey-route__stop-link\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" data-enhance-nav=\"false\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture[stop.TitleKey]));
            sb.Append("\"><span class=\"journey-route__vignette");
            if (stop.ShowCheck)
            {
                sb.Append(" is-done");
            }

            if (isActive)
            {
                sb.Append(" is-current");
            }

            sb.Append("\"><img src=\"");
            sb.Append(WebUtility.HtmlEncode(stop.VignetteUrl));
            sb.Append("\" alt=\"\" width=\"56\" height=\"56\" loading=\"eager\" decoding=\"async\" /></span></a></div><div class=\"discovery-mobile-stop__body\"><a class=\"discovery-mobile-stop__text journey-route__stop-link\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" data-enhance-nav=\"false\" aria-controls=\"");
            sb.Append(WebUtility.HtmlEncode($"discovery-panel-{stop.PhaseId}"));
            sb.Append("\"><span class=\"journey-route__title\">");
            sb.Append(WebUtility.HtmlEncode(culture[stop.TitleKey]));
            sb.Append("</span><span class=\"journey-route__status ");
            sb.Append(WebUtility.HtmlEncode(stop.StatusClass));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(stop.StatusText));
            sb.Append("</span>");
            if (isActive && stop.ShowContinue)
            {
                sb.Append("<span class=\"btn btn-primary btn-sm journey-route__cta\">");
                sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Continue"]));
                sb.Append("</span>");
            }

            sb.Append("</a>");
            if (isActive && model.PanelTitleKey is not null)
            {
                AppendMobileInlinePanel(sb, culture, stop, model);
            }

            sb.Append("</div></div></li>");
        }

        sb.Append("</ol></div>");

        sb.Append("<h2 class=\"journey-q\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.TestsHeading"]));
        sb.Append("</h2><div class=\"journey-route__tests\">");
        foreach (var test in model.TestRows)
        {
            sb.Append("<div class=\"journey-route__test");
            if (!test.IsComplete)
            {
                sb.Append(" is-todo");
            }

            sb.Append("\"><img class=\"journey-route__test-art\" src=\"");
            sb.Append(WebUtility.HtmlEncode(test.AccentUrl));
            sb.Append("\" alt=\"\" width=\"64\" height=\"64\" loading=\"lazy\" /><p class=\"journey-route__test-title\">");
            sb.Append(WebUtility.HtmlEncode(culture[test.TitleKey]));
            sb.Append("</p><div class=\"journey-route__test-meta\">");
            if (test.IsComplete)
            {
                sb.Append("<span class=\"status-pill status-pill--success journey-route__test-status\">");
                sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Done"]));
                sb.Append("</span><a class=\"btn btn-secondary journey-route__test-report\" href=\"");
                sb.Append(WebUtility.HtmlEncode(test.ReportHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.FullReport"]));
                sb.Append("</a>");
                if (!string.IsNullOrWhiteSpace(test.ShedHref))
                {
                    sb.Append("<a class=\"btn btn-secondary journey-route__test-report\" href=\"");
                    sb.Append(WebUtility.HtmlEncode(test.ShedHref));
                    sb.Append("\" data-enhance-nav=\"false\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Discovery.Shed.DiveDeeper"]));
                    sb.Append("</a>");
                }
            }
            else
            {
                sb.Append("<span class=\"status-pill journey-route__test-pill-todo journey-route__test-status\">");
                sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.TestTodo"]));
                sb.Append("</span>");
                if (!string.IsNullOrWhiteSpace(test.ProgressText))
                {
                    sb.Append("<span class=\"status-pill journey-route__test-pill-progress\">");
                    sb.Append(WebUtility.HtmlEncode(test.ProgressText));
                    sb.Append("</span>");
                }

                if (!string.IsNullOrWhiteSpace(test.StartHref))
                {
                    sb.Append("<a class=\"btn btn-primary journey-route__test-start\" href=\"");
                    sb.Append(WebUtility.HtmlEncode(test.StartHref));
                    sb.Append("\" data-enhance-nav=\"false\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Start"]));
                    sb.Append("</a>");
                }

                if (!string.IsNullOrWhiteSpace(test.ShedHref))
                {
                    sb.Append("<a class=\"btn btn-secondary journey-route__test-report\" href=\"");
                    sb.Append(WebUtility.HtmlEncode(test.ShedHref));
                    sb.Append("\" data-enhance-nav=\"false\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Discovery.Shed.DiveDeeper"]));
                    sb.Append("</a>");
                }
            }

            sb.Append("</div></div>");
        }

        sb.Append("</div></div>");
    }

    private static void AppendMobileInlinePanel(
        StringBuilder sb,
        CultureState culture,
        OverviewStop stop,
        OverviewModel model)
    {
        sb.Append("<div id=\"discovery-panel-");
        sb.Append(WebUtility.HtmlEncode(stop.PhaseId));
        sb.Append("\" class=\"journey-route-mobile__inline\" role=\"region\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture[stop.TitleKey]));
        sb.Append("\"><header class=\"journey-route-mobile__inline-head\"><h3>");
        if (model.PanelTitleKey is not null)
        {
            sb.Append(WebUtility.HtmlEncode(culture[model.PanelTitleKey]));
        }

        sb.Append("</h3><div class=\"journey-route-mobile__inline-actions\"><span class=\"journey-route__status ");
        sb.Append(WebUtility.HtmlEncode(stop.StatusClass));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(stop.StatusText));
        sb.Append("</span>");
        if (stop.ShowContinue)
        {
            sb.Append("<span class=\"btn btn-primary btn-sm\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Continue"]));
            sb.Append("</span>");
        }

        sb.Append("</div></header>");
        if (model.PanelLeadKey is not null)
        {
            sb.Append("<p class=\"journey-route-mobile__inline-lead\">");
            sb.Append(WebUtility.HtmlEncode(culture[model.PanelLeadKey]));
            sb.Append("</p>");
        }

        sb.Append("<ul class=\"journey-route__sections journey-route__panel--compact\">");
        foreach (var section in model.PanelSections)
        {
            sb.Append("<li><div><h4>");
            sb.Append(WebUtility.HtmlEncode(section.Title));
            sb.Append("</h4><p>");
            sb.Append(WebUtility.HtmlEncode(section.Summary));
            sb.Append("</p></div>");
            if (section.ShowAdjust && !string.IsNullOrWhiteSpace(section.AdjustHref))
            {
                sb.Append("<a class=\"journey-route__adjust\" href=\"");
                sb.Append(WebUtility.HtmlEncode(section.AdjustHref));
                sb.Append("\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Adjust"]));
                sb.Append("</a>");
            }

            sb.Append("</li>");
        }

        sb.Append("</ul></div>");
    }

    private static void AppendShed(StringBuilder sb, CultureState culture, ShedModel model, string token)
    {
        sb.Append("<section class=\"journey-step journey-shed\" data-testid=\"discovery-shed\">");
        sb.Append("<p class=\"journey-step__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Zone.Deep"]));
        sb.Append(" · ");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Discovery.Shed.EyebrowDone", model.ShedStep)));
        sb.Append("</p><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Shed.Title"]));
        sb.Append("</h1><p class=\"journey-step__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Discovery.Shed.Lead", model.TestName)));
        sb.Append("</p>");
        if (model.ImpressionLines.Count > 0)
        {
            sb.Append("<ul class=\"journey-shed__gained\">");
            foreach (var line in model.ImpressionLines)
            {
                sb.Append("<li><span aria-hidden=\"true\">✓</span> ");
                sb.Append(WebUtility.HtmlEncode(line));
                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        if (!model.FullyDone)
        {
            sb.Append("<fieldset class=\"journey-shed__fieldset\"><legend class=\"journey-q\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Shed.DeeperLegend"]));
            sb.Append("</legend><div class=\"journey-dive\" role=\"radiogroup\">");
            foreach (var opt in model.Options)
            {
                var selected = model.SelectedLevel == opt.Level;
                sb.Append("<form method=\"post\" action=\"");
                sb.Append(DiscoverySsrUrls.DieptePostPath);
                sb.Append("\" class=\"journey-dive__form\"><input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
                sb.Append(WebUtility.HtmlEncode(token));
                sb.Append("\" /><input type=\"hidden\" name=\"shedStep\" value=\"");
                sb.Append(model.ShedStep);
                sb.Append("\" /><input type=\"hidden\" name=\"level\" value=\"");
                sb.Append(opt.Level);
                sb.Append("\" /><button type=\"submit\" class=\"journey-dive__opt");
                if (selected)
                {
                    sb.Append(" is-on");
                }

                if (opt.Disabled)
                {
                    sb.Append(" is-done");
                }

                sb.Append("\" aria-pressed=\"");
                sb.Append(selected ? "true" : "false");
                sb.Append('"');
                if (opt.Disabled)
                {
                    sb.Append(" disabled");
                }

                sb.Append("><b><span class=\"journey-dive__rd\" aria-hidden=\"true\"></span>");
                sb.Append(WebUtility.HtmlEncode(culture[opt.TitleKey]));
                sb.Append("</b><small>");
                sb.Append(WebUtility.HtmlEncode(culture[opt.SubKey]));
                sb.Append("</small></button></form>");
            }

            sb.Append("</div></fieldset>");
        }

        sb.Append("<div class=\"journey-step__foot\"><span class=\"journey-step__sp\"></span>");
        sb.Append("<a class=\"journey-later\" href=\"");
            sb.Append(DiscoverySsrUrls.Overview());
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Back"]));
            sb.Append("</a>");
        if (model.ShowDiveDeeper)
        {
            sb.Append("<form method=\"post\" action=\"");
            sb.Append(DiscoverySsrUrls.DuikPostPath);
            sb.Append("\" style=\"display:inline\"><input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
            sb.Append(WebUtility.HtmlEncode(token));
            sb.Append("\" /><input type=\"hidden\" name=\"shedStep\" value=\"");
            sb.Append(model.ShedStep);
            sb.Append("\" /><button type=\"submit\" class=\"btn btn-primary\">");
            sb.Append(WebUtility.HtmlEncode(model.PrimaryLabel));
            sb.Append("</button></form>");
        }

        sb.Append("</div></section>");
    }

    private static void AppendTest(StringBuilder sb, CultureState culture, TestModel model, string token)
    {
        sb.Append("<section class=\"journey-step\" data-testid=\"discovery-test-step\">");
        sb.Append("<p class=\"journey-step__eyebrow\"><span class=\"journey-step__eyebrow-line\">");
        sb.Append(WebUtility.HtmlEncode(culture["Discovery.Zone.Deep"]));
        sb.Append("</span><span class=\"journey-step__eyebrow-step\"> · ");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Discovery.StepOf", model.Step)));
        sb.Append("</span></p><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture[model.TitleKey]));
        sb.Append("</h1>");

        if (model.BandComplete || model.Seed.MiniQuestions.Count == 0)
        {
            sb.Append("<p>");
            sb.Append(WebUtility.HtmlEncode(culture["Onboarding.MiniAllDone"]));
            sb.Append("</p><div class=\"journey-step__foot\"><a class=\"btn btn-ghost\" href=\"");
            sb.Append(DiscoverySsrUrls.Overview());
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Back"]));
            sb.Append("</a><form method=\"post\" action=\"");
            sb.Append(DiscoverySsrUrls.Path);
            sb.Append("\"><input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
            sb.Append(WebUtility.HtmlEncode(token));
            sb.Append("\" /><input type=\"hidden\" name=\"discoveryAction\" value=\"mini-continue\" /><input type=\"hidden\" name=\"step\" value=\"");
            sb.Append(model.Step);
            sb.Append("\" /><button type=\"submit\" class=\"btn btn-primary\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Continue"]));
            sb.Append("</button></form></div>");
        }
        else
        {
            sb.Append("<form method=\"post\" action=\"");
            sb.Append(DiscoverySsrUrls.Path);
            sb.Append("\" class=\"journey-test-form\"><input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
            sb.Append(WebUtility.HtmlEncode(token));
            sb.Append("\" /><input type=\"hidden\" name=\"discoveryAction\" value=\"mini-pick\" /><input type=\"hidden\" name=\"step\" value=\"");
            sb.Append(model.Step);
            sb.Append("\" /><input type=\"hidden\" name=\"diepte\" value=\"");
            sb.Append(JourneyTestFlow.DepthQueryValue(model.TargetLevel));
            sb.Append("\" />");
            sb.Append(OnboardingClientStepShells.BuildDiscoveryMiniBlock(
                culture,
                model.Step,
                model.Seed,
                model.ActiveQuestion1Based,
                model.TitleKey,
                model.HintKey));
            sb.Append("</form><div class=\"journey-step__foot\"><a class=\"journey-later\" href=\"");
            sb.Append(DiscoverySsrUrls.Overview());
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Discovery.Overview.Back"]));
            sb.Append("</a></div>");
        }

        sb.Append("</section>");
    }

    public static string PhaseStepsLabel(CultureState culture, int remaining)
        => remaining == 1
            ? culture["Discovery.Rail.PhaseOneStep"]
            : culture.Format("Discovery.Rail.PhaseSteps", remaining);

    public static string Halte(string id) => DiscoveryOverviewIllustrationAssets.Halte(id);
}
