using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>Canonical candidate links to a published vacancy (banenkaart deep link).</summary>
public static class CandidateVacancyUrls
{
    public static string BanenkaartDetail(Guid vacancyId)
        => QueryHelpers.AddQueryString(KbRoutes.Map, "vacancyId", vacancyId.ToString("D"));
}
