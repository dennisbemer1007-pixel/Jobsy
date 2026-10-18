using System.Security.Claims;
using Bunit;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Components.Web;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class DreamJobStepBunitTests : BunitContext
{
    public DreamJobStepBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DreamJobIllustrationOptions.EnabledKey] = "false",
            })
            .Build());
    }

    [Fact]
    public void Flag_off_renders_classic_card_without_inspiration_chips()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DreamJobIllustrationOptions.EnabledKey] = "false",
            })
            .Build());

        var draft = new OnboardingProfileDraft();
        var cut = Render<DreamJobStep>(p => p
            .Add(c => c.Draft, draft)
            .Add(c => c.ChooseDream, (DreamJob _) => Task.CompletedTask)
            .Add(c => c.ChooseTitle, (string _) => Task.CompletedTask)
            .Add(c => c.ChooseOwnDream, () => Task.CompletedTask)
            .Add(c => c.DreamKeyDown, (KeyboardEventArgs _) => Task.CompletedTask));

        Assert.NotNull(cut.Find("[data-testid=dream-job-step-classic]"));
        Assert.Throws<ElementNotFoundException>(() => cut.Find("[data-testid=dream-inspiration-chips]"));
    }

    [Fact]
    public void Inspiration_chip_fills_search_field_and_shows_confirmation()
    {
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DreamJobIllustrationOptions.EnabledKey] = "true",
            })
            .Build());

        var draft = new OnboardingProfileDraft();
        var cut = Render<DreamJobStep>(p => p
            .Add(c => c.Draft, draft)
            .Add(c => c.ChooseDream, (DreamJob _) => Task.CompletedTask)
            .Add(c => c.ChooseTitle, (string _) => Task.CompletedTask)
            .Add(c => c.ChooseInspiration, (string title) =>
            {
                draft.Dream = title;
                draft.DreamSearch = title;
                return Task.CompletedTask;
            })
            .Add(c => c.ChooseOwnDream, () => Task.CompletedTask)
            .Add(c => c.DreamKeyDown, (KeyboardEventArgs _) => Task.CompletedTask));

        var chips = cut.FindAll("[data-testid=dream-inspiration-chip]");
        Assert.True(chips.Count >= 7);

        chips[0].Click();

        var input = cut.Find("[data-testid=dream-job-search]");
        Assert.Equal("Dierenarts", input.GetAttribute("value"));
        Assert.NotNull(cut.Find("[data-testid=dream-job-confirm]"));
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeHostEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/workspace/Jobsy.Web";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
