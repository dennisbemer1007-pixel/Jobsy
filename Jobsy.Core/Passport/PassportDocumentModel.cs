using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

/// <summary>
/// Whitelist of facts that may reach the DNA-paspoort PDF and partner view.
/// A reflection test fails when a property is added without updating <see cref="Whitelist"/>.
/// </summary>
public sealed record PassportDocumentModel(
    string FullName,
    string Initials,
    string? WorkRegion,
    string PassportMemberNumber,
    bool OpenForWork,
    string? AvailableFrom,
    decimal? MinHoursPerWeek,
    decimal? MaxHoursPerWeek,
    PassportShifts Shifts,
    IReadOnlyList<string> Licences,
    string? PreferredTransport,
    int? MaxTravelMinutes,
    bool? HasOwnCar,
    IReadOnlyList<string> ContractPreferences,
    IReadOnlyList<string> Roles,
    IReadOnlyList<PassportLanguageLine> Languages,
    string? DutchLevel,
    SharedWorkPreferences? WorkPreferences,
    IReadOnlyList<string> EmployerPreferences,
    IReadOnlyList<PassportSectorChoice> Sectors,
    IReadOnlyList<PassportExperienceLine> Experience,
    IReadOnlyList<PassportCertificateLine> Certificates,
    IReadOnlyList<string> Education,
    string? AboutMe,
    IReadOnlyList<PassportTestStatus> Tests,
    PassportVerification Verification,
    PassportContact? Contact,
    PassportCoBrand? CoBrand,
    string? PublicId,
    string? QrUrl,
    DateTime GeneratedAtUtc,
    string PrimaryLanguage,
    string? SecondaryLanguage,
    bool IncludesPage2,
    bool ShareReady)
{
    /// <summary>Every public property path the partner document may carry. Update this when adding a property.</summary>
    public static readonly string[] Whitelist =
    [
        "PassportDocumentModel.FullName",
        "PassportDocumentModel.Initials",
        "PassportDocumentModel.WorkRegion",
        "PassportDocumentModel.PassportMemberNumber",
        "PassportDocumentModel.OpenForWork",
        "PassportDocumentModel.AvailableFrom",
        "PassportDocumentModel.MinHoursPerWeek",
        "PassportDocumentModel.MaxHoursPerWeek",
        "PassportDocumentModel.Shifts",
        "PassportShifts.Morning",
        "PassportShifts.Afternoon",
        "PassportShifts.Evening",
        "PassportShifts.Night",
        "PassportDocumentModel.Licences",
        "PassportDocumentModel.PreferredTransport",
        "PassportDocumentModel.MaxTravelMinutes",
        "PassportDocumentModel.HasOwnCar",
        "PassportDocumentModel.ContractPreferences",
        "PassportDocumentModel.Roles",
        "PassportDocumentModel.Languages",
        "PassportLanguageLine.Code",
        "PassportLanguageLine.Level",
        "PassportDocumentModel.DutchLevel",
        "PassportDocumentModel.WorkPreferences",
        "SharedWorkPreferences.Indoor",
        "SharedWorkPreferences.Outdoor",
        "SharedWorkPreferences.PhysicalWork",
        "SharedWorkPreferences.Pace",
        "PassportDocumentModel.EmployerPreferences",
        "PassportDocumentModel.Sectors",
        "PassportSectorChoice.Code",
        "PassportSectorChoice.ReasonCodes",
        "PassportSectorChoice.OwnReason",
        "PassportSectorChoice.ExampleRoles",
        "PassportDocumentModel.Experience",
        "PassportExperienceLine.Role",
        "PassportExperienceLine.Employer",
        "PassportExperienceLine.Period",
        "PassportDocumentModel.Certificates",
        "PassportCertificateLine.Name",
        "PassportCertificateLine.Year",
        "PassportDocumentModel.Education",
        "PassportDocumentModel.AboutMe",
        "PassportDocumentModel.Tests",
        "PassportTestStatus.Name",
        "PassportTestStatus.Done",
        "PassportTestStatus.CompletedAtUtc",
        "PassportDocumentModel.Verification",
        "PassportVerification.IsVerified",
        "PassportVerification.TestDates",
        "PassportVerification.EmailVerified",
        "PassportVerification.PhoneVerified",
        "PassportVerification.PhoneRequired",
        "PassportVerification.LastTestAtUtc",
        "PassportDocumentModel.Contact",
        "PassportContact.Email",
        "PassportContact.Phone",
        "PassportContact.WhatsAppOk",
        "PassportDocumentModel.CoBrand",
        "PassportCoBrand.DisplayName",
        "PassportCoBrand.Kind",
        "PassportCoBrand.ConsentAtUtc",
        "PassportCoBrand.LogoPng",
        "PassportDocumentModel.PublicId",
        "PassportDocumentModel.QrUrl",
        "PassportDocumentModel.GeneratedAtUtc",
        "PassportDocumentModel.PrimaryLanguage",
        "PassportDocumentModel.SecondaryLanguage",
        "PassportDocumentModel.IncludesPage2",
        "PassportDocumentModel.ShareReady"
    ];
}

