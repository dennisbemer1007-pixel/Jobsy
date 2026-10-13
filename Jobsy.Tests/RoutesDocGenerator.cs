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
        sb.AppendLine("`/banenkaart`, `/hoe-werkt-lobsy`, `/candidate/...`, `/employer/...`, `/vacancies/...`).");
        sb.AppendLine("**Do not rename routes** for cosmetics — bookmarks, QR landings, and emails depend on them.");
        sb.AppendLine("Product narrative per role: [`ROLES_AND_VIEWS.md`](../ROLES_AND_VIEWS.md).");
        sb.AppendLine("Authorization intent: [`security/roles-matrix.md`](security/roles-matrix.md).");
        sb.AppendLine();
        sb.AppendLine("## Landing + banenkaart (landing 04–05)");
        sb.AppendLine();
        sb.AppendLine("- `/` — public landing page (static SSR, no MapLibre / no Blazor runtime; indexed). Signed-in users are **302** → role home (passport ON default: discovery/paspoort for candidates; passport OFF: `/banenkaart`). Legacy map deep-link query on `/` → **301** `/banenkaart?…`.");
        sb.AppendLine("- `/banenkaart` — public job map (indexed).");
        sb.AppendLine("- `/banen` — legacy; **301** → `/banenkaart` (query preserved; middleware, not a Blazor page).");
        sb.AppendLine("- `/bewaard`, `/candidate/saved`, `/candidate/bewaard` — legacy Bewaard URLs; **302** → `/candidate/liked` (query preserved; `BewaardRedirectMiddleware`).");
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
        sb.AppendLine("## Minimal API (public shell)");
        sb.AppendLine();
        sb.AppendLine("Not Blazor `@page` routes — documented here for discoverability (landing stack).");
        sb.AppendLine();
        sb.AppendLine("| Route | Notes |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine("| `/taal/{lang}` | Sets `Jobsy.Culture` cookie; 302 to local `returnUrl` only; `noindex` |");
        sb.AppendLine("| `/account/cookie-consent/analytics-token` | POST; same-origin analytics consent token for static cookie banner |");
        sb.AppendLine("| `/account/email-code/start` | POST; antiforgery; starts passwordless e-mail code (Web → API) |");
        sb.AppendLine("| `/account/email-code/verify` | POST; antiforgery; verifies code and signs in |");
        sb.AppendLine("| `/mail/afmelden` | POST; RFC 8058 one-click / form unsubscribe (no antiforgery; rate-limited) |");
        sb.AppendLine("| `/account/mail-instellingen` | POST; antiforgery; save optional mail toggles (Web → API) |");
        sb.AppendLine("| `/melden` | POST; antiforgery; forwards a content report to `api/reports` (rate-limited; no IP stored) |");
        sb.AppendLine("| `/partner/flyer.pdf` | GET; anonymous; proxies the partner flyer pdf (`?code=` optional); 302 → `/` with werkgevers-actief OFF; rate-limited |");
        sb.AppendLine("| `/register?van=ontdek` | GET; 302 → `/account-maken?van=ontdek` (legacy test CTA) |");
        sb.AppendLine("| `/banen` | GET/HEAD; **301** → `/banenkaart` (+ query) |");
        sb.AppendLine("| `/bewaard` | GET/HEAD; **302** → `/candidate/liked` (+ query) |");
        sb.AppendLine("| `/candidate/saved` | GET/HEAD; **302** → `/candidate/liked` (+ query) |");
        sb.AppendLine("| `/candidate/bewaard` | GET/HEAD; **302** → `/candidate/liked` (+ query) |");
        sb.AppendLine();
        sb.AppendLine("## Kandidaat banen notes");
        sb.AppendLine();
        sb.AppendLine("- Banenkaart list mode: query `?weergave=lijst` on `/banenkaart`. Persisted in `sessionStorage jobsy.kb.weergave`.");
        sb.AppendLine("- Employer viewed hook: `POST api/applications/{id}/viewed` (07) records at most one `EmployerViewed` timeline event.");
        sb.AppendLine("- Werkgevers gating (paspoort 01): when `PlatformFeature.Employers` lands, candidate job pages/APIs return the feature gate / `404 feature_disabled`. Until then KB-FALLBACK(C) comments mark the intended sites.");
        sb.AppendLine("- Map route constant: `KbRoutes.Map` (`/banenkaart`). Saved: `KbRoutes.Saved` (`/candidate/liked`). With passport ON, Bewaard is a tab under Sollicitaties.");
        sb.AppendLine();
        sb.AppendLine("## Carrière notes");
        sb.AppendLine();
        sb.AppendLine("- `/carriere` deep-links one step with `?stap={n}`; an unknown or future step falls back to the overview.");
        sb.AppendLine("- `/candidate/talent-contacts` only shows employer contact details after the candidate accepts; a Pending request shows the share preview first.");
        sb.AppendLine("- `/candidate/hoe-werkt-lobsy` is the candidate how-to guide (Kandidaat only); other roles get their own guide and never see the five stones.");
        sb.AppendLine("- Career API: `GET api/me/career-path/dream-options`, `GET api/me/career-path/archived`, `POST api/me/career-path/archived/{id}/restore`, `GET api/me/talent-contacts/{id}/share-preview`, `GET api/me/journey-summary`.");
        sb.AppendLine("- `POST api/me/career-path/courses/claim` is a **410 Gone** stub (`use_passport_proof`); candidates prove courses through the passport. Removed after 2026-10-30.");
        sb.AppendLine();
        sb.AppendLine("## Notes");
        sb.AppendLine();
        sb.AppendLine("- **Admin redesign 06.4 must host `PayoutRunsSection` in a tab Rondes** on `/admin/financien/uitbetalingen` and keep mark-paid closing payout requests. Until then the fallback is `/admin/sales-managers?tab=uitbetalingen`.");
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
