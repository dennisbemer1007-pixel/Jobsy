using Microsoft.JSInterop;

namespace Jobsy.Web.Components.Pages.Werkgever;

internal static class KandidatenkaartMapInterop
{
    public static Task EnsureLoadedAsync(IJSRuntime js)
        => js.InvokeVoidAsync("import", "/js/features/kandidatenkaart-map.js").AsTask();

    public static Task MountAsync(IJSRuntime js, string elementId, object options)
        => js.InvokeVoidAsync("JobsyKandidatenkaartMap.mount", elementId, options).AsTask();

    public static Task UpdateAreasAsync(IJSRuntime js, string elementId, object areas, string? selectedKey)
        => js.InvokeVoidAsync("JobsyKandidatenkaartMap.updateAreas", elementId, areas, selectedKey).AsTask();

    public static Task DestroyAsync(IJSRuntime js, string elementId)
        => js.InvokeVoidAsync("JobsyKandidatenkaartMap.destroy", elementId).AsTask();
}
