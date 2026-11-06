using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>
/// Pupil-tile words for a value or culture code. Teachers and school admins
/// must not see the English enum name.
/// </summary>
public static class PupilDimensionText
{
    public static string Value(CultureState culture, string? code)
        => Label(culture, "School.Dim.Val.", code);

    public static string Culture(CultureState culture, string? code)
        => Label(culture, "School.Dim.Cult.", code);

    public static bool IsRawEnumName(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Trim() is
            SchwartzValuesCatalog.Autonomy
            or SchwartzValuesCatalog.Connection
            or SchwartzValuesCatalog.Achievement
            or SchwartzValuesCatalog.Stability
            or SchwartzValuesCatalog.Impact
            or CulturePersonalityCatalog.Informal
            or CulturePersonalityCatalog.PeopleFirst
            or CulturePersonalityCatalog.Innovation
            or CulturePersonalityCatalog.Collaboration
            or CulturePersonalityCatalog.Flexibility;
    }

    private static string Label(CultureState culture, string prefix, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "—";
        }

        var key = prefix + code.Trim();
        var text = culture[key];
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, key, StringComparison.OrdinalIgnoreCase))
        {
            return code.Trim();
        }

        return text;
    }
}
