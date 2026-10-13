using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Ops;

namespace Jobsy.Infrastructure.Ops;

public static class TestDataQueryExtensions
{
    public static IQueryable<User> ExcludeTestUsers(this IQueryable<User> query)
        => query.Where(u => !u.IsTestAccount);

    public static IQueryable<Company> ExcludeTestCompanies(this IQueryable<Company> query)
        => query.Where(c => !c.IsTestData);

    public static IQueryable<Vacancy> ExcludeTestVacancies(this IQueryable<Vacancy> query)
        => query.Where(v => !v.IsTestData);

    public static IQueryable<Vacancy> VisibleTo(this IQueryable<Vacancy> query, ClaimsPrincipal? principal)
        => TestDataRules.CanSeeTestData(principal)
            ? query
            : query.Where(v => !v.IsTestData);

    public static IQueryable<Company> VisibleTo(this IQueryable<Company> query, ClaimsPrincipal? principal)
        => TestDataRules.CanSeeTestData(principal)
            ? query
            : query.Where(c => !c.IsTestData);

    public static IQueryable<User> VisibleTo(this IQueryable<User> query, ClaimsPrincipal? principal)
        => TestDataRules.CanSeeTestData(principal)
            ? query
            : query.Where(u => !u.IsTestAccount);

    public static IQueryable<School> ExcludeTestSchools(this IQueryable<School> query)
        => query.Where(s => !s.IsTestData);

    public static IQueryable<SchoolClass> ExcludeTestSchoolClasses(this IQueryable<SchoolClass> query)
        => query.Where(c => !c.IsTestData);
}
