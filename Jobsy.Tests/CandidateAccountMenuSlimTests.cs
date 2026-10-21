using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Web.Components.Layout;

namespace Jobsy.Tests;

public class CandidateAccountMenuSlimTests
{
    [Fact]
    public void Pure_candidate_uses_slim_account_menu()
    {
        var user = CandidatePrincipal();
        Assert.True(AuthHeader.UseCandidateSlimMenu(user));
    }

    [Fact]
    public void Employer_and_admin_keep_full_account_menu()
    {
        var employer = CandidatePrincipal(JobsyRoles.BranchManager);
        Assert.False(AuthHeader.UseCandidateSlimMenu(employer));

        var admin = CandidatePrincipal(JobsyRoles.Admin);
        Assert.False(AuthHeader.UseCandidateSlimMenu(admin));
    }

    [Fact]
    public void Candidate_slim_menu_source_has_five_actions_and_footer_logout()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "AuthHeader.razor");
        var source = File.ReadAllText(path);
        Assert.Contains("UseCandidateSlimMenu(context.User)", source, StringComparison.Ordinal);
        Assert.Contains("account-menu__footer", source, StringComparison.Ordinal);

        var slimStart = source.IndexOf("if (UseCandidateSlimMenu(context.User))", StringComparison.Ordinal);
        var slimEnd = source.IndexOf("else", slimStart, StringComparison.Ordinal);
        Assert.True(slimStart >= 0 && slimEnd > slimStart);
        var slimBlock = source.Substring(slimStart, slimEnd - slimStart);
        Assert.Contains("Nav.Profile", slimBlock, StringComparison.Ordinal);
        Assert.Contains("CandidateSettings.Title", slimBlock, StringComparison.Ordinal);
        Assert.Contains("Nav.HowLobsyWorks", slimBlock, StringComparison.Ordinal);
        Assert.Contains("Feedback.Button", slimBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("candidate-delete-account-menu", slimBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("Footer.Privacy", slimBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("MailSettings.Title", slimBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_page_links_cover_removed_menu_items()
    {
        var path = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "CandidateSettings.razor");
        var source = File.ReadAllText(path);
        Assert.Contains("CandidateSettings.Mail.Title", source, StringComparison.Ordinal);
        Assert.Contains("candidate-delete-account-settings", source, StringComparison.Ordinal);
        Assert.Contains("/candidate/privacy", source, StringComparison.Ordinal);

        var footer = Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "AppFooter.razor");
        var footerSource = File.ReadAllText(footer);
        Assert.Contains("Footer.Privacy", footerSource, StringComparison.Ordinal);
        Assert.Contains("Footer.Terms", footerSource, StringComparison.Ordinal);
        Assert.Contains("Footer.Usage", footerSource, StringComparison.Ordinal);
        Assert.Contains("Footer.About", footerSource, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal CandidatePrincipal(params string[] extraRoles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Demo Kandidaat"),
            new(ClaimTypes.Email, "kandidaat@jobsy.local"),
            new(ClaimTypes.Role, JobsyRoles.Candidate),
        };
        claims.AddRange(extraRoles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

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
