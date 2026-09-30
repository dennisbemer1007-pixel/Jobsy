using System.Text;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

/// <summary>
/// Builds <c>docs/ROUTES.md</c> from Blazor <c>@page</c> + authorize attributes.
/// Keep in sync via <see cref="RoutesDocFreshnessTests"/>.
/// </summary>
public static class RoutesDocGenerator
{
    public const string RelativeDocPath = "docs/ROUTES.md";

    public static string Generate()
    {
        var index = RazorRouteIndex.Load();
        var root = RepoRoot.Find();
        var componentsRoot = Path.Combine(root, "Jobsy.Web", "Components")
            + Path.DirectorySeparatorChar;

        var rows = new List<(string Route, string Component, string Access)>();
        foreach (var page in index.Pages)
        {
            var rel = page.FilePath.StartsWith(componentsRoot, StringComparison.OrdinalIgnoreCase)
                ? page.FilePath[componentsRoot.Length..].Replace('\\', '/')
                : Path.GetFileName(page.FilePath);

            var access = DescribeAccess(page);
            foreach (var template in page.Templates)
            {
                rows.Add((template, rel, access));
            }
        }

        rows.Sort((a, b) =>
        {
            var c = string.Compare(a.Route, b.Route, StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : string.Compare(a.Component, b.Component, StringComparison.OrdinalIgnoreCase);
        });

        var sb = new StringBuilder();
        sb.AppendLine("# Blazor routes");
        sb.AppendLine();
        sb.AppendLine("Generated from `Jobsy.Web/Components/**/*.razor` `@page` directives and");
        sb.AppendLine("`[Authorize]` / `[AllowAnonymous]` attributes. **Do not hand-edit the table** —");
        sb.AppendLine("regenerate with:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("JOBSY_UPDATE_ROUTES_DOC=1 dotnet test Jobsy.Tests/Jobsy.Tests.csproj \\");
        sb.AppendLine("  --filter \"FullyQualifiedName~RoutesDocFreshness\"");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Guard: `Jobsy.Tests/RoutesDocFreshnessTests`.");
        sb.AppendLine();
        sb.AppendLine("## NL / EN mix");
        sb.AppendLine();
        sb.AppendLine("Routes intentionally mix Dutch and English segments (`/profiel`, `/carriere`,");
        sb.AppendLine("`/banen`, `/hoe-werkt-lobsy`, `/candidate/...`, `/employer/...`, `/vacancies/...`).");
        sb.AppendLine("**Do not rename routes** for cosmetics — bookmarks, QR landings, and emails depend on them.");
        sb.AppendLine("Product narrative per role: [`ROLES_AND_VIEWS.md`](../ROLES_AND_VIEWS.md).");
        sb.AppendLine("Authorization intent: [`security/roles-matrix.md`](security/roles-matrix.md).");
        sb.AppendLine();
        sb.AppendLine("## Access column");
        sb.AppendLine();
        sb.AppendLine("| Value | Meaning |");
        sb.AppendLine("|-------|---------|");
        sb.AppendLine("| `anonymous` | `[AllowAnonymous]` |");
        sb.AppendLine("| `authenticated` | `[Authorize]` without `Roles=` |");
        sb.AppendLine("| role list | `[Authorize(Roles=\"…\")]` (Jobsy role claim names) |");
        sb.AppendLine("| `any (no Authorize attribute)` | No attribute — Web has no FallbackPolicy |");
        sb.AppendLine();
        sb.AppendLine($"## Table ({rows.Count} routes)");
        sb.AppendLine();
        sb.AppendLine("| Route | Component | Access |");
        sb.AppendLine("|-------|-----------|--------|");
        foreach (var (route, component, access) in rows)
        {
            sb.Append("| `").Append(EscapeTicks(route)).Append("` | `")
                .Append(EscapeTicks(component)).Append("` | ")
                .Append(access).AppendLine(" |");
        }

        sb.AppendLine();
        sb.AppendLine("## Werkgever legacy redirects (D2)");
        sb.AppendLine();
        sb.AppendLine("Old `/employer`, `/branch` and `/regional` URLs answer **301** to `/werkgever/…`");
        sb.AppendLine("for at least one release. Source: `WerkgeverLegacyRoutes.Table`.");
        sb.AppendLine("// Remove after {release}");
        sb.AppendLine();
        sb.AppendLine("| Old | New |");
        sb.AppendLine("|-----|-----|");
        foreach (var row in Jobsy.Web.Navigation.WerkgeverLegacyRoutes.Table)
        {
            var neu = row.NewPath;
            if (!string.IsNullOrEmpty(row.Tab))
            {
                neu += (neu.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "tab=" + row.Tab;
            }

            sb.Append("| `").Append(EscapeTicks(row.OldPath)).Append("` | `")
                .Append(EscapeTicks(neu)).AppendLine("` |");
        }

        sb.AppendLine();
        sb.AppendLine("| Special | Behaviour |");
        sb.AppendLine("|---------|------------|");
        sb.AppendLine("| `/home` (employer roles only) | 301 → `/werkgever` |");
        sb.AppendLine("| `/employer/onboarding-checkout`, `/tokens/checkout-return`, `/tokens/checkout-stub` | **unchanged** (payment return URLs) |");
        sb.AppendLine();
        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static string Normalize(string markdown)
        => markdown.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";

    private static string DescribeAccess(RazorPageInfo page)
    {
        if (page.AllowAnonymous)
        {
            return "anonymous";
        }

        if (page.Authorize && page.Roles.Count > 0)
        {
            return string.Join(", ", page.Roles);
        }

        if (page.Authorize)
        {
            return "authenticated";
        }

        return "any (no Authorize attribute)";
    }

    private static string EscapeTicks(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);
}
