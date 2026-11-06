using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/me/email-preferences")]
[Authorize]
public sealed class MeEmailPreferencesController : ControllerBase
{
    private readonly IUserLookupService _users;
    private readonly IEmailPreferenceService _preferences;

    public MeEmailPreferencesController(IUserLookupService users, IEmailPreferenceService preferences)
    {
        _users = users;
        _preferences = preferences;
    }

    public sealed record PreferenceDto(string Key, string Label, bool Enabled);

    public sealed record PreferencesResponse(
        IReadOnlyList<PreferenceDto> Optional,
        IReadOnlyList<string> Always,
        bool ReminderEmailsEnabled = true);

    public sealed record UpdatePreferencesRequest(
        IReadOnlyList<PreferenceDto>? Items,
        bool? ReminderEmailsEnabled = null);

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<PreferencesResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var items = await _preferences.GetForUserAsync(user.Id, cancellationToken);
        var reminders = await _preferences.AreReminderEmailsEnabledAsync(user.Id, cancellationToken);
        return Ok(new PreferencesResponse(
            items.Select(i => new PreferenceDto(i.Key, i.Label, i.Enabled)).ToList(),
            AlwaysSentLabels(user.Role),
            reminders));
    }

    [HttpPut]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<PreferencesResponse>> Put(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByPrincipalAsync(User, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return Unauthorized();
        }

        if (request.ReminderEmailsEnabled == false)
        {
            await _preferences.DisableReminderEmailsAsync(user.Id, "Settings", cancellationToken);
        }
        else
        {
            if (request.ReminderEmailsEnabled == true)
            {
                await _preferences.EnableReminderEmailsAsync(user.Id, "Settings", cancellationToken);
            }

            var allowed = (await _preferences.GetForUserAsync(user.Id, cancellationToken))
                .Select(i => i.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in request.Items ?? Array.Empty<PreferenceDto>())
            {
                if (string.IsNullOrWhiteSpace(item.Key) || !allowed.Contains(item.Key))
                {
                    continue;
                }

                if (item.Enabled)
                {
                    await _preferences.OptInAsync(user.Email, item.Key, cancellationToken);
                }
                else
                {
                    await _preferences.OptOutAsync(user.Email, item.Key, "Settings", cancellationToken);
                }
            }
        }

        var refreshed = await _preferences.GetForUserAsync(user.Id, cancellationToken);
        var reminders = await _preferences.AreReminderEmailsEnabledAsync(user.Id, cancellationToken);
        return Ok(new PreferencesResponse(
            refreshed.Select(i => new PreferenceDto(i.Key, i.Label, i.Enabled)).ToList(),
            AlwaysSentLabels(user.Role),
            reminders));
    }

    private static IReadOnlyList<string> AlwaysSentLabels(UserRole role) => role switch
    {
        UserRole.Candidate =>
        [
            "Codes (inloggen, solliciteren)",
            "Sollicitaties en reacties",
            "Beveiliging"
        ],
        UserRole.BranchManager or UserRole.RegionalManager or UserRole.EnterpriseManager or UserRole.Intermediary =>
        [
            "Codes en uitnodigingen",
            "Sollicitaties op je vacatures",
            "Beveiliging"
        ],
        _ =>
        [
            "Codes",
            "Uitnodigingen",
            "Beveiliging"
        ]
    };
}
