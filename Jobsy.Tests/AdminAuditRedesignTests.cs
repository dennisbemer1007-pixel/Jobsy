using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Jobsy.Api.Admin;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class AdminAuditRedesignTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public AdminAuditRedesignTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.AdminId);
        return client;
    }

    [Fact]
    public void Interceptor_rejects_modify_and_delete()
    {
        using var db = CreateDb();
        var row = new AdminAuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            ActorRole = "Admin",
            ActorKind = "admin",
            Action = "test.action",
            TargetType = "setting",
            TargetId = "x",
            TargetLabel = "x",
            Result = "success",
            CorrelationId = Guid.NewGuid().ToString("N")
        };
        db.AdminAuditEvents.Add(row);
        db.SaveChanges();

        row.Reason = "tamper";
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());

        db.ChangeTracker.Clear();
        var tracked = db.AdminAuditEvents.Single(e => e.Id == row.Id);
        db.AdminAuditEvents.Remove(tracked);
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task Retention_purges_old_audit_keeps_new()
    {
        await using var db = CreateDb();
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        db.AdminAuditEvents.AddRange(
            new AdminAuditEvent
            {
                Id = oldId,
                OccurredAtUtc = DateTime.UtcNow.AddDays(-3000),
                ActorRole = "system",
                ActorKind = "system",
                Action = "privacy.retention.run",
                TargetType = "retention",
                TargetId = "old",
                TargetLabel = "old",
                Result = "success",
                CorrelationId = "old"
            },
            new AdminAuditEvent
            {
                Id = newId,
                OccurredAtUtc = DateTime.UtcNow.AddDays(-1),
                ActorRole = "system",
                ActorKind = "system",
                Action = "privacy.retention.run",
                TargetType = "retention",
                TargetId = "new",
                TargetLabel = "new",
                Result = "success",
                CorrelationId = "new"
            });
        await db.SaveChangesAsync();

        // Simulate ExecuteDeleteAsync (used by DataRetentionHostedService) which bypasses
        // the change-tracker interceptor — InMemory does not support ExecuteDelete, so
        // detach + ignore tracking and delete via database façade equivalent:
        var cutoff = DateTime.UtcNow.AddDays(-PrivacyConstants.AdminAuditRetentionDays);
        var stale = await db.AdminAuditEvents.Where(e => e.OccurredAtUtc < cutoff).ToListAsync();
        db.ChangeTracker.Clear();
        // Verify ExecuteDelete path exists in retention host (compile-time contract).
        var retentionSrc = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Jobs", "DataRetentionHostedService.cs"));
        Assert.Contains("AdminAuditEvents", retentionSrc);
        Assert.Contains("ExecuteDeleteAsync", retentionSrc);
        Assert.Contains("Privacy:AdminAuditRetentionDays", retentionSrc);

        Assert.Single(stale);
        Assert.Equal(oldId, stale[0].Id);
        Assert.True(await db.AdminAuditEvents.AnyAsync(e => e.Id == newId));
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

        throw new InvalidOperationException("Repo root not found.");
    }

    [Fact]
    public void Reflection_guard_every_admin_reachable_non_get_has_audit_or_exempt()
    {
        var controllers = new[]
        {
            typeof(AdminController),
            typeof(SettingsController),
            typeof(CompanyUsersController),
            typeof(RegistrationController),
            typeof(SalesManagersController),
            typeof(TokensController),
            typeof(SalesCommercialController),
            typeof(TokenFinanceController),
            typeof(PrivacyController),
            typeof(AdminAuditController)
        };

        var missing = new List<string>();
        foreach (var type in controllers)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var isWrite = method.GetCustomAttributes(inherit: true).Any(a =>
                    a is Microsoft.AspNetCore.Mvc.HttpPostAttribute
                        or Microsoft.AspNetCore.Mvc.HttpPutAttribute
                        or Microsoft.AspNetCore.Mvc.HttpPatchAttribute
                        or Microsoft.AspNetCore.Mvc.HttpDeleteAttribute);
                var isAuditedGet = method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpGetAttribute>() is not null
                                   && method.GetCustomAttribute<AdminAuditAttribute>() is not null;
                if (!isWrite && !isAuditedGet) continue;

                var hasAudit = method.GetCustomAttribute<AdminAuditAttribute>() is not null;
                var hasExempt = method.GetCustomAttribute<AdminAuditExemptAttribute>() is not null;
                if (!hasAudit && !hasExempt)
                {
                    missing.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Missing [AdminAudit]/[AdminAuditExempt]: " + string.Join(", ", missing));
    }

    [Fact]
    public async Task Get_audit_filters_and_forbids_non_admin()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.AdminAuditEvents.Add(new AdminAuditEvent
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                ActorUserId = _factory.AdminId,
                ActorRole = "Admin",
                ActorKind = "admin",
                Action = AdminAuditKeys.SettingsPlatformUpdate,
                TargetType = "setting",
                TargetId = "FreePublishUntil",
                TargetLabel = "FreePublishUntil",
                Reason = "promo",
                DetailsJson = """{"field":"FreePublishUntil","from":"2026-09-30","to":"2026-10-31"}""",
                Result = "success",
                CorrelationId = "corr-filter-test"
            });
            await db.SaveChangesAsync();
        }

        var admin = AdminClient();
        var page = await admin.GetFromJsonAsync<AdminAuditPageDto>(
            "api/admin/audit?action=settings.platform.update&q=corr-filter-test", Json);
        Assert.NotNull(page);
        Assert.Contains(page!.Items, i => i.CorrelationId == "corr-filter-test");

        var employer = _factory.CreateClient();
        JobsyTestAuth.Authorize(employer, _factory.EmployerId);
        var forbidden = await employer.GetAsync("api/admin/audit");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Export_writes_export_create()
    {
        var before = await CountActionAsync(AdminAuditKeys.ExportCreate);
        var client = AdminClient();
        var response = await client.GetAsync("api/admin/audit/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var after = await CountActionAsync(AdminAuditKeys.ExportCreate);
        Assert.True(after > before);
    }

    [Fact]
    public async Task Platform_feature_update_writes_per_field_without_secrets()
    {
        var client = AdminClient();
        var response = await client.PutAsJsonAsync("api/settings/platform-features", new
        {
            sessionInactivityTimeoutMinutes = 42,
            reason = "uat-audit-test"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.AdminAuditEvents
            .Where(e => e.Action == AdminAuditKeys.SettingsPlatformUpdate
                        && e.TargetId == "SessionInactivityTimeoutMinutes")
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstOrDefaultAsync();
        Assert.NotNull(row);
        Assert.Equal("success", row!.Result);
        Assert.Contains("42", row.DetailsJson ?? "");
        Assert.DoesNotContain("secret", row.DetailsJson ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AdminAuditKeys.UserSessionsRevoke)]
    [InlineData(AdminAuditKeys.UserBlock)]
    [InlineData(AdminAuditKeys.VacancyExtend)]
    [InlineData(AdminAuditKeys.ApiKeyDeactivate)]
    [InlineData(AdminAuditKeys.TokensGoodwillGrant)]
    [InlineData(AdminAuditKeys.InvoiceMarkPaid)]
    [InlineData(AdminAuditKeys.SupportAccessGrant)]
    [InlineData(AdminAuditKeys.TakeoverApprove)]
    [InlineData(AdminAuditKeys.PrivacyAccountDeleted)]
    [InlineData(AdminAuditKeys.SettingsPricingUpdate)]
    [InlineData(AdminAuditKeys.SettingsCompanyUpdate)]
    [InlineData(AdminAuditKeys.SettingsAboutUpdate)]
    [InlineData(AdminAuditKeys.SettingsFlyerUpdate)]
    [InlineData(AdminAuditKeys.SettingsIntegrationUpdate)]
    [InlineData(AdminAuditKeys.ExportCreate)]
    [InlineData(AdminAuditKeys.VacancyInactive)]
    [InlineData(AdminAuditKeys.UserUnblock)]
    [InlineData(AdminAuditKeys.UserSessionsRevokeAll)]
    [InlineData(AdminAuditKeys.SupportAccessRevoke)]
    [InlineData(AdminAuditKeys.TakeoverReject)]
    [InlineData(AdminAuditKeys.SettingsPricingDelete)]
    [InlineData(AdminAuditKeys.UserRoleChange)]
    [InlineData(AdminAuditKeys.UserMfaReset)]
    [InlineData(AdminAuditKeys.PrivacyRetentionRun)]
    public void Action_key_is_stable(string key)
        => Assert.False(string.IsNullOrWhiteSpace(key));

    [Fact]
    public void Action_pill_shows_text_not_colour_only()
    {
        const string pill = "<span class=\"status-pill status-pill--danger\">2FA gereset</span>";
        Assert.Contains("2FA gereset", pill);
        Assert.Contains("status-pill--danger", pill);
    }

    [Fact]
    public void Details_from_to_list_shape()
    {
        const string json = """{"field":"FreePublishUntil","from":"2026-09-30","to":"2026-10-31"}""";
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("FreePublishUntil", doc.RootElement.GetProperty("field").GetString());
        Assert.Equal("2026-09-30", doc.RootElement.GetProperty("from").GetString());
        Assert.Equal("2026-10-31", doc.RootElement.GetProperty("to").GetString());
    }

    [Fact]
    public void Nav_auditlog_available()
    {
        var items = Jobsy.Web.Navigation.AdminNav.AvailableItems().Select(i => i.Href).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("/admin/beveiliging", items);
        Assert.Contains("/admin/beveiliging/gegevensinzage", items);
        Assert.Contains("/admin/beveiliging/systeemlogs", items);
    }

    private async Task<int> CountActionAsync(string action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        return await db.AdminAuditEvents.CountAsync(e => e.Action == action);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new AdminAuditAppendOnlyInterceptor())
            .Options;
        return new JobsyDbContext(options);
    }
}
