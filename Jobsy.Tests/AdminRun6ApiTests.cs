using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Admin;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public class AdminRun6ApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public AdminRun6ApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Turning_employers_off_writes_an_audit_row_with_the_reason()
    {
        var client = AdminClient();
        var on = await client.PutAsJsonAsync("api/settings/platform-features", new { employersEnabled = true, reason = "run6 employers on" });
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        var off = await client.PutAsJsonAsync("api/settings/platform-features", new { employersEnabled = false, reason = "run6 werkgevers uit" });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.AdminAuditEvents
            .Where(e => e.Action == AdminAuditKeys.SettingsPlatformUpdate && e.TargetLabel == "EmployersEnabled")
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstAsync();
        Assert.Equal("run6 werkgevers uit", row.Reason);
        Assert.Contains("\"to\":\"False\"", row.DetailsJson ?? "", StringComparison.Ordinal);

        var flags = await client.GetStringAsync("api/settings/feature-flags");
        Assert.DoesNotContain("whatsAppRemindersConfigured", flags, StringComparison.OrdinalIgnoreCase);
        var features = await client.GetStringAsync("api/settings/platform-features");
        Assert.Contains("whatsAppRemindersConfigured", features, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Vacancy_like_older_than_a_week_is_in_the_all_time_drilldown()
    {
        Guid likeId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var like = await db.VacancyLikes.FirstOrDefaultAsync(l => l.VacancyId == _factory.VacancyId && l.UserId == _factory.CandidateId);
            if (like is null)
            {
                like = new VacancyLike
                {
                    Id = Guid.NewGuid(),
                    VacancyId = _factory.VacancyId,
                    UserId = _factory.CandidateId,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                };
                db.VacancyLikes.Add(like);
            }
            else
            {
                like.CreatedAt = DateTime.UtcNow.AddDays(-10);
            }

            await db.SaveChangesAsync();
            likeId = like.Id;
        }

        var client = AdminClient();
        var week = await client.GetFromJsonAsync<List<MetricDrilldownItemDto>>(
            $"api/admin/vacancies/{_factory.VacancyId:D}/metrics/likes?period=week", Json) ?? [];
        var all = await client.GetFromJsonAsync<List<MetricDrilldownItemDto>>(
            $"api/admin/vacancies/{_factory.VacancyId:D}/metrics/likes?period=all", Json) ?? [];
        var list = await client.GetFromJsonAsync<List<AdminVacancyDetailDto>>("api/admin/vacancies", Json) ?? [];
        var vacancy = list.Single(v => v.Id == _factory.VacancyId);

        Assert.DoesNotContain(week, i => i.Id == likeId);
        Assert.Contains(all, i => i.Id == likeId);
        Assert.Equal(vacancy.LikeCount, all.Count);
    }

    [Fact]
    public async Task Misuse_report_is_listed_masked_and_audited_when_handled()
    {
        var reportId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.ReferenceMisuseReports.Add(new ReferenceMisuseReport
            {
                Id = reportId,
                UserId = _factory.CandidateId,
                ReferenceConfirmationId = Guid.NewGuid(),
                Message = "Dit verzoek klopt niet",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = AdminClient();
        var todo = await client.GetFromJsonAsync<AdminTodoResponse>("api/admin/todo", Json);
        Assert.NotNull(todo);
        Assert.Contains(todo!.Items, i => i.Key == "reference-misuse" && i.Href == "/admin/beveiliging/referent-misbruik");

        var raw = await client.GetStringAsync("api/admin/reference-misuse");
        Assert.DoesNotContain(_factory.CandidateEmail, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", raw, StringComparison.Ordinal);
        var rows = JsonSerializer.Deserialize<List<ReferenceMisuseItemDto>>(raw, Json) ?? [];
        var row = rows.Single(r => r.Id == reportId);
        Assert.Equal("Kandidaat T.", row.CandidateMasked);
        Assert.Equal("open", row.Status);
        Assert.Equal("Dit verzoek klopt niet", row.Message);

        var handled = await client.PostAsync($"api/admin/reference-misuse/{reportId:D}/handled", content: null);
        Assert.Equal(HttpStatusCode.NoContent, handled.StatusCode);

        using var verify = _factory.Services.CreateScope();
        var auditDb = verify.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var audit = await auditDb.AdminAuditEvents.SingleAsync(e => e.Action == AdminAuditKeys.ReferenceMisuseHandled && e.TargetId == reportId.ToString("D"));
        Assert.Equal("Melding misbruik referent", audit.TargetLabel);

        var after = await client.GetFromJsonAsync<AdminTodoResponse>("api/admin/todo", Json);
        Assert.DoesNotContain(after!.Items, i => i.Key == "reference-misuse");
    }

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.AdminId);
        return client;
    }
}
