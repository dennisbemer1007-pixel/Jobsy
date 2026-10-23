using Jobsy.Core.Contracts;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.RegionHosting;
using Jobsy.Web.Services;

namespace Jobsy.Web.Banenkaart;

internal static class BanenkaartSsrData
{
    internal const int ListPageSize = 20;

    internal sealed record PageModel(
        BanenkaartSsrQuery.State Query,
        bool EmployersEnabled,
        bool IsAuthenticated,
        bool IsCandidate,
        bool OpenForWork,
        bool ShowFitFilters,
        bool ShowStartPrompt,
        bool CompanyMapEnabled,
        string MapBootJson,
        IReadOnlyList<VacancyListItem> Vacancies,
        IReadOnlyList<VacancyCategoryItem> Categories,
        IReadOnlyList<string> BranchOptions,
        string? AddressLabel,
        bool ShowEmptyTravelMessage,
        int ActiveFilterCount,
        string ResultCountHeading,
        string? ResultCountSubtitle,
        uint HighlightSeed,
        bool HasMoreList,
        int ListTotalCount);

    internal static async Task<PageModel?> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        RegionHostState regionHost,
        BanenkaartSsrQuery.State query,
        bool isAuthenticated,
        bool isCandidate,
        CancellationToken cancellationToken = default)
    {
        var flags = await featureFlags.GetAsync(cancellationToken);
        if (!flags.EmployersEnabled)
        {
            return null;
        }

        await regionHost.EnsureInitializedAsync();

        var branchOptions = new List<string>();
        var categories = new List<VacancyCategoryItem>();
        VacancyMapView? prefetchedMapView = null;

        async Task LoadBranchOptionsAsync()
        {
            try
            {
                var options = await api.GetMasterdataAsync(MasterdataCategories.Branch, ct: cancellationToken);
                branchOptions = options.Select(o => o.Label).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            }
            catch
            {
                // optional masterdata
            }
        }

        async Task LoadCategoriesAsync()
        {
            try
            {
                categories = (await api.GetVacancyCategoriesAsync(cancellationToken))
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToList();
            }
            catch
            {
                categories = [];
            }
        }

        async Task LoadMapViewAsync()
        {
            try
            {
                var dto = await api.GetVacancyMapViewAsync(cancellationToken);
                if (dto is not null
                    && dto.PinCount > 0
                    && double.IsFinite(dto.Lat)
                    && double.IsFinite(dto.Lng)
                    && double.IsFinite(dto.Zoom))
                {
                    prefetchedMapView = new VacancyMapView(dto.Lat, dto.Lng, dto.Zoom, dto.PinCount);
                }
            }
            catch
            {
                // optional
            }
        }

        double? originLat = query.OriginLat;
        double? originLng = query.OriginLng;
        string? addressLabel = query.OriginLabel;
        var showStartPrompt = false;
        var openForWork = false;
        var showFitFilters = false;

        async Task LoadCandidateOriginAsync()
        {
            if (!isCandidate)
            {
                return;
            }

            try
            {
                var profile = await api.GetMyProfileAsync(cancellationToken);
                if (profile is not null)
                {
                    openForWork = profile.OpenForWork;
                    showFitFilters = openForWork;
                    if (originLat is null && profile.HomeLatitude is double hLat && profile.HomeLongitude is double hLng)
                    {
                        var resolved = KbMapStart.Resolve(new KbMapStart.Input(
                            UrlOrStateOrigin: null,
                            SessionOrigin: null,
                            HomeLatitude: hLat,
                            HomeLongitude: hLng,
                            HomeAddressLabel: profile.Preferences.HomeAddress,
                            StoredOrigin: null,
                            IsLoggedInCandidate: true));
                        if (resolved.Origin is { } origin)
                        {
                            originLat = origin.Latitude;
                            originLng = origin.Longitude;
                            addressLabel = origin.Label ?? addressLabel;
                        }
                    }
                }
            }
            catch
            {
                // profile optional
            }
        }

        await Task.WhenAll(
            LoadBranchOptionsAsync(),
            LoadCategoriesAsync(),
            LoadMapViewAsync(),
            LoadCandidateOriginAsync());

        if (!isCandidate && originLat is null)
        {
            showStartPrompt = true;
        }

        if (originLat is double lat && originLng is double lng)
        {
            showStartPrompt = false;
        }

        var effectiveQuery = query with
        {
            OriginLat = originLat,
            OriginLng = originLng,
            OriginLabel = addressLabel
        };

        var companyFocus = effectiveQuery.CompanyIds.Count > 0;
        var useOrigin = originLat is not null && originLng is not null && !companyFocus;

        decimal? minWage = effectiveQuery.MinHourlyWageCents is int minC ? minC / 100m : null;
        decimal? maxWage = effectiveQuery.MaxHourlyWageCents is int maxC ? maxC / 100m : null;

        IReadOnlyList<VacancyListItem> vacancies = [];
        List<Dictionary<string, object?>> bootPins = [];
        VacancyMapView? mapView = prefetchedMapView;
        var zoomOutsideTravel = false;
        var hasMoreList = false;
        var discoverTotal = 0;

        var listSkip = (Math.Max(1, effectiveQuery.Page) - 1) * ListPageSize;

        async Task<VacancyListPage> DiscoverAsync()
        {
            try
            {
                return await api.DiscoverVacanciesPageAsync(
                    useOrigin ? originLat : null,
                    useOrigin ? originLng : null,
                    effectiveQuery.Transport,
                    effectiveQuery.MaxTravelMinutes,
                    useOrigin ? effectiveQuery.RadiusKm : null,
                    companyFocus ? null : effectiveQuery.AgeYears,
                    companyFocus ? null : minWage,
                    companyFocus ? null : maxWage,
                    companyFocus || effectiveQuery.WorkTypes.Count == 0 ? null : effectiveQuery.WorkTypes,
                    companyFocus ? null : effectiveQuery.SearchQuery,
                    companyFocus ? null : effectiveQuery.MinHoursPerWeek,
                    companyFocus ? null : effectiveQuery.MaxHoursPerWeek,
                    companyFocus || effectiveQuery.CategoryIds.Count == 0 ? null : effectiveQuery.CategoryIds,
                    suitableFor65Plus: null,
                    companyIds: effectiveQuery.CompanyIds.Count == 0 ? null : effectiveQuery.CompanyIds,
                    skip: listSkip,
                    take: ListPageSize,
                    minMatchPercent: isCandidate && effectiveQuery.MinMatchPercent > 0 ? effectiveQuery.MinMatchPercent : null,
                    ct: cancellationToken);
            }
            catch
            {
                return new VacancyListPage();
            }
        }

        var discoverPage = await DiscoverAsync();
        vacancies = discoverPage.Items;
        hasMoreList = discoverPage.HasMore;
        discoverTotal = discoverPage.TotalCount;

        if (vacancies.Count == 0 && useOrigin)
        {
            zoomOutsideTravel = true;
        }

        var pinCentroid = VacancyMapViewCalculator.FromPoints(
            vacancies
                .Where(v => double.IsFinite(v.Latitude) && double.IsFinite(v.Longitude))
                .Select(v => (v.Latitude, v.Longitude)));

        if (mapView is null || !mapView.HasPins)
        {
            mapView = pinCentroid.HasPins ? pinCentroid : mapView ?? pinCentroid;
        }

        var region = regionHost.Current;
        var opening = VacancyMapViewCalculator.ResolveOpening(
            mapView ?? pinCentroid,
            originLat,
            originLng,
            region?.Latitude,
            region?.Longitude,
            companyFocus,
            effectiveQuery.MaxTravelMinutes,
            effectiveQuery.Transport,
            effectiveQuery.RadiusKm);

        bootPins = [];

        var catalogUrl = BanenkaartClientCatalog.AllPath;
        var highlightSeed = HashVacancyIds(vacancies.Select(v => v.Id));
        var bootJson = BanenkaartMapBoot.BuildJson(
            culture,
            effectiveQuery,
            opening,
            bootPins,
            catalogUrl,
            preferFilledLocation: !companyFocus,
            isAuthenticated,
            originLat,
            originLng,
            zoomOutsideTravel,
            highlightSeed);

        var defaults = new KbFilterDefaults(
            AgeYears: effectiveQuery.AgeYears,
            MinHoursPerWeek: effectiveQuery.MinHoursPerWeek,
            MaxHoursPerWeek: effectiveQuery.MaxHoursPerWeek);

        var activeFilters = KbFilterBadge.Count(
            new KbFilterState(
                effectiveQuery.Transport,
                effectiveQuery.MaxTravelMinutes,
                effectiveQuery.RadiusKm,
                effectiveQuery.AgeYears,
                effectiveQuery.MinHoursPerWeek,
                effectiveQuery.MaxHoursPerWeek,
                effectiveQuery.SearchQuery,
                effectiveQuery.WorkTypes.Count,
                effectiveQuery.CategoryIds.Count,
                effectiveQuery.MinHourlyWageCents is not null,
                effectiveQuery.MaxHourlyWageCents is not null,
                false,
                effectiveQuery.MinMatchPercent,
                originLat is not null),
            defaults);

        var count = discoverTotal > 0 ? discoverTotal : vacancies.Count;
        var heading = culture.Format("Kb.Filter.ResultCount", count);

        string? subtitle = originLat is not null
            ? culture.Format(
                "Kb.Filter.ResultWithin",
                effectiveQuery.MaxTravelMinutes,
                UiLabels.TransportVerb(culture, effectiveQuery.Transport))
            : null;

        return new PageModel(
            effectiveQuery,
            flags.EmployersEnabled,
            isAuthenticated,
            isCandidate,
            openForWork,
            showFitFilters,
            showStartPrompt,
            flags.IsEnabled(PlatformFeature.CompanyMapInitiative),
            bootJson,
            vacancies,
            categories,
            branchOptions,
            addressLabel,
            zoomOutsideTravel && vacancies.Count == 0,
            activeFilters,
            heading,
            subtitle,
            highlightSeed,
            hasMoreList,
            discoverTotal);
    }

    private static uint HashVacancyIds(IEnumerable<Guid> ids)
    {
        uint hash = 2166136261;
        foreach (var id in ids)
        {
            var bytes = id.ToByteArray();
            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= 16777619;
            }
        }

        return hash;
    }
}
