using Jobsy.Api.Security;

namespace Jobsy.Api.Vacancies;

public static class VacancyListPaging
{
    public static (int Take, int MaxTake) ResolveDiscoverTake(int? requestedTake, bool isAuthenticated)
    {
        var max = isAuthenticated
            ? PublicApiLimits.MaxDiscoverTakeAuthenticated
            : PublicApiLimits.MaxDiscoverTakePublic;
        var take = requestedTake ?? PublicApiLimits.DefaultListTake;
        take = Math.Clamp(take, 1, max);
        return (take, max);
    }

    public static (int Take, int MaxTake) ResolveCatalogTake(int? requestedTake, bool isAuthenticated)
    {
        var max = isAuthenticated
            ? PublicApiLimits.MaxListTakeAuthenticated
            : PublicApiLimits.MaxListTakePublic;
        var take = requestedTake ?? PublicApiLimits.DefaultListTake;
        take = Math.Clamp(take, 1, max);
        return (take, max);
    }

    public static int ClampSkip(int skip) => Math.Max(0, skip);
}
