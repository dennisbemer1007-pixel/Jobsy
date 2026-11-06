using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilVoPaspoortSsrHtml
{
    public static string BuildBody(CultureState culture, string antiforgeryHtml, string savePath, PupilVoPaspoortFormModel model, string? error)
    {
        var sb = new StringBuilder();
        sb.Append("""<div class="ll-reis" data-testid="leerling-vo-paspoort"><section class="ll-card">""");
        sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Vo.Form.Title"])).Append("</h1>");
        sb.Append("<p class=\"muted\">").Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Vo.Form.Lead"])).Append("</p>");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\">");
        sb.Append(antiforgeryHtml);
        AppendField(sb, culture["LeerlingPassport.Vo.Form.Subjects"], "Subjects", model.Subjects);
        sb.Append("<label class=\"ll-field\"><span>").Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Vo.Form.Profile"])).Append("</span><select name=\"ProfileKey\">");
        foreach (var (val, label) in VoProfiles())
        {
            var sel = model.ProfileKey == val ? " selected=\"selected\"" : "";
            sb.Append("<option value=\"").Append(PupilLeerlingSsrParts.E(val)).Append('"').Append(sel).Append('>');
            sb.Append(PupilLeerlingSsrParts.E(label)).Append("</option>");
        }
        sb.Append("</select></label>");
        AppendField(sb, culture["LeerlingPassport.Vo.Form.SideJob"], "SideJob", model.SideJob);
        sb.Append("<label class=\"ll-field\"><span>").Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Vo.Form.Travel"])).Append("</span><select name=\"TravelKey\">");
        sb.Append("<option value=\"\">").Append('—').Append("</option>");
        foreach (var (val, key) in new[] { ("fiets", "LeerlingPassport.Vo.Travel.Fiets"), ("ov", "LeerlingPassport.Vo.Travel.Ov"), ("fiets-ov", "LeerlingPassport.Vo.Travel.FietsOv") })
        {
            var sel = model.TravelKey == val ? " selected=\"selected\"" : "";
            sb.Append("<option value=\"").Append(val).Append('"').Append(sel).Append('>');
            sb.Append(PupilLeerlingSsrParts.E(culture[key])).Append("</option>");
        }
        sb.Append("</select></label>");
        AppendTextArea(sb, culture["LeerlingPassport.Vo.Form.Learn"], "LearningGoals", model.LearningGoals);
        AppendTextArea(sb, culture["LeerlingPassport.Vo.Form.OwnWords"], "OwnWords", model.OwnWords);
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }
        sb.Append("<div class=\"ll-result__actions\"><a class=\"btn-secondary\" href=\"/leerling/droombaan\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Vo.Form.Skip"]));
        sb.Append("</a><button type=\"submit\" class=\"btn-primary\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Vo.Form.Save"])).Append("</button></div></form></section></div>");
        return sb.ToString();
    }

    private static void AppendField(StringBuilder sb, string label, string name, string value)
    {
        sb.Append("<label class=\"ll-field\"><span>").Append(PupilLeerlingSsrParts.E(label)).Append("</span><input type=\"text\" name=\"");
        sb.Append(name).Append("\" value=\"").Append(PupilLeerlingSsrParts.E(value)).Append("\" /></label>");
    }

    private static void AppendTextArea(StringBuilder sb, string label, string name, string value)
    {
        sb.Append("<label class=\"ll-field\"><span>").Append(PupilLeerlingSsrParts.E(label)).Append("</span><textarea rows=\"3\" name=\"");
        sb.Append(name).Append("\">").Append(PupilLeerlingSsrParts.E(value)).Append("</textarea></label>");
    }

    private static IEnumerable<(string val, string label)> VoProfiles() =>
    [
        ("", "—"),
        ("natuur-gezondheid", "Natuur & Gezondheid"),
        ("nt", "Natuur & Techniek"),
        ("economie", "Economie"),
        ("cultuur-maatschappij", "Cultuur & Maatschappij"),
    ];
}

public sealed record PupilVoPaspoortFormModel(
    string Subjects,
    string ProfileKey,
    string SideJob,
    string TravelKey,
    string LearningGoals,
    string OwnWords);
