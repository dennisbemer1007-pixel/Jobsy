using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Jobsy.Web.RegionHosting;

/// <summary>
/// Holds the active regional CNAME host for the current browser session.
/// Resolved from the request hostname; persisted in localStorage for soft continuity.
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
            if (resolved is null)
            {
                // Soft fallback: last known regional host from localStorage (field-specific branding continuity).
                try
                {
                    var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                    if (!string.IsNullOrWhiteSpace(stored)
                        && !string.Equals(stored, host, StringComparison.OrdinalIgnoreCase))
                    {
                        resolved = await _api.ResolveRegionHostAsync(stored);
                    }
                }
                catch
                {
                    // localStorage may be unavailable
                }
            }

            Current = resolved;
            if (resolved is not null)
            {
                try
                {
                    await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, resolved.Hostname);
                }
                catch
                {
                    // ignore
                }
            }

            Changed?.Invoke();
        }
        catch
        {
            Current = null;
        }
    }
}
