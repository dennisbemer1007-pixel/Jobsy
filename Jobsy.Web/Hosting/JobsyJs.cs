using Microsoft.JSInterop;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Lazy-loads JS that the anonymous homepage must not parse (session idle, downloads, richtext, feedback).
/// </summary>
public static class JobsyJs
{
    public static ValueTask EnsureExtrasAsync(IJSRuntime js)
        => js.InvokeVoidAsync("jobsyExtras.ensure");

    public static ValueTask EnsureFeedbackAsync(IJSRuntime js)
        => js.InvokeVoidAsync("lobsyFeedbackEnsure");
}