public sealed record PassportShifts(string Morning, string Afternoon, string Evening, string Night);

public sealed record PassportLanguageLine(string Code, string? Level);

public sealed record PassportExperienceLine(string? Role, string Employer, string? Period);

public sealed record PassportCertificateLine(string Name, int? Year);

/// <summary>DNA test as done or not done. Completion date only — no outcome.</summary>
public sealed record PassportTestStatus(string Name, bool Done, DateTime? CompletedAtUtc);

public sealed record PassportContact(string? Email, string? Phone, bool WhatsAppOk);

public sealed record PassportCoBrand(string DisplayName, string Kind, DateTime? ConsentAtUtc, byte[]? LogoPng);

/// <summary>Already-loaded facts. Test-outcome columns are not on this type.</summary>
public sealed record PassportBuildInput(
    Guid UserId,
    string FullName,
    bool OpenForWork,
    DateOnly? AvailableFromDate,
    CandidatePreferencesDto Preferences,
    string? FilledFieldsJson,
    string? ConfirmedFieldsJson,
    IReadOnlyList<PassportTestStatus> Tests,
    DateTime? EmailVerifiedAtUtc,
    DateTime? PhoneVerifiedAtUtc,
    bool PhoneVerificationEnabled,
    string? Email,
    string? Phone,
    bool IncludeContact,
    bool WhatsAppOk,
    string PrimaryLanguage,
    string? SecondaryLanguage,
    bool IncludesPage2,
    string? PublicId,
    string? QrUrl,
    DateTime GeneratedAtUtc,
    PassportCoBrand? CoBrand);

