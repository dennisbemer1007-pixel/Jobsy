using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/private-preferences")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class CandidatePrivatePreferencesController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JobsyDbContext _db;
    private readonly IUserLookupService _users;
    private readonly ICandidateMatchSnapshotService _matchSnapshots;
    private readonly ILogger<CandidatePrivatePreferencesController> _logger;

    public CandidatePrivatePreferencesController(
        JobsyDbContext db,
        IUserLookupService users,
        ICandidateMatchSnapshotService matchSnapshots,
        ILogger<CandidatePrivatePreferencesController> logger)
    {
        _db = db;
        _users = users;
        _matchSnapshots = matchSnapshots;
        _logger = logger;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<CandidatePrivatePreferencesDto>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var row = await _db.CandidatePrivatePreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        return Ok(ToDto(row));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidatePrivatePreferencesDto>> Put(
        [FromBody] UpdateCandidatePrivatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var lookup = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (lookup is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        var (dislikes, custom) = CandidatePrivatePreferencesValidator.Sanitize(
            request.Dislikes,
            request.CustomDislikes,
            dropped => _logger.LogInformation("Private preferences dropped: {Dropped}", dropped));

        var row = await _db.CandidatePrivatePreferences
            .FirstOrDefaultAsync(p => p.UserId == lookup.Id, cancellationToken);
        if (row is null)
        {
            row = new CandidatePrivatePreferences { UserId = lookup.Id };
            _db.CandidatePrivatePreferences.Add(row);
        }

        row.DislikesJson = JsonSerializer.Serialize(dislikes, JsonOptions);
        row.CustomDislikesJson = JsonSerializer.Serialize(custom, JsonOptions);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _matchSnapshots.MarkInputsStaleAsync(lookup.Id, cancellationToken);

        return Ok(ToDto(row));
    }

    private static CandidatePrivatePreferencesDto ToDto(CandidatePrivatePreferences? row)
    {
        if (row is null)
        {
            return new CandidatePrivatePreferencesDto([], [], null);
        }

        return new CandidatePrivatePreferencesDto(
            ParseJsonArray(row.DislikesJson),
            ParseJsonArray(row.CustomDislikesJson),
            row.UpdatedAtUtc);
    }

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
