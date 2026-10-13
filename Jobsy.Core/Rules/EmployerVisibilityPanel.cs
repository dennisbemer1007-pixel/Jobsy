using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Dashboard "Wat ziet een kandidaat?" rows — derived only from <see cref="PublicVisibility"/>
/// so the panel can never disagree with public queries.
/// </summary>
public static class EmployerVisibilityPanel
{
    public const string CompanyPage = "company_page";
    public const string MapSearchMatch = "map_search_match";
    public const string SearchEngines = "search_engines";
    public const string Matches = "matches";

    public sealed record Row(string Key, bool Visible);

    public static IReadOnlyList<Row> ForCompany(Company? company)
    {
        var visible = PublicVisibility.IsCompanyPublic(company);
        return
        [
            new(CompanyPage, visible),
            new(MapSearchMatch, visible),
            new(Matches, visible),
            new(SearchEngines, visible)
        ];
    }

    public static IReadOnlyList<Row> ForStatus(CompanyVerificationStatus status)
        => ForCompany(new Company { VerificationStatus = status });
}
