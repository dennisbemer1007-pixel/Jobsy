using System.Net.Http;
using Bunit;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests.Scholen;

public sealed class LeerlingReisGroep78BunitTests : BunitContext
{
    public LeerlingReisGroep78BunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddScoped<LeerlingReisShellState>();
        Services.AddScoped<LeerlingGroep78ShellTheme>();
        Services.AddSingleton<IPupilQuestionSetRegistry>(new PupilQuestionSetRegistry());
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://api.test/") }));
    }

    [Fact]
    public void Themed_vraag_shell_does_not_render_saveError_literal()
    {
        var def = Services.GetRequiredService<IPupilQuestionSetRegistry>().Get(PupilQuestionSet.Groep78);
        var item = def.Bank.GetById("9001");
        Assert.NotNull(item);

        var progress = new PupilProgressStateDto(
            Guid.NewGuid(), "8A", "ABC-123", PupilCodeStatus.InProgress,
            0, 60, 0, 0, false,
            item!.Id, item.WorldKey, new Dictionary<string, int>(),
            true, false, false, false, [], [], null, null,
            PupilQuestionSet.Groep78, "question", null, PupilJourneyTheme.Eilandtuin);

        var cut = Render<LeerlingReisTuinQuestion>(p => p
            .Add(c => c.State, progress)
            .Add(c => c.Def, def)
            .Add(c => c.Item, item)
            .Add(c => c.Saved, false)
            .Add(c => c.SaveError, (string?)null));

        var markup = cut.Markup;
        Assert.DoesNotContain("_saveError", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("_saved", markup, StringComparison.Ordinal);
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));
    }
}
