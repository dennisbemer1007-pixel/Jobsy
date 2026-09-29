using System.Reflection;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Jobsy.Tests.Werkgever;

public class WerkgeverPageAuthorizeTests
{
    [Fact]
    public void Every_werkgever_page_authorize_matches_matrix()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var dir = Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Werkgever");
        Assert.True(Directory.Exists(dir), dir);

        var pages = Directory.GetFiles(dir, "*.razor")
            .Where(f => !Path.GetFileName(f).StartsWith("_", StringComparison.Ordinal)
                        && !Path.GetFileName(f).Contains("Legacy", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var row in WerkgeverRightsMatrix.Pages)
        {
            var match = pages.Select(p => (Path: p, Text: File.ReadAllText(p)))
                .FirstOrDefault(p => p.Text.Contains($"@page \"{row.Route}\"", StringComparison.Ordinal));
            Assert.True(match.Path is not null, $"Missing page for {row.Route}");
            Assert.Contains($"Authorize(Roles = \"{row.AuthorizeRoles}\")", match.Text, StringComparison.Ordinal);
        }
    }
}

public class WerkgeverNavVisibilityTests
{
    [Theory]
    [InlineData(EmployerRole.Bedrijfsmanager)]
    [InlineData(EmployerRole.Regiomanager)]
    [InlineData(EmployerRole.Vestigingsmanager)]
    public void Nav_for_role_matches_matrix_visibility(EmployerRole role)
    {
        var ctx = new WerkgeverNavContext(
            HasApiOrCsvImport: true,
            HasTakeovers: true,
            HasSalesReferral: true,
            CandidateInsightsEnabled: true);
        var items = WerkgeverNav.For(role, ctx).SelectMany(g => g.Items).ToList();
        foreach (var row in WerkgeverRightsMatrix.Pages)
        {
            var navItem = items.FirstOrDefault(i =>
                string.Equals(WerkgeverNav.Normalize(i.Href), WerkgeverNav.Normalize(row.Route), StringComparison.OrdinalIgnoreCase));
            var allowed = WerkgeverRightsMatrix.RoleAllowed(row, role);
            if (row.Route is "/werkgever/vacatures/nieuw" or "/werkgever/partner" or "/werkgever/partner/uitbetalen"
                or "/werkgever/koppelingen"
                || row.Route.Contains('{', StringComparison.Ordinal))
            {
                // Nieuw / detail / payout stub routes have no nav item; partner/koppelingen are conditional.
                continue;
            }

            if (allowed)
            {
                Assert.NotNull(navItem);
            }
            else
            {
                Assert.Null(navItem);
            }
        }

        Assert.DoesNotContain(items, i => i.Visibility.For(role) == RoleVisibilityKind.Hidden);
    }
}

public class WerkgeverFeatureGateTests
{
    [Fact]
    public void RequiresFeatureAttribute_absent_or_present_on_werkgever_pages()
    {
        var attr = Type.GetType("Jobsy.Core.Features.RequiresFeatureAttribute, Jobsy.Core")
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a =>
                       {
                           try { return a.GetTypes(); }
                           catch { return []; }
                       })
                       .FirstOrDefault(t => t.Name == "RequiresFeatureAttribute");

        if (attr is null)
        {
            // Dependencies A: ABSENT — vacuous pass.
            Assert.Null(attr);
            return;
        }

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var dir = Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Werkgever");
        foreach (var file in Directory.GetFiles(dir, "*.razor"))
        {
            if (Path.GetFileName(file).Contains("Legacy", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (!text.Contains("@page ", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Contains("RequiresFeature", text, StringComparison.Ordinal);
        }
    }
}
