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
    public async Task Career_path_persists_complete_uncomplete_and_claim_auto()
    {
        await ClearCareerPlansAsync();
        var client = Authed();

        var empty = await client.GetAsync("api/me/career-path");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal("null", (await empty.Content.ReadAsStringAsync()).Trim());

        var generated = await client.PostAsJsonAsync("api/me/career-path", new { dreamTitle = "Teamleider logistiek" });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        var plan = await generated.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Teamleider logistiek", plan.GetProperty("dreamTitle").GetString());
        var steps = plan.GetProperty("steps").EnumerateArray().ToList();
        Assert.True(steps.Count >= 3);
        Assert.All(steps, s => Assert.False(string.IsNullOrWhiteSpace(s.GetProperty("id").GetString())));
        Assert.Contains(steps, s => s.GetProperty("status").GetString() == "Active");

        var saved = await client.GetFromJsonAsync<JsonElement>("api/me/career-path", JsonOpts);
        Assert.Equal(plan.GetProperty("dreamTitle").GetString(), saved.GetProperty("dreamTitle").GetString());
        Assert.Equal(steps.Count, saved.GetProperty("steps").GetArrayLength());

        // Manual complete + undo on the active step (may have no courses).
        var active = steps.First(s => s.GetProperty("status").GetString() == "Active");
        var stepKey = active.GetProperty("id").GetString()!;
        var completed = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var afterComplete = await completed.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(
            "Completed",
            afterComplete.GetProperty("steps").EnumerateArray()
                .Single(s => s.GetProperty("id").GetString() == stepKey)
                .GetProperty("status").GetString());

        var undone = await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(stepKey)}/uncomplete", null);
        Assert.Equal(HttpStatusCode.OK, undone.StatusCode);
        Assert.Equal(
            "Active",
            (await undone.Content.ReadFromJsonAsync<JsonElement>(JsonOpts))
                .GetProperty("steps").EnumerateArray()
                .Single(s => s.GetProperty("id").GetString() == stepKey)
                .GetProperty("status").GetString());

        // Fresh plan for claim/auto — ManualUndo must not block a clean step.
        await ClearCareerPlansAsync();
        var fresh = await client.PostAsJsonAsync("api/me/career-path", new { dreamTitle = "Teamleider logistiek" });
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
        var freshPlan = await fresh.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var courseStep = freshPlan.GetProperty("steps").EnumerateArray()
            .First(s =>
                s.TryGetProperty("courses", out var courses)
                && courses.ValueKind == JsonValueKind.Array
                && courses.GetArrayLength() > 0);
        var courseStepKey = courseStep.GetProperty("id").GetString()!;
        var courseNames = courseStep.GetProperty("courses").EnumerateArray()
            .Select(c => c.GetString()!)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
        Assert.NotEmpty(courseNames);

        JsonElement afterClaim = default;
        foreach (var courseName in courseNames)
        {
            var claim = await client.PostAsJsonAsync("api/me/career-path/courses/claim", new { courseName });
            Assert.Equal(HttpStatusCode.OK, claim.StatusCode);
            afterClaim = await claim.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        }

        var claimedStep = afterClaim.GetProperty("steps").EnumerateArray()
            .Single(s => s.GetProperty("id").GetString() == courseStepKey);
        Assert.All(
            claimedStep.GetProperty("courseStatuses").EnumerateArray(),
            c => Assert.True(c.GetProperty("onProfile").GetBoolean()));
        Assert.Equal("Completed", claimedStep.GetProperty("status").GetString());
        Assert.Equal(100, claimedStep.GetProperty("stepMatchPercent").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var user = await db.Users.FindAsync(_factory.CandidateId);
        Assert.NotNull(user);
        Assert.Contains(courseNames[0], user!.PreferencesJson ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Regenerate_replaces_plan_and_clears_progress()
    {
        await ClearCareerPlansAsync();
        var client = Authed();
        var first = await client.PostAsJsonAsync("api/me/career-path", new { dreamTitle = "Planner" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var plan = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var key = plan.GetProperty("steps")[0].GetProperty("id").GetString()!;
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync($"api/me/career-path/steps/{Uri.EscapeDataString(key)}/complete", null)).StatusCode);

        var second = await client.PostAsJsonAsync("api/me/career-path", new { dreamTitle = "HR-adviseur" });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var next = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("HR-adviseur", next.GetProperty("dreamTitle").GetString());
        Assert.DoesNotContain(
            next.GetProperty("steps").EnumerateArray(),
            s => s.GetProperty("status").GetString() == "Completed");
    }

    private async Task ClearCareerPlansAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var plans = await db.CandidateCareerPlans
            .Where(p => p.UserId == _factory.CandidateId)
            .ToListAsync();
        if (plans.Count == 0)
        {
            return;
        }

        db.CandidateCareerPlans.RemoveRange(plans);
        await db.SaveChangesAsync();
    }

    private HttpClient Authed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", _factory.CandidateEmail);
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", RoleFunctionalWebAppFactory.DevSecret);
        return client;
    }
}
