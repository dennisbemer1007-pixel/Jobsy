using Jobsy.Core.Features;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Jobsy.Web.Features;

/// <summary>
/// Reads <see cref="RequiresFeatureAttribute"/> on the page type and redirects when the
/// requirement is not met. Wraps <see cref="AuthorizeRouteView"/> in Routes.razor.
/// </summary>
public sealed class FeatureRouteGate : ComponentBase
{
    [Inject] private IFeatureFlags FeatureFlags { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    [Parameter] public Microsoft.AspNetCore.Components.RouteData RouteData { get; set; } = default!;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool _checked;
    private bool _allowed = true;

    protected override async Task OnParametersSetAsync()
    {
        _checked = false;
        _allowed = true;

        var attrs = RouteData.PageType
            .GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
            .OfType<RequiresFeatureAttribute>()
            .ToList();

        if (attrs.Count == 0)
        {
            _checked = true;
            return;
        }

        var snap = await FeatureFlags.GetAsync();
        var state = await AuthState.GetAuthenticationStateAsync();
        foreach (var attr in attrs)
        {
            var enabled = snap.IsEnabled(attr.Feature);
            var ok = attr.WhenEnabled ? enabled : !enabled;
            if (!ok)
            {
                var fallback = string.IsNullOrWhiteSpace(attr.FallbackPath)
                    ? FeatureRoutes.HomeFor(state.User, snap)
                    : attr.FallbackPath!;

                // Passport OFF: keep ?tab= mapped to classic Kompas tabs.
                if (attr.Feature == PlatformFeature.CandidatePassport
                    && attr.WhenEnabled
                    && !string.IsNullOrWhiteSpace(attr.FallbackPath))
                {
                    fallback = PassportRedirects.ToClassicProfileUrl(Navigation.Uri);
                }

                _allowed = false;
                _checked = true;
                Navigation.NavigateTo(fallback, forceLoad: false, replace: true);
                return;
            }
        }

        _checked = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_checked && _allowed && ChildContent is not null)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}
