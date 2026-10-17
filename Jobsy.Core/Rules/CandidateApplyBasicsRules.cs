using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
namespace Jobsy.Core.Rules;

/// <summary>
/// Minimum candidate profile required to apply: Even kennismaken basics + enough data for Lobsy-CV.
/// Tests, paspoort PDF, about-me and motivation are never part of this gate.
/// </summary>
public enum ApplyBasicsField
{
    Name,
    BirthDate,
    Location,
    Hours,
    Availability,
    AvailabilityStart,
    WorkStatus,
    OpenForWork
}

public sealed record ApplyBasicsGap(ApplyBasicsField Field, string MessageKey);

public static class CandidateApplyBasicsRules
{
    public static IReadOnlyList<ApplyBasicsGap> Evaluate(
        User user,
        CandidatePreferencesDto prefs)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(prefs);

        var gaps = new List<ApplyBasicsGap>(8);
        if (string.IsNullOrWhiteSpace(CandidateNameRules.ComposeFullName(user.FirstName, user.LastName, user.FullName)))
        {
            gaps.Add(new(ApplyBasicsField.Name, "Apply.Gap.Name"));
        }

        if (user.DateOfBirth is null)
        {
            gaps.Add(new(ApplyBasicsField.BirthDate, "Apply.Gap.BirthDate"));
        }

        if (!HasLocation(user, prefs))
        {
            gaps.Add(new(ApplyBasicsField.Location, "Apply.Gap.Location"));
        }

        if (prefs.MinHoursPerWeek is not > 0
            || prefs.MaxHoursPerWeek is null
            || prefs.MaxHoursPerWeek < prefs.MinHoursPerWeek)
        {
            gaps.Add(new(ApplyBasicsField.Hours, "Apply.Gap.Hours"));
        }

        if (CountAvailabilitySlots(prefs) == 0)
        {
            gaps.Add(new(ApplyBasicsField.Availability, "Apply.Gap.Availability"));
        }

        var immediate = user.AvailableFromDate is null;
        if (!AvailabilityStartRules.StepIsValid(immediate, user.AvailableFromDate))
        {
            gaps.Add(new(ApplyBasicsField.AvailabilityStart, "Apply.Gap.AvailabilityStart"));
        }

        if (prefs.WorkStatuses is not { Count: > 0 })
        {
            gaps.Add(new(ApplyBasicsField.WorkStatus, "Apply.Gap.WorkStatus"));
        }

        if (!user.OpenForWork)
        {
            gaps.Add(new(ApplyBasicsField.OpenForWork, "Apply.Gap.OpenForWork"));
        }

        return gaps;
    }

    public static bool IsReady(User user, CandidatePreferencesDto prefs)
        => Evaluate(user, prefs).Count == 0;

    /// <summary>Web/API profile DTO variant (same rules as <see cref="Evaluate"/>).</summary>
    public static bool IsReady(
        Guid userId,
        string? firstName,
        string? lastName,
        string? fullName,
        DateOnly? dateOfBirth,
        bool openForWork,
        DateOnly? availableFromDate,
        double? homeLatitude,
        double? homeLongitude,
        CandidatePreferencesDto prefs)
    {
        var user = new User
        {
            Id = userId,
            FirstName = firstName,
            LastName = lastName,
            FullName = fullName ?? "",
            DateOfBirth = dateOfBirth,
            OpenForWork = openForWork,
            AvailableFromDate = availableFromDate,
            HomeLocation = homeLatitude is double lat && homeLongitude is double lng
                ? new ValueObjects.GeoPoint(lat, lng)
                : null,
            Role = Enums.UserRole.Candidate
        };
        return IsReady(user, prefs);
    }

    public static string? FirstBlockingMessage(IReadOnlyList<ApplyBasicsGap> gaps)
        => gaps.Count == 0 ? null : gaps[0].MessageKey;

    public static bool HasLocation(User user, CandidatePreferencesDto prefs)
    {
        var city = LobsyCvModelFactory.ExtractCity(prefs.HomeAddress);
        var postcode = ExtractPostcode(prefs.HomeAddress);
        var resolved = user.HomeLocation is not null
                       || !string.IsNullOrWhiteSpace(postcode);
        return OnboardingWizardValidation.HasResolvableLocation(
            city,
            user.HomeLocation?.Latitude,
            user.HomeLocation?.Longitude,
            postcode ?? "",
            resolved);
    }

    public static int CountAvailabilitySlots(CandidatePreferencesDto prefs)
    {
        if (prefs.Availability is not { Count: > 0 } map)
        {
            return 0;
        }

        var count = 0;
        foreach (var (_, slots) in map)
        {
            count += slots?.Length ?? 0;
        }

        return count;
    }

    private static string? ExtractPostcode(string? homeAddress)
    {
        if (string.IsNullOrWhiteSpace(homeAddress))
        {
            return null;
        }

        var parts = homeAddress.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var raw = part.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
            if (raw.Length == 6 && char.IsDigit(raw[0]))
            {
                return raw[..4] + " " + raw[4..];
            }
        }

        return null;
    }
}
