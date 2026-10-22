using System.Text.Json;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Components.Shared;
using Jobsy.Web.Localization;
namespace Jobsy.Web.Onboarding;

internal static class OnboardingClientStepInitialState
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Build(
        int step,
        OnboardingProfileDraft draft,
        CultureState culture,
        IReadOnlyList<(string Code, string TitleKey, string LeadKey)> presets,
        OnboardingClientStepSeed? seed = null)
    {
        seed ??= new OnboardingClientStepSeed();
        object payload = step switch
        {
            1 => BuildStep1(draft, culture),
            2 => BuildStep2(draft, culture, presets),
            3 => BuildStep3(draft, culture),
            4 => BuildStep4(draft, culture, seed),
            5 => BuildStep5(draft, culture, seed),
            6 => BuildStep6(draft, seed),
            7 when draft.HasConsent => BuildMiniStep(draft, seed),
            7 => BuildStep7(draft, seed),
            >= 8 and <= 10 => BuildMiniStep(draft, seed),
            _ => new { },
        };

        return JsonSerializer.Serialize(payload, Json);
    }

    private static object BuildStep1(OnboardingProfileDraft draft, CultureState culture)
    {
        var workStatuses = WorkStatusCatalog.AllCodes
            .Select(code => new
            {
                code,
                label = culture[WorkStatusCatalog.LabelKey(code)],
                icon = ChoiceTileIcons.ForWorkStatus(code) ?? "",
            })
            .ToList();

        return new
        {
            firstName = draft.FirstName,
            lastName = draft.LastName,
            phone = draft.Phone,
            whatsAppAllowed = draft.WhatsAppAllowed,
            workStatusesSelected = draft.WorkStatuses.ToList(),
            workStatusOptions = workStatuses,
            addressQuery = draft.AddressQuery,
            homeLat = draft.HomeLat,
            homeLng = draft.HomeLng,
            birthDateText = draft.BirthDate is { } d
                ? JobsyDateInputRules.FormatDisplayDate(d, culture.Language)
                : "",
            strings = new
            {
                addressPlaceholder = culture["Discovery.AddressPlaceholder"],
                suggestLoading = culture["Common.Loading"],
                suggestEmpty = culture["Discovery.AddressNoResults"],
                suggestFallback = string.Equals(culture.Language, "en", StringComparison.OrdinalIgnoreCase)
                    ? "No suggestions; keep typing your address"
                    : "Geen suggesties, typ je adres verder",
            },
        };
    }

    private static object BuildStep2(
        OnboardingProfileDraft draft,
        CultureState culture,
        IReadOnlyList<(string Code, string TitleKey, string LeadKey)> presets)
    {
        var presetTiles = presets
            .Select(p => new
            {
                code = p.Code,
                title = culture[p.TitleKey],
                lead = culture[p.LeadKey],
            })
            .ToList();

        var days = AvailabilityDayPartMatrixExport.Days;
        var slots = AvailabilityDayPartMatrixExport.Slots;

        return new
        {
            presets = draft.Presets.ToList(),
            presetsOverridden = draft.PresetsOverridden,
            fineTune = draft.FineTune,
            minHours = draft.MinHours ?? 12,
            maxHours = draft.MaxHours ?? 40,
            availability = draft.Availability.ToList(),
            immediate = draft.Immediate,
            availableFrom = draft.AvailableFrom?.ToString("yyyy-MM-dd"),
            presetTiles,
            days,
            slots,
            dayLabels = days.ToDictionary(d => d, d => UiLabels.Weekday(culture, d)),
            slotLabels = slots.ToDictionary(s => s, s => UiLabels.AvailabilitySlot(culture, s)),
        };
    }

    private static object BuildStep3(OnboardingProfileDraft draft, CultureState culture)
    {
        var transportOptions = TransportLabels.Selectable
            .Select(code => new { code, label = TransportLabel(culture, code) })
            .ToList();
        var travelChoices = new[] { 10, 20, 30, 45 };

        return new
        {
            transport = TransportLabels.SplitMany(draft.Transport).ToList(),
            maxTravel = draft.MaxTravel > 0 ? draft.MaxTravel : 30,
            customTravel = draft.CustomTravel,
            transportOptions,
            travelChoices,
        };
    }

    private static object BuildStep4(OnboardingProfileDraft draft, CultureState _, OnboardingClientStepSeed seed)
    {
        return new
        {
            noWork = draft.NoWork,
            jobs = seed.Jobs.Count > 0
                ? seed.Jobs
                : draft.Jobs.Select(j => new OnboardingClientStepSeed.JobSeed(
                    j.EmployerName ?? "",
                    j.StartMonth,
                    j.EndMonth,
                    j.IsCurrent == true)).ToList(),
        };
    }

    private static object BuildStep5(OnboardingProfileDraft draft, CultureState culture, OnboardingClientStepSeed _)
    {
        var levels = OnboardingWizardCatalog.EducationRadioLevels
            .Select(level => new
            {
                code = level,
                label = level == EducationLevelLabels.None ? culture["Education.None"] : level,
            })
            .ToList();

        return new
        {
            education = draft.Education,
            levels,
        };
    }

    private static object BuildStep6(OnboardingProfileDraft draft, OnboardingClientStepSeed seed)
    {
        return new
        {
            dream = draft.Dream,
            dreamSearch = draft.DreamSearch,
            dreamTitles = seed.DreamTitles,
        };
    }

    private static object BuildStep7(OnboardingProfileDraft draft, OnboardingClientStepSeed seed)
    {
        return new
        {
            consentRequested = draft.ConsentRequested || seed.ConsentRequested,
            hasConsent = draft.HasConsent || seed.HasConsent,
        };
    }

    private static object BuildMiniStep(OnboardingProfileDraft _, OnboardingClientStepSeed seed)
    {
        return new
        {
            questions = seed.MiniQuestions.Select(q => new { id = q.Id, text = q.Text }).ToList(),
            answers = seed.MiniAnswers,
        };
    }

    private static string TransportLabel(CultureState culture, string transport) => transport switch
    {
        TransportLabels.Bike => culture["Transport.Bike"],
        TransportLabels.EBike => culture["Transport.EBike"],
        TransportLabels.Car => culture["Transport.Car"],
        TransportLabels.PublicTransport => culture["Transport.Transit"],
        TransportLabels.Walking => culture["Transport.Walk"],
        _ => transport,
    };
}

/// <summary>Day/slot codes for client matrix (mirrors AvailabilityDayPartMatrix).</summary>
internal static class AvailabilityDayPartMatrixExport
{
    public static readonly string[] Days = DayPartMatrix.DayCodes;
    public static readonly string[] Slots = DayPartMatrix.DayPartCodes;
}
