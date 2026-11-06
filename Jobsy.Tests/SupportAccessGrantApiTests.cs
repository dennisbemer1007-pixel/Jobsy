using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class SupportAccessGrantApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public SupportAccessGrantApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.AdminId);
        return client;
    }

    private async Task ClearGrantsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.SupportAccessGrants.RemoveRange(db.SupportAccessGrants);
        await db.SaveChangesAsync();
    }

    private sealed class GrantDto
    {
        public Guid Id { get; set; }
        public Guid AdminUserId { get; set; }
        public Guid? SubjectUserId { get; set; }
        public Guid? SubjectCompanyId { get; set; }
        public string Scope { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
    public async Task Without_grant_admin_users_stay_masked_with_grant_only_subject_unmasked()
    {
        await ClearGrantsAsync();
        var client = AdminClient();
        var before = await client.GetFromJsonAsync<AdminUsersPageDto>("api/admin/users?page=1&pageSize=50", Json);
        Assert.NotNull(before);
        var candidate = before!.Items.First(u => u.Id == _factory.CandidateId);
        var sales = before.Items.First(u => u.Id == _factory.SalesId);
        Assert.Contains("***", candidate.Email);
        Assert.Contains("***", sales.Email);

        var grantResponse = await client.PostAsJsonAsync(
            "api/admin/support-access",
            new
            {
                subjectUserId = _factory.CandidateId,
                subjectCompanyId = (Guid?)null,
                scope = (int)SupportAccessScope.Contact,
                reason = "Candidate cannot log in — ticket FB-99",
                ticketReference = "FB-99",
                durationMinutes = 60
            });
        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
        var grant = await grantResponse.Content.ReadFromJsonAsync<GrantDto>(Json);
        Assert.NotNull(grant);
        Assert.True(grant!.IsActive);

        var after = await client.GetFromJsonAsync<AdminUsersPageDto>("api/admin/users?page=1&pageSize=50", Json);
        Assert.NotNull(after);
        var unmasked = after!.Items.First(u => u.Id == _factory.CandidateId);
        var stillMasked = after.Items.First(u => u.Id == _factory.SalesId);
        Assert.Equal(_factory.CandidateEmail, unmasked.Email);
        Assert.Contains("***", stillMasked.Email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            Assert.True(await db.PersonalDataAccessLogs.AnyAsync(l =>
                l.ActorUserId == _factory.AdminId
                && l.SubjectUserId == _factory.CandidateId
                && l.Action == "reveal"
                && l.SupportAccessGrantId == grant.Id));
        }

        var revoke = await client.PostAsync($"api/admin/support-access/{grant.Id:D}/revoke", null);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var afterRevoke = await client.GetFromJsonAsync<AdminUsersPageDto>("api/admin/users?page=1&pageSize=50", Json);
        Assert.Contains("***", afterRevoke!.Items.First(u => u.Id == _factory.CandidateId).Email);
    }

    [Fact]
    public async Task Grant_expires_and_subject_is_masked_again()
    {
        await ClearGrantsAsync();
        var client = AdminClient();
        var grantResponse = await client.PostAsJsonAsync(
            "api/admin/support-access",
            new
            {
                subjectUserId = _factory.CandidateId,
                scope = (int)SupportAccessScope.Contact,
                reason = "Expiry check for temporary support access",
                durationMinutes = 15
            });
        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
        var grant = await grantResponse.Content.ReadFromJsonAsync<GrantDto>(Json);
        Assert.NotNull(grant);

        var mid = await client.GetFromJsonAsync<AdminUsersPageDto>("api/admin/users?page=1&pageSize=50", Json);
        Assert.Equal(_factory.CandidateEmail, mid!.Items.First(u => u.Id == _factory.CandidateId).Email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.SupportAccessGrants.FirstAsync(g => g.Id == grant!.Id);
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var expired = await client.GetFromJsonAsync<AdminUsersPageDto>("api/admin/users?page=1&pageSize=50", Json);
        Assert.Contains("***", expired!.Items.First(u => u.Id == _factory.CandidateId).Email);
    }

    [Fact]
    public async Task List_support_access_visible_to_admin()
    {
        await ClearGrantsAsync();
        var client = AdminClient();
        var grantResponse = await client.PostAsJsonAsync(
            "api/admin/support-access",
            new
            {
                subjectUserId = _factory.SalesId,
                scope = (int)(SupportAccessScope.Contact | SupportAccessScope.Applications),
                reason = "Four-eyes list must show this grant entry",
                durationMinutes = 60
            });
        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<GrantDto>>(
            "api/admin/support-access?activeOnly=true&take=50", Json);
        Assert.NotNull(list);
        Assert.Contains(list!, g => g.SubjectUserId == _factory.SalesId && g.IsActive);
    }
}
