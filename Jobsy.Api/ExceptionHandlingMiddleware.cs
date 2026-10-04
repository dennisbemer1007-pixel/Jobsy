using System.Net;
using System.Text.Json;
using Jobsy.Core.Diagnostics;
using Jobsy.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Api;

/// <summary>
/// Central exception middleware: logs server-side, returns generic ProblemDetails to clients.
/// Never leaks stack traces or sensitive PII in responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex, "Unhandled exception after response started for {Method} {Path}",
                context.Request.Method, context.Request.Path.Value);
            throw ex;
        }

        var (status, title, detail) = MapException(ex);
        var supportCode = SupportCodeGenerator.Create();
        TagSentry(supportCode);
        _logger.LogError(ex, "Unhandled exception {SupportCode} for {Method} {Path} → {Status}",
            supportCode, context.Request.Method, context.Request.Path.Value, status);

        if (status >= 500)
        {
            try
            {
                var platform = context.RequestServices.GetService<IPlatformErrorLog>();
                if (platform is not null)
                {
                    await platform.WriteAsync(
                        "Api",
                        ex.GetType().Name,
                        supportCode,
                        context.Request.Path.Value,
                        CancellationToken.None);
                }
            }
            catch (Exception)
            {
                // Logging must not replace the problem response.
            }
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path.Value
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        // E2: the same short code the web 500 page shows, so support can match both sides.
        problem.Extensions["supportCode"] = supportCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOptions),
            context.RequestAborted);
    }

    private (int Status, string Title, string Detail) MapException(Exception ex) => ex switch
    {
        ForbiddenCompanyAccessException => (
            (int)HttpStatusCode.Forbidden,
            "Geen toegang",
            "Je hebt geen rechten voor dit bedrijf."),
        DomainException domain => (
            (int)HttpStatusCode.BadRequest,
            "Aanvraag afgewezen",
            SanitizeClientMessage(domain.Message)),
        UnauthorizedAccessException => (
            (int)HttpStatusCode.Forbidden,
            "Geen toegang",
            "Je hebt geen rechten voor deze actie."),
        KeyNotFoundException => (
            (int)HttpStatusCode.NotFound,
            "Niet gevonden",
            "Het gevraagde item bestaat niet."),
        OperationCanceledException => (
            499,
            "Verzoek geannuleerd",
            "Het verzoek is geannuleerd."),
        _ => (
            (int)HttpStatusCode.InternalServerError,
            "Interne serverfout",
            _env.IsDevelopment()
                ? SanitizeClientMessage(ex.Message)
                : "Er ging iets mis. Probeer het later opnieuw.")
    };

    private static void TagSentry(string supportCode)
    {
        try
        {
            SentrySdk.ConfigureScope(scope => scope.SetTag("support_code", supportCode));
        }
        catch (Exception)
        {
            // Sentry is optional (no DSN in Development / tests); never fail the response over it.
        }
    }

    private static string SanitizeClientMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Er ging iets mis.";
        }

        // Strip emails / long tokens that might have landed in exception text.
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
            "[redacted]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return sanitized.Length > 400 ? sanitized[..400] : sanitized;
    }
}
