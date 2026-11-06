using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Web.Bedrijvenkaart;

internal static class BedrijvenkaartSsrData
{
    internal sealed record PageModel(
        IReadOnlyList<CompanyMapPinDto> Pins,
        int RadiusKm,
        int OpenCount,
        bool CompanyMapEnabled,
        double OriginLat,
        double OriginLng,
        bool OriginFromProfile);

    internal static async Task<PageModel?> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        HttpContext? http,
        int radiusKm,
        CancellationToken ct = default)
    {
        var flags = await featureFlags.GetAsync(ct);
        var initiativeOn = flags.IsEnabled(PlatformFeature.CompanyMapInitiative);
        var previewOn = BedrijvenkaartSsrPreview.IsEnabled(configuration);
        if (!initiativeOn && !previewOn)
        {
            return null;
        }

        var origin = await BedrijvenkaartMapOrigin.ResolveAsync(http, api, ct);

        IReadOnlyList<CompanyMapPinDto> pins;
        try
        {
            pins = await api.GetCompanyMapPinsAsync(origin.Lat, origin.Lng, radiusKm, ct);
        }
        catch
        {
            pins = [];
        }

        if (pins.Count == 0 && environment.IsDevelopment())
        {
            pins = BedrijvenkaartDemoPins.All;
        }

        var openCount = pins.Count(p => p.OpenForInitiative);
        return new PageModel(
            pins,
            radiusKm,
            openCount,
            initiativeOn,
            origin.Lat,
            origin.Lng,
            origin.FromProfile);
    }
}
