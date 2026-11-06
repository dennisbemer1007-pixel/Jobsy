using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 04 §1: the candidate contact-request API. Nothing is shared without an explicit
/// confirmation, the share preview equals what the employer receives, and declines carry a reason.
/// </summary>
[Collection("SequentialApi")]
public class TalentContacts04ApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public TalentContacts04ApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Accept_needs_confirmed_share_and_then_shares_the_contact()
    {
        var id = await SeedRequestAsync(TalentContactStatus.Pending);
        var client = CandidateClient();

        var unconfirmed = await client.PostAsJsonAsync(
            $"api/me/talent-contacts/{id}/respond",
            new { accept = true });
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);
        Assert.Equal("confirm_share_required", await CodeAsync(unconfirmed));

        Assert.Equal(
            TalentContactStatus.Pending,
            await StatusAsync(id));

        var confirmed = await client.PostAsJsonAsync(
            $"api/me/talent-contacts/{id}/respond",
            new { accept = true, confirmedShare = true });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        var dto = await confirmed.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("ContactShared", dto.GetProperty("status").GetString());
        Assert.Equal(TalentContactStatus.ContactShared, await StatusAsync(id));
    }

    [Fact]
    public async Task Share_preview_shows_exactly_what_the_employer_receives()
    {
        var id = await SeedRequestAsync(TalentContactStatus.Pending);
        var client = CandidateClient();

        var preview = await client.GetAsync($"api/me/talent-contacts/{id}/share-preview");
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var body = await preview.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var name = Text(body, "name");
        var email = Text(body, "email");
        var phone = Text(body, "phone");
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.False(string.IsNullOrWhiteSpace(email));
        Assert.False(string.IsNullOrWhiteSpace(Text(body, "companyName")));

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                $"api/me/talent-contacts/{id}/respond",
                new { accept = true, confirmedShare = true })).StatusCode);

        // The revealed employer row must carry the very same fields (one PII source).
        var employer = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.EmployerId);
        var requests = await employer.GetFromJsonAsync<JsonElement>("api/employer/talent/requests", JsonOpts);
        var row = requests.EnumerateArray().Single(r => r.GetProperty("id").GetGuid() == id);
        Assert.True(row.GetProperty("piiRevealed").GetBoolean());
        Assert.Equal(name, Text(row, "candidateFullName"));
        Assert.Equal(email, Text(row, "candidateEmail"));
        Assert.Equal(phone, Text(row, "candidatePhone"));
    }

    private static string? Text(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    [Fact]
    public async Task Share_preview_of_a_foreign_request_is_not_found()
    {
        var id = await SeedRequestAsync(TalentContactStatus.Pending, forOtherCandidate: true);
        var response = await CandidateClient().GetAsync($"api/me/talent-contacts/{id}/share-preview");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", await CodeAsync(response));
    }

    [Fact]
    public async Task Declines_store_the_reason_and_the_employer_dto_exposes_it()
    {
        var notInterested = await SeedRequestAsync(TalentContactStatus.Pending);
        var alreadyPlaced = await SeedRequestAsync(TalentContactStatus.RefundEligible);
        var client = CandidateClient();

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                $"api/me/talent-contacts/{notInterested}/respond",
                new { accept = false })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                $"api/me/talent-contacts/{alreadyPlaced}/respond",
                new { accept = false, alreadyPlaced = true })).StatusCode);

        Assert.Equal(TalentContactDeclineReasons.NotInterested, await ReasonAsync(notInterested));
        Assert.Equal(TalentContactDeclineReasons.AlreadyPlaced, await ReasonAsync(alreadyPlaced));

        var inbox = await client.GetFromJsonAsync<JsonElement>("api/me/talent-contacts", JsonOpts);
        Assert.Equal(
            TalentContactDeclineReasons.AlreadyPlaced,
            inbox.EnumerateArray()
                .Single(r => r.GetProperty("id").GetGuid() == alreadyPlaced)
                .GetProperty("candidateDeclineReason").GetString());

        var employer = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.EmployerId);
        var requests = await employer.GetFromJsonAsync<JsonElement>("api/employer/talent/requests", JsonOpts);
        Assert.Equal(
            TalentContactDeclineReasons.NotInterested,
            requests.EnumerateArray()
                .Single(r => r.GetProperty("id").GetGuid() == notInterested)
                .GetProperty("candidateDeclineReason").GetString());
    }

    [Fact]
    public async Task A_withdrawn_request_cannot_be_answered()
    {
        var id = await SeedRequestAsync(TalentContactStatus.WithdrawnRefunded);
        var response = await CandidateClient().PostAsJsonAsync(
            $"api/me/talent-contacts/{id}/respond",
            new { accept = false });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("cannot_respond", await CodeAsync(response));
    }

    [Fact]
    public async Task With_the_employers_gate_off_the_candidate_inbox_is_gone()
    {
        var id = await SeedRequestAsync(TalentContactStatus.Pending);
        await SetEmployersAsync(false);
        try
        {
            var client = CandidateClient();
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await client.GetAsync("api/me/talent-contacts")).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await client.GetAsync($"api/me/talent-contacts/{id}/share-preview")).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await client.PostAsJsonAsync(
                    $"api/me/talent-contacts/{id}/respond",
                    new { accept = true, confirmedShare = true })).StatusCode);
        }
        finally
        {
            await SetEmployersAsync(true);
        }

        Assert.Equal(HttpStatusCode.OK, (await CandidateClient().GetAsync("api/me/talent-contacts")).StatusCode);
    }

    private async Task SetEmployersAsync(bool enabled)
    {
        using var scope = _factory.Services.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
        await features.UpdateAsync(new PlatformFeatureUpdate(EmployersEnabled: enabled));
        scope.ServiceProvider.GetRequiredService<Jobsy.Core.Features.IFeatureFlags>().Invalidate();
    }

    private async Task<Guid> SeedRequestAsync(
        TalentContactStatus status,
        bool forOtherCandidate = false)
    {
        var id = Guid.NewGuid();
        // Creating a client first lets the factory seed its users before we add rows.
        _factory.CreateClient().Dispose();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        var candidateId = _factory.CandidateId;
        if (forOtherCandidate)
        {
            candidateId = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = candidateId,
                Email = $"other-{candidateId:N}@jobsy.local",
                FullName = "Andere Kandidaat",
                PhoneNumber = "+31600000099",
                Role = UserRole.Candidate,
                IsActive = true
            });
        }

        var created = DateTime.UtcNow.AddHours(-1);
        db.TalentContactRequests.Add(new TalentContactRequest
        {
            Id = id,
            CompanyId = _factory.CompanyId,
            EmployerUserId = _factory.EmployerId,
            CandidateUserId = candidateId,
            Status = status,
            Message = "We zoeken iemand zoals jij.",
            CreatedAtUtc = created,
            RespondByUtc = created.AddHours(48)
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<TalentContactStatus> StatusAsync(Guid id)
        => (await RowAsync(id)).Status;

    private async Task<string?> ReasonAsync(Guid id)
        => (await RowAsync(id)).CandidateDeclineReason;

    private async Task<TalentContactRequest> RowAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        return await db.TalentContactRequests.AsNoTracking().FirstAsync(r => r.Id == id);
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private HttpClient CandidateClient()
        => JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
}
