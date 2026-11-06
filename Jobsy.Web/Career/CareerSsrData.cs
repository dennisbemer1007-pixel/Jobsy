using Jobsy.Core.Uwv;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Jobsy.Web.UwvFeatures;
using Jobsy.Web.UwvMockups;

namespace Jobsy.Web.Career;

internal static class CareerSsrData
{
    internal sealed record PageData(
        string ActiveTab,
        IReadOnlyCollection<string> ExpandedGroups,
        string? ErrorMessage,
        bool Saved,
        bool EmptyProfile,
        string HeroTitle,
        string HeroLead,
        string? DreamTitle,
        UwvKansberoepenDataset? KansDataset);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        CultureState culture,
        string? tabRaw,
        string? meerRaw,
        string? errorMessage,
        bool saved,
        IUwvKansberoepenCatalog kansCatalog,
        CancellationToken cancellationToken = default)
    {
        var tab = CareerSsrTabs.Normalize(tabRaw);
        var expanded = ParseExpanded(meerRaw);
        var profile = await api.GetMyProfileAsync(cancellationToken);
        var emptyProfile = UwvCarriereProfileRules.PreferencesLookEmpty(profile?.Preferences);
        var career = await api.GetCareerPathAsync(cancellationToken);
        var dream = string.IsNullOrWhiteSpace(career?.DreamTitle) ? null : career!.DreamTitle.Trim();
        var lang = culture.Language;
        var heroTitle = emptyProfile
            ? UwvMockupCopy.Career.EmptyTitle(lang)
            : UwvMockupCopy.Career.Title(lang);
        var heroLead = emptyProfile
            ? UwvMockupCopy.Career.EmptyLead(lang)
            : UwvMockupCopy.Career.Lead(lang);
        var kans = kansCatalog.GetDefault();

        return new PageData(
            tab,
            expanded,
            errorMessage,
            saved,
            emptyProfile,
            heroTitle,
            heroLead,
            dream,
            kans);
    }

    private static HashSet<string> ParseExpanded(string? meerRaw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(meerRaw))
        {
            return set;
        }

        foreach (var part in meerRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            set.Add(part.ToLowerInvariant());
        }

        return set;
    }
}
