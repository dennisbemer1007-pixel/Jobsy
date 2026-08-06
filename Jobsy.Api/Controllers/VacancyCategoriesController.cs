using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/vacancy-categories")]
public class VacancyCategoriesController : ControllerBase
{
    private readonly IVacancyCategoryService _categories;

    public VacancyCategoriesController(IVacancyCategoryService categories) => _categories = categories;

    /// <summary>Active categories for create dropdown, map filter and legend.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<VacancyCategoryDto>>> GetActive(CancellationToken cancellationToken)
        => Ok((await _categories.GetActiveAsync(cancellationToken))
            .Where(c => c.Id != VacancyCategoryDefaults.HighlightId
                        && !string.Equals(c.Slug, "highlight", StringComparison.OrdinalIgnoreCase))
            .ToList());

    [HttpGet("field-catalog")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public ActionResult<IReadOnlyList<VacancyCategoryFieldDto>> GetFieldCatalog()
        => Ok(VacancyCategoryExtraFields.All
            .Select(f => new VacancyCategoryFieldDto(f.Key, f.Label, f.InputType, f.Options))
            .ToList());

    [HttpGet("admin")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<IReadOnlyList<VacancyCategoryDto>>> GetAllAdmin(CancellationToken cancellationToken)
        => Ok(await _categories.GetAllAdminAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<VacancyCategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _categories.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<VacancyCategoryDto>> Create(
        [FromBody] UpsertVacancyCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _categories.CreateAsync(
                request.Name,
                request.ColorHex,
                request.PublishCostTokens,
                request.HighlightAvailable,
                request.HighlightCostTokens,
                request.PushBomAvailable,
                request.PushBomCostTokens,
                request.IsAlwaysFree,
                ParseKind(request.PlacementKind),
                request.ExtraFields,
                request.SortOrder,
                request.ShowInMapFilter ?? true,
                request.ShowInLegend ?? true,
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<ActionResult<VacancyCategoryDto>> Update(
        Guid id,
        [FromBody] UpsertVacancyCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _categories.UpdateAsync(
                id,
                request.Name,
                request.ColorHex,
                request.PublishCostTokens,
                request.HighlightAvailable,
                request.HighlightCostTokens,
                request.PushBomAvailable,
                request.PushBomCostTokens,
                request.IsAlwaysFree,
                ParseKind(request.PlacementKind),
                request.ExtraFields,
                request.SortOrder,
                request.IsActive,
                request.ShowInMapFilter ?? true,
                request.ShowInLegend ?? true,
                cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _categories.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private static VacancyKind ParseKind(string? value)
        => VacancyKindLabels.ParseOrDefault(value);
}
