using Microsoft.Extensions.Configuration;

namespace Jobsy.Core.Features;

/// <summary>
/// Preview gate for NoBlazor bedrijvenkaart SSR until it replaces <c>/bedrijvenkaart</c>.
/// </summary>
public static class BedrijvenkaartSsrPreview
{
    public const string PreviewPath = "/bedrijvenkaart/ssr";
    public const string ConfigurationKey = "JobsyFeatures:BedrijvenkaartSsrPreview";

    public static bool IsEnabled(IConfiguration configuration)
        => bool.TryParse(configuration[ConfigurationKey], out var enabled) && enabled;
}
