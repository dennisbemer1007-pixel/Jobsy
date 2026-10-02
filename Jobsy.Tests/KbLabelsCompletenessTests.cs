using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class KbLabelsCompletenessTests
{
    public static IEnumerable<object[]> Languages()
    {
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            yield return [lang];
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void ApplicationStatus_FitBand_Transport_Timeline_Saved_have_labels(string lang)
    {
        var culture = CreateCulture(lang);

        foreach (var status in KbLabels.AllApplicationStatuses)
        {
            var label = KbLabels.Status(culture, status);
            Assert.False(string.IsNullOrWhiteSpace(label), $"{lang} status {status}");
            // Candidate wording must not dump raw enum names for Dutch (canonical UI).
            if (lang == "nl")
            {
                Assert.DoesNotContain(status.ToString(), label, StringComparison.Ordinal);
            }
        }

        foreach (var band in KbLabels.AllFitBands)
        {
            var label = KbLabels.FitBand(culture, band);
            Assert.False(string.IsNullOrWhiteSpace(label), $"{lang} band {band}");
        }

        foreach (var mode in KbLabels.AllTransportModes)
        {
            Assert.False(string.IsNullOrWhiteSpace(KbLabels.Transport(culture, mode)), $"{lang} transport {mode}");
            Assert.False(string.IsNullOrWhiteSpace(KbLabels.TransportVerb(culture, mode)), $"{lang} verb {mode}");
        }

        foreach (var step in KbLabels.AllTimelineSteps)
        {
            Assert.False(string.IsNullOrWhiteSpace(KbLabels.TimelineStep(culture, step)), $"{lang} timeline {step}");
        }

        foreach (var state in KbLabels.AllSavedStates)
        {
            Assert.False(string.IsNullOrWhiteSpace(KbLabels.SavedState(culture, state)), $"{lang} saved {state}");
        }
    }

    [Fact]
    public void CategoryColor_validates_hex_and_falls_back_to_token()
    {
        Assert.Equal("--category-color:#F54A1B", KbCategoryColor.Style("#F54A1B"));
        Assert.Equal("--category-color:#0f2d5c", KbCategoryColor.Style("#0f2d5c"));
        Assert.Equal($"--category-color:{KbCategoryColor.FallbackToken}", KbCategoryColor.Style(null));
        Assert.Equal($"--category-color:{KbCategoryColor.FallbackToken}", KbCategoryColor.Style("#fff"));
        Assert.Equal($"--category-color:{KbCategoryColor.FallbackToken}", KbCategoryColor.Style("red"));
        Assert.Equal($"--category-color:{KbCategoryColor.FallbackToken}", KbCategoryColor.Style("#GG0000"));
    }

    [Fact]
    public void KbRoutes_map_uses_fallback_home_when_banenkaart_absent()
    {
        Assert.Equal("/", KbRoutes.Map); // KB-FALLBACK(E)
        Assert.Equal("/candidate/applications", KbRoutes.Applications);
        Assert.Equal("/candidate/liked", KbRoutes.Saved);
        Assert.Equal("/candidate/shared", KbRoutes.Shared);
        Assert.Equal("/profiel", KbRoutes.PaspoortTests);
    }

    [Fact]
    public async Task DislikeSource_fallback_returns_none()
    {
        var source = KbNoDislikeSource.Instance;
        var result = await source.GetMatchingDislikeCodesAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.Empty(result);
    }

    private static CultureState CreateCulture(string lang)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new NoopJs());
        services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        var sp = services.BuildServiceProvider();
        var culture = new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>());
        var field = typeof(CultureState).GetField("<Language>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(culture, lang);
        return culture;
    }

    private sealed class NoopJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
