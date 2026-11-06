using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

/// <summary>SSR body HTML for teacher pupil code detail (no Blazor).</summary>
public static class TeacherCodeDetailSsrHtml
{
    public static string BuildBody(
        Guid classId,
        Guid codeId,
        TeacherCodeDetailDto detail,
        PupilG78PassportWebDto? g78Passport,
        bool passportV2,
        CultureState culture)
    {
        if ((detail.ResultPending || (detail.Status == PupilCodeStatus.Completed && detail.Story is null))
            && !(detail.QuestionSet == PupilQuestionSet.MboHbo && detail.PdfAvailable))
        {
            return AppendBreadcrumb(new StringBuilder(), classId, detail, culture)
                .Append("<h1>")
                .Append(E(detail.DisplayCode))
                .Append("</h1></header><p class=\"sch-empty-state panel-section\">")
                .Append(E(culture["Leraar.Detail.ResultPending"]))
                .Append("</p>")
                .ToString();
        }

        if (detail.QuestionSet == PupilQuestionSet.MboHbo && detail.PdfAvailable)
        {
            return BuildMboHboCompleted(classId, codeId, detail, culture);
        }

        if (detail.Status != PupilCodeStatus.Completed)
        {
            return BuildInProgress(classId, detail, culture);
        }

        return BuildCompleted(classId, codeId, detail, g78Passport, passportV2, culture);
    }

    private static string BuildMboHboCompleted(Guid classId, Guid codeId, TeacherCodeDetailDto detail, CultureState culture)
    {
        var sb = AppendBreadcrumb(new StringBuilder(), classId, detail, culture, "leraar-code-detail-mbo");
        sb.Append("<h1>");
        sb.Append(E(detail.DisplayCode));
        sb.Append("</h1><p class=\"muted\">MBO/HBO-paspoort · ");
        sb.Append(detail.ProgressCurrent.ToString(CultureInfo.InvariantCulture));
        sb.Append('/');
        sb.Append(detail.ProgressTotal.ToString(CultureInfo.InvariantCulture));
        sb.Append(" testen afgerond");
        if (detail.DurationMinutes is int dm)
        {
            sb.Append(" · ");
            sb.Append(dm.ToString(CultureInfo.InvariantCulture));
            sb.Append(" min");
        }

        sb.Append("</p><div class=\"sch-admin-toolbar\"><a class=\"btn-primary\" href=\"");
        sb.Append($"/leraar/klas/{classId:D}/code/{codeId:D}/report.pdf");
        sb.Append("\">");
        sb.Append(E(culture["Leraar.Detail.Pdf"]));
        sb.Append("</a><a class=\"btn-compact\" href=\"");
        sb.Append($"/leraar/klas/{classId:D}/mbo-hbo");
        sb.Append("\">MBO/HBO-voortgang</a></div><p class=\"sch-banner-info\">");
        sb.Append(E(culture["Leraar.Detail.NoNameNote"]));
        sb.Append("</p></header>");

        if (detail.MboHboOutcomes is { Count: > 0 } outcomes)
        {
            sb.Append("<div class=\"sch-detail-grid\">");
            foreach (var block in outcomes)
            {
                sb.Append("<section class=\"sch-card\"><h2>");
                sb.Append(E(block.Title));
                sb.Append("</h2><p>");
                sb.Append(E(block.Summary));
                sb.Append("</p>");
                if (block.TopBars.Count > 0)
                {
                    sb.Append("<ul class=\"sch-prompt-list\">");
                    foreach (var bar in block.TopBars)
                    {
                        sb.Append("<li>");
                        sb.Append(E(bar.Label));
                        sb.Append(" · ");
                        sb.Append(bar.Percent.ToString(CultureInfo.InvariantCulture));
                        sb.Append("%</li>");
                    }

                    sb.Append("</ul>");
                }
                else if (block.Highlights.Count > 0)
                {
                    sb.Append("<div class=\"sch-chip-list\">");
                    foreach (var highlight in block.Highlights)
                    {
                        sb.Append("<span class=\"sch-chip\">");
                        sb.Append(E(highlight));
                        sb.Append("</span>");
                    }

                    sb.Append("</div>");
                }

                sb.Append("</section>");
            }

            sb.Append("</div>");
        }
        else
        {
            sb.Append("<section class=\"sch-card panel-section\"><p>Alle vijf testen zijn afgerond. Download het paspoort-PDF of bekijk het klassenoverzicht.</p></section>");
        }

        return sb.ToString();
    }

