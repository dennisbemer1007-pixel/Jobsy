using Bunit;
using Jobsy.Core.Ai;
using Jobsy.Core.Legal;
using Jobsy.Core.Options;
using Jobsy.Web.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class AiProviderChoiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OpenAI")]
    [InlineData("openai")]
    public void Default_and_openai_stay_on_openai(string? provider)
    {
        var decision = AiProviderChoice.Decide(provider, "mistral-key");

        Assert.Equal(AiProviderKind.OpenAI, decision.Kind);
        Assert.False(decision.RequestedMistralWithoutKey);
        Assert.False(decision.UnknownProvider);
    }

    [Fact]
    public void Mistral_with_a_key_is_mistral()
    {
        var decision = AiProviderChoice.Decide(" Mistral ", " secret ");

        Assert.Equal(AiProviderKind.Mistral, decision.Kind);
        Assert.Equal(AiProviderNames.Mistral, decision.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Mistral_without_a_key_falls_back_to_openai(string? key)
    {
        var decision = AiProviderChoice.Decide("Mistral", key);

        Assert.Equal(AiProviderKind.OpenAI, decision.Kind);
        Assert.True(decision.RequestedMistralWithoutKey);
    }

    [Fact]
    public void Unknown_provider_stays_on_openai()
    {
        var decision = AiProviderChoice.Decide("Anthropic", "key");

        Assert.Equal(AiProviderKind.OpenAI, decision.Kind);
        Assert.True(decision.UnknownProvider);
    }
}

public class AiCapabilityTests
{
    [Fact]
    public void Only_chat_and_json_mode_are_called_and_both_work_on_mistral()
    {
        foreach (var capability in Enum.GetValues<AiCapability>())
        {
            var called = AiCapabilities.IsCalledByLobsy(capability);
            Assert.Equal(called, AiCapabilities.WorksOnMistral(capability));
            Assert.Equal(capability is AiCapability.Chat or AiCapability.JsonMode, called);
        }
    }

    [Fact]
    public void Mistral_defaults_match_the_documented_eu_endpoint()
    {
        Assert.Equal("https://api.mistral.ai/v1/", MistralOptions.DefaultBaseUrl);
        Assert.Equal("mistral-small-latest", MistralOptions.DefaultModel);
        Assert.Equal("OpenAI", new AiOptions().Provider);
    }
}

public class LegalAiProcessorSelectionTests
{
    [Fact]
    public void OpenAI_is_listed_only_when_it_is_the_provider()
    {
        var rows = LegalAiProcessorSelection.Resolve(AiProviderKind.OpenAI);

        Assert.Contains(rows, row => row.Id == "openai");
        Assert.DoesNotContain(rows, row => row.Id == "mistral");
        Assert.Equal("Verenigde Staten", LegalProcessors.ById("openai").Region);
        Assert.Equal(LegalProcessors.DataPrivacyFramework, LegalProcessors.ById("openai").TransferBasisKey);
    }

    [Fact]
    public void Mistral_replaces_openai_and_names_paris_in_the_eu()
    {
        var rows = LegalAiProcessorSelection.Resolve("Mistral", "key");
        var mistral = Assert.Single(rows, row => row.Id == "mistral");

        Assert.DoesNotContain(rows, row => row.Id == "openai");
        Assert.Equal("Mistral AI", mistral.Name);
        Assert.Contains("Parijs", mistral.Region, StringComparison.Ordinal);
        Assert.Contains("EU", mistral.Region, StringComparison.Ordinal);
        Assert.Equal(LegalProcessors.InsideEu, mistral.TransferBasisKey);
        Assert.Equal(ProcessorStatus.Active, mistral.Status);
    }

    [Fact]
    public void Mistral_without_a_key_keeps_the_openai_row()
    {
        var rows = LegalAiProcessorSelection.Resolve("Mistral", null);

        Assert.Contains(rows, row => row.Id == "openai");
        Assert.DoesNotContain(rows, row => row.Id == "mistral");
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("ro")]
    [InlineData("ar")]
    public void Processor_copy_and_changelog_exist_in_every_language(string language)
    {
        Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("Legal.Processor.mistral.Purpose", language)));
        var data = UiStrings.Get("Legal.Processor.mistral.Data", language);
        Assert.False(string.IsNullOrWhiteSpace(data));
        if (language == "ar")
        {
            Assert.Contains("الأوروبي", data, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains("EU", data, StringComparison.OrdinalIgnoreCase);
        }
        var change = UiStrings.Get("Legal.Change.Privacy.2026-10-06", language);
        Assert.NotEqual("Legal.Change.Privacy.2026-10-06", change);
        var lead = UiStrings.Get("Profile.OwnCvLead.Mistral", language);
        Assert.NotEqual("Profile.OwnCvLead.Mistral", lead);
        Assert.DoesNotContain("VS", lead, StringComparison.Ordinal);
        Assert.DoesNotContain("US", lead, StringComparison.Ordinal);
    }
}

/// <summary>The privacy page follows the configured provider. Default tests stay on OpenAI.</summary>
public class PrivacyMistralProcessorTests : PrivacyRenderTestBase
{
    public PrivacyMistralProcessorTests()
    {
        Services.AddSingleton<IOptions<AiOptions>>(
            Options.Create(new AiOptions { Provider = "Mistral" }));
        Services.AddSingleton<IOptions<MistralOptions>>(
            Options.Create(new MistralOptions { ApiKey = "mistral-test-key" }));
    }

    [Fact]
    public void The_table_names_mistral_and_not_openai()
    {
        var table = RenderPrivacy().Find("#delen .pp-table__grid").TextContent;

        Assert.Contains("Mistral AI", table, StringComparison.Ordinal);
        Assert.Contains("Parijs", table, StringComparison.Ordinal);
        Assert.Contains("EU", table, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", table, StringComparison.Ordinal);
    }

    [Fact]
    public void The_cv_sentence_says_the_data_stays_in_the_eu()
    {
        var ai = RenderPrivacy().Find("#ai").TextContent;

        Assert.Contains("Mistral AI", ai, StringComparison.Ordinal);
        Assert.Contains("EU", ai, StringComparison.Ordinal);
        Assert.DoesNotContain("naar OpenAI", ai, StringComparison.Ordinal);
    }
}
