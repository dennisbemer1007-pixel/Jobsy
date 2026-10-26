using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

namespace Jobsy.Web.Passport;

internal static class PassportSsrData
{
    internal sealed record PageData(
        string ActiveTab,
        string? ErrorMessage,
        bool Saved,
        FeatureFlagSnapshot Flags,
        MeProfile Profile,
        CandidateKompasState? Kompas,
        CandidateDnaSummary? Dna,
        PassportSsrDisplay.CardModel Card,
        PassportSsrDisplay.OverviewModel Overview,
        PassportSsrDisplay.DnaPanelModel DnaPanel,
        string? FirstName,
        int TestsNextStepCount,
        string? DreamTitle,
        bool FromDiscovery);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        string? tabRaw,
        string? errorMessage,
        bool saved,
        bool fromDiscovery = false,
        CancellationToken cancellationToken = default)
    {
        var flags = await featureFlags.GetAsync(cancellationToken);
        if (!flags.CandidatePassportEnabled)
        {
            return null;
        }

        var tab = PassportTabs.Normalize(tabRaw);
        var dnaTask = api.GetMyKompasDnaResultAsync(cancellationToken);
        var kompasTask = api.GetMyKompasResultAsync(cancellationToken);
        var profileTask = api.GetMyProfileAsync(cancellationToken);
        var careerTask = api.GetCareerPathAsync(cancellationToken);
        await Task.WhenAll(dnaTask, kompasTask, profileTask, careerTask);

        var dnaResult = await dnaTask;
        var kompasResult = await kompasTask;
        var profile = await profileTask ?? new MeProfile();
        var career = await careerTask;

        CandidateDnaSummary? dna = !dnaResult.TemporarilyUnavailable ? dnaResult.Value : null;
        CandidateKompasState? kompas = !kompasResult.TemporarilyUnavailable ? kompasResult.Value : null;

        var card = PassportSsrDisplay.BuildCard(
            culture,
            profile,
            kompas,
            dna,
            flags.EmployersEnabled);
        var overview = PassportSsrDisplay.BuildOverview(culture, kompas, dna);
        var dnaPanel = PassportSsrDisplay.BuildDnaPanel(culture, profile, kompas, dna);
        var firstName = string.IsNullOrWhiteSpace(profile.FirstName)
            ? profile.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            : profile.FirstName.Trim();

        var testsNext = 0;
        if (kompas is not null)
        {
            CandidateDnaViewBuilder.TextLookup t = k => culture[k];
            CandidateDnaViewBuilder.TextFormat format = (k, args) => culture.Format(k, args);
            var view = CandidateDnaViewBuilder.FromKompas(kompas, t, format, culture.Language);
            testsNext = view.Cards.Count(c => !c.Completed);
        }

        return new PageData(
            tab,
            errorMessage,
            saved,
            flags,
            profile,
            kompas,
            dna,
            card,
            overview,
            dnaPanel,
            firstName,
            testsNext,
            string.IsNullOrWhiteSpace(career?.DreamTitle) ? null : career!.DreamTitle.Trim(),
            fromDiscovery);
    }
}
