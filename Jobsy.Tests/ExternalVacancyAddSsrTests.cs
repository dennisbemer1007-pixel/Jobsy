using Bunit;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class ExternalVacancyAddSsrTests : BunitContext
{
    public ExternalVacancyAddSsrTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>()));
    }

    [Fact]
    public void Add_page_shell_includes_url_field_and_submit()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var html = ExternalVacancyAddSsrShells.BuildBody(culture, "https://example.com/job");

        Assert.Contains("data-testid=\"kb-ext-add\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-ext-url-input\"", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"kb-ext-add-submit\"", html, StringComparison.Ordinal);
        Assert.Contains("https://example.com/job", html, StringComparison.Ordinal);
    }
}
