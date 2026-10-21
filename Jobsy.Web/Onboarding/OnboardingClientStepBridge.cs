using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Onboarding;

/// <summary>Apply browser-collected onboarding step payloads onto the server draft (Volgende only).</summary>
public static class OnboardingClientStepBridge
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Apply(int step, OnboardingProfileDraft draft, string json)
    {
        if (step == 1)
        {
            ApplyStep1(draft, JsonSerializer.Deserialize<Step1Payload>(json, Json) ?? new Step1Payload());
            return;
        }

        if (step == 2)
        {
            ApplyStep2(draft, JsonSerializer.Deserialize<Step2Payload>(json, Json) ?? new Step2Payload());
            return;
        }

        if (step == 3)
        {
            ApplyStep3(draft, JsonSerializer.Deserialize<Step3Payload>(json, Json) ?? new Step3Payload());
            return;
        }

        if (step == 4)
        {
            ApplyStep4(draft, JsonSerializer.Deserialize<Step4Payload>(json, Json) ?? new Step4Payload());
            return;
        }

        if (step == 5)
        {
            ApplyStep5(draft, JsonSerializer.Deserialize<Step5Payload>(json, Json) ?? new Step5Payload());
            return;
        }

        if (step == 6)
        {
            ApplyStep6(draft, JsonSerializer.Deserialize<Step6Payload>(json, Json) ?? new Step6Payload());
        }
    }

    public static bool ParseStep7Consent(string json)
        => (JsonSerializer.Deserialize<Step7Payload>(json, Json) ?? new Step7Payload()).ConsentRequested;

    public static Dictionary<int, int> ParseMiniAnswers(string json)
    {
        var payload = JsonSerializer.Deserialize<MiniPayload>(json, Json) ?? new MiniPayload();
        var map = new Dictionary<int, int>();
        if (payload.Answers is null)
        {
            return map;
        }

        foreach (var (key, value) in payload.Answers)
        {
            if (value is >= 1 and <= 5)
            {
                map[key] = value;
            }
        }

        return map;
    }

    private static void ApplyStep1(OnboardingProfileDraft draft, Step1Payload p)
    {
        draft.FirstName = (p.FirstName ?? string.Empty).Trim();
        draft.LastName = (p.LastName ?? string.Empty).Trim();
        draft.Phone = (p.Phone ?? string.Empty).Trim();
        draft.WhatsAppAllowed = p.WhatsAppAllowed;

        draft.WorkStatuses.Clear();
        foreach (var code in p.WorkStatuses ?? [])
        {
            if (WorkStatusCatalog.Canonical(code) is { } canonical)
            {
                draft.WorkStatuses.Add(canonical);
            }
        }

        draft.AddressQuery = (p.AddressQuery ?? string.Empty).Trim();
        if (p.HomeLat is double lat && p.HomeLng is double lng)
        {
            draft.HomeLat = lat;
            draft.HomeLng = lng;
        }
        else
        {
            draft.HomeLat = null;
            draft.HomeLng = null;
        }

        draft.Postcode = string.Empty;
        draft.City = string.Empty;
        draft.PostcodeResolved = false;
        draft.GeolocationPendingConfirm = false;

        if (TryParseBirthDate(p.BirthDateText, out var dob))
        {
            draft.BirthDate = dob;
        }
        else
        {
            draft.BirthDate = null;
        }
    }

    private static void ApplyStep2(OnboardingProfileDraft draft, Step2Payload p)
    {
        draft.Presets.Clear();
        foreach (var code in p.Presets ?? [])
        {
            if (AvailabilityPresetRules.IsKnown(code))
            {
                draft.Presets.Add(code.Trim());
            }
        }

        draft.PresetsOverridden = p.PresetsOverridden;
        draft.FineTune = p.FineTune;
        draft.MinHours = p.MinHours;
        draft.MaxHours = p.MaxHours;

        draft.Availability.Clear();
        foreach (var key in p.Availability ?? [])
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                draft.Availability.Add(key);
            }
        }

        draft.Immediate = p.Immediate;
        draft.AvailableFrom = TryParseIsoDate(p.AvailableFrom) ?? draft.AvailableFrom;
        if (p.Immediate)
        {
            draft.AvailableFrom = null;
        }
    }

    private static bool TryParseBirthDate(string? text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return JobsyDateInputRules.TryParseFlexibleDate(text.Trim(), out date);
    }

    private static DateOnly? TryParseIsoDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return null;
        }

        return DateOnly.TryParse(iso, out var d) ? d : null;
    }

    internal sealed class Step1Payload
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public bool WhatsAppAllowed { get; set; }
        public List<string>? WorkStatuses { get; set; }
        public string? AddressQuery { get; set; }
        public double? HomeLat { get; set; }
        public double? HomeLng { get; set; }
        public string? BirthDateText { get; set; }
    }

    internal sealed class Step2Payload
    {
        public List<string>? Presets { get; set; }
        public bool PresetsOverridden { get; set; }
        public bool FineTune { get; set; }
        public decimal? MinHours { get; set; }
        public decimal? MaxHours { get; set; }
        public List<string>? Availability { get; set; }
        public bool Immediate { get; set; } = true;
        public string? AvailableFrom { get; set; }
    }

    private static void ApplyStep3(OnboardingProfileDraft draft, Step3Payload p)
    {
        draft.Transport = TransportLabels.JoinMany(p.Transport ?? []);
        draft.MaxTravel = p.MaxTravel > 0 ? p.MaxTravel : draft.MaxTravel;
        draft.CustomTravel = p.CustomTravel;
        if (!p.CustomTravel && p.SelectedTravel is int minutes)
        {
            draft.MaxTravel = minutes;
        }
    }

    private static void ApplyStep4(OnboardingProfileDraft draft, Step4Payload p)
    {
        draft.NoWork = p.NoWork;
        if (p.NoWork)
        {
            draft.Jobs.Clear();
            return;
        }

        draft.Jobs.Clear();
        foreach (var job in p.Jobs ?? [])
        {
            draft.Jobs.Add(new CandidateEmployerHistory
            {
                EmployerName = job.Employer ?? "",
                StartMonth = job.StartMonth,
                EndMonth = job.EndMonth,
                IsCurrent = job.IsCurrent,
            });
        }
    }

    private static void ApplyStep5(OnboardingProfileDraft draft, Step5Payload p)
    {
        draft.Education = (p.Education ?? string.Empty).Trim();
    }

    private static void ApplyStep6(OnboardingProfileDraft draft, Step6Payload p)
    {
        var dream = (p.Dream ?? p.DreamSearch ?? string.Empty).Trim();
        draft.Dream = dream;
        draft.DreamSearch = (p.DreamSearch ?? dream).Trim();
    }

    internal sealed class Step3Payload
    {
        public List<string>? Transport { get; set; }
        public int MaxTravel { get; set; }
        public int? SelectedTravel { get; set; }
        public bool CustomTravel { get; set; }
    }

    internal sealed class Step4Payload
    {
        public bool NoWork { get; set; }
        public List<JobPayload>? Jobs { get; set; }

        public sealed class JobPayload
        {
            public string? Employer { get; set; }
            public string? StartMonth { get; set; }
            public string? EndMonth { get; set; }
            public bool IsCurrent { get; set; }
        }
    }

    internal sealed class Step5Payload
    {
        public string? Education { get; set; }
    }

    internal sealed class Step6Payload
    {
        public string? Dream { get; set; }
        public string? DreamSearch { get; set; }
    }

    internal sealed class Step7Payload
    {
        public bool ConsentRequested { get; set; }
    }

    internal sealed class MiniPayload
    {
        public Dictionary<int, int>? Answers { get; set; }
    }
}
