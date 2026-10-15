using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class OnboardingWizardValidationTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);

    [Fact]
    public void Personal_step_requires_name_birth_and_location()
    {
        var issues = OnboardingWizardValidation.ValidatePersonal(
            firstName: "",
            lastName: "Jansen",
            birthDate: Today.AddYears(-20),
            city: null,
            homeLat: null,
            homeLng: null,
            postcode: "",
            postcodeResolved: false,
            today: Today);

        Assert.Contains(issues, i => i.Field == OnboardingValidationField.FirstName);
        Assert.Contains(issues, i => i.Field == OnboardingValidationField.Location);
    }

    [Fact]
    public void Personal_step_accepts_coordinates_without_city()
    {
        var issues = OnboardingWizardValidation.ValidatePersonal(
            firstName: "Sam",
            lastName: "Demo",
            birthDate: Today.AddYears(-22),
            city: null,
            homeLat: 52.0,
            homeLng: 4.3,
            postcode: "",
            postcodeResolved: false,
            today: Today);

        Assert.Empty(issues);
    }

    [Fact]
    public void Availability_custom_requires_at_least_one_cell()
    {
        var issues = OnboardingWizardValidation.ValidateAvailability(
            minHours: 8,
            maxHours: 24,
            availabilityCount: 0,
            immediate: true,
            availableFrom: null,
            requirePresetSelection: true);

        Assert.Contains(issues, i => i.Field == OnboardingValidationField.AvailabilityPreset);
    }

    [Fact]
    public void Transport_step_requires_mode()
    {
        var issues = OnboardingWizardValidation.ValidateTransport("", 30);
        Assert.Contains(issues, i => i.Field == OnboardingValidationField.Transport);
    }
}
