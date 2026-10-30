using Jobsy.Core.Uwv;
using Jobsy.Web.Services;
using Jobsy.Web.UwvFeatures;

namespace Jobsy.Web.UwvMockups;

internal sealed class WatDrijftJouSsrData
{
    internal bool UwvEnabled { get; init; }
    internal IReadOnlyList<UwvDriverChip> Drivers { get; init; } = [];

    internal static async Task<WatDrijftJouSsrData> LoadAsync(
        IUwvFeaturesGate uwvGate,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        if (!await uwvGate.IsEnabledAsync(cancellationToken))
        {
            return new WatDrijftJouSsrData { UwvEnabled = false };
        }

        var state = await api.GetUwvToolkitAsync(cancellationToken);
        var keys = state?.DrijfverenDriverKeys?.Where(UwvDriverKeys.IsValid).ToList();
        if (keys is null || keys.Count == 0)
        {
            keys = UwvDriverKeys.All.Take(3).ToList();
            await api.SaveUwvDrijfverenAsync(keys.ToArray(), cancellationToken);
        }

        var drivers = keys
            .Select(key =>
            {
                var idx = UwvDriverKeys.All.ToList().IndexOf(key);
                return idx >= 0 && idx < UwvMockupData.Drivers.Count ? UwvMockupData.Drivers[idx] : null;
            })
            .Where(d => d is not null)
            .Select(d => d!)
            .ToList();

        return new WatDrijftJouSsrData
        {
            UwvEnabled = true,
            Drivers = drivers,
        };
    }
}
