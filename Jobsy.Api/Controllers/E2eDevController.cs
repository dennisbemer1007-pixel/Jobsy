using System.Security.Claims;
using Jobsy.Core.Dev;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

/// <summary>Development-only helpers for Playwright (never enabled in Production).</summary>
[ApiController]
[Route("api/e2e")]
public sealed class E2eDevController : ControllerBase
{
    [HttpGet("last-email-code")]
    [AllowAnonymous]
    public IActionResult LastEmailCode([FromQuery] string? email)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        if (!DevEmailCodeCapture.Enabled)
        {
            return NotFound(new { message = "JOBSY_E2E_CAPTURE_EMAIL_CODES is off" });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email required" });
        }

        if (!DevEmailCodeCapture.TryGetEntry(email, out var entry))
        {
            return NotFound(new { message = "no code captured yet" });
        }

        return Ok(new
        {
            email = email.Trim().ToLowerInvariant(),
            code = entry.Code,
            sequence = entry.Sequence,
            challengeId = entry.ChallengeId
        });
    }

    [HttpGet("open-email-challenge")]
    [AllowAnonymous]
    public async Task<IActionResult> OpenEmailChallenge(
        [FromQuery] string? email,
        [FromServices] JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        if (!DevEmailCodeCapture.Enabled || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email required" });
        }

        var normalized = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var challenge = await db.EmailSignInChallenges.AsNoTracking()
            .Where(c => c.EmailNormalized == normalized
                        && c.ConsumedAtUtc == null
                        && c.ExpiresAtUtc > now)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new { c.Id, c.CreatedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge is null)
        {
            return NotFound(new { message = "no open challenge" });
        }

        string? code = null;
        long? sequence = null;
        if (DevEmailCodeCapture.TryGetForChallenge(challenge.Id, out var forChallenge))
        {
            code = forChallenge;
            if (DevEmailCodeCapture.TryGetEntry(email, out var capture))
            {
                sequence = capture.Sequence;
            }
        }

        return Ok(new
        {
            email = normalized,
            challengeId = challenge.Id,
            code,
            sequence,
            createdAtUtc = challenge.CreatedAtUtc
        });
    }

    [HttpPost("clear-email-codes")]
    [AllowAnonymous]
    public IActionResult ClearEmailCodes()
    {
        if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        if (!DevEmailCodeCapture.Enabled)
        {
            return NotFound(new { message = "JOBSY_E2E_CAPTURE_EMAIL_CODES is off" });
        }

        DevEmailCodeCapture.Clear();
        return Ok(new { cleared = true });
    }

    [HttpPost("fixture/external-vacancy")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalVacancyFixture(
        [FromQuery] string? email,
        [FromServices] JobsyDbContext db,
        [FromServices] IHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment() || !DevEmailCodeCapture.Enabled)
        {
            return NotFound();
        }

        Guid userId;
        if (User.Identity?.IsAuthenticated == true
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var claimId))
        {
            userId = claimId;
        }
        else if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "email required when unauthenticated" });
        }
        else
        {
            var normalized = email.Trim().ToLowerInvariant();
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
            if (user is null)
            {
                return NotFound(new { message = "user not found" });
            }

            userId = user.Id;
        }

        var id = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = id,
            CandidateUserId = userId,
            SourceUrl = "https://example.com/e2e-vacancy",
            SourceHost = "example.com",
            Title = "E2E Buitenbaan",
            CompanyName = "Demo Werkgever",
            Place = "Den Haag",
            HoursText = "32 uur",
            PayText = "€ 14/uur",
            StartText = "Per direct",
            TrainingText = "Inwerktraject",
            RequirementsBulletsJson = "[\"Rijbewijs B\"]",
            Status = CandidateExternalVacancyStatus.Saved,
            SavedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { id });
    }

    [HttpPost("fixture/fresh-vacancy")]
    [AllowAnonymous]
    public async Task<IActionResult> FreshVacancyFixture(
        [FromQuery] string? email,
        [FromServices] JobsyDbContext db,
        [FromServices] IHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        _ = email;

        var template = await db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active)
            .OrderByDescending(v => v.PublishedAtUtc ?? v.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            return NotFound(new { message = "no active vacancy template" });
        }

        var id = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Vacancies.Add(new Vacancy
        {
            Id = id,
            Title = $"Usability3 sollicitatie {id.ToString("N")[..8]}",
            Description = template.Description,
            HourlyWage = template.HourlyWage,
            StartDate = today,
            EndDate = today.AddMonths(2),
            Status = VacancyStatus.Active,
            CompanyId = template.CompanyId,
            Location = template.Location,
            RequiredTransport = template.RequiredTransport,
            WorkTypes = template.WorkTypes,
            WorkTypeLabels = template.WorkTypeLabels,
            MaxApplications = template.MaxApplications,
            SalaryTableId = template.SalaryTableId,
            CategoryId = template.CategoryId,
            MinHoursPerWeek = template.MinHoursPerWeek,
            MaxHoursPerWeek = template.MaxHoursPerWeek,
            FlexibleTimes = template.FlexibleTimes,
            RequireEmailVerification = template.RequireEmailVerification,
            PublishedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { id });
    }

    /// <summary>Playwright Kompas: employers on, demo match profile, refreshed vacancy index.</summary>
    [HttpPost("fixture/kompas-finale")]
    [AllowAnonymous]
    public async Task<IActionResult> KompasFinaleFixture(
        [FromQuery] string? email,
        [FromServices] JobsyDbContext db,
        [FromServices] IHostEnvironment env,
        [FromServices] IPlatformFeatureService platformFeatures,
        [FromServices] IVacancyDiscoveryIndex discovery,
        [FromServices] ICandidateMatchSnapshotService matchSnapshots,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment() || string.IsNullOrWhiteSpace(email))
        {
            return env.IsDevelopment() ? BadRequest(new { message = "email required" }) : NotFound();
        }

        await platformFeatures.UpdateAsync(new PlatformFeatureUpdate(EmployersEnabled: true), cancellationToken);

        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "user not found" });
        }

        var anchorVacancy = await db.Vacancies.AsNoTracking()
            .Where(v => v.Status == VacancyStatus.Active)
            .OrderByDescending(v => v.PublishedAtUtc ?? v.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        user.DateOfBirth ??= new DateOnly(1998, 6, 15);
        user.HomeLocation = anchorVacancy is null
            ? new GeoPoint(52.0, 4.3)
            : new GeoPoint(anchorVacancy.Location.Latitude, anchorVacancy.Location.Longitude);
        user.OpenForWork = true;

        await EnsureCompletedMiniProfileAsync(db, user.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await discovery.RefreshAsync(cancellationToken);
        var live = await matchSnapshots.ComputeLiveAsync(user.Id, cancellationToken);
        return Ok(new { matchCount = live.Count, employersEnabled = true });
    }

    private static async Task EnsureCompletedMiniProfileAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        const int pct = 82;

        var competency = await db.CandidateCompetencies.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (competency is null)
        {
            competency = new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            db.CandidateCompetencies.Add(competency);
        }

        competency.Status = CandidateCompetencyStatuses.Completed;
        competency.SamenwerkenPercent = pct;
        competency.ResultaatgerichtheidPercent = pct;
        competency.StressbestendigheidPercent = pct;
        competency.InnovatiePercent = pct;
        competency.ExtraversiePercent = pct;
        competency.UpdatedAtUtc = now;

        var career = await db.CandidateCareerInterests.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (career is null)
        {
            career = new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            db.CandidateCareerInterests.Add(career);
        }

        career.Status = CandidateCompetencyStatuses.Completed;
        career.RealisticPercent = pct;
        career.InvestigativePercent = pct;
        career.ArtisticPercent = pct;
        career.SocialPercent = pct;
        career.EnterprisingPercent = pct;
        career.ConventionalPercent = pct;
        career.UpdatedAtUtc = now;

        var culture = await db.CandidateCulturePersonalityProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (culture is null)
        {
            culture = new CandidateCulturePersonalityProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            db.CandidateCulturePersonalityProfiles.Add(culture);
        }

        culture.Status = CandidateCompetencyStatuses.Completed;
        culture.AutonomyPercent = pct;
        culture.InformalPercent = pct;
        culture.CollaborationPercent = pct;
        culture.FlexibilityPercent = pct;
        culture.InnovationPercent = pct;
        culture.PeopleFirstPercent = pct;
        culture.OpennessPercent = pct;
        culture.ConscientiousnessPercent = pct;
        culture.ExtraversionPercent = pct;
        culture.AgreeablenessPercent = pct;
        culture.EmotionalStabilityPercent = pct;
        culture.UpdatedAtUtc = now;

        var values = await db.CandidateValuesProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (values is null)
        {
            values = new CandidateValuesProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now
            };
            db.CandidateValuesProfiles.Add(values);
        }

        values.Status = CandidateCompetencyStatuses.Completed;
        values.AutonomyPercent = pct;
        values.ConnectionPercent = pct;
        values.AchievementPercent = pct;
        values.StabilityPercent = pct;
        values.ImpactPercent = pct;
        values.UpdatedAtUtc = now;
    }
}
