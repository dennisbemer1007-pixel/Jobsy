using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

[Collection("SequentialApi")]
public class CandidateCareerPlanApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public CandidateCareerPlanApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Career_path_persists_complete_and_uncomplete()
    {
        await ClearCareerAsync();
        var client = Authed();

        var empty = await client.GetAsync("api/me/career-path");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal("null", (await empty.Content.ReadAsStringAsync()).Trim());

        var generated = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = "teamleider-logistiek" });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        var plan = await generated.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Teamleider logistiek", plan.GetProperty("dreamTitle").GetString());
        Assert.False(plan.GetProperty("fromAi").GetBoolean());
        Assert.Equal("nl", plan.GetProperty("planLanguage").GetString());
        Assert.False(string.IsNullOrWhiteSpace(plan.GetProperty("dreamFitBand").GetString()));
        var steps = plan.GetProperty("steps").EnumerateArray().ToList();
        Assert.True(steps.Count >= 3);
        Assert.All(steps, s => Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("id").GetString())));
        Assert.All(steps, s => Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("stepFitBand").GetString())));
        Assert.Contains(steps, s => s.GetProperty("status").GetString() == "Active");

        var saved = await client.GetFromJsonAsync<JsonElement>("api/me/career-path", JsonOpts);
        Assert.Equal(plan.GetProperty("dreamTitle").GetString(), saved.GetProperty("dreamTitle").GetString());
        Assert.Equal(steps.Count, saved.GetProperty("steps").GetArrayLength());

        var active = steps.First(s => s.GetProperty("status").GetString() == "Active");
        var stepKey = active.GetProperty("id").GetString()!;
        Assert.Contains("Complete", active.GetProperty("actionKinds").EnumerateArray().Select(a => a.GetString()));

        var completed = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var afterComplete = await completed.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(
            "Completed",
            afterComplete.GetProperty("steps").EnumerateArray()
                .Single(s => s.GetProperty("id").GetString() == stepKey)
                .GetProperty("status").GetString());

        // Idempotent: completing the same (already Completed) step again is still 200.
        var again = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/complete", null);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        // Completing a step that is not the active one is rejected.
        var farStep = steps.Last().GetProperty("id").GetString()!;
        if (farStep != stepKey)
        {
            var rejected = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(farStep)}/complete", null);
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            var body = await rejected.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
            Assert.Equal("complete_previous_first", body.GetProperty("code").GetString());
        }

        var undone = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/uncomplete", null);
        Assert.Equal(HttpStatusCode.OK, undone.StatusCode);
        Assert.Equal(
            "Active",
            (await undone.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
                .GetProperty("steps").EnumerateArray()
                .Single(s => s.GetProperty("id").GetString() == stepKey)
                .GetProperty("status").GetString());

        // Undo when nothing is completed is rejected.
        var undoRejected = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/uncomplete", null);
        Assert.Equal(HttpStatusCode.Conflict, undoRejected.StatusCode);
        var undoBody = await undoRejected.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("undo_last_first", undoBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Claim_course_stub_returns_410_with_code()
    {
        await ClearCareerAsync();
        var client = Authed();
        await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = "kok" });

        var claim = await client.PostAsJsonAsync("api/me/career-path/courses/claim", new { courseName = "Iets" });
        Assert.Equal((HttpStatusCode)410, claim.StatusCode);
        var body = await claim.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("use_passport_proof", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Invalid_free_text_is_rejected_with_dream_text_invalid()
    {
        await ClearCareerAsync();
        var client = Authed();
        var response = await client.PostAsJsonAsync(
            "api/me/career-path",
            new { freeText = "een twee drie vier vijf zes zeven acht" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("dream_text_invalid", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Regenerate_archives_old_plan_and_carries_over_completed_steps()
    {
        await ClearCareerAsync();
        var client = Authed();

        var first = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = "planner-logistiek" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var plan = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var firstStepKey = plan.GetProperty("steps")[0].GetProperty("id").GetString()!;
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(firstStepKey)}/complete", null)).StatusCode);

        var second = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = "hr-adviseur" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var next = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("HR-adviseur", next.GetProperty("dreamTitle").GetString());

        var archived = await client.GetFromJsonAsync<JsonElement>("api/me/career-path/archived", JsonOpts);
        var archivedList = archived.EnumerateArray().ToList();
        Assert.Contains(archivedList, a => a.GetProperty("dreamTitle").GetString() == "Planner logistiek");
        var archivedEntry = archivedList.First(a => a.GetProperty("dreamTitle").GetString() == "Planner logistiek");
        Assert.True(archivedEntry.GetProperty("completedSteps").GetInt32() >= 1);

        var restoreId = archivedEntry.GetProperty("planId").GetGuid();
        var restore = await client.PostAsync($"api/me/career-path/archived/{restoreId}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        var restored = await restore.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Planner logistiek", restored.GetProperty("dreamTitle").GetString());
        Assert.Contains(
            restored.GetProperty("steps").EnumerateArray(),
            s => s.GetProperty("id").GetString() == firstStepKey && s.GetProperty("status").GetString() == "Completed");
    }

    [Fact]
    public async Task Restore_of_foreign_or_missing_plan_is_404()
    {
        await ClearCareerAsync();
        var client = Authed();
        var response = await client.PostAsync($"api/me/career-path/archived/{Guid.NewGuid()}/restore", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dream_options_search_returns_catalog_matches()
    {
        var client = Authed();
        var options = await client.GetFromJsonAsync<JsonElement>("api/me/career-path/dream-options?q=orderpick", JsonOpts);
        var results = options.GetProperty("results").EnumerateArray().ToList();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.GetProperty("title").GetString() == "Orderpicker");
    }

    [Fact]
    public async Task Generation_guard_reuses_active_plan_for_same_dream_within_window()
    {
        await ClearCareerAsync();
        var client = Authed();
        var first = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = "kok" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstPlan = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        var second = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = "kok" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondPlan = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(
            firstPlan.GetProperty("steps")[0].GetProperty("id").GetString(),
            secondPlan.GetProperty("steps")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task Generation_guard_enforces_daily_limit()
    {
        await ClearCareerAsync();
        var client = Authed();
        string[] keys = ["orderpicker", "productiemedewerker", "heftruckchauffeur", "schoonmaker", "magazijnmedewerker", "zorghulp"];

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = keys[i], force = true });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var sixth = await client.PostAsJsonAsync("api/me/career-path", new { catalogKey = keys[5], force = true });
        Assert.Equal((HttpStatusCode)429, sixth.StatusCode);
        var body = await sixth.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("generation_limit", body.GetProperty("code").GetString());
    }

    private async Task ClearCareerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var plans = await db.CandidateCareerPlans
            .Where(p => p.UserId == _factory.CandidateId)
            .ToListAsync();
        if (plans.Count > 0)
        {
            db.CandidateCareerPlans.RemoveRange(plans);
        }

        var generations = await db.CandidateCareerGenerations
            .Where(g => g.UserId == _factory.CandidateId)
            .ToListAsync();
        if (generations.Count > 0)
        {
            db.CandidateCareerGenerations.RemoveRange(generations);
        }

        await db.SaveChangesAsync();
    }

    private HttpClient Authed()
    {
        return JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
    }
}
