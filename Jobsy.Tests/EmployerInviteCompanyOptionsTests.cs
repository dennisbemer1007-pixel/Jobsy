using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class EmployerInviteCompanyOptionsTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Branch2Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Org_and_single_vestiging_same_name_yield_at_most_one_extra()
    {
        var companies = BemerOrgAndBranch();

        var extrasWhenBranchPrimary = EmployerInviteCompanyOptions.ExtraMembershipChoices(companies, BranchId);
        Assert.Empty(extrasWhenBranchPrimary);

        var extrasWhenOrgPrimary = EmployerInviteCompanyOptions.ExtraMembershipChoices(companies, OrgId);
        Assert.Single(extrasWhenOrgPrimary);
        Assert.Equal(BranchId, extrasWhenOrgPrimary[0].Id);
    }

    [Fact]
    public void Extra_choices_never_include_primary_or_organisation_wrapper()
    {
        var companies = BemerOrgAndTwoBranches();

        var extras = EmployerInviteCompanyOptions.ExtraMembershipChoices(companies, BranchId);
        Assert.Equal([Branch2Id], extras.Select(c => c.Id));
        Assert.DoesNotContain(extras, c => c.Id == OrgId);
        Assert.DoesNotContain(extras, c => c.Id == BranchId);
    }

    [Fact]
    public void Branch_manager_primary_is_the_vestiging_not_the_organisation()
    {
        var companies = BemerOrgAndBranch();
        var primary = EmployerInviteCompanyOptions.PrimaryChoices(companies, "BranchManager");
        Assert.Single(primary);
        Assert.Equal(BranchId, primary[0].Id);

        var coerced = EmployerInviteCompanyOptions.CoercePrimary(primary, OrgId);
        Assert.Equal(BranchId, coerced);
    }

    [Fact]
    public void Enterprise_manager_primary_is_the_organisation()
    {
        var companies = BemerOrgAndBranch();
        var primary = EmployerInviteCompanyOptions.PrimaryChoices(companies, "EnterpriseManager");
        Assert.Single(primary);
        Assert.Equal(OrgId, primary[0].Id);
    }

    [Fact]
    public void Labels_distinguish_organisation_from_vestiging_with_the_same_name()
    {
        var companies = BemerOrgAndBranch();
        Assert.Equal("Bemer IT Solutions (organisatie)", EmployerInviteCompanyOptions.Label(companies[0], companies));
        Assert.Equal("Bemer IT Solutions — Laan 1", EmployerInviteCompanyOptions.Label(companies[1], companies));
    }

    [Fact]
    public void Users_page_lists_extra_lidmaatschappen_from_helper()
    {
        var users = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Pages/Employer/Users.razor"));
        Assert.Contains("InviteExtraCompanies", users);
        Assert.Contains("EditExtraCompanies", users);
        Assert.Contains("EmployerInviteCompanyOptions", users);
        Assert.Contains("@foreach (var c in InviteExtraCompanies)", users);
        Assert.Contains("@foreach (var c in EditExtraCompanies)", users);
        Assert.Contains("DropCoveredMemberships(_editMemberships", users);
    }

    private static List<InviteCompanyOption> BemerOrgAndBranch() =>
    [
        new(OrgId, "Bemer IT Solutions", "Laan 1", ParentCompanyId: null),
        new(BranchId, "Bemer IT Solutions", "Laan 1", OrgId, "000012345678")
    ];

    private static List<InviteCompanyOption> BemerOrgAndTwoBranches() =>
    [
        new(OrgId, "Bemer IT Solutions", "Laan 1", ParentCompanyId: null),
        new(BranchId, "Bemer IT Solutions", "Laan 1", OrgId, "000012345678"),
        new(Branch2Id, "Bemer IT Solutions", "Laan 2", OrgId, "000012345679")
    ];

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
