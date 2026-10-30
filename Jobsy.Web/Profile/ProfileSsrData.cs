using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Profile;

internal static class ProfileSsrData
{
    internal sealed record SectionStatus(bool Complete, string? MissingHintCultureKey);

    internal sealed record PageData(
        bool LoadError,
        MeProfile? Profile,
        SectionStatus Personal,
        SectionStatus Preferences,
        SectionStatus Availability,
        SectionStatus Experience);

    internal static async Task<PageData> LoadAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await api.GetMyProfileAsync(cancellationToken);
            if (profile is null)
            {
                return new PageData(true, null, Empty(), Empty(), Empty(), Empty());
            }

            return new PageData(
                false,
                profile,
                PersonalStatus(profile),
                PreferencesStatus(profile),
                AvailabilityStatus(profile),
                ExperienceStatus(profile));
        }
        catch
        {
            return new PageData(true, null, Empty(), Empty(), Empty(), Empty());
        }
    }

    private static SectionStatus Empty() => new(false, "Profile.Section.Incomplete");

    private static SectionStatus PersonalStatus(MeProfile profile)
    {
        var ok = !string.IsNullOrWhiteSpace(profile.FirstName)
                 && !string.IsNullOrWhiteSpace(profile.LastName)
                 && profile.HasDateOfBirth
                 && !string.IsNullOrWhiteSpace(profile.PhoneNumber)
                 && !string.IsNullOrWhiteSpace(profile.Preferences.HomeAddress);
        return new SectionStatus(ok, ok ? null : "Profile.Section.Incomplete");
    }

    private static SectionStatus PreferencesStatus(MeProfile profile)
    {
        var prefs = profile.Preferences;
        var ok = prefs.Roles.Count > 0
                 && prefs.MaxTravelMinutes is > 0
                 && !string.IsNullOrWhiteSpace(prefs.PreferredTransport);
        return new SectionStatus(ok, ok ? null : "Profile.Section.Incomplete");
    }

    private static SectionStatus AvailabilityStatus(MeProfile profile)
    {
        var ok = profile.OpenForWork
                 || profile.AvailableFromDate is not null
                 || profile.Preferences.Availability.Count > 0;
        return new SectionStatus(ok, ok ? null : "Profile.Section.Incomplete");
    }

    private static SectionStatus ExperienceStatus(MeProfile profile)
    {
        var prefs = profile.Preferences;
        var ok = profile.UploadedCv is not null
                 || prefs.Employers.Count > 0
                 || prefs.NoWorkExperience == true;
        return new SectionStatus(ok, ok ? null : "Profile.Section.Incomplete");
    }
}
