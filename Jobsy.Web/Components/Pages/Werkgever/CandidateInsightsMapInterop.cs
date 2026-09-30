using Microsoft.JSInterop;

namespace Jobsy.Web.Components.Pages.Werkgever;

internal static class CandidateInsightsMapInterop
{
    private const string EnsureScript = """
        window.__jobsyInsightsMapReady = window.__jobsyInsightsMapReady || new Promise(function (resolve, reject) {
          if (window.JobsyCandidateInsightsMap) { resolve(); return; }
          var s = document.createElement('script');
          s.src = 'js/features/kandidaatinzichten-map.js?v=20260928-insights';
          s.onload = function () { resolve(); };
          s.onerror = reject;
          document.head.appendChild(s);
        });
        """;

    public static async Task EnsureLoadedAsync(IJSRuntime js)
    {
        await js.InvokeVoidAsync("eval", EnsureScript);
        await js.InvokeVoidAsync("eval", "window.__jobsyInsightsMapReady");
    }

    public static Task MountAsync(IJSRuntime js, string elementId, object options)
        => js.InvokeVoidAsync("JobsyCandidateInsightsMap.mount", elementId, options).AsTask();

    public static Task DestroyAsync(IJSRuntime js, string elementId)
        => js.InvokeVoidAsync("JobsyCandidateInsightsMap.destroy", elementId).AsTask();
}
