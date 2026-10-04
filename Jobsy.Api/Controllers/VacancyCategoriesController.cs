using System.Security.Claims;
using Jobsy.Api.Models;
using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Jobsy.Core.Features;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/vacancy-categories")]
public class VacancyCategoriesController : ControllerBase
{
    private readonly IVacancyCategoryService _categories;
    private readonly IAdminAuditLog _audit;

    public VacancyCategoriesController(IVacancyCategoryService categories, IAdminAuditLog audit)
    {
        _categories = categories;
        _audit = audit;
    }

    /// <summary>Active categories for create dropdown, map filter and legend.</summary>
    [HttpGet]
    [AllowAnonymous]
    [RequiresFeature(PlatformFeature.Employers)]
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
    [RequiresFeature(PlatformFeature.Employers)]
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
            await WriteCategoryAsync(AdminAuditKeys.VacancyCategoryCreate, created.Id, created.Name, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return Validation(ex);
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
            if (updated is null)
            {
                return NotFound();
            }

            await WriteCategoryAsync(AdminAuditKeys.VacancyCategoryUpdate, updated.Id, updated.Name, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return Validation(ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _categories.GetByIdAsync(id, cancellationToken);
        var ok = await _categories.DeleteAsync(id, cancellationToken);
        if (!ok)
        {
            return NotFound();
        }

        await WriteCategoryAsync(
            AdminAuditKeys.VacancyCategoryDelete,
            id,
            existing?.Name ?? id.ToString(),
            cancellationToken);
        return NoContent();
    }

    private static VacancyKind ParseKind(string? value)
        => VacancyKindLabels.ParseOrDefault(value);

    private static BadRequestObjectResult Validation(ArgumentException ex)
        => new(new
        {
            code = "validation",
            message = ex.Message,
            detail = ex.Message,
            userMessage = true
        });

    private Task WriteCategoryAsync(string action, Guid id, string name, CancellationToken cancellationToken)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        Guid? actor = Guid.TryParse(raw, out var parsed) ? parsed : null;
        return _audit.WriteAsync(
            new AdminAuditEntry(
                Action: action,
                TargetType: "vacancy-category",
                TargetId: id.ToString("D"),
                TargetLabel: name,
                ActorUserId: actor,
                ActorRole: "Admin"),
            cancellationToken);
    }
}
