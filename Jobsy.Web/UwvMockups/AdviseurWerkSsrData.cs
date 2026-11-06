using Jobsy.Web.Services;
using Jobsy.Web.UwvFeatures;

namespace Jobsy.Web.UwvMockups;

internal sealed class AdviseurWerkSsrData
{
    internal bool UwvEnabled { get; init; }
    internal string Lang { get; init; } = "nl";
    internal string CandidateDisplayName { get; init; } = "";
    internal string? OndernemerOutcomeKey { get; init; }
    internal IReadOnlyList<string> DrijfverenDriverKeys { get; init; } = [];
    internal IReadOnlyList<string> CompetenceLabels { get; init; } = [];
    internal IReadOnlyList<string> DefaultSelectedSections { get; init; } =
    [
        Jobsy.Core.Uwv.UwvAdviseurSectionKeys.Career,
        Jobsy.Core.Uwv.UwvAdviseurSectionKeys.Drijfveren,
        Jobsy.Core.Uwv.UwvAdviseurSectionKeys.Competences,
    ];

    internal static async Task<AdviseurWerkSsrData> LoadAsync(
        IUwvFeaturesGate uwvGate,
        JobsyApiClient api,
        string lang,
        CancellationToken cancellationToken)
    {
        if (!await uwvGate.IsEnabledAsync(cancellationToken))
        {
            return new AdviseurWerkSsrData { UwvEnabled = false };
        }

        var profile = await api.GetMyProfileAsync(cancellationToken);
        var name = profile?.FullName?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            name = UwvMockupCopy.SampleCandidateName(lang);
        }

        var toolkit = await api.GetUwvToolkitAsync(cancellationToken);
        var competences = (await api.GetVerifiedCompetencesAsync(cancellationToken))
            .Select(c => c.Label)
            .ToList();

        return new AdviseurWerkSsrData
        {
            UwvEnabled = true,
            Lang = lang,
            CandidateDisplayName = name,
            OndernemerOutcomeKey = toolkit?.OndernemerOutcomeKey,
            DrijfverenDriverKeys = toolkit?.DrijfverenDriverKeys ?? [],
            CompetenceLabels = competences,
        };
    }
}
