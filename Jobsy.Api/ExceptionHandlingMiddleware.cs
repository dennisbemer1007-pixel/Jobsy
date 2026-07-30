using System.Net;
using System.Text.Json;
using Jobsy.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

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
        _logger.LogError(ex, "Unhandled exception for {Method} {Path} → {Status}",
            context.Request.Method, context.Request.Path.Value, status);

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
