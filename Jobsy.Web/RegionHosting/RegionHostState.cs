using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Jobsy.Web.RegionHosting;

/// <summary>
/// Holds the active regional CNAME host for the current browser hostname only.
/// Persists the matched hostname in localStorage for analytics continuity, but never
/// applies another region's branding when the current host has no RegionHost.
/// </summary>
public sealed class RegionHostState
{
    private const string StorageKey = "Jobsy.RegionHostHostname";

    private readonly JobsyApiClient _api;
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _js;
    private bool _initialized;

    public RegionHostState(JobsyApiClient api, NavigationManager nav, IJSRuntime js)
    {
        _api = api;
        _nav = nav;
        _js = js;
    }

    public RegionHostItem? Current { get; private set; }

    public event Action? Changed;

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            var host = new Uri(_nav.Uri).Host;
            var resolved = await _api.ResolveRegionHostAsync(host);
            Current = resolved;

            try
            {
                if (resolved is not null)
                {
                    await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, resolved.Hostname);
                }
                else
                {
                    // Clear stale regional branding when visiting apex / unknown hosts.
                    await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                }
            }
            catch
            {
                // localStorage may be unavailable
            }

            Changed?.Invoke();
        }
        catch
        {
            Current = null;
        }
    }
}
