using Jobsy.Core.Entities;
using Jobsy.Core.Security;

namespace Jobsy.Infrastructure.Data;

public static class UserEmailQueryExtensions
{
    public static IQueryable<User> WhereEmailEquals(this IQueryable<User> users, string email)
    {
        var hash = EmailLookupHash.Compute(email);
        return users.Where(u => u.EmailLookupHash == hash);
    }

    public static IQueryable<LocalAuthCredential> WhereCredentialEmailEquals(
        this IQueryable<LocalAuthCredential> credentials,
        string email)
    {
        var hash = EmailLookupHash.Compute(email);
        return credentials.Where(c => c.EmailLookupHash == hash);
    }

    public static IQueryable<Application> WhereCandidateEmailEquals(
        this IQueryable<Application> apps,
        string email)
    {
        var hash = EmailLookupHash.Compute(email);
        return apps.Where(a => a.CandidateEmailLookupHash == hash);
    }
}
