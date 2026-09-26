using Jobsy.Core.Privacy;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/parental-consent")]
public sealed class ParentalConsentController : ControllerBase
{
    private readonly JobsyDbContext _db;

    public ParentalConsentController(JobsyDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet("confirm")]
    [EnableRateLimiting("otp-verify")]
    public async Task<ContentResult> Confirm([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Page("Deze link is ongeldig.");
        }

        var now = DateTime.UtcNow;
        var tokenHash = VerificationCodes.Hash(token.Trim());
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.ParentalConsentTokenHash == tokenHash
                 && u.ParentalConsentTokenExpiresAt != null
                 && u.ParentalConsentTokenExpiresAt > now,
            cancellationToken);
        if (user is null || !CandidateConsentRules.RequiresParentalConsent(user))
        {
            return Page("Deze link is ongeldig of verlopen.");
        }

        user.ParentalConsentAt = now;
        user.ParentalConsentTokenHash = null;
        user.ParentalConsentTokenExpiresAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        return Page("Dank je. De toestemming is bevestigd. Het kind kan Lobsy nu gebruiken.");
    }

    private static ContentResult Page(string message)
        => new()
        {
            ContentType = "text/html; charset=utf-8",
            Content = $"<!doctype html><html lang=\"nl\"><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Lobsy toestemming</title></head><body><main><h1>Toestemming</h1><p>{System.Net.WebUtility.HtmlEncode(message)}</p></main></body></html>"
        };
}
