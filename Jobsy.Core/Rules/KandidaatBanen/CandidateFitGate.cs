using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>
/// D2: candidacy fit % is shown only when the culture <em>or</em> values test is complete.
/// Provisional answers do not open the gate (same rule as <see cref="ProfileVacancyMatchContext"/> scores).
/// </summary>
public readonly record struct CandidateFitGate(bool HasCompleteCulture, bool HasCompleteValues)
{
    public bool IsOpen => HasCompleteCulture || HasCompleteValues;

    public static CandidateFitGate Closed { get; } = new(false, false);

    public static CandidateFitGate FromContext(ProfileVacancyMatchContext? context)
    {
        if (context is null)
        {
            return Closed;
        }

        // CultureScores / ValuesScores are non-null only when complete (ProfileVacancyMatchService).
        return new CandidateFitGate(
            context.CultureScores is not null,
            context.ValuesScores is not null);
    }

    public static CandidateFitGate FromFlags(bool cultureCompleted, bool valuesCompleted)
        => new(cultureCompleted, valuesCompleted);
}
