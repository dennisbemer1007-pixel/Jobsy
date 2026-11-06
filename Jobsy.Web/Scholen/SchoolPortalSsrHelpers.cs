using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

internal static class SchoolPortalSsrHelpers
{
    public static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static void AppendKpi(StringBuilder sb, string label, string value, string? delta, string deltaTone = "neutral")
    {
        sb.Append("<div class=\"ent-kpi-card\"><p class=\"ent-kpi-card__label\">");
        sb.Append(E(label));
        sb.Append("</p><p class=\"ent-kpi-card__value\">");
        sb.Append(E(value));
        sb.Append("</p>");
        if (!string.IsNullOrWhiteSpace(delta))
        {
            sb.Append("<p class=\"ent-kpi-card__delta ent-kpi-card__delta--");
            sb.Append(E(deltaTone));
            sb.Append("\">");
            sb.Append(E(delta));
            sb.Append("</p>");
        }

        sb.Append("</div>");
    }

    public static string WindowLabel(CultureState culture, TestWindowState state, DateOnly? closesOn)
        => state switch
        {
            TestWindowState.Open when closesOn is DateOnly d
                => $"{culture["School.TestWindow.StatusOpen"]} t/m {d:dd-MM}",
            TestWindowState.Open => culture["School.TestWindow.StatusOpen"],
            TestWindowState.Closed => culture["School.TestWindow.StatusClosed"],
            _ => culture["School.TestWindow.StatusNotOpen"]
        };

    public static string WindowPill(TestWindowState s) => s switch
    {
        TestWindowState.Open => "sch-pill--ok",
        TestWindowState.Closed => "sch-pill--muted",
        _ => "sch-pill--warn"
    };

    public static string StatusPill(PupilCodeStatus s) => s switch
    {
        PupilCodeStatus.Completed => "sch-pill--ok",
        PupilCodeStatus.InProgress => "sch-pill--warn",
        _ => "sch-pill--muted"
    };

    public static string StatusLabel(CultureState culture, SchoolPortalCodeRowDto code)
        => PupilCodeStatusText.Label(culture, code.Status, code.LastSeenAtUtc);

    public static string ResultsStatusLabel(CultureState culture, PupilCodeStatus status) => status switch
    {
        PupilCodeStatus.Completed => culture["School.Status.Completed"],
        PupilCodeStatus.InProgress => culture["Leraar.Status.Busy"],
        _ => culture["School.Status.NotStarted"]
    };

    public static void AppendDataTableOpen(StringBuilder sb)
        => sb.Append("<div class=\"ent-data-table-wrap\"><table class=\"ent-data-table data-table\">");

    public static void AppendDataTableClose(StringBuilder sb)
        => sb.Append("</table></div>");

    public static void AppendEmptyTable(StringBuilder sb, string message)
    {
        sb.Append("<p class=\"ent-data-table__state\">");
        sb.Append(E(message));
        sb.Append("</p>");
    }

    public static string AntiforgeryField(Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet tokens)
        => tokens.RequestToken is { } token
            ? $"""<input type="hidden" name="{E(tokens.FormFieldName)}" value="{E(token)}" />"""
            : "";

    public static string FormatAmsterdamDate(DateTime utc)
        => LobsyTime.ToAmsterdam(utc).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    public static string FormatAmsterdamShort(DateTime utc)
        => LobsyTime.ToAmsterdam(utc).ToString("dd-MM", CultureInfo.InvariantCulture);
}
