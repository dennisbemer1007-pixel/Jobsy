using Jobsy.Api.Authorization;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/wages")]
public class WagesController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ISalaryService _salaryService;

    public WagesController(JobsyDbContext db, ISalaryService salaryService)
    {
        _db = db;
        _salaryService = salaryService;
    }

    /// <summary>Current effective WML rates (latest EffectiveFrom &lt;= today per age).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MinimumWageRateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _db.MinimumWageRates
            .AsNoTracking()
            .Where(r => r.EffectiveFrom <= today)
            .ToListAsync(cancellationToken);

        var rates = all
            .GroupBy(r => r.AgeYears)
            .Select(g => g.OrderByDescending(r => r.EffectiveFrom).First())
            .OrderByDescending(r => r.AgeYears)
            .Select(r => new MinimumWageRateDto(r.Id, r.AgeYears, r.HourlyRate, r.Label, r.EffectiveFrom))
            .ToList();

        if (rates.Count == 0)
        {
            rates = Enumerable.Range(15, 7)
                .Select(age => new MinimumWageRateDto(
                    Guid.Empty,
                    age,
                    _salaryService.GetMinimumHourlyWage(age),
                    $"Leeftijd {age}",
                    today))
                .Reverse()
                .ToList();
        }

        return Ok(rates);
    }

    [HttpGet("check")]
    [AllowAnonymous]
    public ActionResult<object> Check([FromQuery] decimal hourlyWage, [FromQuery] int ageYears = 21)
    {
        var minimum = _salaryService.GetMinimumHourlyWage(ageYears);
        return Ok(new
        {
            hourlyWage,
            ageYears,
            minimum,
            meetsMinimum = _salaryService.MeetsMinimumWage(hourlyWage, ageYears)
        });
    }

    [HttpPut]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<MinimumWageRateDto>> Upsert(
        [FromBody] UpsertWageRateRequest request,
        CancellationToken cancellationToken)
    {
        MinimumWageRateDto dto;
        if (request.Id is Guid id)
        {
            var existing = await _db.MinimumWageRates.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            existing.AgeYears = request.AgeYears;
            existing.HourlyRate = request.HourlyRate;
            existing.Label = request.Label;
            existing.EffectiveFrom = request.EffectiveFrom;
            await _db.SaveChangesAsync(cancellationToken);
            dto = new MinimumWageRateDto(existing.Id, existing.AgeYears, existing.HourlyRate, existing.Label, existing.EffectiveFrom);
        }
        else
        {
            var entity = new Core.Entities.MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = request.AgeYears,
                HourlyRate = request.HourlyRate,
                Label = request.Label,
                EffectiveFrom = request.EffectiveFrom
            };
            _db.MinimumWageRates.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            dto = new MinimumWageRateDto(entity.Id, entity.AgeYears, entity.HourlyRate, entity.Label, entity.EffectiveFrom);
        }

        return Ok(dto);
    }

    /// <summary>
    /// Stub for the half-yearly WML update: clones current rates onto the due EffectiveFrom
    /// (today when today is 1 Jan / 1 Jul; otherwise the next half-year boundary).
    /// </summary>
    [HttpPost("semi-annual-update")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<SemiAnnualWageUpdateResultDto>> SemiAnnualUpdate(
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFrom = ResolveSemiAnnualEffectiveFrom(today);

        var allRates = await _db.MinimumWageRates.AsNoTracking().ToListAsync(cancellationToken);
        var current = allRates
            .Where(r => r.EffectiveFrom <= today)
            .GroupBy(r => r.AgeYears)
            .Select(g => g.OrderByDescending(r => r.EffectiveFrom).First())
            .ToList();
        if (current.Count == 0)
        {
            current = allRates
                .GroupBy(r => r.AgeYears)
                .Select(g => g.OrderByDescending(r => r.EffectiveFrom).First())
                .ToList();
        }

        if (current.Count == 0)
        {
            return BadRequest(new { message = "Geen bestaande WML-tarieven om te kopiëren." });
        }

        var created = 0;
        foreach (var rate in current)
        {
            if (allRates.Any(r => r.AgeYears == rate.AgeYears && r.EffectiveFrom == effectiveFrom))
            {
                continue;
            }

            _db.MinimumWageRates.Add(new Core.Entities.MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = rate.AgeYears,
                HourlyRate = rate.HourlyRate,
                Label = rate.Label,
                EffectiveFrom = effectiveFrom
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _db.PlatformLogs.Add(new Core.Entities.PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Wages",
            Message = $"Semi-annual WML stub: {created} rates scheduled for {effectiveFrom:yyyy-MM-dd}.",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new SemiAnnualWageUpdateResultDto(
            effectiveFrom,
            created,
            created == 0
                ? $"Tarieven voor {effectiveFrom:dd-MM-yyyy} bestonden al."
                : $"{created} tarieven aangemaakt vanaf {effectiveFrom:dd-MM-yyyy} (stub — geen CBS-import)."));
    }

    public static DateOnly ResolveSemiAnnualEffectiveFrom(DateOnly today)
    {
        if (today is { Month: 1 or 7, Day: 1 })
        {
            return today;
        }

        return today.Month < 7
            ? new DateOnly(today.Year, 7, 1)
            : new DateOnly(today.Year + 1, 1, 1);
    }
}
