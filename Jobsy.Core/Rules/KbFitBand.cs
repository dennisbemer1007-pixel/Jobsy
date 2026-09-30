namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate-facing fit band for display (file 04 calibrates percentages).
/// Strong ≥ 75, Good 65–74, Some &lt; 65.
/// </summary>
public enum KbFitBand
{
    Some = 0,
    Good = 1,
    Strong = 2
}