    private static string BuildInProgress(Guid classId, TeacherCodeDetailDto detail, CultureState culture)
    {
        var sb = AppendBreadcrumb(new StringBuilder(), classId, detail, culture);
        sb.Append("<h1>");
        sb.Append(E(detail.DisplayCode));
        sb.Append("</h1></header>");
        if (detail.QuestionSet == PupilQuestionSet.MboHbo
            && detail.ProgressTotal == PupilMboHboTeacherProgress.BlockCount
            && detail.ProgressCurrent == PupilMboHboTeacherProgress.BlockCount)
        {
            sb.Append("<p class=\"sch-empty-state panel-section\">Alle MBO/HBO-testen zijn ingevuld. ");
            sb.Append("<a class=\"btn-compact\" href=\"");
            sb.Append($"/leraar/klas/{classId:D}/mbo-hbo");
            sb.Append("\">Bekijk MBO/HBO-voortgang</a></p>");
        }
        else
        {
            sb.Append("<p class=\"sch-empty-state panel-section\">");
            sb.Append(E(string.Format(CultureInfo.CurrentCulture, culture["Leraar.Detail.InProgress"], detail.ProgressCurrent, detail.ProgressTotal)));
            sb.Append("</p>");
        }

        return sb.ToString();
    }

    private static string BuildCompleted(
        Guid classId,
        Guid codeId,
        TeacherCodeDetailDto detail,
        PupilG78PassportWebDto? g78Passport,
        bool passportV2,
        CultureState culture)
    {
        var sb = AppendBreadcrumb(new StringBuilder(), classId, detail, culture, "leraar-code-detail");
        sb.Append("<h1>");
        sb.Append(E(detail.DisplayCode));
        sb.Append("</h1><p class=\"muted\">Klas ");
        sb.Append(E(detail.ClassName));
        if (detail.CompletedAtUtc is DateTime done)
        {
            sb.Append(" · ");
            sb.Append(E(culture["Leraar.Detail.CompletedOn"]));
            sb.Append(' ');
            sb.Append(E(LobsyTime.ToAmsterdam(done).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)));
        }

        sb.Append(" · ");
        sb.Append(E(QuestionCount(culture, detail)));
        if (detail.DurationMinutes is int mins)
        {
            sb.Append(" · ");
            sb.Append(mins.ToString(CultureInfo.InvariantCulture));
            sb.Append(" min");
        }

        sb.Append("</p><div class=\"sch-admin-toolbar\"><a class=\"btn-primary\" href=\"");
        sb.Append($"/leraar/klas/{classId:D}/code/{codeId:D}/report.pdf");
        sb.Append("\">");
        sb.Append(E(culture["Leraar.Detail.Pdf"]));
        sb.Append("</a></div><p class=\"sch-banner-info\">");
        sb.Append(E(culture["Leraar.Detail.NoNameNote"]));
        sb.Append("</p><p class=\"muted\">");
        sb.Append(E(culture["Leraar.Detail.OwnListHint"]));
        sb.Append("</p></header>");

        if (g78Passport is { Enabled: true })
        {
            sb.Append("<section class=\"sch-card sch-passport-g78\" data-testid=\"leraar-paspoort-g78\"><h2>");
            sb.Append(E(culture["LeerlingPassport.Page.Title"]));
            sb.Append("</h2>");
            sb.Append(PupilG78PassportSsrHtml.Render(g78Passport, culture));
            sb.Append("</section>");
        }

