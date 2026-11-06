using Jobsy.Web.Models;
using Jobsy.Web.Werkgever;

namespace Jobsy.Tests.Werkgever;

public class WgOrgTreeTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RegionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BranchA = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid BranchB = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid BranchC = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private static List<CompanySummary> Companies() =>
    [
        new() { Id = OrgId, Name = "Org BV", ParentCompanyId = null, ActiveVacancies = 1, TokenBalance = 10 },
        new() { Id = BranchA, Name = "Naaldwijk", ParentCompanyId = OrgId, ActiveVacancies = 5, TokenBalance = 20 },
        new() { Id = BranchB, Name = "De Lier", ParentCompanyId = OrgId, ActiveVacancies = 2, TokenBalance = 5 },
        new() { Id = BranchC, Name = "Monster", ParentCompanyId = OrgId, ActiveVacancies = 3, TokenBalance = 8 },
    ];

    private static List<RegionItem> Regions() =>
    [
        new()
        {
            Id = RegionId,
            Name = "Westland",
            OrganizationCompanyId = OrgId,
            Companies =
            [
                new() { CompanyId = BranchA, CompanyName = "Naaldwijk" },
                new() { CompanyId = BranchB, CompanyName = "De Lier" },
            ]
        }
    ];

    [Fact]
    public void Build_BM_sees_org_regions_and_branches_with_missing_manager_pill()
    {
        var managed = new HashSet<Guid> { BranchA, BranchC };
        var roots = OrgTreeBuilder.Build(Companies(), Regions(), managed);
        Assert.Single(roots);
        Assert.Equal(OrgTreeNodeKind.Org, roots[0].Kind);
        Assert.Equal(3, roots[0].ChildCount);

        var region = roots[0].Children!.First(c => c.Kind == OrgTreeNodeKind.Region && c.Id == RegionId);
        Assert.Equal(2, region.ChildCount);
        var deLier = region.Children!.Single(c => c.Id == BranchB);
        Assert.True(deLier.MissingManager);

        var unassigned = roots[0].Children!.First(c => c.Id == Guid.Empty);
        Assert.Contains(unassigned.Children!, c => c.Id == BranchC);
    }

    [Fact]
    public void Build_RM_filters_to_accessible_companies()
    {
        var accessible = new HashSet<Guid> { BranchA, BranchB };
        var roots = OrgTreeBuilder.Build(Companies(), Regions(), managedBranchIds: accessible, accessible);
        Assert.Single(roots);
        var flat = OrgTreeBuilder.Flatten(roots).Where(n => n.Kind == OrgTreeNodeKind.Vestiging).Select(n => n.Id).ToHashSet();
        Assert.Equal(accessible, flat);
        Assert.DoesNotContain(BranchC, flat);
    }

    [Fact]
    public void Find_and_parse_node_query_sync()
    {
        var roots = OrgTreeBuilder.Build(Companies(), Regions(), new HashSet<Guid> { BranchA, BranchB, BranchC });
        Assert.True(OrgTreeNode.TryParse("region:" + RegionId, out var kind, out var id));
        Assert.Equal(OrgTreeNodeKind.Region, kind);
        Assert.Equal(RegionId, id);

        var found = OrgTreeBuilder.Find(roots, OrgTreeNode.RegionKey(RegionId));
        Assert.NotNull(found);
        Assert.Equal("Westland", found!.Name);

        var orgDefault = OrgTreeBuilder.Find(roots, "org");
        Assert.Equal(OrgId, orgDefault!.Id);
    }

    [Fact]
    public void Flatten_supports_keyboard_order()
    {
        var roots = OrgTreeBuilder.Build(Companies(), Regions(), new HashSet<Guid>());
        var flat = OrgTreeBuilder.Flatten(roots);
        Assert.True(flat.Count >= 5);
        Assert.Equal(OrgTreeNodeKind.Org, flat[0].Kind);
        // After org: region(s) then children — ArrowDown walks this list.
        Assert.Contains(flat, n => n.Id == BranchA);
    }
}
