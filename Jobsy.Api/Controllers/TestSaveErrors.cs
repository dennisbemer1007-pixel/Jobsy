using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Mvc;

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
}
