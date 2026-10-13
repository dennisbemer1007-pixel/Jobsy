using Jobsy.Core.Rules;

namespace Jobsy.Web.Localization;

internal static class ResearchDemographicsOptionLabels
{
    public static string Label(CultureState culture, string category, string code)
    {
        if (string.Equals(code, ResearchDemographicsCatalog.PreferNotToSay, StringComparison.Ordinal))
        {
            return culture["Research.Demo.PreferNot"];
        }

        var key = $"Research.Demo.{category}.{code.Replace("-", "_", StringComparison.Ordinal)}";
        var text = culture[key];
        return string.Equals(text, key, StringComparison.OrdinalIgnoreCase) ? code : text;
    }

    public static string Placeholder(CultureState culture) => culture["Research.Demo.Choose"];
}
