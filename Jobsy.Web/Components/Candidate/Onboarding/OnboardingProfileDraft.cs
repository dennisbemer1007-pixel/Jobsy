using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Web.Components.Candidate.Onboarding;

/// <summary>
/// Shared mutable form state for the classic onboarding wizard and De ontdekkingsreis.
/// Answers live here; progress pointers stay on the onboarding row.
/// </summary>
public sealed class OnboardingProfileDraft
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool WhatsAppAllowed { get; set; }
    public string Postcode { get; set; } = "";
    public string City { get; set; } = "";
    public string BirthDay { get; set; } = "";
    public string BirthMonth { get; set; } = "";
    public string BirthYear { get; set; } = "";
    public DateOnly? BirthDate { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public bool Immediate { get; set; } = true;
    public bool FineTune { get; set; }
    public bool PresetsOverridden { get; set; }
    public bool NoWork { get; set; }
    public bool CustomTravel { get; set; }
    public bool ShowCity { get; set; }
    public bool PostcodeMissing { get; set; }
    public bool PostcodeResolved { get; set; }
    public bool HasConsent { get; set; }
    public bool ConsentRequested { get; set; }
    public int MaxTravel { get; set; } = 30;
    public decimal? MinHours { get; set; } = 8;
    public decimal? MaxHours { get; set; } = 24;
    public string Transport { get; set; } = "";
    public string Dream { get; set; } = "";
    public string DreamSearch { get; set; } = "";
    public string Education { get; set; } = "";
    public string EducationDirection { get; set; } = "";
    public double? HomeLat { get; set; }
    public double? HomeLng { get; set; }
    public string? AboutMe { get; set; }
    public string? DutchLevel { get; set; }

    public HashSet<string> Availability { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Presets { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DrivingLicenses { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> EmployerPreferences { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Hobbies { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Dislikes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CandidateEmployerHistory> Jobs { get; } = [];
    public List<CandidateCertificate> Certificates { get; } = [];
    public List<string> LearningGoals { get; } = [];
    public List<string> HobbyFreeText { get; } = [];
    public List<CandidateLanguage> SpokenLanguages { get; } = [];
    public List<string> CustomDislikes { get; } = [];
    public List<AddressSuggestion> Suggestions { get; } = [];

    public EventCallback Changed { get; set; }

    public Task NotifyChangedAsync()
        => Changed.HasDelegate ? Changed.InvokeAsync() : Task.CompletedTask;
}
