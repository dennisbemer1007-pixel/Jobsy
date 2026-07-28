using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/masterdata")]
public class MasterdataController : ControllerBase
{
    private readonly JobsyDbContext _db;

    public MasterdataController(JobsyDbContext db) => _db = db;

    /// <summary>Active options for forms (candidate/vacancy/discovery).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MasterdataOptionDto>>> GetActive(
        [FromQuery] string? category = null,
        [FromQuery] string? audience = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MasterdataOptions.AsNoTracking().Where(o => o.IsActive);
        if (MasterdataCategories.IsKnown(category))
        {
            var cat = MasterdataCategories.Normalize(category!);
            query = query.Where(o => o.Category == cat);
        }

        if (string.Equals(audience, "candidate", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.ShowOnCandidate);
        }
        else if (string.Equals(audience, "vacancy", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.ShowOnVacancy);
        }

        var items = await query
            .OrderBy(o => o.Category)
            .ThenBy(o => o.SortOrder)
            .ThenBy(o => o.Label)
            .Select(o => ToDto(o))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("admin")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<IReadOnlyList<MasterdataOptionDto>>> GetAllAdmin(CancellationToken cancellationToken)
    {
        var items = await _db.MasterdataOptions.AsNoTracking()
            .OrderBy(o => o.Category)
            .ThenBy(o => o.SortOrder)
            .ThenBy(o => o.Label)
            .Select(o => ToDto(o))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<MasterdataOptionDto>> Create(
        [FromBody] UpsertMasterdataOptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(request, out var category, out var value, out var label, out var error))
        {
            return BadRequest(new { message = error });
        }

        var exists = await _db.MasterdataOptions.AnyAsync(
            o => o.Category == category && o.Value == value,
            cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "Deze waarde bestaat al in deze categorie." });
        }

        var sortOrder = request.SortOrder
            ?? ((await _db.MasterdataOptions.Where(o => o.Category == category).MaxAsync(o => (int?)o.SortOrder, cancellationToken)) ?? -1) + 1;

        var entity = new MasterdataOption
        {
            Id = Guid.NewGuid(),
            Category = category,
            Value = value,
            Label = label,
            SortOrder = Math.Max(0, sortOrder),
            IsActive = request.IsActive ?? true,
            ShowOnCandidate = request.ShowOnCandidate ?? category != MasterdataCategories.MinEmployers,
            ShowOnVacancy = request.ShowOnVacancy
                ?? !string.Equals(value, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase)
        };

        if (category == MasterdataCategories.EducationLevel
            && string.Equals(value, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
        {
            entity.ShowOnVacancy = false;
        }

        _db.MasterdataOptions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetActive), new { category }, ToDto(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<MasterdataOptionDto>> Update(
        Guid id,
        [FromBody] UpsertMasterdataOptionRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.MasterdataOptions.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (!TryNormalize(request, out var category, out var value, out var label, out var error))
        {
            return BadRequest(new { message = error });
        }

        var duplicate = await _db.MasterdataOptions.AnyAsync(
            o => o.Id != id && o.Category == category && o.Value == value,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "Deze waarde bestaat al in deze categorie." });
        }

        entity.Category = category;
        entity.Value = value;
        entity.Label = label;
        if (request.SortOrder is int sort)
        {
            entity.SortOrder = Math.Max(0, sort);
        }

        if (request.IsActive is bool active)
        {
            entity.IsActive = active;
        }

        if (request.ShowOnCandidate is bool onCandidate)
        {
            entity.ShowOnCandidate = onCandidate;
        }

        if (request.ShowOnVacancy is bool onVacancy)
        {
            entity.ShowOnVacancy = onVacancy;
        }

        if (category == MasterdataCategories.EducationLevel
            && string.Equals(value, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
        {
            entity.ShowOnVacancy = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.MasterdataOptions.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _db.MasterdataOptions.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool TryNormalize(
        UpsertMasterdataOptionRequest request,
        out string category,
        out string value,
        out string label,
        out string? error)
    {
        category = "";
        value = "";
        label = "";
        error = null;

        if (!MasterdataCategories.IsKnown(request.Category))
        {
            error = "Ongeldige categorie.";
            return false;
        }

        category = MasterdataCategories.Normalize(request.Category!);
        value = (request.Value ?? request.Label ?? "").Trim();
        label = (request.Label ?? request.Value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(label))
        {
            error = "Waarde en label zijn verplicht.";
            return false;
        }

        if (value.Length > 128 || label.Length > 128)
        {
            error = "Waarde/label mag maximaal 128 tekens zijn.";
            return false;
        }

        if (category == MasterdataCategories.MinEmployers)
        {
            if (!int.TryParse(value, out var n) || n is < 1 or > 20)
            {
                error = "Minimaal aantal werkgevers moet een getal tussen 1 en 20 zijn.";
                return false;
            }

            value = n.ToString();
            label = string.IsNullOrWhiteSpace(request.Label) ? value : label;
        }

        return true;
    }

    private static MasterdataOptionDto ToDto(MasterdataOption o) => new(
        o.Id,
        o.Category,
        o.Value,
        o.Label,
        o.SortOrder,
        o.IsActive,
        o.ShowOnCandidate,
        o.ShowOnVacancy);
}

public record MasterdataOptionDto(
    Guid Id,
    string Category,
    string Value,
    string Label,
    int SortOrder,
    bool IsActive,
    bool ShowOnCandidate,
    bool ShowOnVacancy);

public record UpsertMasterdataOptionRequest(
    string? Category,
    string? Value,
    string? Label,
    int? SortOrder = null,
    bool? IsActive = null,
    bool? ShowOnCandidate = null,
    bool? ShowOnVacancy = null);
