using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Localization;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

/// <summary>
/// Raw facts for one passport. The builder turns these into display lines.
/// Date of birth, age, photo, address and scores are not fields here.
/// </summary>
public sealed record PassportPdfFacts(
    string Language,
    Guid? UserId,
    string FullName,
    bool OpenForWork,
    string? WorkRegion,
    DateOnly? AvailableFrom,
    decimal? MinHours,
    decimal? MaxHours,
    bool FlexibleTimes,
    IReadOnlyDictionary<string, string[]>? Availability,
    string? PreferredTransport,
    IReadOnlyList<string>? Licenses,
    int? MaxTravelMinutes,
    bool? HasOwnCar,
    bool IncludeContact,
    string? Email,
    string? Phone,
    bool WhatsApp,
    IReadOnlyList<CandidateLanguageDto>? SpokenLanguages,
    string? DutchLevel,
    SharedWorkPreferences? WorkPreferences,
    bool ShareEmployerPreferences,
    IReadOnlyList<string>? EmployerPreferences,
    IReadOnlyList<string>? Roles,
    IReadOnlyList<string>? ContractPreferences,
    IReadOnlyList<PassportExperienceFact>? Experience,
    int ExperienceCountWithoutNames,
    IReadOnlyList<PassportPaperFact>? Certificates,
    IReadOnlyList<string>? Educations,
    string? EducationDirection,
    string? OwnWords,
    string? Motivation,
    IReadOnlyList<PassportDnaLayerFact> Dna,
    bool EmailVerified,
    bool PhoneVerified,
    bool PhoneVerificationRequired,
    DateTime GeneratedAtUtc,
    string? DreamTitle = null,
    IReadOnlyList<PassportReferenceQuote>? ReferenceQuotes = null,
    IReadOnlyList<string>? LearningGoals = null);

public static class PassportPdfFactsFactory
{
    public static PassportPdfFacts FromUser(
        User user,
        CandidatePreferencesDto preferences,
        IReadOnlyList<PassportDnaLayerFact> dna,
        bool includeContact,
        bool phoneVerificationRequired,
        DateTime generatedAtUtc)
    {
        var employers = (preferences.Employers ?? [])
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
            .Select(e => new PassportExperienceFact(
                e.EmployerName.Trim(),
                Clean(e.Role),
                e.StartMonth,
                e.EndMonth,
                e.Years,
                Clean(e.Description)))
            .ToList();

        return new PassportPdfFacts(
            Language: JobsyLanguages.Normalize(preferences.Language),
            UserId: user.Id,
            FullName: user.FullName,
            OpenForWork: user.OpenForWork,
            WorkRegion: WorkRegionRules.Sanitize(preferences.WorkRegion),
            AvailableFrom: user.AvailableFromDate,
            MinHours: preferences.MinHoursPerWeek,
            MaxHours: preferences.MaxHoursPerWeek,
            FlexibleTimes: preferences.FlexibleTimes == true,
            Availability: preferences.Availability,
            PreferredTransport: Clean(preferences.PreferredTransport),
            Licenses: preferences.DrivingLicenses,
            MaxTravelMinutes: preferences.MaxTravelMinutes,
            HasOwnCar: preferences.HasOwnCar,
            IncludeContact: includeContact,
            Email: includeContact ? Clean(user.Email) : null,
            Phone: includeContact ? Clean(user.PhoneNumber) : null,
            WhatsApp: includeContact && user.WhatsAppContactAllowed,
            SpokenLanguages: preferences.SpokenLanguages,
            DutchLevel: Clean(preferences.DutchLevel),
            WorkPreferences: preferences.WorkPreferences,
            ShareEmployerPreferences: preferences.ShareEmployerPreferences == true,
            EmployerPreferences: preferences.EmployerPreferences,
            Roles: preferences.Roles,
            ContractPreferences: preferences.ContractPreferences,
            Experience: employers,
            ExperienceCountWithoutNames: 0,
            Certificates: Papers(preferences.Certificates),
            Educations: preferences.Educations,
            EducationDirection: Clean(preferences.EducationDirection),
            OwnWords: Clean(preferences.AboutMe),
            Motivation: Clean(preferences.DefaultMotivation),
            Dna: dna,
            EmailVerified: user.EmailVerifiedAtUtc is not null,
            PhoneVerified: user.PhoneVerifiedAtUtc is not null,
            PhoneVerificationRequired: phoneVerificationRequired,
            GeneratedAtUtc: generatedAtUtc,
            LearningGoals: Goals(preferences.LearningGoals));
    }

