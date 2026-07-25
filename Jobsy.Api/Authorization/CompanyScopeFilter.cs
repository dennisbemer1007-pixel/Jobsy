using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Jobsy.Api.Authorization;

/// <summary>
/// Ensures the current user may access the company identified by route/query/body <c>companyId</c>.
/// Branch managers are limited to their own company (data isolation).
/// Fails closed with 400 when the attribute is present but no company id can be resolved.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireCompanyAccessAttribute : TypeFilterAttribute
{
    public RequireCompanyAccessAttribute()
        : base(typeof(CompanyScopeFilter))
    {
    }
}

public sealed class CompanyScopeFilter : IAsyncActionFilter
{
    public const string CompanyIdArgumentNames = "companyId";

    private readonly ICompanyAuthorizationService _companyAuth;

    public CompanyScopeFilter(ICompanyAuthorizationService companyAuth)
    {
        _companyAuth = companyAuth;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!TryResolveCompanyId(context, out var companyId))
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "BadRequest",
                message = "CompanyId is verplicht."
            });
            return;
        }

        try
        {
            await _companyAuth.EnsureCanAccessCompanyAsync(context.HttpContext.User, companyId);
        }
        catch (ForbiddenCompanyAccessException)
        {
            context.Result = new ObjectResult(new
            {
                error = "Forbidden",
                message = "Je hebt geen toegang tot data van dit bedrijf."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }

    private static bool TryResolveCompanyId(ActionExecutingContext context, out Guid companyId)
    {
        companyId = Guid.Empty;

        if (context.ActionArguments.TryGetValue("companyId", out var named)
            && named is Guid namedGuid
            && namedGuid != Guid.Empty)
        {
            companyId = namedGuid;
            return true;
        }

        foreach (var (key, argument) in context.ActionArguments)
        {
            if (argument is Guid guid
                && guid != Guid.Empty
                && key.Contains("company", StringComparison.OrdinalIgnoreCase))
            {
                companyId = guid;
                return true;
            }

            if (argument is null)
            {
                continue;
            }

            var type = argument.GetType();
            var prop = type.GetProperty(
                "CompanyId",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.IgnoreCase);
            if (prop?.GetValue(argument) is Guid bodyCompanyId && bodyCompanyId != Guid.Empty)
            {
                companyId = bodyCompanyId;
                return true;
            }
        }

        if (context.HttpContext.Request.Query.TryGetValue("companyId", out var queryValue)
            && Guid.TryParse(queryValue.FirstOrDefault(), out var queryCompanyId)
            && queryCompanyId != Guid.Empty)
        {
            companyId = queryCompanyId;
            return true;
        }

        if (context.RouteData.Values.TryGetValue("companyId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeParsed)
            && routeParsed != Guid.Empty)
        {
            companyId = routeParsed;
            return true;
        }

        return false;
    }
}
