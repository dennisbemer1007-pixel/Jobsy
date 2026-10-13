using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Api.Controllers;
using Jobsy.Core.Contracts;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class DiscoveryPreferencesDtoTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Old_preferences_json_round_trips_unchanged()
    {
        var oldJson = MeController.SerializePreferences(
            ["horeca"],
            30,
            "Fiets",
            "nl",
            aboutMe: "Hallo",
            defaultMotivation: "Ik wil graag werken.");
        var prefs = MeController.ParsePreferences(oldJson);
        Assert.Null(prefs.SpokenLanguages);
        Assert.Null(prefs.DutchLevel);
        Assert.Null(prefs.EmployerPreferences);
        Assert.Null(prefs.LearningGoals);
        Assert.Null(prefs.Hobbies);

        var again = MeController.SerializePreferences(prefs);
        var againPrefs = MeController.ParsePreferences(again);
        Assert.Equal(prefs.AboutMe, againPrefs.AboutMe);
        Assert.Equal(prefs.DefaultMotivation, againPrefs.DefaultMotivation);
        Assert.Equal(prefs.MaxTravelMinutes, againPrefs.MaxTravelMinutes);
        Assert.Null(againPrefs.SpokenLanguages);
        Assert.Null(againPrefs.DutchLevel);
    }

    [Fact]
    public void New_discovery_fields_round_trip()
    {
        var dto = new CandidatePreferencesDto(
            ["zorg"],
            20,
            "Fiets",
            "nl",
            SpokenLanguages: [new CandidateLanguageDto("en", "goed"), new CandidateLanguageDto("ar", "basis")],
            DutchLevel: "goed",
            EmployerPreferences: ["small-team", "close-to-home"],
            LearningGoals: ["beter plannen", "Nederlands oefenen"],
            Hobbies: ["sport", "muziek-vrij"]);

        var json = MeController.SerializePreferences(dto);
        var parsed = MeController.ParsePreferences(json);
        Assert.Equal("goed", parsed.DutchLevel);
        Assert.Equal(2, parsed.SpokenLanguages!.Count);
        Assert.Contains(parsed.SpokenLanguages, l => l.Code == "en" && l.Level == "goed");
        Assert.Equal(["small-team", "close-to-home"], parsed.EmployerPreferences);
        Assert.Equal(2, parsed.LearningGoals!.Count);
        Assert.Contains("sport", parsed.Hobbies!);
        Assert.Contains("muziek-vrij", parsed.Hobbies!);
    }

    [Fact]
    public void SystemTextJson_deserializes_old_payload_without_new_fields()
    {
        const string legacy = """{"roles":["retail"],"maxTravelMinutes":45,"preferredTransport":"Fiets"}""";
        var prefs = JsonSerializer.Deserialize<CandidatePreferencesDto>(legacy, Json);
        Assert.NotNull(prefs);
        Assert.Null(prefs!.SpokenLanguages);
        Assert.Null(prefs.Hobbies);
    }
}

public class DiscoveryValidatorsTests
{
    [Fact]
    public void Preferences_validator_drops_unknown_codes_trims_and_dedups()
    {
        var dropped = new List<string>();
        var prefs = new CandidatePreferencesDto(
            [],
            null,
            null,
            SpokenLanguages:
            [
                new(" EN ", "GOED"),
                new("en", "basis"),
                new("x", "goed"),
                new("nl", "nope")
            ],
            DutchLevel: "unknown",
            EmployerPreferences: [" small-team ", "SMALL-TEAM", "not-a-code"],
            LearningGoals:
            [
                "  plan  ",
                "plan",
                new string('x', 80)
            ],
            Hobbies: ["SPORT", "sport", "custom-hobby-that-is-way-too-long-for-the-limit-xxxxx", "weird"]);

        var clean = CandidatePreferencesValidator.Sanitize(prefs, dropped.Add);
        Assert.Null(clean.DutchLevel);
        Assert.Equal(2, clean.SpokenLanguages!.Count);
        Assert.Contains(clean.SpokenLanguages, l => l.Code == "en" && l.Level == "goed");
        Assert.Contains(clean.SpokenLanguages, l => l.Code == "nl" && l.Level is null);
        Assert.Equal(["small-team"], clean.EmployerPreferences);
        Assert.Equal(2, clean.LearningGoals!.Count);
        Assert.Equal("plan", clean.LearningGoals[0]);
        Assert.Equal(60, clean.LearningGoals[1].Length);
        Assert.Contains("sport", clean.Hobbies!);
        Assert.All(clean.Hobbies!, h => Assert.True(h.Length <= DiscoveryCatalogs.MaxHobbyFreeTextLength));
        Assert.Contains(dropped, d => d.Contains("DutchLevel", StringComparison.Ordinal));
        Assert.Contains(dropped, d => d.Contains("EmployerPreferences", StringComparison.Ordinal));
    }

