using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

public sealed class BeheerTodosApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly RoleFunctionalWebAppFactory _factory;

    public BeheerTodosApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Guest_and_candidate_forbidden_admin_can_crud_and_audit()
    {
        var guest = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("api/admin/beheer-todos")).StatusCode);

        var candidate = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
        Assert.Equal(HttpStatusCode.Forbidden, (await candidate.GetAsync("api/admin/beheer-todos")).StatusCode);

        var admin = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);

        var create = await admin.PostAsJsonAsync(
            "api/admin/beheer-todos",
            new
            {
                title = "Test taak",
                body = "Regel 1\nRegel 2",
                dueDate = (DateOnly?)null,
                priority = BeheerTodoPriority.Normaal,
                status = BeheerTodoStatus.Open,
                category = "Test",
            });
        if (!create.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"create {(int)create.StatusCode}: {await create.Content.ReadAsStringAsync()}");
        }
        var created = await create.Content.ReadFromJsonAsync<BeheerTodoDto>(Json);
        Assert.NotNull(created);
        Assert.Equal("Test taak", created!.Title);

        var list = await admin.GetFromJsonAsync<List<BeheerTodoDto>>("api/admin/beheer-todos", Json);
        Assert.NotNull(list);
        Assert.Contains(list!, t => t.Id == created.Id);

        var update = await admin.PutAsJsonAsync(
            $"api/admin/beheer-todos/{created.Id}",
            new
            {
                title = "Test taak bijgewerkt",
                body = created.Body,
                dueDate = new DateOnly(2026, 12, 1),
                priority = BeheerTodoPriority.Hoog,
                status = BeheerTodoStatus.Bezig,
                category = "Test",
            });
        if (!update.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"update {(int)update.StatusCode}: {await update.Content.ReadAsStringAsync()}");
        }

        var markDone = await admin.PostAsync($"api/admin/beheer-todos/{created.Id}/mark-done", null);
        if (!markDone.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"markDone {(int)markDone.StatusCode}: {await markDone.Content.ReadAsStringAsync()}");
        }
        var done = await markDone.Content.ReadFromJsonAsync<BeheerTodoDto>(Json);
        Assert.Equal(BeheerTodoStatus.Klaar, done!.Status);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var audits = await db.AdminAuditEvents.AsNoTracking()
                .Where(e => e.TargetId == created.Id.ToString("D"))
                .Select(e => e.Action)
                .ToListAsync();
            Assert.Contains(AdminAuditKeys.BeheerTodoCreate, audits);
            Assert.Contains(AdminAuditKeys.BeheerTodoUpdate, audits);
            Assert.Contains(AdminAuditKeys.BeheerTodoMarkDone, audits);
        }

        var delete = await admin.DeleteAsync($"api/admin/beheer-todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"api/admin/beheer-todos/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Migration_seed_includes_cotan_todo_idempotent()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.BeheerTodos.AsNoTracking()
            .FirstOrDefaultAsync(t => t.SeedKey == "seed-cotan-nip-mail");
        Assert.NotNull(row);
        Assert.Equal("COTAN", row!.Category);
        Assert.Equal(BeheerTodoPriority.Hoog, row.Priority);
        Assert.Contains("directie@nip.nl", row.Body, StringComparison.Ordinal);
    }
}
