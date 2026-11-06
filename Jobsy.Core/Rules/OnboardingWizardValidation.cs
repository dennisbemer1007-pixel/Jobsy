using Jobsy.Core.Privacy;

namespace Jobsy.Core.Rules;

public enum OnboardingValidationField
{
    FirstName,
    LastName,
    BirthDate,
    Location,
    AvailabilityPreset,
    AvailabilityGrid,
    AvailabilityStart,
    Transport,
    Dream,
    Consent
}

public sealed record OnboardingValidationIssue(OnboardingValidationField Field, string MessageKey);

/// <summary>Shared step validation for classic wizard and discovery journey (no UI strings).</summary>
public static class OnboardingWizardValidation
{
    private static readonly System.Text.RegularExpressions.Regex NlPostcode =
        new("^[1-9][0-9]{3} [A-Z]{2}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public static bool HasResolvableLocation(
        string? city,
        double? homeLat,
        double? homeLng,
        string postcode,
        bool postcodeResolved,
        bool geolocationPendingConfirm = false)
    {
        if (geolocationPendingConfirm)
        {
            return false;
        }

        if (homeLat is not null && homeLng is not null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            return true;
        }

        return postcodeResolved && IsNlPostcode(postcode);
    }

    public static IReadOnlyList<OnboardingValidationIssue> ValidatePersonal(
        string? firstName,
        string? lastName,
        DateOnly? birthDate,
        string? city,
        double? homeLat,
        double? homeLng,
        string postcode,
        bool postcodeResolved,
        DateOnly today,
        bool geolocationPendingConfirm = false)
    {
        var issues = new List<OnboardingValidationIssue>(4);
        if (string.IsNullOrWhiteSpace(firstName))
        {
            issues.Add(new(OnboardingValidationField.FirstName, "Onboarding.Validation.FirstName"));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            issues.Add(new(OnboardingValidationField.LastName, "Onboarding.Validation.LastName"));
        }

        if (birthDate is not DateOnly born)
        {
            issues.Add(new(OnboardingValidationField.BirthDate, "Onboarding.Validation.BirthDate"));
        }
        else if (CandidateConsentRules.IsBelowMinimumAge(born, today))
        {
            issues.Add(new(OnboardingValidationField.BirthDate, "Onboarding.Age.TooYoung"));
        }

        if (!HasResolvableLocation(city, homeLat, homeLng, postcode, postcodeResolved, geolocationPendingConfirm))
        {
            issues.Add(new(OnboardingValidationField.Location, "Onboarding.Validation.Location"));
        }

        return issues;
    }

    public static IReadOnlyList<OnboardingValidationIssue> ValidateAvailability(
        decimal? minHours,
        decimal? maxHours,
        int availabilityCount,
        bool immediate,
        DateOnly? availableFrom,
        bool requirePresetSelection)
    {
        var issues = new List<OnboardingValidationIssue>(4);
        if (requirePresetSelection && availabilityCount == 0)
        {
            issues.Add(new(OnboardingValidationField.AvailabilityPreset, "Onboarding.AvailabilityRequired"));
        }
        else if (availabilityCount == 0)
        {
            issues.Add(new(OnboardingValidationField.AvailabilityGrid, "Onboarding.Validation.AvailabilityGrid"));
        }

        if (minHours is not > 0 || maxHours is null || maxHours < minHours)
        {
            issues.Add(new(OnboardingValidationField.AvailabilityGrid, "Onboarding.Validation.HoursRange"));
        }

        if (!AvailabilityStartRules.StepIsValid(immediate, availableFrom))
        {
            issues.Add(new(OnboardingValidationField.AvailabilityStart, "Onboarding.AvailabilityStartDateInvalid"));
        }

        return issues;
    }

    public static IReadOnlyList<OnboardingValidationIssue> ValidateTransport(string? transport, int maxTravel)
    {
        var issues = new List<OnboardingValidationIssue>(2);
        if (string.IsNullOrWhiteSpace(transport))
        {
            issues.Add(new(OnboardingValidationField.Transport, "Onboarding.Validation.Transport"));
        }

        if (maxTravel <= 0)
        {
            issues.Add(new(OnboardingValidationField.Transport, "Onboarding.Validation.MaxTravel"));
        }

        return issues;
    }

    public static IReadOnlyList<OnboardingValidationIssue> ValidateDream(string? dream)
    {
        return string.IsNullOrWhiteSpace(dream)
            ? [new(OnboardingValidationField.Dream, "Onboarding.Validation.Dream")]
            : [];
    }

    public static IReadOnlyList<OnboardingValidationIssue> ValidateConsent(bool consentRequested)
    {
        return consentRequested
            ? []
            : [new(OnboardingValidationField.Consent, "Onboarding.Validation.Consent")];
    }

    public static string FieldAnchorId(OnboardingValidationField field)
        => field switch
        {
            OnboardingValidationField.FirstName => "onboarding-field-firstname",
            OnboardingValidationField.LastName => "onboarding-field-lastname",
            OnboardingValidationField.BirthDate => "onboarding-field-birthdate",
            OnboardingValidationField.Location => "onboarding-field-location",
            OnboardingValidationField.AvailabilityPreset => "onboarding-field-availability-presets",
            OnboardingValidationField.AvailabilityGrid => "onboarding-field-availability-grid",
            OnboardingValidationField.AvailabilityStart => "onboarding-field-availability-start",
            OnboardingValidationField.Transport => "onboarding-field-transport",
            OnboardingValidationField.Dream => "onboarding-field-dream",
            OnboardingValidationField.Consent => "onboarding-field-consent",
            _ => "onboarding-validation"
        };

    private static bool IsNlPostcode(string postcode)
    {
        var raw = (postcode ?? "").Replace(" ", "").ToUpperInvariant();
        if (raw.Length != 6)
        {
            return false;
        }

        var normalized = raw[..4] + " " + raw[4..];
        return NlPostcode.IsMatch(normalized);
    }
}
