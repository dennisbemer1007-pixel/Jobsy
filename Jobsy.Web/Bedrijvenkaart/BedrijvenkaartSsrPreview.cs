using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Bedrijvenkaart;

/// <summary>
/// Preview gate for NoBlazor bedrijvenkaart SSR until it replaces <c>/bedrijvenkaart</c> (final PR).
/// </summary>
public static class BedrijvenkaartSsrPreview
{
    public const string PreviewPath = "/bedrijvenkaart/ssr";
    public const string ConfigurationKey = "JobsyFeatures:BedrijvenkaartSsrPreview";

    public static bool IsEnabled(IConfiguration configuration)
        => configuration.GetValue<bool>(ConfigurationKey);
}
