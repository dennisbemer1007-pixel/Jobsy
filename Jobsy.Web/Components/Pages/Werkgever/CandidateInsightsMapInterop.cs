using Jobsy.Web.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Web.Components.Pages.Werkgever;

internal static class CandidateInsightsMapInterop
{
    public static Task EnsureLoadedAsync(IJSRuntime js)
        => js.InvokeVoidAsync(
            "jobsyDom.ensureScript",
            WwwrootAssetCatalog.Url("js/features/kandidaatinzichten-map.js"),
            "JobsyCandidateInsightsMap").AsTask();

    public static Task MountAsync(IJSRuntime js, string elementId, object options)
        => js.InvokeVoidAsync("JobsyCandidateInsightsMap.mount", elementId, options).AsTask();

    public static Task DestroyAsync(IJSRuntime js, string elementId)
        => js.InvokeVoidAsync("JobsyCandidateInsightsMap.destroy", elementId).AsTask();
}
