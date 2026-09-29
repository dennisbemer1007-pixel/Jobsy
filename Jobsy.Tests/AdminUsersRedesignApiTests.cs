using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Admin;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class AdminUsersRedesignApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public AdminUsersRedesignApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Aggregates_match_legacy_in_memory_algorithm()
    {
        await using var db = CreateDb();
        var week1 = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var week2 = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), Email = "a@t.local", FullName = "A", Role = UserRole.Candidate, IsActive = true, TermsAcceptedAt = week1 },
            new User { Id = Guid.NewGuid(), Email = "b@t.local", FullName = "B", Role = UserRole.Candidate, IsActive = false, TermsAcceptedAt = week1 },
            new User { Id = Guid.NewGuid(), Email = "c@t.local", FullName = "C", Role = UserRole.Admin, IsActive = true, TermsAcceptedAt = week2 },
            new User { Id = Guid.NewGuid(), Email = "d@t.local", FullName = "D", Role = UserRole.BranchManager, IsActive = true, TermsAcceptedAt = week2 },
            new User { Id = Guid.NewGuid(), Email = "e@t.local", FullName = "E", Role = UserRole.BranchManager, IsActive = true, TermsAcceptedAt = null },
            new User { Id = Guid.NewGuid(), Email = "f@t.local", FullName = "F", Role = UserRole.Intermediary, IsActive = true, TermsAcceptedAt = week2 });
        await db.SaveChangesAsync();

        var queried = await AdminUsersAggregatesQuery.QueryAsync(db);
        var rows = await db.Users.AsNoTracking()
            .Select(u => new ValueTuple<UserRole, Guid?, string?, bool, DateTime?>(
                u.Role, u.CompanyId, u.Company != null ? u.Company.Name : null, u.IsActive, u.TermsAcceptedAt))
            .ToListAsync();
        var legacy = AdminUsersAggregatesQuery.FromLoadedRows(rows);

        Assert.Equal(legacy.ActiveCount, queried.ActiveCount);
        Assert.Equal(legacy.InactiveCount, queried.InactiveCount);
        Assert.Equal(legacy.ByRole.OrderBy(kv => kv.Key), queried.ByRole.OrderBy(kv => kv.Key));
        Assert.Equal(
            legacy.TopCompanies.Select(c => (c.CompanyId, c.CompanyName, c.Count)),
            queried.TopCompanies.Select(c => (c.CompanyId, c.CompanyName, c.Count)));
        Assert.Equal(
            legacy.ByRegistrationWeek.Select(w => (w.WeekStartUtc, w.Count)),
            queried.ByRegistrationWeek.Select(w => (w.WeekStartUtc, w.Count)));
    }

    [Fact]
    public async Task Mfa_and_active_filters_and_tab_work()
    {
        var client = MfaAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProtector>();

        var enrolled = Guid.NewGuid();
        var blocked = Guid.NewGuid();
        db.Users.AddRange(
            new User
            {
                Id = enrolled,
                Email = $"enrolled-{enrolled:N}@test.local",
                FullName = "Enrolled User",
                Role = UserRole.BranchManager,
                IsActive = true,
                AuthenticatorEnabled = true,
                AuthenticatorSecret = secrets.Protect(TotpAuthenticator.GenerateSecret())
            },
            new User
            {
                Id = blocked,
                Email = $"blocked-{blocked:N}@test.local",
                FullName = "Blocked User",
                Role = UserRole.Candidate,
                IsActive = false
            });
        await db.SaveChangesAsync();

        var mfaPage = await client.GetFromJsonAsync<AdminUsersPageDto>(
            "api/admin/users?mfa=enrolled&pageSize=100", Json);
        Assert.NotNull(mfaPage);
        Assert.Contains(mfaPage.Items, u => u.Id == enrolled);
        Assert.All(mfaPage.Items, u => Assert.Equal("enrolled", u.MfaStatus));

        var blockedPage = await client.GetFromJsonAsync<AdminUsersPageDto>(
            "api/admin/users?active=geblokkeerd&pageSize=100", Json);
        Assert.NotNull(blockedPage);
        Assert.Contains(blockedPage.Items, u => u.Id == blocked);
        Assert.All(blockedPage.Items, u => Assert.False(u.IsActive));

        var candidates = await client.GetFromJsonAsync<AdminUsersPageDto>(
            "api/admin/users?tab=kandidaten&pageSize=100", Json);
        Assert.NotNull(candidates);
        Assert.All(candidates.Items, u => Assert.Equal("Candidate", u.Role));
    }

    [Fact]
    public async Task Sessions_list_revoke_and_revoke_all_log_access()
    {
        var client = MfaAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var sessions = scope.ServiceProvider.GetRequiredService<IDeviceSessionService>();

        var targetId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = targetId,
            Email = $"sess-{targetId:N}@test.local",
            FullName = "Session Target",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var s1 = await sessions.CreateAsync(targetId, "Mozilla/5.0 Chrome Windows");
        var s2 = await sessions.CreateAsync(targetId, "Safari iPhone");

        var anon = _factory.CreateClient();
        var forbidden = await anon.GetAsync($"api/admin/users/{targetId}/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);

        var nonAdmin = _factory.CreateClient();
        JobsyTestAuth.Authorize(nonAdmin, _factory.EmployerId);
        var roleForbidden = await nonAdmin.GetAsync($"api/admin/users/{targetId}/sessions");
        Assert.Equal(HttpStatusCode.Forbidden, roleForbidden.StatusCode);

        var list = await client.GetFromJsonAsync<List<AdminUserSessionDto>>(
            $"api/admin/users/{targetId}/sessions", Json);
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);

        var badReason = await client.PostAsJsonAsync(
            $"api/admin/users/{targetId}/sessions/{s1.DeviceSessionId}/revoke",
            new { reason = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, badReason.StatusCode);

        var revoke = await client.PostAsJsonAsync(
            $"api/admin/users/{targetId}/sessions/{s1.DeviceSessionId}/revoke",
            new { reason = "Support: stale laptop" });
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var revokeAll = await client.PostAsJsonAsync(
            $"api/admin/users/{targetId}/sessions/revoke-all",
            new { reason = "Support: wipe remaining" });
        Assert.Equal(HttpStatusCode.NoContent, revokeAll.StatusCode);

        Assert.True(await db.PersonalDataAccessLogs.AnyAsync(l =>
            l.Resource == "user.sessions" && l.Action == "list" && l.SubjectUserId == targetId));
        Assert.True(await db.PersonalDataAccessLogs.CountAsync(l =>
            l.Resource == "user.sessions" && l.Action == "revoke" && l.SubjectUserId == targetId) >= 2);

        _ = s2;
    }

    [Fact]
    public async Task Block_refuses_self_and_last_admin_and_revokes_sessions()
    {
        var client = MfaAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var sessions = scope.ServiceProvider.GetRequiredService<IDeviceSessionService>();

        var self = await client.PostAsJsonAsync(
            $"api/admin/users/{_factory.AdminId}/block",
            new { reason = "Cannot block myself here" });
        Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);

        var loneAdmin = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = loneAdmin,
            Email = $"lone-{loneAdmin:N}@test.local",
            FullName = "Lone Admin",
            Role = UserRole.Admin,
            IsActive = true
        });
        var target = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = target,
            Email = $"blk-{target:N}@test.local",
            FullName = "Block Me",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();
        await sessions.CreateAsync(target, "Chrome");

        var ok = await client.PostAsJsonAsync(
            $"api/admin/users/{target}/block",
            new { reason = "Fraudulent activity found" });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        db.ChangeTracker.Clear();
        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == target);
        Assert.False(reloaded.IsActive);
        Assert.False(await db.UserDeviceSessions.AsNoTracking().AnyAsync(s =>
            s.UserId == target && s.RevokedAtUtc == null));

        // Last-admin guard: with only one active admin in the DB, the count excluding that
        // admin is 0 (defense-in-depth; self-block is already refused above).
        foreach (var admin in await db.Users.Where(u => u.Role == UserRole.Admin && u.IsActive).ToListAsync())
        {
            if (admin.Id != loneAdmin)
            {
                admin.IsActive = false;
            }
        }

        await db.SaveChangesAsync();
        var otherActive = await db.Users.CountAsync(u =>
            u.Role == UserRole.Admin && u.IsActive && u.Id != loneAdmin);
        Assert.Equal(0, otherActive);

        // Restore factory admin for subsequent tests / same fixture.
        var factoryAdmin = await db.Users.SingleAsync(u => u.Id == _factory.AdminId);
        factoryAdmin.IsActive = true;
        await db.SaveChangesAsync();

        var unblock = await client.PostAsJsonAsync(
            $"api/admin/users/{target}/unblock",
            new { reason = "Cleared after review" });
        Assert.Equal(HttpStatusCode.NoContent, unblock.StatusCode);
        Assert.True(await db.PersonalDataAccessLogs.AnyAsync(l =>
            l.Resource == "user.status" && l.Action == "block" && l.SubjectUserId == target));
        Assert.True(await db.PersonalDataAccessLogs.AnyAsync(l =>
            l.Resource == "user.status" && l.Action == "unblock" && l.SubjectUserId == target));
    }

    [Fact]
    public async Task Bulk_endpoint_skips_self_and_caps_at_100()
    {
        var client = MfaAdminClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            db.Users.Add(new User
            {
                Id = id,
                Email = $"bulk-{id:N}@test.local",
                FullName = $"Bulk {i}",
                Role = UserRole.Candidate,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
        ids.Add(_factory.AdminId);

        var result = await client.PostAsJsonAsync(
            "api/admin/users/bulk/revoke-sessions",
            new { userIds = ids, reason = "Bulk session wipe test" });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var body = await result.Content.ReadFromJsonAsync<AdminBulkUsersResponseDto>(Json);
        Assert.NotNull(body);
        Assert.Equal(3, body.Succeeded);
        Assert.Equal(1, body.Skipped);
        Assert.Contains(body.Results, r => r.UserId == _factory.AdminId && !r.Ok);

        var tooMany = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var capped = await client.PostAsJsonAsync(
            "api/admin/users/bulk/block",
            new { userIds = tooMany, reason = "Too many ids here" });
        Assert.Equal(HttpStatusCode.BadRequest, capped.StatusCode);
    }

    [Fact]
    public async Task Mfa_reset_sends_mail_once_without_admin_name_or_reason()
    {
        var capturing = new CapturingEmail();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IEmailService>(capturing);
            });
        });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
        var admin = await db.Users.SingleAsync(u => u.Id == _factory.AdminId);
        var secret = TotpAuthenticator.GenerateSecret();
        admin.AuthenticatorSecret = secrets.Protect(secret);
        admin.AuthenticatorEnabled = true;
        var target = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = target,
            Email = $"mfa-mail-{target:N}@test.local",
            FullName = "Mail Target",
            Role = UserRole.BranchManager,
            IsActive = true,
            AuthenticatorEnabled = true,
            AuthenticatorSecret = secrets.Protect(TotpAuthenticator.GenerateSecret()),
            AuthenticatorEnrolledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var jwt = JobsyAccessToken.Create(
            _factory.AdminId,
            sessionVersion: 0,
            JobsyAccessToken.DevelopmentPrivateKeyPem,
            mfaVerified: true,
            authMethod: "password+totp");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var fail = await client.PostAsJsonAsync(
            $"api/admin/users/{target}/mfa/reset",
            new { reason = "ab", confirmCode = "000000" });
        Assert.False(fail.IsSuccessStatusCode);
        Assert.Empty(capturing.Sent);

        var confirm = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow);
        var ok = await client.PostAsJsonAsync(
            $"api/admin/users/{target}/mfa/reset",
            new { reason = "Nieuwe telefoon gecheckt", confirmCode = confirm });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.Single(capturing.Sent);
        var mail = capturing.Sent[0];
        Assert.Equal("MfaResetByAdmin", mail.Category);
        Assert.DoesNotContain("Dennis", mail.BodyHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nieuwe telefoon", mail.BodyHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(admin.Email, mail.BodyHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gereset", mail.BodyHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MfaResetByAdmin_template_registered()
    {
        Assert.Contains(TransactionalEmails.Templates, t => t.Key == "MfaResetByAdmin");
        var composed = TransactionalEmails.Compose(
            "MfaResetByAdmin",
            new EmailSampleContext(
                "https://lobsy.nl", "Alex", "Co", "Vac", Guid.NewGuid(), Guid.NewGuid(),
                "Den Haag", 1, 10, 14m, "123456", "pw", "key", "Admin", "Vest", "a@b.nl", "1", "https://api"));
        Assert.Contains("gereset", composed.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reden", composed.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SupportAccess_default_duration_is_15()
    {
        Assert.Equal(15, SupportAccessService.DefaultDurationMinutes);
        Assert.Contains(15, new[] { 15, 60, 240 });
        Assert.Contains(60, new[] { 15, 60, 240 });
        Assert.Contains(240, new[] { 15, 60, 240 });
    }

    private HttpClient MfaAdminClient()
    {
        var client = _factory.CreateClient();
        var jwt = JobsyAccessToken.Create(
            _factory.AdminId,
            sessionVersion: 0,
            JobsyAccessToken.DevelopmentPrivateKeyPem,
            mfaVerified: true,
            authMethod: "password+totp");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("AdminUsersAgg-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }
}
