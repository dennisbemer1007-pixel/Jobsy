using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Http.Extensions;

namespace Jobsy.Web.Profile;

internal static class ProfileSsrLegacyRedirects
{
    internal static string? TryGetRedirectUrl(HttpRequest request, bool passportEnabled)
    {
        var tab = request.Query["tab"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tab))
        {
            return null;
        }

        if (passportEnabled)
        {
            return PassportRedirects.TryToPassportUrl(request.GetDisplayUrl());
        }

        var normalized = CandidateKompasTabs.Normalize(tab);
        return normalized switch
        {
            CandidateKompasTabs.Dna => "/home?tab=dna",
            CandidateKompasTabs.Tests => "/home?tab=tests",
            CandidateKompasTabs.Fit => "/home?tab=fit",
            CandidateKompasTabs.Profile => "/candidate/profile/bewerken?section=personal",
            _ => "/candidate/profile/bewerken"
        };
    }
}
