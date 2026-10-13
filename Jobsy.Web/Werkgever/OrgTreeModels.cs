namespace Jobsy.Web.Werkgever;

public enum OrgTreeNodeKind
{
    Org,
    Region,
    Vestiging
}

public sealed record OrgTreeNode(
    string Key,
    OrgTreeNodeKind Kind,
    Guid Id,
    string Name,
    int? ChildCount = null,
    int LiveVacancies = 0,
    decimal TokenBalance = 0,
    bool MissingManager = false,
    Guid? ParentId = null,
    string? RegionName = null,
    IReadOnlyList<OrgTreeNode>? Children = null)
{
    public static string OrgKey(Guid id) => $"org:{id:D}";
    public static string RegionKey(Guid id) => $"region:{id:D}";
    public static string VestigingKey(Guid id) => $"vestiging:{id:D}";

    public static bool TryParse(string? node, out OrgTreeNodeKind kind, out Guid id)
    {
        kind = default;
        id = default;
        if (string.IsNullOrWhiteSpace(node))
        {
            return false;
        }

        var value = node.Trim();
        if (string.Equals(value, "org", StringComparison.OrdinalIgnoreCase))
        {
            kind = OrgTreeNodeKind.Org;
            return true;
        }

        var parts = value.Split(':', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out id))
        {
            return false;
        }

        if (string.Equals(parts[0], "org", StringComparison.OrdinalIgnoreCase))
        {
            kind = OrgTreeNodeKind.Org;
            return true;
        }

        if (string.Equals(parts[0], "region", StringComparison.OrdinalIgnoreCase))
        {
            kind = OrgTreeNodeKind.Region;
            return true;
        }

        if (string.Equals(parts[0], "vestiging", StringComparison.OrdinalIgnoreCase))
        {
            kind = OrgTreeNodeKind.Vestiging;
            return true;
        }

        return false;
    }
}

public static class OrgTreeBuilder
{
    public static IReadOnlyList<OrgTreeNode> Build(
        IReadOnlyList<Models.CompanySummary> companies,
        IReadOnlyList<Models.RegionItem> regions,
        IReadOnlySet<Guid> managedBranchIds,
        IReadOnlySet<Guid>? accessibleCompanyIds = null)
    {
        var orgs = companies.Where(c => c.ParentCompanyId is null).OrderBy(c => c.Name).ToList();
        var branches = companies.Where(c => c.ParentCompanyId is not null).OrderBy(c => c.Name).ToList();
        if (accessibleCompanyIds is not null)
        {
            orgs = orgs.Where(o => accessibleCompanyIds.Contains(o.Id)
                                   || branches.Any(b => b.ParentCompanyId == o.Id && accessibleCompanyIds.Contains(b.Id)))
                .ToList();
            branches = branches.Where(b => accessibleCompanyIds.Contains(b.Id)).ToList();
            regions = regions.Where(r =>
                    accessibleCompanyIds.Contains(r.OrganizationCompanyId)
                    || r.Companies.Any(c => accessibleCompanyIds.Contains(c.CompanyId)))
                .ToList();
        }

        var result = new List<OrgTreeNode>();
        foreach (var org in orgs)
        {
            var orgBranches = branches.Where(b => b.ParentCompanyId == org.Id).ToList();
            var orgRegions = regions
                .Where(r => r.OrganizationCompanyId == org.Id)
                .OrderBy(r => r.Name)
                .ToList();

            var regionNodes = new List<OrgTreeNode>();
            var assigned = new HashSet<Guid>();
            foreach (var region in orgRegions)
            {
                var regionBranches = orgBranches
                    .Where(b => region.Companies.Any(c => c.CompanyId == b.Id))
                    .ToList();
                foreach (var b in regionBranches)
                {
                    assigned.Add(b.Id);
                }

                regionNodes.Add(new OrgTreeNode(
                    OrgTreeNode.RegionKey(region.Id),
                    OrgTreeNodeKind.Region,
                    region.Id,
                    region.Name,
                    ChildCount: regionBranches.Count,
                    LiveVacancies: regionBranches.Sum(b => b.ActiveVacancies),
                    TokenBalance: regionBranches.Sum(b => b.TokenBalance),
                    ParentId: org.Id,
                    Children: regionBranches.Select(ToVestiging).ToList()));
            }

            var unassigned = orgBranches.Where(b => !assigned.Contains(b.Id)).Select(ToVestiging).ToList();
            var children = new List<OrgTreeNode>();
            children.AddRange(regionNodes);
            if (unassigned.Count > 0)
            {
                children.Add(new OrgTreeNode(
                    $"region:none:{org.Id:D}",
                    OrgTreeNodeKind.Region,
                    Guid.Empty,
                    "Zonder regio",
                    ChildCount: unassigned.Count,
                    LiveVacancies: unassigned.Sum(b => b.LiveVacancies),
                    TokenBalance: unassigned.Sum(b => b.TokenBalance),
                    ParentId: org.Id,
                    Children: unassigned));
            }

            result.Add(new OrgTreeNode(
                OrgTreeNode.OrgKey(org.Id),
                OrgTreeNodeKind.Org,
                org.Id,
                org.Name,
                ChildCount: orgBranches.Count,
                LiveVacancies: orgBranches.Sum(b => b.ActiveVacancies) + org.ActiveVacancies,
                TokenBalance: org.TokenBalance,
                Children: children));
        }

        return result;

        OrgTreeNode ToVestiging(Models.CompanySummary b) => new(
            OrgTreeNode.VestigingKey(b.Id),
            OrgTreeNodeKind.Vestiging,
            b.Id,
            b.Name,
            LiveVacancies: b.ActiveVacancies,
            TokenBalance: b.TokenBalance,
            MissingManager: !managedBranchIds.Contains(b.Id),
            ParentId: b.ParentCompanyId,
            RegionName: regions.FirstOrDefault(r => r.Companies.Any(c => c.CompanyId == b.Id))?.Name);
    }

    public static OrgTreeNode? Find(IReadOnlyList<OrgTreeNode> roots, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return roots.Count > 0 ? roots[0] : null;
        }

        if (string.Equals(key, "org", StringComparison.OrdinalIgnoreCase))
        {
            return roots.Count > 0 ? roots[0] : null;
        }

        foreach (var root in roots)
        {
            var found = FindRecursive(root, key);
            if (found is not null)
            {
                return found;
            }
        }

        return roots.Count > 0 ? roots[0] : null;
    }

    private static OrgTreeNode? FindRecursive(OrgTreeNode node, string key)
    {
        if (string.Equals(node.Key, key, StringComparison.OrdinalIgnoreCase))
        {
            return node;
        }

        if (node.Children is null)
        {
            return null;
        }

        foreach (var child in node.Children)
        {
            var found = FindRecursive(child, key);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public static IReadOnlyList<OrgTreeNode> Flatten(IReadOnlyList<OrgTreeNode> roots)
    {
        var list = new List<OrgTreeNode>();
        foreach (var root in roots)
        {
            Walk(root, list);
        }

        return list;

        static void Walk(OrgTreeNode node, List<OrgTreeNode> acc)
        {
            acc.Add(node);
            if (node.Children is null)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                Walk(child, acc);
            }
        }
    }
}