public static class PassportDocumentFactory
{
    public static PassportDocumentModel Build(PassportBuildInput input)
    {
        var prefs = CandidatePreferencesValidator.Sanitize(input.Preferences);
        var filled = input.FilledFieldsJson;
        var confirmed = input.ConfirmedFieldsJson;
        bool Dropped(string key) => PassportCvConfirmation.IsDropped(filled, confirmed, key);

        var roles = Dropped("gewenste rollen")
            ? []
            : (prefs.Roles ?? []).Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).ToList();
        var licences = Dropped("rijbewijs")
            ? []
            : (prefs.DrivingLicenses ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
        var experience = Dropped("werkervaring")
            ? []
            : (prefs.Employers ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.EmployerName))
                .Select(item => new PassportExperienceLine(
                    string.IsNullOrWhiteSpace(item.Role) ? null : item.Role.Trim(),
                    item.EmployerName.Trim(),
                    Period(item.StartMonth, item.EndMonth)))
                .ToList();
        var education = Dropped("opleiding")
            ? []
            : (prefs.Educations ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
        var certificates = Dropped("certificaten")
            ? []
            : (prefs.Certificates ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .Select(item => new PassportCertificateLine(item.Name.Trim(), item.Year))
                .ToList();
        var about = Dropped("over mij") || string.IsNullOrWhiteSpace(prefs.AboutMe) ? null : prefs.AboutMe.Trim();
        var phone = Dropped("telefoon") || string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();

        var availability = prefs.Availability ?? new Dictionary<string, string[]>();
        var flexible = prefs.FlexibleTimes == true;
        var shifts = new PassportShifts(
            PassportShiftRules.Derive(DayPartMatrix.DayPartCodes[0], availability, flexible),
            PassportShiftRules.Derive(DayPartMatrix.DayPartCodes[1], availability, flexible),
            PassportShiftRules.Derive(DayPartMatrix.DayPartCodes[2], availability, flexible),
            PassportShiftRules.Derive(DayPartMatrix.DayPartCodes[3], availability, flexible));

        var tests = input.Tests ?? [];
        var completed = tests.Count(test => test.Done || test.CompletedAtUtc is not null);
        var verification = PassportVerificationRules.Evaluate(
            tests.Select(test => test.CompletedAtUtc).ToList(),
            input.EmailVerifiedAtUtc,
            input.PhoneVerifiedAtUtc,
            input.PhoneVerificationEnabled);

        PassportContact? contact = null;
        if (input.IncludeContact)
        {
            contact = new PassportContact(
                string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
                phone,
                input.WhatsAppOk && phone is not null);
        }

        return new PassportDocumentModel(
            FullName: string.IsNullOrWhiteSpace(input.FullName) ? "" : input.FullName.Trim(),
            Initials: Initials(input.FullName),
            WorkRegion: prefs.WorkRegion,
            PassportMemberNumber: PassportMemberNumber.Format(input.UserId),
            OpenForWork: input.OpenForWork,
            AvailableFrom: input.AvailableFromDate?.ToString("yyyy-MM-dd"),
            MinHoursPerWeek: prefs.MinHoursPerWeek,
            MaxHoursPerWeek: prefs.MaxHoursPerWeek,
            Shifts: shifts,
            Licences: licences,
            PreferredTransport: prefs.PreferredTransport,
            MaxTravelMinutes: prefs.MaxTravelMinutes,
            HasOwnCar: prefs.HasOwnCar,
            ContractPreferences: prefs.ContractPreferences ?? [],
            Roles: roles,
            Languages: (prefs.SpokenLanguages ?? []).Select(lang => new PassportLanguageLine(lang.Code, lang.Level)).ToList(),
            DutchLevel: prefs.DutchLevel,
            WorkPreferences: prefs.WorkPreferences,
            EmployerPreferences: prefs.ShareEmployerPreferences == true ? prefs.EmployerPreferences ?? [] : [],
            Sectors: prefs.PassportSectors ?? [],
            Experience: experience,
            Certificates: certificates,
            Education: education,
            AboutMe: about,
            Tests: tests,
            Verification: verification,
            Contact: contact,
            CoBrand: input.CoBrand,
            PublicId: input.PublicId,
            QrUrl: input.QrUrl,
            GeneratedAtUtc: input.GeneratedAtUtc,
            PrimaryLanguage: input.PrimaryLanguage,
            SecondaryLanguage: input.SecondaryLanguage,
            IncludesPage2: input.IncludesPage2,
            ShareReady: PassportCompletenessRules.IsShareReady(prefs, completed));
    }

    private static string? Period(string? start, string? end)
    {
        if (string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(end))
        {
            return start;
        }

        return string.IsNullOrWhiteSpace(start) ? end : $"{start} – {end}";
    }

    private static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "XX";
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var letters = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            var letter = part.FirstOrDefault(char.IsLetter);
            if (letter == default)
            {
                continue;
            }

            letters.Append(char.ToUpperInvariant(letter));
            if (letters.Length == 3)
            {
                break;
            }
        }

        return letters.Length == 0 ? "XX" : letters.ToString();
    }
}
