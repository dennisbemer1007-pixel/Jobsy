using System.Text.Json;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;

namespace Jobsy.Web.Discovery;

internal static class DiscoveryClientStepInitialState
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Build(
        int step,
        OnboardingProfileDraft draft,
        CultureState culture,
        IReadOnlyList<(string Code, string TitleKey, string LeadKey)> presets,
        OnboardingClientStepSeed seed)
    {
        object payload = step switch
        {
            2 => BuildStep2(draft, culture, presets),
            3 => BuildStep3(draft, culture, seed),
            4 => BuildStep4(draft, culture, seed),
            5 => BuildStep5(draft, culture),
            6 => BuildStep6(draft, culture),
            _ => new { },
        };

        return JsonSerializer.Serialize(payload, Json);
    }

    private static object BuildStep2(
        OnboardingProfileDraft draft,
        CultureState culture,
        IReadOnlyList<(string Code, string TitleKey, string LeadKey)> presets)
    {
        var availabilityJson = OnboardingClientStepInitialState.Build(2, draft, culture, presets, null);
        var transportJson = OnboardingClientStepInitialState.Build(3, draft, culture, presets, null);
        return new
        {
            availability = JsonSerializer.Deserialize<object>(availabilityJson),
            transport = JsonSerializer.Deserialize<object>(transportJson),
            drivingLicenses = draft.DrivingLicenses.ToList(),
            licenseOptions = DrivingLicenseLabels.All
                .Select(code => new { code, label = UiLabels.DrivingLicense(culture, code) })
                .ToList(),
        };
    }

    private static object BuildStep3(OnboardingProfileDraft draft, CultureState culture, OnboardingClientStepSeed seed)
    {
        var workJson = OnboardingClientStepInitialState.Build(4, draft, culture, [], seed);
        return new
        {
            work = JsonSerializer.Deserialize<object>(workJson),
            employerPreferences = draft.EmployerPreferences.ToList(),
            employerOptions = DiscoveryCatalogs.EmployerPreferenceCodes
                .Select(code => new { code, label = culture[DiscoveryCatalogs.EmployerLabelKey(code)] })
                .ToList(),
            roles = draft.Roles.ToList(),
            roleOptions = WorkTypeLabels.All
                .Select(role => new { code = role, label = UiLabels.WorkType(culture, role) })
                .ToList(),
        };
    }

    private static object BuildStep4(OnboardingProfileDraft draft, CultureState culture, OnboardingClientStepSeed seed)
    {
        var educationJson = OnboardingClientStepInitialState.Build(5, draft, culture, [], seed);
        var dreamJson = OnboardingClientStepInitialState.Build(6, draft, culture, [], seed);
        return new
        {
            education = JsonSerializer.Deserialize<object>(educationJson),
            dream = JsonSerializer.Deserialize<object>(dreamJson),
            learningGoals = draft.LearningGoals.ToList(),
            certificates = draft.Certificates.Select(c => new { name = c.Name, year = c.Year }).ToList(),
            dutchLevel = draft.DutchLevel,
            spokenLanguages = draft.SpokenLanguages.Select(l => new { code = l.Code, level = l.Level }).ToList(),
        };
    }

    private static object BuildStep5(OnboardingProfileDraft draft, CultureState culture) => new
    {
        hobbies = draft.Hobbies.ToList(),
        hobbyOptions = DiscoveryCatalogs.HobbyCodes
            .Select(code => new { code, label = culture[DiscoveryCatalogs.HobbyLabelKey(code)] })
            .ToList(),
        hobbyFreeText = draft.HobbyFreeText.ToList(),
        aboutMe = draft.AboutMe ?? "",
    };

    private static object BuildStep6(OnboardingProfileDraft draft, CultureState culture) => new
    {
        dislikes = draft.Dislikes.ToList(),
        dislikeOptions = DiscoveryCatalogs.DislikeCodes
            .Select(code => new { code, label = culture[DiscoveryCatalogs.DislikeLabelKey(code)] })
            .ToList(),
        customDislikes = draft.CustomDislikes.ToList(),
    };
}
