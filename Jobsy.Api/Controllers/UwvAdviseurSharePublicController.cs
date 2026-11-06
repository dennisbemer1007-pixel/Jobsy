using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/uwv/adviseur-share")]
public sealed class UwvAdviseurSharePublicController : ControllerBase
{
    private readonly UwvCandidateToolkitService _toolkit;

    public UwvAdviseurSharePublicController(UwvCandidateToolkitService toolkit)
    {
        _toolkit = toolkit;
    }

    [HttpGet("{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<UwvAdviseurShareViewDto>> Get(string token, CancellationToken cancellationToken)
    {
        var view = await _toolkit.GetShareViewAsync(token, cancellationToken);
        return view is null ? NotFound() : Ok(view);
    }
}
