namespace Jobsy.Core.Rules;

/// <summary>
/// Company row used when inviting or editing employer users.
/// KVK-registratie maakt vaak een organisatie én een vestiging met dezelfde handelsnaam.
/// </summary>
public readonly record struct InviteCompanyOption(
    Guid Id,
    string Name,
    string Address,
    Guid? ParentCompanyId,
    string? KvkEstablishmentId = null);

/// <summary>
/// Primary vestiging vs extra lidmaatschappen: no duplicate org/branch of the same company.
/// Extra lidmaatschappen are other vestigingen besides the primary — never the organisation wrapper.
/// </summary>
public static class EmployerInviteCompanyOptions
{
    public static IReadOnlyList<InviteCompanyOption> PrimaryChoices(
        IReadOnlyList<InviteCompanyOption> companies,
        string role)
    {
        if (companies.Count == 0)
        {
            return companies;
        }

        if (IsEnterpriseManager(role))
        {
            var orgs = companies.Where(c => c.ParentCompanyId is null).ToList();
            return orgs.Count > 0 ? orgs : companies;
        }

        if (IsBranchManager(role))
        {
            var branches = companies.Where(c => c.ParentCompanyId is not null).ToList();
            return branches.Count > 0 ? branches : companies;
        }

        return companies;
    }

    /// <summary>
    /// Extra vestigingen besides <paramref name="primaryCompanyId"/>.
    /// When the organisation has child vestigingen, the parent is never offered as extra.
    /// </summary>
    public static IReadOnlyList<InviteCompanyOption> ExtraMembershipChoices(
        IReadOnlyList<InviteCompanyOption> companies,
        Guid? primaryCompanyId)
    {
        if (companies.Count == 0)
        {
            return companies;
        }

        var hasHierarchy = companies.Any(c => c.ParentCompanyId is not null);
        IEnumerable<InviteCompanyOption> pool = hasHierarchy
            ? companies.Where(c => c.ParentCompanyId is not null)
            : companies;

        return pool.Where(c => c.Id != primaryCompanyId).ToList();
    }

    public static Guid? CoercePrimary(IReadOnlyList<InviteCompanyOption> choices, Guid? current)
    {
        if (current is Guid id && choices.Any(c => c.Id == id))
        {
            return current;
        }

        return choices.Count > 0 ? choices[0].Id : null;
    }

    public static string Label(InviteCompanyOption company, IReadOnlyList<InviteCompanyOption> all)
    {
        var isOrgWithChildren = company.ParentCompanyId is null
            && all.Any(c => c.ParentCompanyId == company.Id);
        if (isOrgWithChildren)
        {
            return $"{company.Name} (organisatie)";
        }

        var sameNameCount = all.Count(c =>
            string.Equals(c.Name, company.Name, StringComparison.OrdinalIgnoreCase));
        if (sameNameCount > 1)
        {
            if (!string.IsNullOrWhiteSpace(company.Address))
            {
                return $"{company.Name} — {company.Address}";
            }

            if (!string.IsNullOrWhiteSpace(company.KvkEstablishmentId))
            {
                return $"{company.Name} (vestiging {company.KvkEstablishmentId})";
            }

            if (company.ParentCompanyId is not null)
            {
                return $"{company.Name} (vestiging)";
            }
        }

        return company.Name;
    }

    private static bool IsEnterpriseManager(string role) =>
        string.Equals(role, "EnterpriseManager", StringComparison.Ordinal);

    private static bool IsBranchManager(string role) =>
        string.Equals(role, "BranchManager", StringComparison.Ordinal);
}
