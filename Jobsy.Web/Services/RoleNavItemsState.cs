using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Jobsy.Web.Services;

/// <summary>Shared primary nav catalog for bottom bar and desktop header (same items, same active rules).</summary>
public sealed class RoleNavItemsState : IAsyncDisposable
{
    private readonly NavigationManager _navigation;
    private readonly AuthenticationStateProvider _authState;
    private readonly JobsyApiClient _api;
    private readonly BottomNavRefreshService _navRefresh;
    private readonly IFeatureFlags _featureFlags;
    private readonly CultureState _culture;

    private FeatureFlagSnapshot _flags = FeatureFlagSnapshot.Defaults;
    private bool _hasTakeoverActions;
    private bool _salesOnboardingComplete;
    private bool _ambassadeurOnboardingComplete;
    private bool _csvImportEnabled;
    private bool _maqqieHoursNav;
    private bool _roleFlagsLoaded;
    private string? _lastRoleFingerprint;
    private bool _initialized;

    public RoleNavItemsState(
        NavigationManager navigation,
        AuthenticationStateProvider authState,
        JobsyApiClient api,
        BottomNavRefreshService navRefresh,
        IFeatureFlags featureFlags,
        CultureState culture)
    {
        _navigation = navigation;
        _authState = authState;
        _api = api;
        _navRefresh = navRefresh;
        _featureFlags = featureFlags;
        _culture = culture;
    }

    public IReadOnlyList<NavItem> Items { get; private set; } = RoleNavCatalog.Anonymous;

