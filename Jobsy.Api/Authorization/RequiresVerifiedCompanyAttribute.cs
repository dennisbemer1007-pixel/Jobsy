using System.Reflection;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Authorization;

/// <summary>
/// Blocks unverified companies from publish / token purchase / candidate-data actions.
/// Admin bypasses. Returns 403 <c>company_unverified</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresVerifiedCompanyAttribute : TypeFilterAttribute
{
    public RequiresVerifiedCompanyAttribute()
        : base(typeof(RequiresVerifiedCompanyFilter))
    {
    }
}

public sealed class RequiresVerifiedCompanyFilter : IAsyncActionFilter
{
    private readonly JobsyDbContext _db;
    private readonly ICompanyAuthorizationService _companyAuth;
    private readonly IUserLookupService _users;

    public RequiresVerifiedCompanyFilter(
        JobsyDbContext db,
        ICompanyAuthorizationService companyAuth,
        IUserLookupService users)
    {
        _db = db;
        _companyAuth = companyAuth;
        _users = users;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (_companyAuth.IsAdmin(user)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
        {
            await next();
            return;
        }

        var companyId = await ResolveActingCompanyIdAsync(context, user, context.HttpContext.RequestAborted);
        if (companyId is null || companyId == Guid.Empty)
        {
            // Fail closed: cannot resolve a company → treat as blocked for gated actions.
            context.Result = UnverifiedResult();
            return;
        }

        var status = await ResolveRootVerificationStatusAsync(companyId.Value, context.HttpContext.RequestAborted);
        if (!CompanyVerificationRules.CanPublish(status))
        {
            context.Result = UnverifiedResult();
            return;
        }

        await next();
    }

    private static ObjectResult UnverifiedResult()
        => new(new
        {
            code = CompanyVerificationRules.UnverifiedErrorCode,
            message = CompanyVerificationRules.BlockedMessageNl
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

    private async Task<CompanyVerificationStatus> ResolveRootVerificationStatusAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var row = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.Id, c.ParentCompanyId, c.VerificationStatus })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return CompanyVerificationStatus.Unverified;
        }

        if (row.ParentCompanyId is Guid parentId)
        {
            var parentStatus = await _db.Companies.AsNoTracking()
                .Where(c => c.Id == parentId)
                .Select(c => c.VerificationStatus)
                .FirstOrDefaultAsync(cancellationToken);
            return parentStatus;
        }