        sb.Append("<div class=\"sch-detail-grid\"><div>");
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["Leraar.Detail.StoryTitle"]));
        sb.Append("</h2>");
        if (detail.Story is not null)
        {
            sb.Append("<p>");
            sb.Append(E(detail.Story.Body));
            sb.Append("</p><div class=\"sch-tile-grid\">");
            foreach (var tile in detail.Story.Tiles)
            {
                sb.Append("<div class=\"sch-tile\"><p class=\"sch-tile__label\">");
                sb.Append(E(tile.KidLabel));
                sb.Append("</p><p class=\"sch-tile__value\">");
                sb.Append(E(tile.Explanation));
                sb.Append("</p></div>");
            }

            sb.Append("</div>");
        }

        sb.Append("</section>");
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["Leraar.Detail.Likes"]));
        sb.Append("</h2><div class=\"sch-chip-list\">");
        foreach (var chip in detail.Likes)
        {
            sb.Append("<span class=\"sch-chip\">");
            sb.Append(E(ChipText(culture, chip)));
            sb.Append("</span>");
        }

        if (!string.IsNullOrWhiteSpace(detail.LikeOtherWord))
        {
            sb.Append("<span class=\"sch-chip\">");
            sb.Append(E(OwnWord(culture, detail.LikeOtherWord)));
            sb.Append("</span>");
        }

        sb.Append("</div>");
        if (detail.Dislikes.Count > 0 || !string.IsNullOrWhiteSpace(detail.DislikeOtherWord))
        {
            sb.Append("<h2>");
            sb.Append(E(culture["Leraar.Detail.Dislikes"]));
            sb.Append("</h2><div class=\"sch-chip-list\">");
            foreach (var chip in detail.Dislikes)
            {
                sb.Append("<span class=\"sch-chip\">");
                sb.Append(E(ChipText(culture, chip)));
                sb.Append("</span>");
            }

            if (!string.IsNullOrWhiteSpace(detail.DislikeOtherWord))
            {
                sb.Append("<span class=\"sch-chip\">");
                sb.Append(E(OwnWord(culture, detail.DislikeOtherWord)));
                sb.Append("</span>");
            }

            sb.Append("</div>");
        }

        sb.Append("</section>");
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["Leraar.Detail.Starters"]));
        sb.Append("</h2><ul class=\"sch-prompt-list\">");
        foreach (var line in detail.ConversationStarterKeys)
        {
            sb.Append("<li>");
            sb.Append(E(line));
            sb.Append("</li>");
        }

        sb.Append("</ul></section></div><aside>");
        sb.Append("<section class=\"sch-card\"><h2>");
        sb.Append(E(culture["Leraar.Detail.DreamTitle"]));
        sb.Append("</h2><p class=\"muted\">");
        sb.Append(E(culture["Leraar.Detail.DreamLead"]));
        sb.Append("</p>");
        if (detail.DreamJob is null || string.IsNullOrWhiteSpace(detail.DreamJob.JobKey)
            || string.Equals(detail.DreamJob.JobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("<p>");
            sb.Append(E(culture["Leraar.DreamJobs.Undecided"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<p class=\"sch-dream-title\">");
            sb.Append(E(detail.DreamJob.JobTitle ?? detail.DreamJob.JobKey));
            sb.Append("</p>");
            if (!passportV2)
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(E(string.Format(CultureInfo.CurrentCulture, culture["Leraar.Detail.DreamProgress"], detail.DreamJob.HaveCount, detail.DreamJob.TotalCount)));
                sb.Append("</p>");
            }

            if (detail.DreamJob.HaveItems.Count > 0)
            {
                sb.Append("<h3 class=\"sch-subhead\">");
                sb.Append(E(culture["LeerlingDroom.HaveTitle"]));
                sb.Append("</h3><ul class=\"sch-prompt-list\">");
                foreach (var item in detail.DreamJob.HaveItems)
                {
                    sb.Append("<li>");
                    sb.Append(E(item));
                    sb.Append("</li>");
                }

                sb.Append("</ul>");
            }

            if (detail.DreamJob.LearnItems.Count > 0)
            {
                sb.Append("<h3 class=\"sch-subhead\">");
                sb.Append(E(culture["LeerlingDroom.NeedTitle"]));
                sb.Append("</h3><ul class=\"sch-prompt-list\">");
                foreach (var item in detail.DreamJob.LearnItems)
                {
                    sb.Append("<li>");
                    sb.Append(E(item));
                    sb.Append("</li>");
                }

                sb.Append("</ul>");
            }

            if (detail.DreamJob.RouteSteps.Count > 0)
            {
                sb.Append("<ol class=\"sch-prompt-list\">");
                foreach (var step in detail.DreamJob.RouteSteps)
                {
                    sb.Append("<li>");
                    sb.Append(E(step));
                    sb.Append("</li>");
                }

                sb.Append("</ol>");
            }

            sb.Append("<p class=\"muted\">");
            sb.Append(E(culture["Leraar.Detail.NoLinksNote"]));
            sb.Append("</p>");
        }

        sb.Append("</section></aside></div>");
        return sb.ToString();
    }

    private static StringBuilder AppendBreadcrumb(
        StringBuilder sb,
        Guid classId,
        TeacherCodeDetailDto detail,
        CultureState culture,
        string? headerTestId = null)
    {
        sb.Append("<header class=\"sch-page-header\"");
        if (!string.IsNullOrEmpty(headerTestId))
        {
            sb.Append(" data-testid=\"");
            sb.Append(headerTestId);
            sb.Append('"');
        }

        sb.Append("><p class=\"sch-breadcrumb\"><a href=\"");
        sb.Append($"/leraar/klas/{classId:D}");
        sb.Append("\">Klas ");
        sb.Append(E(detail.ClassName));
        sb.Append("</a> / <a href=\"");
        sb.Append($"/leraar/klas/{classId:D}/codes");
        sb.Append("\">");
        sb.Append(E(culture["Leraar.Codes.Title"]));
        sb.Append("</a> / ");
        sb.Append(E(detail.DisplayCode));
        sb.Append("</p>");
        return sb;
    }

    private static string QuestionCount(CultureState culture, TeacherCodeDetailDto detail)
    {
        var questions = culture["Leraar.Detail.Questions"];
        return detail.ProgressCurrent == detail.ProgressTotal
            ? $"{detail.ProgressTotal} {questions}"
            : $"{detail.ProgressCurrent} van {detail.ProgressTotal} {questions}";
    }

    private static string ChipText(CultureState culture, string key)
    {
        var chip = PupilInterestChipCatalog.DislikeChips
            .FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
        if (chip is null)
        {
            return key;
        }

        var label = culture[chip.LabelKey];
        return string.Equals(label, chip.LabelKey, StringComparison.Ordinal) ? key : label;
    }

    private static string OwnWord(CultureState culture, string word)
        => $"{word} ({culture["Leraar.Detail.OwnWord"]})";

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