    public static PassportPdfFacts FromApplication(
        Application application,
        User? user,
        CandidatePreferencesDto? preferences,
        IReadOnlyList<PassportDnaLayerFact> dna,
        bool includeDirectContact,
        bool phoneVerificationRequired,
        DateTime generatedAtUtc)
    {
        if (user is not null && preferences is not null)
        {
            var live = FromUser(user, preferences, dna, includeDirectContact, phoneVerificationRequired, generatedAtUtc);
            var email = includeDirectContact ? First(application.CandidateEmail, user.Email) : null;
            var phone = includeDirectContact ? First(application.SnapshotPhoneNumber, user.PhoneNumber) : null;
            var whatsApp = includeDirectContact && application.SnapshotWhatsAppAllowed && !string.IsNullOrWhiteSpace(phone);
            var ownWords = First(preferences.AboutMe, application.SnapshotAboutMe);
            var motivation = Clean(application.Motivation);
            return live with
            {
                FullName = First(user.FullName, application.CandidateName) ?? application.CandidateName,
                Email = email,
                Phone = phone,
                WhatsApp = whatsApp,
                OwnWords = ownWords,
                Motivation = motivation
            };
        }

        var availability = LobsyCvModelFactory.ParseAvailabilityPayload(application.SnapshotAvailabilityJson);
        var certificates = LobsyCvModelFactory.ParseCertificatesJson(application.SnapshotCertificatesJson)
            .Select(c => new PassportPaperFact(c.Name, c.Year))
            .ToList();

        return new PassportPdfFacts(
            Language: JobsyLanguages.Default,
            UserId: application.CandidateUserId,
            FullName: application.CandidateName,
            OpenForWork: false,
            WorkRegion: null,
            AvailableFrom: null,
            MinHours: availability.MinHours,
            MaxHours: availability.MaxHours,
            FlexibleTimes: availability.FlexibleTimes,
            Availability: availability.Slots,
            PreferredTransport: Clean(application.PreferredTransport),
            Licenses: SplitCsv(application.SnapshotDrivingLicenses),
            MaxTravelMinutes: application.EstimatedTravelMinutes > 0 ? application.EstimatedTravelMinutes : null,
            HasOwnCar: null,
            IncludeContact: includeDirectContact,
            Email: includeDirectContact ? Clean(application.CandidateEmail) : null,
            Phone: includeDirectContact ? Clean(application.SnapshotPhoneNumber) : null,
            WhatsApp: includeDirectContact && application.SnapshotWhatsAppAllowed,
            SpokenLanguages: null,
            DutchLevel: null,
            WorkPreferences: null,
            ShareEmployerPreferences: false,
            EmployerPreferences: null,
            Roles: null,
            ContractPreferences: null,
            Experience: null,
            ExperienceCountWithoutNames: application.CandidateEmployerCount,
            Certificates: certificates,
            Educations: SplitCsv(application.SnapshotEducations),
            EducationDirection: null,
            OwnWords: Clean(application.SnapshotAboutMe),
            Motivation: Clean(application.Motivation),
            Dna: dna.Count > 0 ? dna : PassportDnaLayer.None(),
            EmailVerified: false,
            PhoneVerified: false,
            PhoneVerificationRequired: phoneVerificationRequired,
            GeneratedAtUtc: generatedAtUtc);
    }

    private static List<string> Goals(IReadOnlyList<string>? goals)
        => (goals ?? [])
            .Where(goal => !string.IsNullOrWhiteSpace(goal) && !goal.Contains('%'))
            .Select(goal => goal.Trim())
            .Take(5)
            .ToList();

    private static List<PassportPaperFact> Papers(IReadOnlyList<CandidateCertificateDto>? certificates)
        => (certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new PassportPaperFact(c.Name.Trim(), c.Year is >= 1950 and <= 2100 ? c.Year : null))
            .ToList();

    private static List<string> SplitCsv(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => part.Length > 0)
                .ToList();

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? First(string? preferred, string? fallback)
        => Clean(preferred) ?? Clean(fallback);
}
