using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Navigation;

public static class ProfileEncouragementContextFactory
{
    public static async Task<ProfileEncouragementContext?> CreateAsync(
        JobsyApiClient api,
        MeProfile? profile,
        CancellationToken ct = default)
    {
        if (profile is null || !string.Equals(profile.Role, "Candidate", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        WhoAmIState? whoAmI = null;
        try
        {
            whoAmI = await api.GetMyWhoAmIAsync(ct);
        }
        catch
        {
            // Optional for encouragement eligibility.
        }

        var valuesComplete = false;
        try
        {
            var values = await api.GetMyValuesAsync(ct);
            valuesComplete = string.Equals(values?.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Values optional for encouragement eligibility.
        }

        return From(profile, whoAmI, valuesComplete);
    }

    public static ProfileEncouragementContext? From(
        MeProfile? profile,
        WhoAmIState? whoAmI,
        bool valuesCompleted)
    {
        if (profile is null || !string.Equals(profile.Role, "Candidate", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var prefs = ToDto(profile.Preferences);
        var cultureOrValues = whoAmI?.CultureCompleted == true || valuesCompleted;
        var basicsReady = CandidateApplyBasicsRules.IsReady(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.FullName,
            profile.DateOfBirth,
            profile.OpenForWork,
            profile.AvailableFromDate,
            profile.HomeLatitude,
            profile.HomeLongitude,
            !profile.NeedsConsentReaccept,
            prefs);

        return new ProfileEncouragementContext
        {
            UserId = profile.Id.ToString("D"),
            CultureOrValuesComplete = cultureOrValues,
            ApplyBasicsReady = basicsReady
        };
    }

    private static CandidatePreferencesDto ToDto(CandidatePreferences prefs)
        => new(
            prefs.Roles ?? [],
            prefs.MaxTravelMinutes,
            prefs.PreferredTransport,
            Availability: prefs.Availability?.ToDictionary(
                k => k.Key,
                v => v.Value?.ToArray() ?? [],
                StringComparer.OrdinalIgnoreCase),
            HomeAddress: prefs.HomeAddress,
            MinHoursPerWeek: prefs.MinHoursPerWeek,
            MaxHoursPerWeek: prefs.MaxHoursPerWeek,
            WorkStatuses: prefs.WorkStatuses);
}
