using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>Lazy-loads Leaflet + map helpers once per circuit.</summary>
public static class MapScripts
{
    public static ValueTask EnsureAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyMaps.ensure");
}
