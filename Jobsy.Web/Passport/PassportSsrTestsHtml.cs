using System.Net;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Passport;

internal static class PassportSsrTestsHtml
{
    internal static void Append(StringBuilder sb, CultureState culture, CandidateKompasState? kompas)
    {
        sb.Append("<div class=\"passport-tests\" data-testid=\"passport-panel-tests-content\">");
        if (kompas is null)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
            sb.Append("</p></div>");
            return;
        }

        var model = TestsOverviewBuilder.FromKompas(kompas);
        sb.Append("<div class=\"passport-tests__depth\" aria-hidden=\"true\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tests.Surface"]));
        sb.Append("</span><div class=\"passport-tests__depth-track\"><span class=\"passport-tests__depth-seg passport-tests__depth-seg--1\"></span><span class=\"passport-tests__depth-seg passport-tests__depth-seg--2\"></span><span class=\"passport-tests__depth-seg passport-tests__depth-seg--3\"></span></div><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tests.Deep"]));
        sb.Append("</span></div><div class=\"passport-tests__layout kompas-card--illustrated\">");
        sb.Append("<div class=\"kompas-card-spot kompas-card-spot--tests\" style=\"background-image:url('");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SpotJobLogistiek));
        sb.Append("');background-position:70% 15%\" aria-hidden=\"true\"></div><div class=\"passport-tests__rows\" role=\"list\">");

        foreach (var tile in model.Tiles)
        {
            var selected = model.HighlightKind == tile.Kind;
            var (quick, extended, report, reportLocked) = TestsOverviewBuilder.ProgressSegments(tile);
            var btnClass = tile.Action == TestsOverviewBuilder.RowAction.Extended
                ? "btn btn-secondary"
                : "btn btn-primary";

            sb.Append("<article class=\"passport-tests__row");
            if (selected)
            {
                sb.Append(" passport-tests__row--selected");
            }

            sb.Append("\" role=\"listitem\"><div class=\"passport-tests__row-main\"><div class=\"passport-tests__row-id\"><span class=\"passport-tests__row-icon\" aria-hidden=\"true\">");
            sb.Append(RowIcon(tile.Kind));
            sb.Append("</span><div><h3 class=\"passport-tests__row-title\">");
            sb.Append(WebUtility.HtmlEncode(culture[tile.TitleKey]));
            sb.Append("</h3><p class=\"passport-tests__row-q muted\">");
            sb.Append(WebUtility.HtmlEncode(culture[tile.QuestionKey]));
            sb.Append("</p></div></div><a class=\"");
            sb.Append(btnClass);
            sb.Append("\" href=\"");
            sb.Append(WebUtility.HtmlEncode(TestsOverviewBuilder.ActionHref(tile)));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture[TestsOverviewBuilder.ActionLabelKey(tile.Action)]));
            sb.Append("</a></div><div class=\"passport-tests__progress\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Tests.DepthAria"]));
            sb.Append("\"><div class=\"passport-tests__bars\"><span class=\"passport-tests__bar ");
            sb.Append(FillClass(quick));
            sb.Append("\" title=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Tests.ScanQuick"]));
            sb.Append("\"></span><span class=\"passport-tests__bar ");
            sb.Append(FillClass(extended));
            sb.Append("\" title=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Tests.ScanExtended"]));
            sb.Append("\"></span><span class=\"passport-tests__bar ");
            sb.Append(FillClass(report));
            if (reportLocked)
            {
                sb.Append(" is-locked");
            }

            sb.Append("\" title=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Tests.Report"]));
            sb.Append("\"></span></div><div class=\"passport-tests__bar-labels muted\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Tests.ScanQuick"]));
            sb.Append("</span><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Tests.ScanExtended"]));
            sb.Append("</span><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Tests.Report"]));
            sb.Append("</span></div></div></article>");
        }

        sb.Append("<p class=\"passport-tests__cost\"><a class=\"btn btn-ghost\" href=\"/profiel/tests/competentie\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tests.WhatCosts"]));
        sb.Append("</a></p></div>");

        AppendLockedAside(sb, culture, model);
        sb.Append("</div></div>");
    }

    private static void AppendLockedAside(StringBuilder sb, CultureState culture, TestsOverviewBuilder.OverviewModel model)
    {
        if (model.LockedPreviewKind is not AssessmentKind kind)
        {
            sb.Append("<aside class=\"passport-tests__aside\" aria-hidden=\"true\"></aside>");
            return;
        }

        var def = AssessmentTestCatalog.TryGet(kind);
        if (def is null)
        {
            return;
        }

        var tile = model.Tiles.First(t => t.Kind == kind);
        sb.Append("<aside class=\"passport-tests__aside\"><section class=\"passport-tests-locked\" aria-labelledby=\"passport-tests-locked-title\"><header class=\"passport-tests-locked__head\"><h3 id=\"passport-tests-locked-title\" class=\"passport-tests-locked__title\">");
        sb.Append(WebUtility.HtmlEncode(culture[def.TitleKey]));
        sb.Append(" · ");
        sb.Append(WebUtility.HtmlEncode(culture["TestResult.Locked.Title"]));
        sb.Append("</h3></header><p class=\"login-lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Bubble.Tests"]));
        sb.Append("</p><a class=\"btn btn-primary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(def.DeepStartHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture[TestsOverviewBuilder.ActionLabelKey(tile.Action)]));
        sb.Append("</a></section></aside>");
    }

    private static string FillClass(double fill)
    {
        if (fill >= 1)
        {
            return "is-done";
        }

        if (fill <= 0)
        {
            return "";
        }

        if (fill < 0.35)
        {
            return "is-partial is-p25";
        }

        if (fill < 0.60)
        {
            return "is-partial is-p50";
        }

        return "is-partial is-p75";
    }

    private static string RowIcon(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Competence => NavIcons.Claw,
        AssessmentKind.Career => NavIcons.Target,
        AssessmentKind.Culture => NavIcons.HomeHeart,
        _ => NavIcons.Stone
    };
}
