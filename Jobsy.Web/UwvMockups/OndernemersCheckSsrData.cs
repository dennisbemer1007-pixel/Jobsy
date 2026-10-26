using Jobsy.Web.Services;
using Jobsy.Web.UwvFeatures;

namespace Jobsy.Web.UwvMockups;

internal sealed class OndernemersCheckSsrData
{
    internal bool UwvEnabled { get; init; }
    internal string Phase { get; init; } = "intro";
    internal string? OutcomeKey { get; init; }

    internal static async Task<OndernemersCheckSsrData> LoadAsync(
        IUwvFeaturesGate uwvGate,
        JobsyApiClient api,
        string? stap,
        CancellationToken cancellationToken)
    {
        if (!await uwvGate.IsEnabledAsync(cancellationToken))
        {
            return new OndernemersCheckSsrData { UwvEnabled = false };
        }

        var phase = NormalizePhase(stap);
        var state = await api.GetUwvToolkitAsync(cancellationToken);
        return new OndernemersCheckSsrData
        {
            UwvEnabled = true,
            Phase = phase,
            OutcomeKey = state?.OndernemerOutcomeKey,
        };
    }

    private static string NormalizePhase(string? stap) =>
        (stap ?? "intro").ToLowerInvariant() switch
        {
            "question" or "vraag" => "question",
            "result" or "done" => "result",
            _ => "intro",
        };
}