    public event Action? Changed;

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _navigation.LocationChanged += OnLocationChanged;
        _culture.Changed += OnCultureChanged;
        _navRefresh.RefreshRequested += OnNavRefreshRequestedAsync;
        _authState.AuthenticationStateChanged += OnAuthenticationStateChanged;
        await RefreshFlagsAsync();
        await RefreshItemsAsync(forceRoleApis: true);
    }

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> authStateTask)
    {
        try
        {
            _ = await authStateTask;
            await RefreshFlagsAsync();
            await RefreshItemsAsync(forceRoleApis: true);
            Changed?.Invoke();
        }
        catch
        {
            // async void: swallow dispose/auth races.
        }
    }

    private void OnCultureChanged()
    {
        if (Items.Count == 0)
        {
            return;
        }

        Changed?.Invoke();
    }

    private async Task OnNavRefreshRequestedAsync()
    {
        await RefreshFlagsAsync();
        await RefreshItemsAsync(forceRoleApis: true);
        Changed?.Invoke();
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            await RefreshItemsAsync(forceRoleApis: false);
            Changed?.Invoke();
        }
        catch
        {
            // async void: swallow dispose/nav races.
        }
    }

    private async Task RefreshFlagsAsync()
    {
        try
        {
            _flags = await _featureFlags.GetAsync();
        }
        catch
        {
            _flags = FeatureFlagSnapshot.Defaults;
        }
    }

    private async Task RefreshItemsAsync(bool forceRoleApis)
    {
        var state = await _authState.GetAuthenticationStateAsync();
        var fingerprint = string.Join(',', state.User.Claims
            .Where(c => c.Type is System.Security.Claims.ClaimTypes.Role or "role")
            .Select(c => c.Value)
            .OrderBy(v => v, StringComparer.Ordinal));
        var catalog = RoleNavCatalog.ForUser(state.User, _flags);

        var rolesChanged = !string.Equals(_lastRoleFingerprint, fingerprint, StringComparison.Ordinal);
        _lastRoleFingerprint = fingerprint;

        // Render primary nav immediately; optional role API calls below may enrich the catalog.
        Items = catalog;
        Changed?.Invoke();

        if (RoleClaimMatching.HasRole(state.User, JobsyRoles.BranchManager)
            && !RoleClaimMatching.HasRole(state.User, JobsyRoles.Admin)
            && !RoleClaimMatching.HasRole(state.User, JobsyRoles.EnterpriseManager)
            && !RoleClaimMatching.HasRole(state.User, JobsyRoles.RegionalManager))
        {
            if (forceRoleApis || rolesChanged || !_roleFlagsLoaded)
            {
                await RefreshTakeoverFlagAsync();
            }

            if (!_hasTakeoverActions)
            {
                catalog = catalog
                    .Where(i => !string.Equals(i.Href, RoleNavCatalog.Takeovers.Href, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        if (RoleClaimMatching.HasRole(state.User, JobsyRoles.SalesManager)
            && !RoleClaimMatching.HasRole(state.User, JobsyRoles.Admin))
        {
            if (forceRoleApis || rolesChanged || !_roleFlagsLoaded)
            {
                await RefreshSalesOnboardingFlagAsync();
            }

            if (_salesOnboardingComplete)
            {
                catalog = catalog
                    .Where(i => !string.Equals(i.Href, "/salesmanager/onboarding", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        if (RoleClaimMatching.HasRole(state.User, JobsyRoles.Ambassadeur)
            && !RoleClaimMatching.HasRole(state.User, JobsyRoles.Admin))
        {
            if (forceRoleApis || rolesChanged || !_roleFlagsLoaded)
            {
                await RefreshAmbassadeurOnboardingFlagAsync();
            }

            if (_ambassadeurOnboardingComplete)
            {
                catalog = catalog
                    .Where(i => !string.Equals(i.Href, "/ambassadeur/onboarding", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        if (catalog.Any(i => string.Equals(i.Href, RoleNavCatalog.CsvImport.Href, StringComparison.OrdinalIgnoreCase)))
        {
            if (forceRoleApis || rolesChanged || !_roleFlagsLoaded)
            {
                await RefreshCsvImportFlagAsync();
            }

            if (!_csvImportEnabled)
            {
                catalog = catalog
                    .Where(i => !string.Equals(i.Href, RoleNavCatalog.CsvImport.Href, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        if (RoleClaimMatching.HasRole(state.User, JobsyRoles.Candidate)
            && _flags.IsEnabled(PlatformFeature.EmployerPhase2))
        {
            if (forceRoleApis || rolesChanged || !_roleFlagsLoaded)
            {
                await RefreshMaqqieHoursNavAsync();
            }

            if (_maqqieHoursNav)
            {
                catalog = RoleNavCatalog.WithMaqqieHoursNav(catalog);
            }
        }

        _roleFlagsLoaded = true;
        Items = catalog;
    }

    private async Task RefreshTakeoverFlagAsync()
    {
        try
        {
            var inbox = await _api.GetTakeoverInboxAsync();
            _hasTakeoverActions = inbox.Count > 0;
        }
        catch
        {
            _hasTakeoverActions = false;
        }
    }

    private async Task RefreshSalesOnboardingFlagAsync()
    {
        try
        {
            var dash = await _api.GetMySalesManagerDashboardAsync();
            _salesOnboardingComplete = dash?.IsOnboardingComplete == true;
        }
        catch
        {
            _salesOnboardingComplete = false;
        }
    }

    private async Task RefreshAmbassadeurOnboardingFlagAsync()
    {
        try
        {
            var dash = await _api.GetMyAmbassadeurDashboardAsync();
            _ambassadeurOnboardingComplete = dash?.IsOnboardingComplete == true;
        }
        catch
        {
            _ambassadeurOnboardingComplete = false;
        }
    }

    private async Task RefreshCsvImportFlagAsync()
    {
        try
        {
            var companies = await _api.GetMyCompaniesAsync();
            _csvImportEnabled = companies.Any(c => c.CsvBatchImportEnabled);
        }
        catch
        {
            _csvImportEnabled = false;
        }
    }

    private async Task RefreshMaqqieHoursNavAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _maqqieHoursNav = await _api.GetMaqqieHoursActiveAsync(cts.Token);
        }
        catch
        {
            _maqqieHoursNav = false;
        }
    }

    public ValueTask DisposeAsync()
    {
        _navigation.LocationChanged -= OnLocationChanged;
        _culture.Changed -= OnCultureChanged;
        _navRefresh.RefreshRequested -= OnNavRefreshRequestedAsync;
        _authState.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        return ValueTask.CompletedTask;
    }
}