        return row.VerificationStatus;
    }

    private async Task<Guid?> ResolveActingCompanyIdAsync(
        ActionExecutingContext context,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (TryGetGuidArg(context, "companyId", out var companyId)
            || TryGetGuidFromBody(context, "CompanyId", out companyId)
            || TryGetGuidFromQueryOrRoute(context, "companyId", out companyId))
        {
            return companyId;
        }

        if (TryGetGuidArg(context, "id", out var routeId)
            && context.ActionDescriptor.AttributeRouteInfo?.Template is { } template
            && template.Contains("vacancies", StringComparison.OrdinalIgnoreCase)
            && !template.Contains("applications", StringComparison.OrdinalIgnoreCase))
        {
            // /api/vacancies/{id}/… — id is the vacancy.
            var fromVacancy = await CompanyIdFromVacancyAsync(routeId, cancellationToken);
            if (fromVacancy is not null)
            {
                return fromVacancy;
            }
        }

        if (TryGetGuidFromBody(context, "VacancyId", out var vacancyId)
            || TryGetGuidArg(context, "vacancyId", out vacancyId)
            || TryGetGuidFromQueryOrRoute(context, "vacancyId", out vacancyId))
        {
            var fromVacancy = await CompanyIdFromVacancyAsync(vacancyId, cancellationToken);
            if (fromVacancy is not null)
            {
                return fromVacancy;
            }
        }

        if (TryGetGuidFromBody(context, "CheckoutId", out var checkoutId)
            || TryGetGuidArg(context, "checkoutId", out checkoutId))
        {
            var fromCheckout = await CompanyIdFromCheckoutAsync(checkoutId, paymentId: null, cancellationToken);
            if (fromCheckout is not null)
            {
                return fromCheckout;
            }
        }

        if (TryGetStringFromBody(context, "PaymentId", out var paymentId)
            && !string.IsNullOrWhiteSpace(paymentId))
        {
            var fromPayment = await CompanyIdFromCheckoutAsync(null, paymentId, cancellationToken);
            if (fromPayment is not null)
            {
                return fromPayment;
            }
        }

        if (TryGetGuidArg(context, "id", out var applicationId)
            && context.ActionDescriptor.AttributeRouteInfo?.Template is { } appTemplate
            && appTemplate.Contains("applications", StringComparison.OrdinalIgnoreCase))
        {
            var fromApp = await CompanyIdFromApplicationAsync(applicationId, cancellationToken);
            if (fromApp is not null)
            {
                return fromApp;
            }
        }

        if (TryGetGuidArg(context, "applicationId", out var namedAppId)
            || TryGetGuidFromQueryOrRoute(context, "applicationId", out namedAppId))
        {
            var fromApp = await CompanyIdFromApplicationAsync(namedAppId, cancellationToken);
            if (fromApp is not null)
            {
                return fromApp;
            }
        }

        // Talent pool / candidate-insights: home company of the employer.
        var me = await _users.FindByPrincipalAsync(user, cancellationToken);
        if (me?.CompanyId is Guid home)
        {
            return home;
        }

        var accessible = await _companyAuth.GetAccessibleCompanyIdsAsync(user, cancellationToken);
        if (accessible is { Count: > 0 })
        {
            return accessible.First();
        }

        return null;
    }

    private async Task<Guid?> CompanyIdFromVacancyAsync(Guid vacancyId, CancellationToken cancellationToken)
    {
        var row = await _db.Vacancies.AsNoTracking()
            .Where(v => v.Id == vacancyId)
            .Select(v => new { v.CompanyId, v.IntermediaryCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        // Intermediary posts: gate on the bureau (intermediary), not the end-client.
        return row.IntermediaryCompanyId ?? row.CompanyId;
    }

    private async Task<Guid?> CompanyIdFromCheckoutAsync(
        Guid? checkoutId,
        string? paymentId,
        CancellationToken cancellationToken)
    {
        if (checkoutId is Guid id && id != Guid.Empty)
        {
            return await _db.TokenPurchaseCheckouts.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => (Guid?)c.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(paymentId))
        {
            return await _db.TokenPurchaseCheckouts.AsNoTracking()
                .Where(c => c.PaymentId == paymentId)
                .Select(c => (Guid?)c.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private async Task<Guid?> CompanyIdFromApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var row = await _db.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.Vacancy.CompanyId, a.Vacancy.IntermediaryCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        return row.IntermediaryCompanyId ?? row.CompanyId;
    }

    private static bool TryGetGuidArg(ActionExecutingContext context, string name, out Guid id)
    {
        id = Guid.Empty;
        if (!context.ActionArguments.TryGetValue(name, out var value))
        {
            return false;
        }

        if (value is Guid g && g != Guid.Empty)
        {
            id = g;
            return true;
        }

        return false;
    }

    private static bool TryGetGuidFromQueryOrRoute(ActionExecutingContext context, string name, out Guid id)
    {
        id = Guid.Empty;
        if (context.HttpContext.Request.Query.TryGetValue(name, out var q)
            && Guid.TryParse(q.FirstOrDefault(), out var queryId)
            && queryId != Guid.Empty)
        {
            id = queryId;
            return true;
        }

        if (context.RouteData.Values.TryGetValue(name, out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeId)
            && routeId != Guid.Empty)
        {
            id = routeId;
            return true;
        }

        return false;
    }

    private static bool TryGetStringFromBody(ActionExecutingContext context, string propertyName, out string? value)
    {
        value = null;
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var prop = argument.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop?.GetValue(argument) is string s)
            {
                value = s;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetGuidFromBody(ActionExecutingContext context, string propertyName, out Guid id)
    {
        id = Guid.Empty;
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var prop = argument.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop?.GetValue(argument) is Guid bodyId && bodyId != Guid.Empty)
            {
                id = bodyId;
                return true;
            }
        }

        return false;
    }
}
