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
        Assert.Equal("https://api.eu.mistral.ai/v1/", MistralOptions.DefaultBaseUrl);
        Assert.Equal("mistral-small-latest", MistralOptions.DefaultModel);
        Assert.True(MistralEndpoint.InferenceStaysInEu(null));
        Assert.True(MistralEndpoint.InferenceStaysInEu("https://api.eu.mistral.ai/v1/"));
        Assert.False(MistralEndpoint.InferenceStaysInEu("https://api.mistral.ai/v1/"));
        Assert.False(MistralEndpoint.InferenceStaysInEu("https://api.us.mistral.ai/v1/"));
        Assert.Equal(MistralOptions.DefaultBaseUrl, MistralEndpoint.EffectiveBaseUrl("http://127.0.0.1/v1/"));
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
        Assert.Contains("verwerking in de EU", mistral.Region, StringComparison.Ordinal);
        Assert.Contains("buiten de EU", mistral.Region, StringComparison.Ordinal);
        Assert.Equal(LegalProcessors.InsideEu, mistral.TransferBasisKey);
        Assert.Equal(ProcessorStatus.Active, mistral.Status);
    }

    [Fact]
    public void Global_mistral_host_does_not_claim_eu_inference()
    {
        var rows = LegalAiProcessorSelection.Resolve("Mistral", "key", "https://api.mistral.ai/v1/");
        var mistral = Assert.Single(rows, row => row.Id == "mistral");

        Assert.Equal("mistral", mistral.Id);
        Assert.DoesNotContain("verwerking in de EU", mistral.Region, StringComparison.Ordinal);
        Assert.Contains("geen plek", mistral.Region, StringComparison.Ordinal);
        Assert.Contains("buiten de EU", mistral.Region, StringComparison.Ordinal);
        Assert.Equal("Legal.Processor.mistral.Data.Global", mistral.DataKey);
        Assert.Equal(LegalProcessors.NoStatedPlace, mistral.TransferBasisKey);
        Assert.DoesNotContain("EU", UiStrings.Get(mistral.DataKey, "nl"), StringComparison.Ordinal);
        Assert.DoesNotContain("EU", UiStrings.Get(mistral.TransferBasisKey!, "en"), StringComparison.Ordinal);
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
        var regionWord = language switch
        {
            "ar" => "الأوروبي",
            "pl" or "ro" => "UE",
            _ => "EU",
        };
        Assert.Contains(regionWord, data, StringComparison.Ordinal);
        var globalData = UiStrings.Get("Legal.Processor.mistral.Data.Global", language);
        Assert.DoesNotContain(regionWord, globalData, StringComparison.Ordinal);
        var change = UiStrings.Get("Legal.Change.Privacy.2026-10-07", language);
        Assert.NotEqual("Legal.Change.Privacy.2026-10-07", change);
        var lead = UiStrings.Get("Profile.OwnCvLead.Mistral", language);
        Assert.NotEqual("Profile.OwnCvLead.Mistral", lead);
        Assert.Contains(regionWord, lead, StringComparison.Ordinal);
        var globalLead = UiStrings.Get("Profile.OwnCvLead.Mistral.Global", language);
        Assert.NotEqual("Profile.OwnCvLead.Mistral.Global", globalLead);
        Assert.DoesNotContain("VS", globalLead, StringComparison.Ordinal);
        Assert.DoesNotContain("US", globalLead, StringComparison.Ordinal);
        if (language is "nl")
        {
            Assert.Contains("geen plek", globalLead, StringComparison.Ordinal);
            Assert.DoesNotContain("verwerking in de EU", globalLead, StringComparison.Ordinal);
        }
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

        var row = RenderPrivacy().FindAll("#delen tbody tr")
            .Single(tr => tr.TextContent.Contains("Mistral AI", StringComparison.Ordinal));
        Assert.Contains("Parijs", row.TextContent, StringComparison.Ordinal);
        Assert.Contains("verwerking in de EU", row.TextContent, StringComparison.Ordinal);
        Assert.Contains("Binnen de EU", row.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", table, StringComparison.Ordinal);
    }

    [Fact]
    public void The_cv_sentence_says_the_data_stays_in_the_eu()
    {
        var ai = RenderPrivacy().Find("#ai").TextContent;

        Assert.Contains("Mistral AI", ai, StringComparison.Ordinal);
        Assert.Contains("verwerking in de EU", ai, StringComparison.Ordinal);
        Assert.DoesNotContain("naar OpenAI", ai, StringComparison.Ordinal);
    }
}

/// <summary>A configured global host must not claim that inference stays in the EU.</summary>
public class PrivacyMistralGlobalProcessorTests : PrivacyRenderTestBase
{
    public PrivacyMistralGlobalProcessorTests()
    {
        Services.AddSingleton<IOptions<AiOptions>>(
            Options.Create(new AiOptions { Provider = "Mistral" }));
        Services.AddSingleton<IOptions<MistralOptions>>(
            Options.Create(new MistralOptions
            {
                ApiKey = "mistral-test-key",
                BaseUrl = "https://api.mistral.ai/v1/"
            }));
    }

    [Fact]
    public void The_table_and_cv_sentence_do_not_claim_eu_processing()
    {
        var rendered = RenderPrivacy();
        var row = rendered.FindAll("#delen tbody tr")
            .Single(tr => tr.TextContent.Contains("Mistral AI", StringComparison.Ordinal));
        var ai = rendered.Find("#ai").TextContent;

        Assert.Contains("geen plek", row.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("verwerking in de EU", row.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Binnen de EU", row.TextContent, StringComparison.Ordinal);
        Assert.Contains("geen plek", ai, StringComparison.Ordinal);
        Assert.DoesNotContain("verwerking in de EU", ai, StringComparison.Ordinal);
        Assert.DoesNotContain("naar OpenAI", ai, StringComparison.Ordinal);
    }
}
