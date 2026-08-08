using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/candidate-actions")]
[AllowAnonymous]
public class CandidateActionsController : ControllerBase
{
    private readonly JobsyDbContext _db;
    private readonly ICandidateActionTokenService _tokens;
    private readonly IUserNotificationService _notifications;
    private readonly IEmailService _email;
    private readonly IPlatformFeatureService _features;

    public CandidateActionsController(
        JobsyDbContext db,
        ICandidateActionTokenService tokens,
        IUserNotificationService notifications,
        IEmailService email,
        IPlatformFeatureService features)
    {
        _db = db;
        _tokens = tokens;
        _notifications = notifications;
        _email = email;
        _features = features;
    }

    [HttpPost("set-unavailable")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateActionResultDto>> SetUnavailable(
        [FromBody] CandidateActionRequest request,
        CancellationToken cancellationToken)
    {
        var token = await _tokens.FindValidAsync(
            request.Token,
            CandidateActionPurposes.SetUnavailable,
            cancellationToken);
        if (token is null)
        {
            return BadRequest(new CandidateActionResultDto(false, "Deze link is ongeldig of al gebruikt. Pas je status desnoods aan via je profiel."));
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return BadRequest(new CandidateActionResultDto(false, "We konden je account niet vinden. Log in en pas je status aan via je profiel."));
        }

        user.OpenForWork = false;
        await _tokens.MarkUsedAsync(token, cancellationToken);

        await _notifications.CreateAsync(
            new NotificationCreateRequest(
                user.Id,
                "Je staat op Niet beschikbaar",
                "Werkgevers zien je niet meer als open voor werk. Zet dit later weer aan in je profiel als je wilt.",
                "SetUnavailable",
                "/candidate/profile"),
            cancellationToken);

        return Ok(new CandidateActionResultDto(
            true,
            "Je status staat nu op Niet beschikbaar. Werkgevers zien je niet meer als open voor werk."));
    }

    [HttpPost("withdraw-others")]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateActionResultDto>> WithdrawOthers(
        [FromBody] CandidateActionRequest request,
        CancellationToken cancellationToken)
    {
        var token = await _tokens.FindValidAsync(
            request.Token,
            CandidateActionPurposes.WithdrawOtherApplications,
            cancellationToken);
        if (token is null)
        {
            return BadRequest(new CandidateActionResultDto(false, "Deze link is ongeldig of al gebruikt."));
        }

        if (token.RelatedApplicationId is null)
        {
            return BadRequest(new CandidateActionResultDto(false, "Actie is incompleet geconfigureerd."));
        }

        var hired = await _db.Applications
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .FirstOrDefaultAsync(
                a => a.Id == token.RelatedApplicationId.Value && a.CandidateUserId == token.UserId,
                cancellationToken);
        if (hired is null || hired.Status != ApplicationStatus.Hired)
        {
            return BadRequest(new CandidateActionResultDto(false, "Match-sollicitatie niet gevonden."));
        }

        var others = await _db.Applications
            .Include(a => a.Vacancy).ThenInclude(v => v.Company)
            .Where(a => a.CandidateUserId == token.UserId
                        && a.Id != hired.Id
                        && a.EmailVerifiedAt != null
                        && a.Status != ApplicationStatus.Withdrawn
                        && a.Status != ApplicationStatus.Rejected
                        && a.Status != ApplicationStatus.FilledElsewhere
                        && a.Status != ApplicationStatus.Hired)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var other in others)
        {
            other.Status = ApplicationStatus.Withdrawn;
            other.RespondedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _tokens.MarkUsedAsync(token, cancellationToken);

        var deepLinkBase = await BuildDeepLinkAsync("/branch/applicants", cancellationToken);
        foreach (var other in others)
        {
            var contacts = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive
                            && u.Role != UserRole.Candidate
                            && (u.CompanyId == other.Vacancy.CompanyId
                                || u.CompanyMemberships.Any(m => m.CompanyId == other.Vacancy.CompanyId)))
                .Select(u => new { u.Id, u.Email })
                .Distinct()
                .ToListAsync(cancellationToken);

            var title = $"Sollicitatie ingetrokken: {other.Vacancy.Title}";
            var body =
                "De kandidaat heeft deze sollicitatie ingetrokken omdat er inmiddels een andere baan is gevonden.";
            var html = $"""
                <p>Hoi,</p>
                <p>Goed om te weten: de kandidaat heeft de sollicitatie op
                <strong>{WebUtility.HtmlEncode(other.Vacancy.Title)}</strong> ingetrokken.</p>
                <p>Reden: de kandidaat heeft inmiddels een andere baan gevonden.</p>
                <p><a href="{WebUtility.HtmlEncode(deepLinkBase)}">Open sollicitantenoverzicht</a></p>
                """;

            foreach (var contact in contacts)
            {
                await _email.SendAsync(new EmailMessage(
                    contact.Email,
                    title,
                    html,
                    "CandidateWithdrawnOtherJob"), cancellationToken);

                await _notifications.CreateAsync(
                    new NotificationCreateRequest(
                        contact.Id,
                        title,
                        body,
                        "CandidateWithdrawnOtherJob",
                        "/branch/applicants",
                        RelatedEntityType: "Application",
                        RelatedEntityId: other.Id),
                    cancellationToken);
            }
        }

        await _notifications.CreateAsync(
            new NotificationCreateRequest(
                token.UserId,
                "Andere sollicitaties netjes afgerond",
                others.Count == 0
                    ? "Er stonden geen andere open sollicitaties meer — je bent helemaal bij."
                    : $"{others.Count} andere sollicitatie(s) zijn ingetrokken. De werkgevers zijn vriendelijk geïnformeerd.",
                "WithdrawOtherApplications",
                "/candidate/applications"),
            cancellationToken);

        return Ok(new CandidateActionResultDto(
            true,
            others.Count == 0
                ? "Er stonden geen andere open sollicitaties om in te trekken."
                : $"{others.Count} andere sollicitatie(s) zijn succesvol ingetrokken.",
            others.Count));
    }

    private async Task<string> BuildDeepLinkAsync(string relativePath, CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl)
            ? "https://lobsy.nl"
            : snap.PublicWebBaseUrl.TrimEnd('/');
        if (!relativePath.StartsWith('/'))
        {
            relativePath = "/" + relativePath;
        }

        return baseUrl + relativePath;
    }
}
