using Jobsy.Core.Authorization;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/partner-affiliate")]
[Authorize(Roles = $"{JobsyRoles.EnterpriseManager},{JobsyRoles.Intermediary}")]
public class PartnerAffiliateController : ControllerBase
{
    private readonly IPartnerAffiliateService _partners;
    private readonly IUserLookupService _users;

    public PartnerAffiliateController(
        IPartnerAffiliateService partners,
        IUserLookupService users)
    {
        _partners = partners;
        _users = users;
    }

    [HttpGet("me")]
    public async Task<ActionResult<PartnerAffiliateMeDto>> GetMine(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _partners.GetMineAsync(user.Id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("referrals")]
    public async Task<ActionResult<IEnumerable<PartnerAffiliateReferralRowDto>>> GetReferrals(
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _partners.GetReferralsAsync(user.Id, cancellationToken));
    }

    [HttpGet("toolkit")]
    public async Task<ActionResult<PartnerAffiliateToolkitDto>> GetToolkit(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var dto = await _partners.GetToolkitAsync(user.Id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}
