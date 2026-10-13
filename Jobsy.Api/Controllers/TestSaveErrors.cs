using Jobsy.Core.Diagnostics;
using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Api.Controllers;

/// <summary>Shared { code, message } ProblemDetails-style payloads for candidate test saves.</summary>
internal static class TestSaveErrors
{
    public static BadRequestObjectResult ParentalConsent()
        => new(new { code = "parental_consent_required", message = CandidateConsentRules.ParentalConsentRequiredMessage });

    public static BadRequestObjectResult TestConsent()
        => new(new { code = "consent_required", message = CandidateConsentRules.TestConsentRequiredMessage });

    public static BadRequestObjectResult UnknownQuestion()
        => new(new { code = "unknown_question", message = "Onbekend vraagnummer." });

    public static BadRequestObjectResult InvalidAnswer()
        => new(new { code = "invalid_answer", message = "Antwoord moet 1–5 zijn." });

    public static BadRequestObjectResult FromException(InvalidOperationException ex)
    {
        var msg = ex.Message ?? "";
        if (msg.Contains("onbekend", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return UnknownQuestion();
        }

        if (msg.Contains('1') && msg.Contains('5'))
        {
            return InvalidAnswer();
        }

        if (msg.Contains("Beantwoord alle", StringComparison.OrdinalIgnoreCase))
        {
            return new BadRequestObjectResult(new { code = "incomplete", message = msg });
        }

        return new BadRequestObjectResult(new { code = "error", message = msg });
    }

    /// <summary>
    /// Writes the failure to Systeemlogs with an LB- code, then returns the existing payload.
    /// </summary>
    public static async Task<T> LogAsync<T>(
        ControllerBase controller,
        Exception ex,
        T result,
        string category,
        CancellationToken cancellationToken) where T : IActionResult
    {
        var log = controller.HttpContext?.RequestServices.GetService<IPlatformErrorLog>();
        if (log is not null)
        {
            try
            {
                await log.WriteAsync(
                    category,
                    ex.Message,
                    SupportCodeGenerator.Create(),
                    controller.HttpContext?.Request.Path.Value,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // The visitor still gets the original result.
            }
        }

        return result;
    }
}
