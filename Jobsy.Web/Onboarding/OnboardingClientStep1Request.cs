namespace Jobsy.Web.Onboarding;

public sealed class OnboardingClientStep1Request
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
    public bool Advance { get; set; }
}
