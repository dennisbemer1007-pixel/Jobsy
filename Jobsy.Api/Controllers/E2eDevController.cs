using Jobsy.Core.Dev;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        return DevEmailCodeCapture.TryGet(email, out var code)
            ? Ok(new { email = email.Trim().ToLowerInvariant(), code })
            : NotFound(new { message = "no code captured yet" });
    }
}