    [Fact]
    public void Private_validator_caps_custom_and_drops_unknown()
    {
        var dropped = new List<string>();
        var (codes, custom) = CandidatePrivatePreferencesValidator.Sanitize(
            ["night-shifts", "NIGHT-SHIFTS", "teleporting"],
            ["  noise  ", "noise", new string('y', 50), "a", "b", "c", "d", "e"],
            dropped.Add);
        Assert.Equal(["night-shifts"], codes);
        Assert.DoesNotContain("noise", custom);
        Assert.All(custom, c => Assert.True(c.Length <= DiscoveryCatalogs.MaxCustomDislikeLength));
        Assert.True(custom.Count <= DiscoveryCatalogs.MaxCustomDislikes);
        Assert.NotEmpty(dropped);
    }
}

public class DislikeMatchRulesTests
{
    private sealed record Item(string Id, bool Night, int Percent);

    [Fact]
    public void Night_shift_vacancies_sort_after_equal_score_peers()
    {
        var items = new List<Item>
        {
            new("night-a", true, 80),
            new("day-a", false, 80),
            new("night-b", true, 80),
            new("day-b", false, 80)
        };

        var ordered = DislikeMatchRules.DownRankNightShifts(
            items,
            ["night-shifts"],
            x => x.Night);

        Assert.Equal(["day-a", "day-b", "night-a", "night-b"], ordered.Select(x => x.Id).ToList());
        Assert.All(ordered, x => Assert.Equal(80, x.Percent));
    }

    [Fact]
    public void Other_dislike_codes_have_no_effect()
    {
        var items = new List<Item>
        {
            new("night", true, 70),
            new("day", false, 70)
        };
        var ordered = DislikeMatchRules.DownRankNightShifts(items, ["noise", "crowded"], x => x.Night);
        Assert.Equal(["night", "day"], ordered.Select(x => x.Id).ToList());
    }

    [Fact]
    public void Without_night_dislike_order_unchanged()
    {
        var items = new List<Item>
        {
            new("night", true, 90),
            new("day", false, 90)
        };
        var ordered = DislikeMatchRules.DownRankNightShifts(items, [], x => x.Night);
        Assert.Same(items, ordered);
    }
}

public class DiscoveryDislikeGuardTests
{
    private static readonly Regex Forbidden = new(
        @"(?i)(Dislike|CustomDislike|PrivatePreferences)",
        RegexOptions.Compiled);

    [Fact]
    public void Employer_facing_dtos_have_no_dislike_shaped_members()
    {
        var roots = new[]
        {
            typeof(CandidateInsightsDto),
            typeof(LobsyCvModel),
            typeof(LobsyCvWhoAmI),
            typeof(AnonymousTalentCardDto),
            typeof(TalentContactRequestDto),
            typeof(CandidateMatchedVacancyDto)
        };

        foreach (var root in roots)
        {
            Walk(root, root.Name);
        }
    }

    [Fact]
    public void PreferencesSummary_builder_excludes_dislikes()
    {
        var method = typeof(ApplicationsController).GetMethod(
            "BuildCompactPreferencesSummary",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var json = (string)method!.Invoke(null, [
            new CandidatePreferencesDto(
                ["zorg"],
                20,
                "Fiets",
                Hobbies: ["sport"],
                LearningGoals: ["leren"])
        ])!;
        Assert.DoesNotContain("dislike", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hobby", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("roles", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Employer_ai_prompt_builders_omit_dislike_tokens()
    {
        var root = TestRepo.FindRoot();
        var files = new[]
        {
            Path.Combine(root, "Jobsy.Core/Rules/CultureFitPrompt.cs"),
            Path.Combine(root, "Jobsy.Core/Rules/WhoAmIPrompt.cs"),
            Path.Combine(root, "Jobsy.Core/Rules/CareerCompassPrompt.cs"),
            Path.Combine(root, "Jobsy.Infrastructure/Services/TalentPoolService.cs"),
            Path.Combine(root, "Jobsy.Infrastructure/Services/CandidateInsightsComputer.cs")
        };

        foreach (var file in files)
        {
            Assert.True(File.Exists(file), file);
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("DislikesJson", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CustomDislikes", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PrivatePreferences", text, StringComparison.Ordinal);
        }
    }

    private static void Walk(Type type, string path)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid)
            || type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(Guid?)
            || type == typeof(int?) || type == typeof(bool) || type == typeof(bool?)
            || type == typeof(double) || type == typeof(double?) || type.IsEnum)
        {
            return;
        }

        if (type.IsArray)
        {
            Walk(type.GetElementType()!, path + "[]");
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                Walk(arg, path + "<" + arg.Name + ">");
            }
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Assert.False(
                Forbidden.IsMatch(prop.Name),
                $"Forbidden dislike-shaped property {type.Name}.{prop.Name} at {path}");
            if (prop.PropertyType.Namespace?.StartsWith("Jobsy", StringComparison.Ordinal) == true
                || prop.PropertyType.IsGenericType)
            {
                Walk(prop.PropertyType, $"{path}.{prop.Name}");
            }
        }
    }
}

