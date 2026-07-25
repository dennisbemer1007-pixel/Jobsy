using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/privacy")]
[Authorize]
public sealed class PrivacyController : ControllerBase
{
    private readonly IPrivacyDataService _privacy;

    public PrivacyController(IPrivacyDataService privacy)
    {
        _privacy = privacy;
    }

    /// <summary>AVG Art. 15 / 20 — export personal data as JSON.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _privacy.ExportAsync(User, cancellationToken);
            return Ok(payload);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>AVG Art. 17 — anonymize / erase account data.</summary>
    [HttpPost("delete-account")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        try
        {
            await _privacy.DeleteOrAnonymizeAsync(User, cancellationToken);
            return Ok(new { message = "Accountgegevens zijn geanonimiseerd." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
