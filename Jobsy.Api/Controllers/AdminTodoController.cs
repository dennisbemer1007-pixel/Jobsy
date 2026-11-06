using Jobsy.Core.Admin;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/admin/todo")]
[Authorize(Policy = JobsyPolicies.RequireAdmin)]
public sealed class AdminTodoController : ControllerBase
{
    private readonly IAdminTodoService _todo;

    public AdminTodoController(IAdminTodoService todo) => _todo = todo;

    [HttpGet]
    public async Task<ActionResult<AdminTodoResponse>> Get(CancellationToken cancellationToken)
    {
        var snapshot = await _todo.GetAsync(cancellationToken);
        return Ok(new AdminTodoResponse(
            snapshot.Items.Select(Map).ToList(),
            snapshot.CountsByNavKey));
    }

    private static AdminTodoItemDto Map(AdminTodoItem item) => new(
        item.Key,
        item.Severity.ToString().ToLowerInvariant(),
        item.TitleKey,
        item.Subtitle,
        item.Area,
        item.SinceUtc,
        item.ActionLabelKey,
        item.Href,
        item.Count);
}

public sealed record AdminTodoItemDto(
    string Key,
    string Severity,
    string TitleKey,
    string Subtitle,
    string Area,
    DateTime SinceUtc,
    string ActionLabelKey,
    string Href,
    int Count);

public sealed record AdminTodoResponse(
    IReadOnlyList<AdminTodoItemDto> Items,
    IReadOnlyDictionary<string, int> CountsByNavKey);
