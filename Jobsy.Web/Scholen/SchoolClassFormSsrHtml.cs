using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Scholen;

/// <summary>Static HTML for school class create/edit forms (ported from <c>SchoolClassForm.razor</c>).</summary>
public static class SchoolClassFormSsrHtml
{
    public const string KindPrimary = "primary";
    public const string KindVo = "vo";
    public const string KindMboHbo = "mbo-hbo";

    public sealed record TeacherOption(Guid UserId, string DisplayName);

    public sealed class FormModel
    {
        public string ClassName { get; set; } = "";
        public string Kind { get; set; } = KindVo;
        public SchoolLevel Level { get; set; } = SchoolLevel.Havo;
        public int Year { get; set; } = 2;
        public int PupilCount { get; set; } = 28;
        public bool LevelLocked { get; set; }
        public bool ShowPupilCount { get; set; }
        public HashSet<Guid> SelectedTeacherIds { get; set; } = [];
    }

    public static FormModel FromQuery(IQueryCollection query)
    {
        var model = new FormModel();
        ApplyQuery(model, query);
        return model;
    }

    public static void ApplyQuery(FormModel model, IQueryCollection query)
    {
        var className = query["className"].ToString();
        if (!string.IsNullOrWhiteSpace(className))
        {
            model.ClassName = className.Trim()[..Math.Min(12, className.Trim().Length)];
        }

        var kind = query["kind"].ToString();
        if (!string.IsNullOrWhiteSpace(kind))
        {
            model.Kind = NormalizeKind(kind);
        }
        else if (model.LevelLocked || model.ClassName.Length > 0)
        {
            model.Kind = KindFromLevel(model.Level);
        }

        if (Enum.TryParse<SchoolLevel>(query["level"].ToString(), ignoreCase: true, out var level))
        {
            model.Level = level;
            if (string.IsNullOrWhiteSpace(kind))
            {
                model.Kind = KindFromLevel(level);
            }
        }

        if (int.TryParse(query["year"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            model.Year = year;
        }

        if (int.TryParse(query["pupilCount"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
        {
            model.PupilCount = count;
        }

        foreach (var idRaw in query["teacherIds"])
        {
            if (Guid.TryParse(idRaw, out var id))
            {
                model.SelectedTeacherIds.Add(id);
            }
        }

        SyncLevelForKind(model);
    }

    public static bool TryParseForm(IFormCollection form, FormModel seed, out FormModel model, out string? error)
    {
        model = new FormModel
        {
            ClassName = seed.ClassName,
            Kind = seed.Kind,
            Level = seed.Level,
            Year = seed.Year,
            PupilCount = seed.PupilCount,
            LevelLocked = seed.LevelLocked,
            ShowPupilCount = seed.ShowPupilCount,
            SelectedTeacherIds = new HashSet<Guid>(seed.SelectedTeacherIds),
        };

        model.ClassName = form["className"].ToString().Trim();
        if (model.ClassName.Length > 12)
        {
            model.ClassName = model.ClassName[..12];
        }

        if (!model.LevelLocked)
        {
            model.Kind = NormalizeKind(form["kind"].ToString());
        }

        if (Enum.TryParse<SchoolLevel>(form["level"].ToString(), ignoreCase: true, out var level))
        {
            model.Level = level;
        }

        if (int.TryParse(form["year"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            model.Year = year;
        }

        if (int.TryParse(form["pupilCount"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var pupilCount))
        {
            model.PupilCount = pupilCount;
        }

        model.SelectedTeacherIds.Clear();
        foreach (var idRaw in form["teacherIds"])
        {
            if (Guid.TryParse(idRaw, out var id))
            {
                model.SelectedTeacherIds.Add(id);
            }
        }

        SyncLevelForKind(model);

        if (string.IsNullOrWhiteSpace(model.ClassName))
        {
            error = "Vul een klassenaam in.";
            return false;
        }

        error = null;
        return true;
    }

    public static string Build(
        FormModel model,
        IReadOnlyList<TeacherOption>? teachers,
        string formAction,
        string cancelHref,
        string antiforgery,
        CultureState culture,
        string? formError = null)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(formError))
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(E(formError));
            sb.Append("</p>");
        }

        sb.Append("<form method=\"post\" action=\"");
        sb.Append(E(formAction));
        sb.Append("\">");
        sb.Append(antiforgery);

        sb.Append("<label>");
            sb.Append(E(culture["School.Class.Field.Name"]));
        sb.Append(" <input class=\"sch-field\" type=\"text\" name=\"className\" maxlength=\"12\" value=\"");
        sb.Append(E(model.ClassName));
        sb.Append("\" /></label>");
        sb.Append("<p class=\"muted\">");
        sb.Append(E(string.Format(culture["School.Class.NameCount"], Math.Min(12, model.ClassName.Length))));
        sb.Append("</p>");

        if (model.LevelLocked)
        {
            sb.Append("<p class=\"sch-level-locked\" role=\"note\">");
            sb.Append(E(culture["School.Class.LevelLocked"]));
            sb.Append("</p>");
        }

        sb.Append("<fieldset class=\"sch-kind\"");
        if (model.LevelLocked)
        {
            sb.Append(" disabled");
        }

        sb.Append("><legend>");
        sb.Append(E(culture["School.Class.Field.Kind"]));
        sb.Append("</legend><div class=\"sch-kind-cards\" role=\"radiogroup\" aria-label=\"");
        sb.Append(E(culture["School.Class.Field.Kind"]));
        sb.Append("\">");
        AppendKindRadio(sb, model, KindPrimary, culture["School.Class.Kind.Primary"], culture["School.Class.Kind.PrimaryHint"]);
        AppendKindRadio(sb, model, KindVo, culture["School.Class.Kind.Secondary"], culture["School.Class.Kind.SecondaryHint"]);
        AppendKindRadio(sb, model, KindMboHbo, culture["School.Class.Kind.MboHbo"], culture["School.Class.Kind.MboHboHint"]);
        sb.Append("</div></fieldset>");

        if (model.Kind == KindPrimary)
        {
            AppendYearSegment(sb, model, culture, [7, 8], culture["School.Class.Field.Group"]);
        }
        else
        {
            var levels = model.Kind == KindMboHbo ? SchoolLevelRules.MboHboLevels : SchoolLevelRules.VoLevels;
            sb.Append("<label>");
            sb.Append(E(culture["School.Class.Field.Level"]));
            sb.Append(" <select class=\"sch-field\" name=\"level\"");
            if (model.LevelLocked)
            {
                sb.Append(" disabled");
            }

            sb.Append('>');
            foreach (var lv in levels)
            {
                sb.Append("<option value=\"");
                sb.Append(lv.ToString());
                sb.Append('"');
                if (model.Level == lv)
                {
                    sb.Append(" selected");
                }

                sb.Append('>');
                sb.Append(E(culture[SchoolLevelRules.LabelKey(lv)]));
                sb.Append("</option>");
            }

            sb.Append("</select></label>");
            if (model.LevelLocked)
            {
                sb.Append("<input type=\"hidden\" name=\"level\" value=\"");
                sb.Append(model.Level.ToString());
                sb.Append("\" />");
            }

            var maxYear = model.Kind == KindMboHbo ? 4 : 6;
            var years = Enumerable.Range(1, maxYear).ToArray();
            AppendYearSegment(sb, model, culture, years, culture["School.Class.Field.Year"]);
        }

        AppendSetNote(sb, model.Kind, culture);

        if (model.ShowPupilCount)
        {
            sb.Append("<label>");
            sb.Append(E(culture["School.Class.Field.Count"]));
            sb.Append(" <input class=\"sch-field\" type=\"number\" name=\"pupilCount\" min=\"1\" value=\"");
            sb.Append(model.PupilCount.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" /></label><p class=\"muted\">");
            sb.Append(E(culture["School.Class.CodesNote"]));
            sb.Append("</p>");
        }

        if (teachers is { Count: > 0 })
        {
            sb.Append("<fieldset class=\"sch-check-list\"><legend>");
            sb.Append(E(culture["School.Class.Field.Teachers"]));
            sb.Append("</legend>");
            foreach (var t in teachers)
            {
                sb.Append("<label class=\"filter-check\"><input type=\"checkbox\" name=\"teacherIds\" value=\"");
                sb.Append(t.UserId.ToString("D"));
                sb.Append('"');
                if (model.SelectedTeacherIds.Contains(t.UserId))
                {
                    sb.Append(" checked");
                }

                sb.Append(" /> ");
                sb.Append(E(t.DisplayName));
                sb.Append("</label>");
            }

            sb.Append("</fieldset>");
        }

        sb.Append("<div class=\"sch-drawer-actions\">");
        sb.Append("<a class=\"btn-compact\" href=\"");
        sb.Append(E(cancelHref));
        sb.Append("\">");
        sb.Append(E(culture["School.Cancel"]));
        sb.Append("</a><button type=\"submit\" class=\"btn-primary\">");
        sb.Append(E(culture["School.Save"]));
        sb.Append("</button></div></form>");
        return sb.ToString();
    }

    private static void AppendKindRadio(
        StringBuilder sb,
        FormModel model,
        string kind,
        string title,
        string hint)
    {
        sb.Append("<label class=\"sch-kind-card ");
        if (model.Kind == kind)
        {
            sb.Append("is-on");
        }

        sb.Append("\"><input type=\"radio\" name=\"kind\" value=\"");
        sb.Append(E(kind));
        sb.Append('"');
        if (model.Kind == kind)
        {
            sb.Append(" checked");
        }

        if (model.LevelLocked)
        {
            sb.Append(" disabled");
        }

        sb.Append(" /><span class=\"sch-kind-card__title\">");
        sb.Append(E(title));
        sb.Append("</span><span class=\"sch-kind-card__hint\">");
        sb.Append(E(hint));
        sb.Append("</span></label>");
    }

    private static void AppendYearSegment(
        StringBuilder sb,
        FormModel model,
        CultureState culture,
        IReadOnlyList<int> years,
        string legendKey)
    {
        var legend = culture[legendKey];
        sb.Append("<fieldset class=\"sch-seg-fieldset\"><legend>");
        sb.Append(E(legend));
        sb.Append("</legend><div class=\"sch-seg\" role=\"radiogroup\" aria-label=\"");
        sb.Append(E(legend));
        sb.Append("\">");
        foreach (var y in years)
        {
            sb.Append("<label class=\"sch-seg__opt ");
            if (model.Year == y)
            {
                sb.Append("is-on");
            }

            sb.Append("\"><input type=\"radio\" name=\"year\" value=\"");
            sb.Append(y.ToString(CultureInfo.InvariantCulture));
            sb.Append('"');
            if (model.Year == y)
            {
                sb.Append(" checked");
            }

            sb.Append(" /> ");
            if (years.Count == 2 && y is 7 or 8)
            {
                sb.Append(E(culture[y == 7 ? "School.Class.Group.7" : "School.Class.Group.8"]));
            }
            else
            {
                sb.Append(y.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("</label>");
        }

        sb.Append("</div></fieldset>");
    }

    private static void AppendSetNote(StringBuilder sb, string kind, CultureState culture)
    {
        var (titleKey, bodyKey) = kind switch
        {
            KindPrimary => ("School.Class.SetNote.G78.Title", "School.Class.SetNote.G78.Body"),
            KindMboHbo => ("School.Class.SetNote.MboHbo.Title", "School.Class.SetNote.MboHbo.Body"),
            _ => ("School.Class.SetNote.Vo.Title", "School.Class.SetNote.Vo.Body")
        };
        sb.Append("<div class=\"sch-set-note\" role=\"status\"><strong>");
        sb.Append(E(culture[titleKey]));
        sb.Append("</strong><ul><li>");
        sb.Append(E(culture["School.Class.SetNote.Shared"]));
        sb.Append("</li><li>");
        sb.Append(E(culture[bodyKey]));
        sb.Append("</li></ul></div>");
    }

    private static void SyncLevelForKind(FormModel model)
    {
        if (model.LevelLocked)
        {
            model.Kind = KindFromLevel(model.Level);
            return;
        }

        model.Kind = NormalizeKind(model.Kind);
        if (model.Kind == KindPrimary)
        {
            model.Level = SchoolLevel.Groep78;
            if (model.Year is not (7 or 8))
            {
                model.Year = 7;
            }
        }
        else if (model.Kind == KindMboHbo)
        {
            if (!SchoolLevelRules.IsMboHbo(model.Level))
            {
                model.Level = SchoolLevel.Mbo2;
            }

            if (model.Year is < 1 or > 4)
            {
                model.Year = 1;
            }
        }
        else
        {
            if (SchoolLevelRules.IsPrimary(model.Level) || SchoolLevelRules.IsMboHbo(model.Level))
            {
                model.Level = SchoolLevel.Havo;
            }

            if (model.Year is < 1 or > 6)
            {
                model.Year = 2;
            }
        }
    }

    private static string NormalizeKind(string raw)
        => raw switch
        {
            KindPrimary => KindPrimary,
            KindMboHbo or "mbo-hbo" => KindMboHbo,
            "secondary" => KindVo,
            _ => KindVo
        };

    private static string KindFromLevel(SchoolLevel level)
    {
        if (SchoolLevelRules.IsPrimary(level))
        {
            return KindPrimary;
        }

        return SchoolLevelRules.IsMboHbo(level) ? KindMboHbo : KindVo;
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
