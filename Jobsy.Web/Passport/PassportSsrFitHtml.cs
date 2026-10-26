using System.Net;
using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Hosting;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

namespace Jobsy.Web.Passport;

internal static class PassportSsrFitHtml
{
    internal sealed record Panel(
        RoleFitCheckState State,
        IReadOnlyList<PassportCourseCard> CourseSlots,
        string CourseSkill,
        int NewContacts,
        FutureJobsList? FutureJobs,
        string FutureJobsRegionLabel,
        string FutureJobsRegionNote);

    internal static async Task<Panel?> LoadPanelAsync(
        JobsyApiClient api,
        CultureState culture,
        FeatureFlagSnapshot flags,
        MeProfile profile,
        CandidateKompasState? kompas,
        string? checkTitle,
        CancellationToken cancellationToken)
    {
        RoleFitCheckState? state;
        try
        {
            state = await api.GetMyRoleFitAsync(cancellationToken) ?? new RoleFitCheckState();
        }
        catch
        {
            state = new RoleFitCheckState();
        }

        var title = (checkTitle ?? "").Trim();
        if (title.Length >= 2
            && state.IsUnlocked
            && !string.Equals(state.LastResult?.JobTitle, title, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                state = await api.EvaluateRoleFitAsync(title, vacancyId: null, cancellationToken);
            }
            catch
            {
                // Keep GET state when evaluate fails; the form can retry client-side.
            }
        }

        var newContacts = 0;
        try
        {
            var contacts = await api.ListCandidateTalentContactsAsync(cancellationToken) ?? [];
            newContacts = contacts.Count(c =>
                Enum.TryParse<TalentContactStatus>(c.Status, true, out var st)
                && st == TalentContactStatus.Pending);
        }
        catch
        {
            newContacts = 0;
        }

        var courseSlots = new List<PassportCourseCard>();
        var courseSkill = "";
        if (state.LastResult is { } last)
        {
            courseSkill = last.Gaps.FirstOrDefault() ?? last.JobTitle;
            try
            {
                var keys = new List<string>();
                if (last.Gaps.Count > 0)
                {
                    keys.Add(last.Gaps[0]);
                }

                keys.AddRange(last.SearchKeys.Take(4));
                if (!string.IsNullOrWhiteSpace(last.JobTitle))
                {
                    keys.Add(last.JobTitle);
                }

                var blob = string.Join(' ', keys.Where(k => !string.IsNullOrWhiteSpace(k)));
                if (!string.IsNullOrWhiteSpace(blob))
                {
                    courseSlots = await api.GetPassportTrainingOffersAsync(blob, cancellationToken);
                }
            }
            catch
            {
                courseSlots = [];
            }
        }

        FutureJobsList? futureJobs = null;
        var regionLabel = "";
        var regionNote = "";
        if (flags.FutureJobsForYouEnabled)
        {
            try
            {
                var career = await api.GetMyCareerInterestsAsync(cancellationToken);
                var scores = career?.Scores;
                RiasecScores? riasec = scores is { IsComplete: true }
                    ? new RiasecScores(
                        scores.Realistic,
                        scores.Investigative,
                        scores.Artistic,
                        scores.Social,
                        scores.Enterprising,
                        scores.Conventional)
                    : null;
                var prefs = profile.Preferences;
                regionLabel = FutureJobsRegionDisplay.RegionLabel(culture, prefs);
                regionNote = FutureJobsRegionDisplay.RegionNote(culture);
                var education = prefs is null
                    ? null
                    : string.Join(
                        " ",
                        (prefs.Educations ?? []).Append(prefs.EducationDirection ?? "")
                            .Where(item => !string.IsNullOrWhiteSpace(item)));
                futureJobs = FutureJobsForYou.Build(riasec, education);
            }
            catch
            {
                futureJobs = new FutureJobsList(false, [], OccupationOutlook.Shared.Peildatum);
            }
        }

        _ = kompas;
        return new Panel(state, courseSlots, courseSkill, newContacts, futureJobs, regionLabel, regionNote);
    }

    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        FeatureFlagSnapshot flags,
        MeProfile profile,
        CandidateKompasState? kompas,
        Panel? panel,
        bool proactiveMatching)
    {
        sb.Append("<div class=\"passport-fit\" data-passport-fit=\"1\" data-testid=\"passport-panel-fit-content\">");
        if (panel is null)
        {
            sb.Append("<p class=\"login-lead\" data-testid=\"passport-fit-soon\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Soon"]));
            sb.Append("</p></div>");
            return;
        }

        if (proactiveMatching)
        {
            sb.Append("<aside class=\"proactive-promo\" data-testid=\"proactive-promo-fit\"><div class=\"proactive-promo__copy\"><p class=\"proactive-promo__title\">");
            sb.Append(WebUtility.HtmlEncode(culture["ProactiveMatching.Promo.Title"]));
            sb.Append("</p><p class=\"proactive-promo__lead\">");
            sb.Append(WebUtility.HtmlEncode(culture["ProactiveMatching.Promo.Lead"]));
            sb.Append("</p></div><a class=\"btn btn-primary proactive-promo__cta\" href=\"/candidate/proactive-matching\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["ProactiveMatching.Promo.Cta"]));
            sb.Append("</a></aside>");
        }

        if (!panel.State.IsUnlocked)
        {
            AppendLocked(sb, culture, panel, flags.FutureJobsForYouEnabled);
            sb.Append("</div>");
            return;
        }

        AppendUnlocked(sb, culture, flags, profile, kompas, panel);
        sb.Append("</div>");
        sb.Append("<script src=\"");
        sb.Append(WwwrootAssetCatalog.Url("js/passport-ssr-fit.js"));
        sb.Append("\" defer></script>");
    }

    private static void AppendLocked(
        StringBuilder sb,
        CultureState culture,
        Panel panel,
        bool futureJobsEnabled)
    {
        sb.Append("<section class=\"detail-card passport-fit__card\"><h3 class=\"passport-fit__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Title"]));
        sb.Append("</h3><p class=\"login-lead\">");
        var lockMsg = string.IsNullOrWhiteSpace(panel.State.LockMessage)
            ? culture["Fit.Locked"]
            : panel.State.LockMessage;
        sb.Append(WebUtility.HtmlEncode(lockMsg));
        sb.Append("</p><div class=\"passport-fit__locked-actions\"><a class=\"btn btn-primary\" href=\"/candidate/competencies\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Competency.Start"]));
        sb.Append("</a><a class=\"btn btn-secondary\" href=\"/candidate/career\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Career.Start"]));
        sb.Append("</a></div></section>");
        if (futureJobsEnabled && panel.FutureJobs is { } list)
        {
            PassportSsrFutureJobsHtml.Append(sb, culture, list, panel.FutureJobsRegionLabel, panel.FutureJobsRegionNote, showCheck: false);
        }
    }

    private static void AppendUnlocked(
        StringBuilder sb,
        CultureState culture,
        FeatureFlagSnapshot flags,
        MeProfile profile,
        CandidateKompasState? kompas,
        Panel panel)
    {
        var result = panel.State.LastResult;
        var jobPrefill = result?.JobTitle ?? "";
        sb.Append("<div class=\"passport-fit__layout\"><div class=\"passport-fit__main\"><section class=\"detail-card passport-fit__card\" aria-labelledby=\"passport-fit-title\">");
        sb.Append("<header class=\"passport-fit__head\"><h3 id=\"passport-fit-title\" class=\"passport-fit__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Title"]));
        sb.Append("</h3></header><p class=\"passport-fit__sub\"><span class=\"passport-fit__sub-icon\" aria-hidden=\"true\">");
        sb.Append(NavIcons.Antenna);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Subtitle"]));
        sb.Append("</p><form class=\"passport-fit__form\" data-passport-fit-form=\"1\"><label class=\"visually-hidden\" for=\"passport-fit-job\">");
        sb.Append(WebUtility.HtmlEncode(culture["Fit.JobTitle"]));
        sb.Append("</label><input id=\"passport-fit-job\" class=\"passport-fit__input\" type=\"search\" maxlength=\"80\" autocomplete=\"off\" placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Placeholder"]));
        sb.Append("\" data-passport-fit-job=\"1\" value=\"");
        sb.Append(WebUtility.HtmlEncode(jobPrefill));
        sb.Append("\" /><button class=\"btn btn-primary\" type=\"submit\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Check"]));
        sb.Append("</button></form><p class=\"state-message\" role=\"alert\" data-passport-fit-error hidden>");
        sb.Append(WebUtility.HtmlEncode(culture["Common.Error"]));
        sb.Append("</p>");

        if (panel.State.NeedsRecheck)
        {
            sb.Append("<p class=\"state-message\" aria-live=\"polite\">");
            sb.Append(WebUtility.HtmlEncode(culture["Fit.NeedsRecheck"]));
            sb.Append("</p>");
        }

        if (result is not null)
        {
            sb.Append("<div class=\"passport-fit__last\"><p class=\"passport-fit__last-line\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Fit.LastCheck", result.JobTitle)));
            sb.Append("</p>");
            if (result.SimilarRoles.Count > 0)
            {
                sb.Append("<details class=\"passport-fit__similar-details\"><summary class=\"btn btn-ghost\">");
                sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Similar"]));
                sb.Append("</summary><ul class=\"passport-fit__similar\">");
                foreach (var role in result.SimilarRoles)
                {
                    sb.Append("<li><button type=\"button\" class=\"btn btn-secondary\" data-passport-fit-similar=\"");
                    sb.Append(WebUtility.HtmlEncode(role.Title));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(role.Title));
                    sb.Append("</button></li>");
                }

                sb.Append("</ul></details>");
            }

            sb.Append("</div><ol class=\"passport-fit__grid\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Title"]));
            sb.Append("\"><li class=\"passport-fit__cell\"><span class=\"passport-fit__cell-icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Antenna);
            sb.Append("</span><div><p class=\"passport-fit__cell-label muted\">1 · ");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Result.Antennas"]));
            sb.Append("</p><p class=\"passport-fit__cell-value\">");
            sb.Append(WebUtility.HtmlEncode(culture[RoleFitBandRules.LabelKey(result.MatchPercent)]));
            sb.Append("</p></div></li><li class=\"passport-fit__cell\"><span class=\"passport-fit__cell-icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Claw);
            sb.Append("</span><div><p class=\"passport-fit__cell-label muted\">2 · ");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Result.Claws"]));
            sb.Append("</p><p class=\"passport-fit__cell-value\">");
            sb.Append(WebUtility.HtmlEncode(TopStrengths(result)));
            sb.Append("</p></div></li><li class=\"passport-fit__cell passport-fit__cell--warn\"><span class=\"passport-fit__cell-icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Claw);
            sb.Append("</span><div><p class=\"passport-fit__cell-label muted\">3 · ");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Result.Growing"]));
            sb.Append("</p><p class=\"passport-fit__cell-value\">");
            sb.Append(WebUtility.HtmlEncode(FirstLine(result.Gaps)));
            sb.Append("</p></div></li><li class=\"passport-fit__cell\"><span class=\"passport-fit__cell-icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Wave);
            sb.Append("</span><div><p class=\"passport-fit__cell-label muted\">4 · ");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Result.Action"]));
            sb.Append("</p><p class=\"passport-fit__cell-value\">");
            sb.Append(WebUtility.HtmlEncode(FirstLine(result.ActionSteps)));
            sb.Append("</p></div></li></ol>");

            if (panel.CourseSlots.Count > 0)
            {
                AppendCourseSection(sb, culture, "Passport.Fit.CourseEyebrow", panel.CourseSkill, panel.CourseSlots);
            }
        }

        sb.Append("</section></div>");

        if (flags.FutureJobsForYouEnabled && panel.FutureJobs is { } fj)
        {
            PassportSsrFutureJobsHtml.Append(sb, culture, fj, panel.FutureJobsRegionLabel, panel.FutureJobsRegionNote, showCheck: true);
        }

        AppendAside(sb, culture, flags, profile, kompas, panel);
        sb.Append("</div>");
    }

    private static void AppendAside(
        StringBuilder sb,
        CultureState culture,
        FeatureFlagSnapshot flags,
        MeProfile profile,
        CandidateKompasState? kompas,
        Panel panel)
    {
        var result = panel.State.LastResult;
        sb.Append("<aside class=\"passport-fit__aside\">");
        if (result is { CultureFitLabel: { Length: > 0 } cultureLabel })
        {
            sb.Append("<section class=\"detail-card passport-fit__culture\" aria-labelledby=\"passport-fit-culture-title\"><p class=\"passport-fit__culture-icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Antenna);
            sb.Append("</p><h3 id=\"passport-fit-culture-title\" class=\"passport-fit__h\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Fit.CultureTitle", cultureLabel)));
            sb.Append("</h3><p class=\"passport-fit__sub muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.CultureSub"]));
            sb.Append("</p>");
            AppendCulturePoleSliders(sb, culture, kompas);
            if (result.CultureFitPercent is int culturePct)
            {
                sb.Append("<span class=\"status-pill status-pill--");
                sb.Append(RoleFitBandRules.CssModifier(culturePct));
                sb.Append("\">");
                sb.Append(WebUtility.HtmlEncode(culture[RoleFitBandRules.LabelKey(culturePct)]));
                sb.Append("</span>");
            }
            else if (!string.IsNullOrWhiteSpace(result.CultureFitLabel))
            {
                sb.Append("<span class=\"status-pill\">");
                sb.Append(WebUtility.HtmlEncode(result.CultureFitLabel));
                sb.Append("</span>");
            }

            sb.Append("</section>");
        }
        else
        {
            sb.Append("<p class=\"passport-fit__culture-cta\"><a class=\"btn btn-ghost\" href=\"/candidate/culture\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.DoCultureScan"]));
            sb.Append("</a></p>");
        }

        if (flags.EmployersEnabled)
        {
            var topMatches = (kompas?.TopMatches ?? []).Take(3).ToList();
            var openForWork = profile.OpenForWork;
            sb.Append("<section class=\"detail-card passport-fit__vacancies\" aria-labelledby=\"passport-fit-vacancies-title\"><header class=\"passport-fit__head\"><h3 id=\"passport-fit-vacancies-title\" class=\"passport-fit__h\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.VacanciesTitle"]));
            sb.Append("</h3><a class=\"btn btn-ghost\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.Top10"]));
            sb.Append("</a></header><p class=\"passport-fit__sub\"><span class=\"passport-fit__sub-icon\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Stone);
            sb.Append("</span>");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.VacanciesSub"]));
            sb.Append("</p>");

            if (topMatches.Count == 0 && !openForWork)
            {
                sb.Append("<div class=\"kb-open-for-work\" data-testid=\"open-for-work-prompt\" role=\"note\"><p>");
                sb.Append(WebUtility.HtmlEncode(culture["Kb.OpenForWork.Required"]));
                sb.Append("</p><a class=\"btn-compact btn-compact--primary\" href=\"/candidate/paspoort?tab=data&amp;section=availability\" data-passport-tab-link=\"data\" data-enhance-nav=\"false\">");
                sb.Append(WebUtility.HtmlEncode(culture["Kb.OpenForWork.Button"]));
                sb.Append("</a></div>");
            }
            else if (topMatches.Count == 0)
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.VacanciesEmpty"]));
                sb.Append("</p>");
            }
            else
            {
                sb.Append("<ul class=\"passport-fit__vacancy-list\">");
                foreach (var match in topMatches)
                {
                    sb.Append("<li class=\"passport-fit__vacancy\"><span class=\"passport-fit__logo\" aria-hidden=\"true\">");
                    sb.Append(WebUtility.HtmlEncode(Initial(match.CompanyName)));
                    sb.Append("</span><div class=\"passport-fit__vacancy-body\"><a href=\"/vacancies/");
                    sb.Append(match.Id);
                    sb.Append("\" class=\"passport-fit__vacancy-title\" data-enhance-nav=\"false\">");
                    sb.Append(WebUtility.HtmlEncode(match.Title));
                    sb.Append("</a><p class=\"muted\">");
                    sb.Append(WebUtility.HtmlEncode(match.CompanyName));
                    sb.Append("</p></div><span class=\"status-pill status-pill--");
                    sb.Append(RoleFitBandRules.CssModifier(match.MatchPercent));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(culture[RoleFitBandRules.LabelKey(match.MatchPercent)]));
                    sb.Append("</span></li>");
                }

                sb.Append("</ul>");
            }

            sb.Append("<div class=\"passport-fit__contacts\"><div><p class=\"passport-fit__contacts-title\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.EmployersWant"]));
            sb.Append("</p>");
            if (panel.NewContacts > 0)
            {
                sb.Append("<span class=\"status-pill\">");
                sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Fit.NewCount", panel.NewContacts)));
                sb.Append("</span>");
            }

            sb.Append("</div><a class=\"btn btn-secondary\" href=\"/candidate/talent-contacts\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fit.ViewContacts"]));
            sb.Append("</a></div></section>");
        }

        sb.Append("</aside>");
    }

    private static void AppendCulturePoleSliders(StringBuilder sb, CultureState culture, CandidateKompasState? kompas)
    {
        if (kompas is null)
        {
            return;
        }

        CandidateDnaViewBuilder.TextLookup t = k => culture[k];
        CandidateDnaViewBuilder.TextFormat format = (k, args) => culture.Format(k, args);
        var view = CandidateDnaViewBuilder.FromKompas(kompas, t, format, culture.Language);
        var detail = view.CultureDetail;
        if (detail is null || detail.IsGhost || detail.Poles.Count == 0)
        {
            return;
        }

        PassportSsrPanelsHtml.AppendPoleSliders(sb, detail.Poles.Take(6).ToList());
    }

    internal static void AppendCourseSection(
        StringBuilder sb,
        CultureState culture,
        string eyebrowKey,
        string skillLabel,
        IReadOnlyList<PassportCourseCard> slots)
    {
        sb.Append("<section class=\"passport-course\" aria-labelledby=\"passport-fit-course-title\"><div class=\"passport-course__head\"><p class=\"passport-course__eyebrow\" id=\"passport-fit-course-title\"><span aria-hidden=\"true\">");
        sb.Append(NavIcons.Claw);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture[eyebrowKey]));
        sb.Append("</p></div><ul class=\"passport-course__list\">");
        foreach (var slot in slots)
        {
            sb.Append("<li class=\"passport-course__item ");
            sb.Append(slot.IsFree ? "passport-course__item--free" : "passport-course__item--partner");
            sb.Append("\"><div class=\"passport-course__line1\">");
            if (slot.IsFree)
            {
                sb.Append("<span class=\"passport-course__pill passport-course__pill--free\"><span aria-hidden=\"true\">✓</span> ");
                sb.Append(WebUtility.HtmlEncode(culture["Passport.Course.Free"]));
                sb.Append("</span>");
            }
            else
            {
                sb.Append("<span class=\"passport-course__pill passport-course__pill--partner\">");
                sb.Append(WebUtility.HtmlEncode(culture["Passport.Course.Partner"]));
                sb.Append("</span>");
            }

            sb.Append("<strong class=\"passport-course__title\">");
            sb.Append(WebUtility.HtmlEncode(slot.Title));
            sb.Append("</strong></div><p class=\"passport-course__why\">");
            if (slot.IsFree)
            {
                sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Course.FreeWhy", skillLabel)));
            }
            else
            {
                sb.Append(WebUtility.HtmlEncode(culture["Passport.Course.PartnerWhy"]));
            }

            sb.Append("</p></li>");
        }

        sb.Append("</ul></section>");
    }

    private static string TopStrengths(RoleFitCheckResult result)
    {
        var parts = result.Strengths
            .Select(CandidateFitDisplay.SanitizePassportCandidateCopy)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(2)
            .ToList();
        return parts.Count == 0 ? "-" : string.Join(", ", parts!);
    }

    private static string FirstLine(IReadOnlyList<string> lines)
    {
        var first = lines
            .Select(CandidateFitDisplay.SanitizePassportCandidateCopy)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        return string.IsNullOrWhiteSpace(first) ? "-" : first!;
    }

    private static string Initial(string? company)
    {
        if (string.IsNullOrWhiteSpace(company))
        {
            return "?";
        }

        return company.Trim()[..1].ToUpperInvariant();
    }
}
