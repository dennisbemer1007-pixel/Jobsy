using System.Text.Json;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;

namespace Jobsy.Web.Discovery;

public static class DiscoveryClientStepBridge
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Apply(int discoveryStep, OnboardingProfileDraft draft, string json)
    {
        switch (discoveryStep)
        {
            case 2:
                ApplyStep2(draft, JsonSerializer.Deserialize<Step2Payload>(json, Json) ?? new Step2Payload());
                break;
            case 3:
                ApplyStep3(draft, JsonSerializer.Deserialize<Step3Payload>(json, Json) ?? new Step3Payload());
                break;
            case 4:
                ApplyStep4(draft, JsonSerializer.Deserialize<Step4Payload>(json, Json) ?? new Step4Payload());
                break;
            case 5:
                ApplyStep5(draft, JsonSerializer.Deserialize<Step5Payload>(json, Json) ?? new Step5Payload());
                break;
            case 6:
                ApplyStep6(draft, JsonSerializer.Deserialize<Step6Payload>(json, Json) ?? new Step6Payload());
                break;
        }
    }

    private static void ApplyStep2(OnboardingProfileDraft draft, Step2Payload p)
    {
        if (p.Availability is { } availability)
        {
            OnboardingClientStepBridge.Apply(2, draft, availability.GetRawText());
        }

        if (p.Transport is { } transport)
        {
            OnboardingClientStepBridge.Apply(3, draft, transport.GetRawText());
        }

        if (p.DrivingLicenses is null)
        {
            return;
        }

        draft.DrivingLicenses.Clear();
        foreach (var code in p.DrivingLicenses)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                draft.DrivingLicenses.Add(code.Trim());
            }
        }
    }

    private static void ApplyStep3(OnboardingProfileDraft draft, Step3Payload p)
    {
        if (p.Work is { } work)
        {
            OnboardingClientStepBridge.Apply(4, draft, work.GetRawText());
        }

        draft.EmployerPreferences.Clear();
        foreach (var code in p.EmployerPreferences ?? [])
        {
            if (DiscoveryCatalogs.IsKnownEmployerPreference(code))
            {
                draft.EmployerPreferences.Add(DiscoveryCatalogs.CanonicalEmployer(code)!);
            }
        }

        draft.Roles.Clear();
        foreach (var role in p.Roles ?? [])
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                draft.Roles.Add(role.Trim());
            }
        }
    }

    private static void ApplyStep4(OnboardingProfileDraft draft, Step4Payload p)
    {
        if (!string.IsNullOrWhiteSpace(p.Education))
        {
            draft.Education = p.Education.Trim();
        }

        var dream = (p.Dream ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(dream))
        {
            draft.Dream = dream;
            draft.DreamSearch = dream;
        }

        draft.LearningGoals.Clear();
        foreach (var goal in p.LearningGoals ?? [])
        {
            var trimmed = (goal ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && draft.LearningGoals.Count < DiscoveryCatalogs.MaxLearningGoals)
            {
                draft.LearningGoals.Add(trimmed);
            }
        }

        draft.Certificates.Clear();
        foreach (var cert in p.Certificates ?? [])
        {
            if (string.IsNullOrWhiteSpace(cert.Name))
            {
                continue;
            }

            draft.Certificates.Add(new CandidateCertificate { Name = cert.Name.Trim(), Year = cert.Year });
        }

        if (!string.IsNullOrWhiteSpace(p.DutchLevel))
        {
            draft.DutchLevel = p.DutchLevel.Trim();
        }

        draft.SpokenLanguages.Clear();
        foreach (var lang in p.SpokenLanguages ?? [])
        {
            if (string.IsNullOrWhiteSpace(lang.Code))
            {
                continue;
            }

            draft.SpokenLanguages.Add(new CandidateLanguage
            {
                Code = lang.Code.Trim().ToLowerInvariant(),
                Level = string.IsNullOrWhiteSpace(lang.Level) ? null : lang.Level.Trim(),
            });
        }
    }

    private static void ApplyStep5(OnboardingProfileDraft draft, Step5Payload p)
    {
        draft.Hobbies.Clear();
        foreach (var code in p.Hobbies ?? [])
        {
            if (DiscoveryCatalogs.IsKnownHobby(code))
            {
                draft.Hobbies.Add(DiscoveryCatalogs.CanonicalHobby(code)!);
            }
        }

        draft.HobbyFreeText.Clear();
        foreach (var line in p.HobbyFreeText ?? [])
        {
            var trimmed = (line ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && draft.HobbyFreeText.Count < 3)
            {
                draft.HobbyFreeText.Add(trimmed);
            }
        }

        draft.AboutMe = (p.AboutMe ?? string.Empty).Trim();
    }

    private static void ApplyStep6(OnboardingProfileDraft draft, Step6Payload p)
    {
        draft.Dislikes.Clear();
        foreach (var code in p.Dislikes ?? [])
        {
            if (DiscoveryCatalogs.IsKnownDislike(code))
            {
                draft.Dislikes.Add(DiscoveryCatalogs.CanonicalDislike(code)!);
            }
        }

        draft.CustomDislikes.Clear();
        foreach (var line in p.CustomDislikes ?? [])
        {
            var trimmed = (line ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && draft.CustomDislikes.Count < DiscoveryCatalogs.MaxCustomDislikes)
            {
                draft.CustomDislikes.Add(trimmed);
            }
        }
    }

    internal sealed class Step2Payload
    {
        public JsonElement? Availability { get; set; }
        public JsonElement? Transport { get; set; }
        public List<string>? DrivingLicenses { get; set; }
    }

    internal sealed class Step3Payload
    {
        public JsonElement? Work { get; set; }
        public List<string>? EmployerPreferences { get; set; }
        public List<string>? Roles { get; set; }
    }

    internal sealed class Step4Payload
    {
        public string? Education { get; set; }
        public string? Dream { get; set; }
        public List<string>? LearningGoals { get; set; }
        public List<CertificatePayload>? Certificates { get; set; }
        public string? DutchLevel { get; set; }
        public List<LanguagePayload>? SpokenLanguages { get; set; }

        public sealed class CertificatePayload
        {
            public string? Name { get; set; }
            public int? Year { get; set; }
        }

        public sealed class LanguagePayload
        {
            public string? Code { get; set; }
            public string? Level { get; set; }
        }
    }

    internal sealed class Step5Payload
    {
        public List<string>? Hobbies { get; set; }
        public List<string>? HobbyFreeText { get; set; }
        public string? AboutMe { get; set; }
    }

    internal sealed class Step6Payload
    {
        public List<string>? Dislikes { get; set; }
        public List<string>? CustomDislikes { get; set; }
    }
}
