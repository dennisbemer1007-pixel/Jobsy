using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Shared;

/// <summary>
/// Subscribes to <see cref="CultureState.Changed"/> so localized UI refreshes without a full reload.
/// </summary>
public abstract class CultureAwareComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected CultureState Culture { get; set; } = default!;

    protected override void OnInitialized()
    {
        Culture.Changed += HandleCultureChanged;
        base.OnInitialized();
    }

    private void HandleCultureChanged()
        => _ = InvokeAsync(OnCultureChanged);

    /// <summary>
    /// Called on the renderer sync context when the UI language changes.
    /// Default: re-render. Override to refresh derived state, then call <c>base.OnCultureChanged()</c>
    /// (or <see cref="ComponentBase.StateHasChanged"/>) as needed.
    /// </summary>
    protected virtual Task OnCultureChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        Culture.Changed -= HandleCultureChanged;
        GC.SuppressFinalize(this);
    }
}
