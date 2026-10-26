using System.Net;
using System.Text;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

namespace Jobsy.Web.Passport;

internal static class PassportSsrCareerHtml
{
    internal sealed record Panel(
        CareerPlanViewBuilder.PassportCareerView View,
        IReadOnlyList<PassportCourseCard> CourseSlots,
        string CourseSkill);

    internal static async Task<Panel> LoadPanelAsync(
        JobsyApiClient api,
        MeProfile profile,
        CancellationToken cancellationToken)
    {
        CareerPlanViewBuilder.PassportCareerView view;
        try
        {
            var saved = await api.GetCareerPathAsync(cancellationToken);
            if (saved is not null && saved.Steps.Count > 0)
            {
                var paths = new CareerPathService();
                var model = paths.FromApi(saved);
                var evidence = CareerPlanViewBuilder.EvidenceFrom(profile.Preferences);
                var years = CareerPlanViewBuilder.ExperienceYears(profile.Preferences?.Employers);
                view = CareerPlanViewBuilder.BuildPassport(model, evidence, years);
            }
            else
            {
                view = CareerPlanViewBuilder.BuildPassport(new CareerPathService().EmptyDashboard());
            }
        }
        catch
        {
            view = CareerPlanViewBuilder.BuildPassport(null);
        }

        var courseSlots = new List<PassportCourseCard>();
        var courseSkill = view.FocusStep?.Title ?? view.DreamRoleTitle;
        if (view.CourseSearchKeys.Count > 0)
        {
            try
            {
                var blob = string.Join(' ', view.CourseSearchKeys);
                courseSlots = await api.GetPassportTrainingOffersAsync(blob, cancellationToken);
            }
            catch
            {
                courseSlots = [];
            }
        }

        return new Panel(view, courseSlots, courseSkill);
    }

    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        bool employersEnabled,
        Panel panel)
    {
        sb.Append("<div class=\"passport-career\" data-testid=\"passport-panel-career-content\">");
        var view = panel.View;
        if (!view.HasPlan)
        {
            sb.Append("<section class=\"detail-card passport-career__empty kompas-card--illustrated\" aria-labelledby=\"passport-career-empty-title\">");
            sb.Append("<div class=\"kompas-card-spot kompas-card-spot--career\" style=\"background-image:url('");
            sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SpotDroombaan));
            sb.Append("');background-position:85% 20%\" aria-hidden=\"true\"></div><h3 id=\"passport-career-empty-title\" class=\"passport-fit__h\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.EmptyTitle"]));
            sb.Append("</h3><p class=\"login-lead\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.EmptyLead"]));
            sb.Append("</p><a class=\"btn btn-primary\" href=\"/carriere\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.EmptyCta"]));
            sb.Append("</a></section></div>");
            return;
        }

        sb.Append("<div class=\"passport-career__toolbar\"><a class=\"btn btn-secondary\" href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.ChangeDream"]));
        sb.Append("</a></div><section class=\"detail-card passport-career__dream\" aria-labelledby=\"passport-career-dream-title\">");
        sb.Append("<header class=\"passport-fit__head\"><h3 id=\"passport-career-dream-title\" class=\"passport-fit__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.DreamTitle"]));
        sb.Append("</h3><a class=\"btn btn-ghost\" href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.OpenPlan"]));
        sb.Append("</a></header><p class=\"passport-career__dream-name\">");
        sb.Append(WebUtility.HtmlEncode(view.DreamRoleTitle));
        sb.Append("</p><nav class=\"passport-career__shells\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["CareerDash.PlanTitle"]));
        sb.Append("\"><ol class=\"passport-career__shell-list\">");

        foreach (var shell in view.Shells)
        {
            sb.Append("<li class=\"passport-career__shell ");
            sb.Append(ShellKindClass(shell.Kind));
            sb.Append(' ');
            sb.Append(ShellSizeClass(shell));
            sb.Append("\"><span class=\"passport-career__shell-node\" aria-hidden=\"true\">");
            if (shell.Kind == CareerPlanViewBuilder.ShellKind.Done)
            {
                sb.Append("<span class=\"passport-career__shell-check\">✓</span>");
            }
            else if (shell.Kind == CareerPlanViewBuilder.ShellKind.Goal)
            {
                sb.Append("<span class=\"passport-career__shell-goal\">★</span>");
            }
            else
            {
                sb.Append("<span class=\"passport-career__shell-num\">");
                sb.Append(shell.Order);
                sb.Append("</span>");
            }

            sb.Append("</span><span class=\"passport-career__shell-cap\">");
            sb.Append(WebUtility.HtmlEncode(ShellCap(culture, shell)));
            sb.Append("</span><span class=\"passport-career__shell-title\">");
            sb.Append(WebUtility.HtmlEncode(shell.Title));
            sb.Append("</span></li>");
        }

        sb.Append("</ol></nav></section><div class=\"passport-career__grid\">");
        AppendGapsPanel(sb, culture, view);
        AppendCoursePanel(sb, culture, panel);
        AppendMatchPanel(sb, culture, employersEnabled, view);
        sb.Append("</div></div>");
    }

    private static void AppendGapsPanel(StringBuilder sb, CultureState culture, CareerPlanViewBuilder.PassportCareerView view)
    {
        sb.Append("<section class=\"detail-card passport-career__panel\" aria-labelledby=\"passport-career-gaps-title\"><h3 id=\"passport-career-gaps-title\" class=\"passport-fit__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.GapsTitle"]));
        sb.Append("</h3><p class=\"passport-fit__sub\"><span class=\"passport-fit__sub-icon\" aria-hidden=\"true\">");
        sb.Append(NavIcons.Claw);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.GapsSub"]));
        sb.Append("</p>");
        if (view.Gaps.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["CareerDash.MinRequirementsNone"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"passport-career__gaps\">");
            foreach (var gap in view.Gaps)
            {
                sb.Append("<li class=\"passport-career__gap ");
                sb.Append(gap.Met ? "is-met" : "is-gap");
                sb.Append("\">");
                if (gap.Met)
                {
                    sb.Append("<span class=\"passport-career__gap-icon\" aria-hidden=\"true\">✓</span><span>");
                    sb.Append(WebUtility.HtmlEncode(gap.Text));
                    sb.Append(": ");
                    sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.AlreadyHave"]));
                    sb.Append("</span>");
                }
                else
                {
                    sb.Append("<span class=\"passport-career__gap-icon passport-career__gap-icon--warn\" aria-hidden=\"true\">");
                    sb.Append(NavIcons.Claw);
                    sb.Append("</span><span>");
                    sb.Append(WebUtility.HtmlEncode(gap.Text));
                    sb.Append("</span>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("</section>");
    }

    private static void AppendCoursePanel(StringBuilder sb, CultureState culture, Panel panel)
    {
        sb.Append("<section class=\"detail-card passport-career__panel\" aria-labelledby=\"passport-career-course-title\"><h3 id=\"passport-career-course-title\" class=\"passport-fit__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.CourseTitle"]));
        sb.Append("</h3>");
        if (panel.CourseSlots.Count == 0 && panel.View.CourseNames.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.CourseEmpty"]));
            sb.Append("</p>");
        }
        else if (panel.CourseSlots.Count == 0)
        {
            sb.Append("<p class=\"career-courses__plain\"><span class=\"career-courses__icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Claw);
            sb.Append("</span>");
            sb.Append(WebUtility.HtmlEncode(culture.Format("CareerStep.Courses.Plain", string.Join(", ", panel.View.CourseNames))));
            sb.Append("</p>");
        }
        else
        {
            PassportSsrFitHtml.AppendCourseSection(sb, culture, "Passport.Course.GrowFurther", panel.CourseSkill, panel.CourseSlots);
        }

        sb.Append("</section>");
    }

    private static void AppendMatchPanel(
        StringBuilder sb,
        CultureState culture,
        bool employersEnabled,
        CareerPlanViewBuilder.PassportCareerView view)
    {
        sb.Append("<section class=\"detail-card passport-career__panel\" aria-labelledby=\"passport-career-match-title\"><h3 id=\"passport-career-match-title\" class=\"passport-fit__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.MatchTitle"]));
        sb.Append("</h3><p class=\"passport-career__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.MatchEyebrow"]));
        sb.Append("</p><p class=\"passport-fit__sub\"><span class=\"passport-fit__sub-icon\" aria-hidden=\"true\">");
        sb.Append(NavIcons.Stone);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.MatchStone"]));
        sb.Append("</p>");
        if (!string.IsNullOrWhiteSpace(view.StepBandLabelKey) && view.FocusStep is { } focus)
        {
            sb.Append("<p class=\"passport-career__match-text\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Career.MatchBand", culture[view.StepBandLabelKey!], focus.Title)));
            sb.Append("</p>");
        }

        if (employersEnabled)
        {
            sb.Append("<a class=\"btn btn-ghost\" href=\"");
            sb.Append(WebUtility.HtmlEncode(view.VacanciesHref));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Career.VacanciesLink"]));
            sb.Append("</a>");
        }

        sb.Append("</section>");
    }

    private static string ShellCap(CultureState culture, CareerPlanViewBuilder.ShellStep shell)
    {
        if (shell.IsGoal)
        {
            return culture["CareerDash.StepperGoal"];
        }

        if (shell.Kind == CareerPlanViewBuilder.ShellKind.Done && shell.Order == 1)
        {
            return culture["Passport.Career.ShellNow"];
        }

        if (shell.Kind == CareerPlanViewBuilder.ShellKind.Current)
        {
            return culture["Passport.Career.ShellGrowing"];
        }

        return culture.Format("Passport.Career.ShellStep", shell.Order);
    }

    private static string ShellKindClass(CareerPlanViewBuilder.ShellKind kind) => kind switch
    {
        CareerPlanViewBuilder.ShellKind.Done => "is-done",
        CareerPlanViewBuilder.ShellKind.Current => "is-current",
        CareerPlanViewBuilder.ShellKind.Goal => "is-goal",
        _ => "is-future"
    };

    private static string ShellSizeClass(CareerPlanViewBuilder.ShellStep shell)
    {
        if (shell.IsGoal)
        {
            return "passport-career__shell--lg";
        }

        return shell.Order switch
        {
            1 => "passport-career__shell--sm",
            2 => "passport-career__shell--md",
            _ => "passport-career__shell--lg"
        };
    }
}