public class DiscoveryPassportBunitTests : BunitContext
{
    public DiscoveryPassportBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void PassportCard_shows_languages_fact_filled_and_empty()
    {
        var empty = Render<PassportCard>(p => p
            .Add(x => x.DisplayName, "Samira")
            .Add(x => x.Initials, "SE")
            .Add(x => x.MemberNumber, "LB-1")
            .Add(x => x.LookingFor, Array.Empty<string>())
            .Add(x => x.LanguagesLabel, (string?)null));
        Assert.Contains("Talen", empty.Markup, StringComparison.Ordinal);
        Assert.Contains("—", empty.Markup, StringComparison.Ordinal);

        var filled = Render<PassportCard>(p => p
            .Add(x => x.DisplayName, "Samira")
            .Add(x => x.Initials, "SE")
            .Add(x => x.MemberNumber, "LB-1")
            .Add(x => x.LookingFor, Array.Empty<string>())
            .Add(x => x.LanguagesLabel, "Nederlands: goed, Engels"));
        Assert.Contains("Nederlands: goed, Engels", filled.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DnaTab_hides_hobbies_when_empty_shows_when_filled()
    {
        var empty = Render<PassportDnaTab>(p => p
            .Add(x => x.BubbleText, "hoi")
            .Add(x => x.Hobbies, Array.Empty<string>()));
        Assert.DoesNotContain("Waar word je blij van", empty.Markup, StringComparison.Ordinal);

        var filled = Render<PassportDnaTab>(p => p
            .Add(x => x.BubbleText, "hoi")
            .Add(x => x.Hobbies, ["Sport", "Muziek", "Koken", "Games", "Extra"]));
        Assert.Contains("Waar word je blij van", filled.Markup, StringComparison.Ordinal);
        Assert.Contains("Sport", filled.Markup, StringComparison.Ordinal);
        Assert.Contains("+1", filled.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DislikesSection_shows_private_note()
    {
        var root = TestRepo.FindRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Candidate/ProfileSections/DislikesSection.razor"));
        Assert.Contains("Discovery.Dislike.PrivateNote", razor, StringComparison.Ordinal);
        Assert.Contains("DiscoveryCatalogs.DislikeCodes", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void ProofTab_shows_learning_goals_line()
    {
        var root = TestRepo.FindRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Candidate/Passport/PassportProofTab.razor"));
        Assert.Contains("Passport.Proof.WantToLearn", razor, StringComparison.Ordinal);
        Assert.Contains("Editor.LearningGoals", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void DataTab_wires_discovery_accordions()
    {
        var root = TestRepo.FindRoot();
        var razor = File.ReadAllText(Path.Combine(
            root,
            "Jobsy.Web/Components/Candidate/Passport/PassportDataTab.razor"));
        Assert.Contains("JoyAndLearningSection", razor, StringComparison.Ordinal);
        Assert.Contains("DislikesSection", razor, StringComparison.Ordinal);
        Assert.Contains("SavePrivatePreferencesAsync", razor, StringComparison.Ordinal);
        Assert.Contains("ShowDiscoveryFields=\"true\"", razor, StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

public class CourseSlotLearningGoalsTests
{
    [Fact]
    public void Extra_context_keys_from_learning_goals_feed_search_blob()
    {
        var offers = new[]
        {
            Make("free-plan", isFree: true, isPartner: false, keys: "plannen"),
        };
        var noMatch = CourseSlotRules.Pick(offers, new CourseSlotRules.Context([], "zorg"));
        Assert.Empty(noMatch);

        var withGoals = CourseSlotRules.Pick(
            offers,
            new CourseSlotRules.Context([], "zorg", ["beter plannen"]));
        Assert.Single(withGoals);
        Assert.Equal("free-plan", withGoals[0].Offer.Title);
    }

    private static Jobsy.Core.Entities.TrainingOffer Make(string title, bool isFree, bool isPartner, string keys)
    {
        var provider = new Jobsy.Core.Entities.TrainingProvider
        {
            Id = Guid.NewGuid(),
            Name = "LeerPlein",
            Kind = Jobsy.Core.Enums.TrainingProviderKind.RegionalPartner,
            Network = Jobsy.Core.Enums.TrainingNetwork.Direct,
            BaseUrl = "https://leerplein.example/",
            FieldsCsv = "vaardigheden",
            Region = "Den Haag",
            IsActive = true
        };
        return new Jobsy.Core.Entities.TrainingOffer
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Provider = provider,
            Title = title,
            FieldsCsv = "vaardigheden",
            KeysCsv = keys,
            ExternalPath = "/cursussen/" + title.ToLowerInvariant(),
            IsActive = true,
            SortOrder = 1,
            IsFree = isFree,
            IsPartner = isPartner,
            ShowInPassport = true,
            AffiliateCode = isPartner ? "CODE" : null
        };
    }
}
