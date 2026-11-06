using System.Net;
using Jobsy.Core.Admin;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class VacancyPurgeAuditTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public VacancyPurgeAuditTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_purge_writes_vacancy_purged_and_removes_the_vacancy()
    {
        var vacancyId = Guid.Parse("e7000000-0000-0000-0000-000000000001");
        var applicationId = Guid.Parse("e7000000-0000-0000-0000-000000000002");
        const string title = "Zomerhulp kassa";
        await SeedLiveVacancyAsync(vacancyId, applicationId, title);

        using var admin = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        using var response = await admin.DeleteAsync($"api/vacancies/{vacancyId}?purgeApplications=true");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.False(await db.Vacancies.AnyAsync(v => v.Id == vacancyId));
        Assert.False(await db.Applications.AnyAsync(a => a.Id == applicationId || a.VacancyId == vacancyId));

        var audit = await db.AdminAuditEvents.SingleAsync(e =>
            e.Action == AdminAuditKeys.VacancyPurged && e.TargetId == vacancyId.ToString("D"));
        Assert.Equal(AdminAuditKeys.TargetTypes.Vacancy, audit.TargetType);
        Assert.Equal($"{title} · Test Vestiging Westland", audit.TargetLabel);
        Assert.Contains("\"applicationCount\":1", audit.DetailsJson, StringComparison.Ordinal);
        Assert.Equal(_factory.AdminId, audit.ActorUserId);
        Assert.Equal("admin", audit.ActorKind);
    }

    [Fact]
    public async Task Employer_purge_is_forbidden_and_writes_no_audit_row()
    {
        var vacancyId = Guid.Parse("e7000000-0000-0000-0000-000000000011");
        var applicationId = Guid.Parse("e7000000-0000-0000-0000-000000000012");
        await SeedLiveVacancyAsync(vacancyId, applicationId, "Blijft staan");

        using var employer = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.EmployerId);
        using var response = await employer.DeleteAsync($"api/vacancies/{vacancyId}?purgeApplications=true");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.True(await db.Vacancies.AnyAsync(v => v.Id == vacancyId));
        Assert.True(await db.Applications.AnyAsync(a => a.Id == applicationId));
        Assert.False(await db.AdminAuditEvents.AnyAsync(e =>
            e.Action == AdminAuditKeys.VacancyPurged && e.TargetId == vacancyId.ToString("D")));
    }

    [Fact]
    public void Purge_audits_before_the_delete()
    {
        var src = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Api/Controllers/VacanciesController.cs"));
        var start = src.IndexOf("public async Task<IActionResult> Delete(", StringComparison.Ordinal);
        var end = src.IndexOf("private async Task DeleteMatchingAsync", start, StringComparison.Ordinal);
        var body = src[start..end];
        var audit = body.IndexOf("WriteVacancyPurgedAuditAsync", StringComparison.Ordinal);
        var delete = body.IndexOf("DeleteMatchingAsync", StringComparison.Ordinal);
        Assert.True(audit >= 0 && delete > audit, "Audit must be written before the vacancy delete.");
        Assert.Contains("ExecuteDeleteAsync", src, StringComparison.Ordinal);
    }

    private async Task SeedLiveVacancyAsync(Guid vacancyId, Guid applicationId, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (await db.Vacancies.AnyAsync(v => v.Id == vacancyId))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = title,
            Description = "Actieve vacature voor de purge-audit.",
            HourlyWage = 14.50m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Active,
            CompanyId = _factory.CompanyId,
            Location = new GeoPoint(52.0, 4.2),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
            MinHoursPerWeek = 8,
            MaxHoursPerWeek = 16,
            FlexibleTimes = true
        });
        db.Applications.Add(new Application
        {
            Id = applicationId,
            VacancyId = vacancyId,
            CandidateName = "Kandidaat Test",
            CandidateEmail = "purge-kandidaat@jobsy.local",
            PreferredTransport = "Fiets",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();
    }

    private static string FindRoot()
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

public class EmployerRun7CopyTests
{
    [Fact]
    public void Talent_pool_shows_direct_and_hours_when_no_day_parts_are_set()
    {
        var direct = LobsyCvModelFactory.FormatTalentPoolAvailability(
            null, flexibleTimes: true, 8, 40, ["direct"]);
        Assert.Equal("Per direct · 8–40 uur", direct);

        var slots = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ma"] = ["Ochtend"]
        };
        Assert.Equal(
            "Ma: Ochtend",
            LobsyCvModelFactory.FormatTalentPoolAvailability(slots, false, 8, 16, ["direct"]));

        Assert.Null(LobsyCvModelFactory.FormatTalentPoolAvailability(null, false, null, null, null));
    }

    [Fact]
    public void Token_kind_filter_uses_dutch_labels_in_every_locale()
    {
        var page = File.ReadAllText(Path.Combine(FindRoot(), "Jobsy.Web/Components/Pages/Werkgever/TokensMutaties.razor"));
        Assert.Contains("WgTok.Kind.Spend", page, StringComparison.Ordinal);
        Assert.Contains("WgTok.Kind.Purchase", page, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"token-kind-filter\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain(">Spend<", page, StringComparison.Ordinal);
        Assert.Contains("KindLabel(log.Kind)", page, StringComparison.Ordinal);

        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        var pl = new Dictionary<string, string>(StringComparer.Ordinal);
        var ro = new Dictionary<string, string>(StringComparer.Ordinal);
        var ar = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsWerkgever.MergeAll(nl, en, pl, ro, ar);
        Assert.Equal("Besteed", nl["WgTok.Kind.Spend"]);
        Assert.Equal("Spent", en["WgTok.Kind.Spend"]);
        Assert.Equal("Wydane", pl["WgTok.Kind.Spend"]);
        Assert.Equal("Cheltuit", ro["WgTok.Kind.Spend"]);
        Assert.Equal("مُنفَق", ar["WgTok.Kind.Spend"]);
        Assert.Equal("Gekocht", nl["WgTok.Kind.Purchase"]);
        Assert.Equal("Verdeeld", nl["WgTok.Kind.Allocation"]);
        Assert.Equal("Toegekend", nl["WgTok.Kind.Grant"]);
        Assert.Equal("Coulance", nl["WgTok.Kind.Goodwill"]);
        Assert.Equal("Goodwill", en["WgTok.Kind.Goodwill"]);
        Assert.Equal("Transaction type", en["WgTok.Filter.Kind"]);
    }

    [Fact]
    public void User_admin_errors_go_through_Errors_Describe()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRoot(), "Jobsy.Web/Components/Admin/Sections/UsersAdminSection.razor"));
        Assert.DoesNotContain("ex.Message", src, StringComparison.Ordinal);
        Assert.Contains("Errors.Describe(ex)", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_coach_hint_has_a_read_aloud_button()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRoot(), "Jobsy.Web/Components/Leerling/LeerlingCoachCue.razor"));
        Assert.Contains("ll-coach-inline", src, StringComparison.Ordinal);
        Assert.Contains("ReadAloudButton", src, StringComparison.Ordinal);
        Assert.Contains("ReadAloud.Play", src, StringComparison.Ordinal);
    }

    private static string FindRoot()
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
