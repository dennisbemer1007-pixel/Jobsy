using Jobsy.Core.Enums;

namespace Jobsy.Core.Ops;

/// <summary>
/// Fixed test-account keys, e-mail slugs and role mapping. Passwords come only from config.
/// </summary>
public static class TestAccountCatalog
{
    public sealed record Entry(
        string AccountKey,
        string EmailSlug,
        UserRole Role,
        string DisplayName,
        bool RequiresRoleInBuild = true);

    public static IReadOnlyList<Entry> All { get; } =
    [
        new("Candidate", "kandidaat", UserRole.Candidate, "Test Kandidaat"),
        new("CandidateNew", "kandidaat-nieuw", UserRole.Candidate, "Test Kandidaat Nieuw"),
        new("BranchManager", "werkgever", UserRole.BranchManager, "Test Filiaalmanager"),
        new("EnterpriseManager", "bedrijfsmanager", UserRole.EnterpriseManager, "Test Bedrijfsmanager"),
        new("RegionalManager", "regiomanager", UserRole.RegionalManager, "Test Regiomanager"),
        new("Intermediary", "intermediair", UserRole.Intermediary, "Test Intermediair"),
        new("SalesManager", "salesmanager", UserRole.SalesManager, "Test Salesmanager"),
        new("Admin", "admin", UserRole.Admin, "Test Admin"),
        new("Ambassadeur", "ambassadeur", UserRole.Ambassadeur, "Test Ambassadeur"),
        new("Teacher", "leraar", UserRole.Teacher, "Test Leraar"),
        new("SchoolAdmin", "schoolbeheerder", UserRole.SchoolAdmin, "Test Schoolbeheerder"),
    ];

    public static string BuildEmail(string emailSlug, string emailDomain)
        => $"test-{emailSlug}@{emailDomain.Trim()}";

    public static bool RoleExistsInBuild(UserRole role)
        => Enum.IsDefined(typeof(UserRole), role);

    public static Entry? FindByKey(string accountKey)
        => All.FirstOrDefault(e =>
            string.Equals(e.AccountKey, accountKey, StringComparison.OrdinalIgnoreCase));
}
