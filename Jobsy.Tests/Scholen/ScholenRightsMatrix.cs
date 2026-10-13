using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Data-driven §R matrix scaffold. 01 fills admin + non-admin 403 rows.
/// Later files append cases.
/// </summary>
public class ScholenRightsMatrix : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ScholenRightsMatrix(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_can_list_schools_candidate_and_employer_forbidden()
    {
        using var admin = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/admin/schools")).StatusCode);

        using var candidate = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
        Assert.Equal(HttpStatusCode.Forbidden, (await candidate.GetAsync("api/admin/schools")).StatusCode);

        using var employer = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.EmployerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await employer.GetAsync("api/admin/schools")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_school_record_agreement_and_invite()
    {
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        var create = await client.PostAsJsonAsync("api/admin/schools", new CreateSchoolRequest(
            "Voorbeeldcollege",
            "Naaldwijk",
            "00XX",
            ["voorbeeldcollege.nl"],
            true));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var school = await create.Content.ReadFromJsonAsync<SchoolDetailDto>(Json);
        Assert.NotNull(school);

        var agreement = await client.PutAsJsonAsync(
            $"api/admin/schools/{school!.Id}/processor-agreement",
            new RecordProcessorAgreementRequest(DateOnly.FromDateTime(DateTime.UtcNow), "1.0"));
        Assert.Equal(HttpStatusCode.OK, agreement.StatusCode);

        var invite = await client.PostAsJsonAsync(
            $"api/admin/schools/{school.Id}/invite-admin",
            new InviteSchoolAdminRequest("J. Visser", "jvisser@voorbeeldcollege.nl"));
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);

        var badDomain = await client.PostAsJsonAsync(
            $"api/admin/schools/{school.Id}/invite-admin",
            new InviteSchoolAdminRequest("X", "x@other.nl"));
        Assert.Equal(HttpStatusCode.BadRequest, badDomain.StatusCode);
        var body = await badDomain.Content.ReadAsStringAsync();
        Assert.Contains("email_domain_not_allowed", body, StringComparison.Ordinal);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "Scholen.Audit" && l.Message.Contains("school.create", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Schools_feature_gate_blocks_school_api_when_disabled()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
            if (row is null)
            {
                row = new PlatformFeatureSettings
                {
                    Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    SchoolsEnabled = false
                };
                db.PlatformFeatureSettings.Add(row);
            }
            else
            {
                row.SchoolsEnabled = false;
            }

            await db.SaveChangesAsync();
        }

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        var response = await client.GetAsync("api/school/does-not-exist-yet");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("feature_disabled", body, StringComparison.Ordinal);
    }
}
