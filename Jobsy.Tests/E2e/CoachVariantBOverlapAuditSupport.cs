using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class CoachVariantBOverlapAuditSupport
{
    internal sealed record AuditPage(string Name, string Path, string? LoginEmail);

    internal static readonly AuditPage[] Pages =
    [
        new("candidate-home", "/home", KbE2e.DefaultCompleteEmail),
        new("candidate-applications", "/candidate/applications", KbE2e.DefaultCompleteEmail),
        new("onboarding-step-1", "/candidate/start?step=1", "onboarding.e2e@jobsy.local"),
        new("onboarding-step-2", "/candidate/start?step=2", "onboarding.e2e@jobsy.local"),
        new("employer-home", "/employer/vacancies", VacancyWizardE2e.EmployerEmail),
        new("intermediair", "/intermediary", "intermediair@jobsy.local"),
        new("school", "/school", "schoolbeheer@jobsy.local"),
        new("admin", "/admin/organisaties", "admin@jobsy.local"),
        new("leraar", "/leraar", "leraar@jobsy.local"),
        new("mbo-overzicht", "/leerling/mbo-hbo/overzicht", null),
        new("mbo-uitleg", "/leerling/mbo-hbo/uitleg", null),
    ];

    internal sealed class OverlapAuditDto
    {
        public bool Ok { get; set; }
        public bool Skip { get; set; }
        public string Reason { get; set; } = "";
        public List<string> Hits { get; set; } = [];
    }

    internal static async Task<bool> LoginAsync(IPage page, string baseUrl, string email, string password = KbE2e.DefaultPassword)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await KbE2e.TryLoginAsync(page, baseUrl, email, password))
            {
                return true;
            }

            await page.WaitForTimeoutAsync(1200);
        }

        return false;
    }

    internal static async Task EnsureSchoolsEnabledAsync()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true,
            });
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    internal static async Task<(Guid SchoolId, Guid ClassId, string Code)?> SeedFreshMboPupilAsync(
        IBrowser browser,
        string baseUrl)
    {
        _ = browser;
        _ = baseUrl;
        await EnsureSchoolsEnabledAsync();
        return await SeedFreshMboPupilViaDatabaseAsync();
    }

    internal static async Task<(Guid SchoolId, Guid ClassId, string Code)?> SeedFreshMboPupilViaDatabaseAsync()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return null;
        }

        var repoRoot = KbE2e.FindRepoRoot();
        var contentRoot = Path.Combine(repoRoot, "Jobsy.Web");
        var keyPath = Path.Combine(contentRoot, "scholen-code-hmac.key");
        var keySource = Path.Combine(repoRoot, "Jobsy.Api", "scholen-code-hmac.key");
        if (!File.Exists(keyPath) && File.Exists(keySource))
        {
            File.Copy(keySource, keyPath);
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var schoolClass = await db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == DemoSchoolIds.ClassMbo2Id);
        if (schoolClass is null)
        {
            return null;
        }

        if (schoolClass.TestWindow != TestWindowState.Open)
        {
            schoolClass.TestWindow = TestWindowState.Open;
            await db.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder().Build();
        var env = new E2eWebContentHostEnvironment(contentRoot);
        var dp = DataProtectionProvider.Create(Path.Combine(contentRoot, "dp-keys-e2e-audit"));
        var codes = new PupilCodeService(db, dp, config, env);
        var created = await codes.GenerateAsync(1, schoolClass);
        var plain = codes.Unprotect(created[0].CodeProtected);
        if (string.IsNullOrWhiteSpace(plain))
        {
            return null;
        }

        return (DemoSchoolIds.SchoolId, DemoSchoolIds.ClassMbo2Id, PupilCodeFormat.Display(plain));
    }

    internal static async Task<bool> PupilLoginAsync(IPage page, string baseUrl, Guid schoolId, Guid classId, string plainCode)
    {
        var loginUrl = $"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}";
        var classValue = classId.ToString("D");
        await page.Context.ClearCookiesAsync();
        await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.EvaluateAsync(
            """
            ([id, code]) => new Promise((resolve, reject) => {
              const deadline = performance.now() + 30000;
              const tick = () => {
                const select = document.querySelector('select[name=classId]');
                const input = document.querySelector('input[name=code]');
                const option = select && [...select.options].find(o => o.value === id);
                if (select && input && option) {
                  option.selected = true;
                  select.value = id;
                  input.value = code;
                  const form = input.closest('form');
                  if (!form) { reject(new Error('no form')); return; }
                  if (typeof form.requestSubmit === 'function') form.requestSubmit();
                  else form.submit();
                  resolve(true);
                  return;
                }
                if (performance.now() > deadline) reject(new Error('login timeout'));
                else setTimeout(tick, 50);
              };
              tick();
            })
            """,
            new object[] { classValue, plainCode });
        await page.WaitForURLAsync(
            url => !url.Contains("/leerling?", StringComparison.OrdinalIgnoreCase)
                || url.Contains("error=", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });
        if (page.Url.Contains("error=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static async Task WaitForCoachChromeAsync(IPage page, string pageName)
    {
        if (string.Equals(pageName, "vacancy-mock-interview", StringComparison.Ordinal))
        {
            try
            {
                await page.WaitForSelectorAsync("[data-testid=kb-detail]", new() { Timeout = 60_000, State = WaitForSelectorState.Attached });
                await page.WaitForSelectorAsync(
                    "[data-testid=vacancy-mock-interview-cta] .lobsy-coach-cta__btn",
                    new() { Timeout = 90_000, State = WaitForSelectorState.Visible });
            }
            catch
            {
                // evaluated below
            }

            return;
        }

        try
        {
            await page.WaitForSelectorAsync(
                "#lobsy-coach-btn, #lobsy-assistant-launcher-btn, .lobsy-coach-cta--variant-b",
                new() { Timeout = 45_000, State = WaitForSelectorState.Visible });
        }
        catch
        {
            // no-coach surfaces as test failure
        }
    }

    internal static async Task<OverlapAuditDto> EvaluateOverlapAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
            () => JSON.stringify((() => {
              const coachParts = [
                '#lobsy-coach-btn.lobsy-coach-dock__btn--lobster-b',
                '#lobsy-coach-btn',
                '#lobsy-assistant-launcher-btn',
                '.lobsy-coach-cta--variant-b .lobsy-coach-cta__btn'
              ];
              const coachRoots = new Set();
              let coachBox = null;
              for (const sel of coachParts) {
                const el = document.querySelector(sel);
                if (!el) continue;
                const st = getComputedStyle(el);
                if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') continue;
                const r = el.getBoundingClientRect();
                if (r.width < 8 || r.height < 8) continue;
                coachBox = { left: r.left, top: r.top, right: r.right, bottom: r.bottom, sel };
                el.closest('.lobsy-coach-dock, .lobsy-coach-cta, [data-lobsy-coach]')?.querySelectorAll('*').forEach(n => coachRoots.add(n));
                coachRoots.add(el);
                break;
              }
              if (!coachBox) return { ok: true, skip: true, reason: 'no-coach', hits: [] };

              const inter = (a, b) => a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
              const hits = [];
              const isClickable = (el) => {
                if (!el || coachRoots.has(el)) return false;
                if (el.closest('.lobsy-coach-dock, .lobsy-coach-cta, #lobsy-assistant-ssr-shell')) return false;
                const st = getComputedStyle(el);
                if (st.display === 'none' || st.visibility === 'hidden' || st.pointerEvents === 'none' || st.opacity === '0') return false;
                const r = el.getBoundingClientRect();
                if (r.width < 8 || r.height < 8) return false;
                if (el.disabled) return false;
                const tag = (el.tagName || '').toLowerCase();
                if (tag === 'a' || tag === 'button' || tag === 'input' || tag === 'select' || tag === 'textarea') return true;
                if (el.getAttribute('role') === 'button' || el.getAttribute('role') === 'link') return true;
                return false;
              };

              document.querySelectorAll('a,button,input,select,textarea,[role=button],[role=link]').forEach(el => {
                if (!isClickable(el)) return;
                const r = el.getBoundingClientRect();
                const box = { left: r.left, top: r.top, right: r.right, bottom: r.bottom };
                if (inter(coachBox, box)) {
                  const id = el.id ? '#' + el.id : '';
                  const testid = el.getAttribute('data-testid');
                  hits.push((el.tagName || '').toLowerCase() + id + (testid ? '[data-testid=' + testid + ']' : ''));
                }
              });

              return { ok: hits.length === 0, skip: false, reason: hits.length ? hits.join('; ') : 'ok', hits };
            })())
            """);

        return JsonSerializer.Deserialize<OverlapAuditDto>(json, JsonOpts()) ?? new OverlapAuditDto();
    }

    private static JsonSerializerOptions JsonOpts() => new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static async Task WriteSummaryMarkdownAsync(
        string outDir,
        IEnumerable<(string Page, int Viewport, bool Pass, string Note)> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Coach variant B overlap audit");
        sb.AppendLine();
        sb.AppendLine("| Pagina | VP | PASS | Notitie |");
        sb.AppendLine("|--------|-----|------|---------|");
        foreach (var row in rows.OrderBy(r => r.Page).ThenBy(r => r.Viewport))
        {
            sb.AppendLine($"| {row.Page} | {row.Viewport} | {(row.Pass ? "PASS" : "FAIL")} | {row.Note} |");
        }

        await File.WriteAllTextAsync(Path.Combine(outDir, "OVERLAP-AUDIT.md"), sb.ToString());
    }

    internal sealed class E2eWebContentHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Web";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

}
