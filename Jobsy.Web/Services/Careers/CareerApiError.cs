using System.Net;
using System.Text.Json;

namespace Jobsy.Web.Services.Careers;

/// <summary>Expected career API failure with a stable <see cref="Code"/> (B12).</summary>
public sealed class CareerApiError : Exception
{
    public CareerApiError(string code, HttpStatusCode? statusCode = null)
        : base(code)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public HttpStatusCode? StatusCode { get; }

    public static CareerApiError? TryParse(HttpStatusCode statusCode, string? body)
    {
        var code = ExtractCode(body);
        if (string.IsNullOrWhiteSpace(code))
        {
            if (statusCode == HttpStatusCode.Gone)
            {
                return new CareerApiError("use_passport_proof", statusCode);
            }

            return null;
        }

        return new CareerApiError(code, statusCode);
    }

    public static CareerApiError FromResponse(HttpStatusCode statusCode, string? body)
        => TryParse(statusCode, body)
           ?? new CareerApiError("error", statusCode);

    /// <summary>Maps API snake_case codes to <c>CareerErr.*</c> localization keys.</summary>
    public static string LocalizationKey(string code)
        => code switch
        {
            "generation_limit" => "CareerErr.GenerationLimit",
            "complete_previous_first" => "CareerErr.CompletePreviousFirst",
            "undo_last_first" => "CareerErr.UndoLastFirst",
            "dream_text_invalid" => "CareerErr.DreamTextInvalid",
            "generation_in_progress" => "CareerErr.InProgress",
            "use_passport_proof" => "CareerErr.UsePassportProof",
            "ai_unavailable" => "CareerErr.AiUnavailable",
            "no_plan" => "CareerErr.NoPlan",
            _ => "Common.Error"
        };

    private static string? ExtractCode(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("code", out var code))
            {
                return code.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}
