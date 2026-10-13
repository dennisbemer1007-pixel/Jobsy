using Jobsy.Core.Features;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Features;

/// <summary>Renders child content only when the given feature flag matches <see cref="WhenEnabled"/>.</summary>
public sealed class FeatureVisible : ComponentBase
{
    [Inject] private IFeatureFlags FeatureFlags { get; set; } = default!;

    [Parameter] public PlatformFeature Feature { get; set; }
    [Parameter] public bool WhenEnabled { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _show;
    private bool _loaded;

    protected override async Task OnParametersSetAsync()
    {
        var snap = await FeatureFlags.GetAsync();
        var enabled = snap.IsEnabled(Feature);
        _show = WhenEnabled ? enabled : !enabled;
        _loaded = true;
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        if (_loaded && _show && ChildContent is not null)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}
