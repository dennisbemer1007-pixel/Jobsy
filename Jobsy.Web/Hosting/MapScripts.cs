using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>Lazy-loads MapLibre GL on map interaction (desktop) or after first paint (mobile). Never in the initial HTML.</summary>
public static class MapScripts
{
    public static ValueTask EnsureAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure");

    public static ValueTask EnsureDiscoveryAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure", "discovery");

    public static ValueTask<bool> IsReadyAsync(IJSRuntime js, string kind = "discovery")
        => js.InvokeAsync<bool>("jobsyMaps.isReady", kind);

    public static ValueTask EnsureDiscoveryAfterPaintAsync(IJSRuntime js, string elementId = "job-map")
        => js.InvokeVoidAsync("jobsyMaps.ensureAfterPaint", "discovery", elementId);

    public static ValueTask EnsureDetailAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure", "detail");
}
