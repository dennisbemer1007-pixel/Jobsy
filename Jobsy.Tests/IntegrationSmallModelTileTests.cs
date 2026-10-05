using Bunit;
using Jobsy.Core.Options;
using Jobsy.Web.Components.Admin;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class IntegrationSmallModelTileTests : BunitContext
{
    public IntegrationSmallModelTileTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IOptions<MailOptions>>(Options.Create(new MailOptions()));
        Services.AddSingleton<IHostEnvironment>(new FakeHost());
        Services.AddSingleton(new JobsyApiClient(new HttpClient
        {
            BaseAddress = new Uri("http://localhost/")
        }));
    }

    [Fact]
    public void OpenAi_tile_shows_the_optional_small_model_field()
    {
        var cut = Render<IntegrationSettingsTile>(parameters => parameters
            .Add(p => p.Credential, new IntegrationCredentialItem
            {
                Key = "OpenAI",
                DisplayName = "OpenAI",
                Description = "AI",
                SupportsModel = true,
                SupportsApiKey = true,
                Model = "mistral-medium-latest",
                SmallModel = "mistral-small-latest"
            }));

        cut.Find("article").Click();

        Assert.Contains("Klein model (goedkope diensten)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("leeg = zelfde als Model", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("mistral-small-latest", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Tiles_without_a_model_hide_the_small_model_field()
    {
        var cut = Render<IntegrationSettingsTile>(parameters => parameters
            .Add(p => p.Credential, new IntegrationCredentialItem
            {
                Key = "Mollie",
                DisplayName = "Mollie",
                Description = "Betalingen",
                SupportsModel = false,
                SupportsApiKey = true
            }));

        cut.Find("article").Click();

        Assert.DoesNotContain("Klein model", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class FakeHost : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
