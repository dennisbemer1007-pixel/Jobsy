namespace Jobsy.Core.Rules;

/// <summary>
/// Expected career-plan failure with a stable error code (B12). Controllers map this to
/// ProblemDetails with <c>{ code }</c> instead of exposing <see cref="Exception.Message"/>.
/// </summary>
public sealed class CareerPlanException(string code, string? message = null)
    : Exception(message ?? code)
{
    public string Code { get; } = code;

    /// <summary>Set for <see cref="CareerPlanErrorCodes.GenerationLimit"/> (429 retry-after, D10).</summary>
    public DateTime? RetryAfterUtc { get; init; }
}

/// <summary>Stable error codes used by the career-plan API (B12).</summary>
public static class CareerPlanErrorCodes
{
    public const string NoPlan = "no_plan";
    public const string CompletePreviousFirst = "complete_previous_first";
    public const string UndoLastFirst = "undo_last_first";
    public const string GenerationInProgress = "generation_in_progress";
    public const string GenerationLimit = "generation_limit";
    public const string DreamTextInvalid = "dream_text_invalid";
    public const string UsePassportProof = "use_passport_proof";
    public const string NotFound = "not_found";
    public const string StepNotFound = "step_not_found";
}
