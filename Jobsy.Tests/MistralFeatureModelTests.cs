using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class MistralFeatureModelTests
{
    [Fact]
    public void Empty_slots_use_the_shared_model_then_the_default()
    {
        var shared = new MistralOptions
        {
            Model = "mistral-small-latest",
            Models = new MistralFeatureModels { Story = "  ", Chat = "", Compass = null, CareerReport = null }
        };

        Assert.Equal("mistral-small-latest", shared.ModelFor(OpenAiFeature.WhoAmI));
        Assert.Equal("mistral-small-latest", shared.ModelFor(OpenAiFeature.AssistantChat));
        Assert.Equal("mistral-small-latest", shared.ModelFor(OpenAiFeature.CareerCompass));
        Assert.Equal("mistral-small-latest", shared.ModelFor(OpenAiFeature.VacancyContentModeration));

        var blankShared = new MistralOptions { Model = "   ", Models = new MistralFeatureModels() };
        Assert.Equal(MistralOptions.DefaultModel, blankShared.ModelFor(OpenAiFeature.WhoAmI));
        Assert.Equal("mistral-small-latest", MistralOptions.DefaultModel);
    }

    [Fact]
    public void Active_rows_show_the_model_each_surface_uses()
    {
        var options = new MistralOptions
        {
            Model = "mistral-small-latest",
            Models = new MistralFeatureModels
            {
                Story = "mistral-medium-latest",
                CareerReport = "mistral-medium-latest"
            }
        };

        var rows = options.ActiveFeatureModels().ToDictionary(row => row.Feature, row => row.Model);

        Assert.Equal("mistral-medium-latest", rows[MistralFeatureSlots.Story]);
        Assert.Equal("mistral-medium-latest", rows[MistralFeatureSlots.CareerReport]);
        Assert.Equal("mistral-medium-latest", rows[MistralFeatureSlots.Compass]);
        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.Chat]);
        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.Translation]);
        Assert.Equal(12, rows.Count);
    }

    [Fact]
    public void Cheap_rows_use_the_small_model_when_it_is_set()
    {
        var options = new MistralOptions
        {
            Model = "mistral-medium-latest",
            SmallModel = "mistral-small-latest",
            Models = new MistralFeatureModels { Story = "mistral-large-latest" }
        };

        var rows = options.ActiveFeatureModels().ToDictionary(row => row.Feature, row => row.Model);

        Assert.Equal("mistral-large-latest", rows[MistralFeatureSlots.Story]);
        Assert.Equal("mistral-medium-latest", rows[MistralFeatureSlots.MockInterview]);
        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.Translation]);
        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.CvExtraction]);
        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.VacancyModeration]);
        Assert.Equal("mistral-small-latest", options.ModelFor(OpenAiFeature.Translation));
        Assert.Equal("mistral-medium-latest", options.ModelFor(OpenAiFeature.MockInterview));
    }

    [Fact]
    public void Config_section_binds_small_model_keys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:Model"] = "mistral-medium-latest",
                ["OpenAI:SmallModel"] = "mistral-small-latest",
                ["Ai:SmallModel"] = "from-ai",
                ["Mistral:Model"] = "mistral-medium-latest",
                ["Mistral:SmallModel"] = "mistral-small-latest"
            })
            .Build();

        var openAi = new OpenAiOptions();
        var ai = new AiOptions();
        var mistral = new MistralOptions();
        config.GetSection(OpenAiOptions.SectionName).Bind(openAi);
        config.GetSection(AiOptions.SectionName).Bind(ai);
        config.GetSection(MistralOptions.SectionName).Bind(mistral);

        Assert.Equal("mistral-medium-latest", openAi.Model);
        Assert.Equal("mistral-small-latest", openAi.SmallModel);
        Assert.Equal("from-ai", ai.SmallModel);
        Assert.Equal("mistral-small-latest", mistral.ModelFor(OpenAiFeature.CvExtraction));
        Assert.Equal("mistral-medium-latest", mistral.ModelFor(OpenAiFeature.WhoAmI));
    }

    [Fact]
    public void Career_report_row_follows_the_compass_model_when_both_are_set()
    {
        var options = new MistralOptions
        {
            Models = new MistralFeatureModels
            {
                Compass = "mistral-small-latest",
                CareerReport = "mistral-medium-latest"
            }
        };

        var rows = options.ActiveFeatureModels().ToDictionary(row => row.Feature, row => row.Model);

        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.CareerReport]);
        Assert.Equal("mistral-small-latest", rows[MistralFeatureSlots.Compass]);
        Assert.Equal("mistral-small-latest", options.ModelForCompassCall());
    }

    [Fact]
    public void Config_section_binds_the_four_env_keys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mistral:Model"] = "mistral-small-latest",
                ["Mistral:Models:Story"] = "mistral-medium-latest",
                ["Mistral:Models:CareerReport"] = "mistral-medium-latest",
                ["Mistral:Models:Compass"] = "",
                ["Mistral:Models:Chat"] = "mistral-small-latest"
            })
            .Build();

        var options = new MistralOptions();
        config.GetSection(MistralOptions.SectionName).Bind(options);

        Assert.Equal("mistral-medium-latest", options.ModelFor(OpenAiFeature.WhoAmI));
        Assert.Equal("mistral-medium-latest", options.ModelFor(OpenAiFeature.CareerCompass));
        Assert.Equal("mistral-small-latest", options.ModelFor(OpenAiFeature.AssistantChat));
    }

    [Fact]
    public void Admin_status_lists_the_four_models_only_for_mistral()
    {
        using var db = new JobsyDbContext(
            new DbContextOptionsBuilder<JobsyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var mistral = Controller(db, new AiOptions { Provider = "Mistral" }, new MistralOptions
        {
            ApiKey = "secret",
            Model = "mistral-small-latest",
            Models = new MistralFeatureModels { Story = "mistral-medium-latest" }
        });
        var mistralOk = Assert.IsType<OkObjectResult>(mistral.GetAiProvider().Result);
        var mistralBody = Assert.IsType<AiProviderStatusDto>(mistralOk.Value);
        Assert.Equal(MistralOptions.DefaultModel, mistralBody.Model);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<AiFeatureModelDto>>(mistralBody.FeatureModels);
        Assert.Equal(12, rows.Count);
        Assert.Equal("mistral-medium-latest", Assert.Single(rows, row => row.Feature == "Story").Model);
        Assert.Equal("mistral-small-latest", Assert.Single(rows, row => row.Feature == "Chat").Model);

        var openAi = Controller(db, new AiOptions { Provider = "OpenAI" }, new MistralOptions
        {
            ApiKey = "secret",
            Models = new MistralFeatureModels { Story = "mistral-medium-latest" }
        });
        var openAiOk = Assert.IsType<OkObjectResult>(openAi.GetAiProvider().Result);
        var openAiBody = Assert.IsType<AiProviderStatusDto>(openAiOk.Value);
        Assert.Null(openAiBody.FeatureModels);
    }

    private static SettingsController Controller(JobsyDbContext db, AiOptions ai, MistralOptions mistral)
        => new(
            db,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            new PlatformFeatureService(
                db,
                Options.Create(new JobsyFeatureOptions()),
                new ConfigurationBuilder().Build()),
            new PlatformCompanySettingsService(db),
            new MarketingFlyerSettingsService(db),
            new MarketingFlyerPdfService(
                new MarketingFlyerSettingsService(db),
                new PlatformCompanySettingsService(db),
                new PlatformFeatureService(
                    db,
                    Options.Create(new JobsyFeatureOptions()),
                    new ConfigurationBuilder().Build())),
            new FlexCommercialService(db),
            new NoOpAdminAuditLog(),
            new NoOpAdminAuditContext(),
            new FakeUserLookup(),
            Options.Create(ai),
            Options.Create(mistral));
}
