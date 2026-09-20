using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/training-offers")]
[Authorize(Policy = JobsyPolicies.RequireCandidate)]
public sealed class TrainingUpskillController : ControllerBase
{
    private readonly ITrainingUpskillService _training;
    private readonly IUserLookupService _users;

    public TrainingUpskillController(ITrainingUpskillService training, IUserLookupService users)
    {
        _training = training;
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TrainingOfferCardDto>>> Recommend(
        [FromQuery] string? jobTitle,
        [FromQuery] string? campaign,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await _training.RecommendAsync(
            user.Id,
            jobTitle,
            null,
            campaign ?? TrainingTracking.CampaignFit,
            cancellationToken));
    }

    [HttpPost("{offerId:guid}/track")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<TrainingTrackedLinkDto>> Track(
        Guid offerId,
        [FromBody] TrainingTrackRequest? request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Gebruiker niet gevonden in Jobsy." });
        }

        try
        {
            return Ok(await _training.TrackAsync(user.Id, offerId, request?.Campaign, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/admin/training")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class TrainingAdminController : ControllerBase
{
    private readonly ITrainingUpskillService _training;

    public TrainingAdminController(ITrainingUpskillService training) => _training = training;

    [HttpGet("providers")]
    public async Task<ActionResult<IReadOnlyList<TrainingProviderAdminDto>>> List(CancellationToken cancellationToken)
        => Ok(await _training.ListProvidersAdminAsync(cancellationToken));

    [HttpPut("providers")]
    public async Task<ActionResult<TrainingProviderAdminDto>> UpsertProvider(
        [FromBody] TrainingProviderUpsertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _training.UpsertProviderAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("providers/{id:guid}")]
    public async Task<IActionResult> DeleteProvider(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _training.DeleteProviderAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("offers")]
    public async Task<ActionResult<TrainingOfferAdminDto>> UpsertOffer(
        [FromBody] TrainingOfferUpsertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _training.UpsertOfferAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("offers/{id:guid}")]
    public async Task<IActionResult> DeleteOffer(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _training.DeleteOfferAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("conversions")]
    public async Task<ActionResult<TrainingConversionDto>> RecordConversion(
        [FromBody] TrainingConversionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _training.RecordConversionAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? providerId,
        CancellationToken cancellationToken)
    {
        if (year < 2020 || month is < 1 or > 12)
        {
            return BadRequest(new { message = "Kies een geldige maand." });
        }

        var csv = await _training.ExportCsvAsync(year, month, providerId, cancellationToken);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"lobsy-opleidingen-{year:D4}-{month:D2}.csv");
    }
}

public sealed record TrainingTrackRequest(string? Campaign);
