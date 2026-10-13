using Jobsy.Core.Entities;

namespace Jobsy.Core.Sales;

/// <summary>
/// Organisation commercial unit: attribution, window and snapshots live on the root
/// (<c>ParentCompanyId ?? Id</c>). Vestigingen share one window (D1).
/// </summary>
public static class SalesCommercialUnit
{
    public const int MaxParentWalkDepth = 5;

    public static Guid RootIdOf(Company company)
    {
        ArgumentNullException.ThrowIfNull(company);
        return company.ParentCompanyId ?? company.Id;
    }

    public static Guid RootIdOf(Guid companyId, Guid? parentCompanyId)
        => parentCompanyId ?? companyId;

    /// <summary>
    /// Walk parent links up to <see cref="MaxParentWalkDepth"/> levels.
    /// Normal trees are one level (org → vestiging); deeper trees are rare.
    /// </summary>
    public static Guid ResolveRootId(Guid companyId, Func<Guid, Guid?> parentOf)
    {
        var current = companyId;
        for (var depth = 0; depth < MaxParentWalkDepth; depth++)
        {
            var parent = parentOf(current);
            if (parent is null || parent == current)
            {
                return current;
            }

            current = parent.Value;
        }

        return current;
    }
}
