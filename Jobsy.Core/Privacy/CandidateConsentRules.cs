using Jobsy.Core.Entities;

namespace Jobsy.Core.Privacy;

/// <summary>Central rules for optional candidate profiling and minors.</summary>
public static class CandidateConsentRules
{
    public const int MinimumCandidateAge = 13;
    public const int ParentalConsentAge = 16;
    public const int TalentPoolMinimumAge = 18;

    public const string TestConsentRequiredMessage =
        "Geef eerst toestemming voor de tests en AI-analyse in je profiel.";
    public const string ParentalConsentRequiredMessage =
        "Vraag eerst toestemming aan je ouder of voogd. Daarna kun je deze functie gebruiken.";
    public const string TalentPoolAdultOnlyMessage =
        "De talentpool is alleen beschikbaar vanaf 18 jaar.";

    public static bool RequiresParentalConsent(User user, DateOnly? today = null)
        => AgeYears(user.DateOfBirth, today) is int age && age < ParentalConsentAge;

    public static bool CanUseCandidateFeatures(User user, DateOnly? today = null)
        => !RequiresParentalConsent(user, today) || user.ParentalConsentAt is not null;

    public static bool HasCurrentTestAiConsent(User user)
        => user.TestAiConsentAt is not null
           && string.Equals(user.TestAiConsentVersion, PrivacyConstants.CandidateProfilingConsentVersion, StringComparison.Ordinal);

    public static bool CanAppearInTalentPool(User user, DateOnly? today = null)
        => user.TalentPoolConsentAt is not null
           && string.Equals(user.TalentPoolConsentVersion, PrivacyConstants.CandidateProfilingConsentVersion, StringComparison.Ordinal)
           && AgeYears(user.DateOfBirth, today) is int age
           && age >= TalentPoolMinimumAge;

    public static int? AgeYears(DateOnly? dateOfBirth, DateOnly? today = null)
        => Jobsy.Core.Rules.AgeRules.AgeYearsFromDateOfBirth(dateOfBirth, today);
}
