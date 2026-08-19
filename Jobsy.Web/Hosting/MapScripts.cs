using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>Lazy-loads MapLibre GL after first paint (500ms + near-viewport) for discovery; detail maps load on demand.</summary>
public static class MapScripts
{
    public static ValueTask EnsureAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure");

    public static ValueTask EnsureDiscoveryAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure", "discovery");

    public static ValueTask EnsureDiscoveryAfterPaintAsync(IJSRuntime js, string elementId = "job-map")
        => js.InvokeVoidAsync("jobsyMaps.ensureAfterPaint", "discovery", elementId);

    public static ValueTask EnsureDetailAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure", "detail");
}
