namespace Jobsy.Core.Scholen;

/// <summary>
/// One source for how long a pupil session takes. School copy, the lesbrief and the ouderbrief
/// all use these phrases so the numbers cannot drift.
/// </summary>
public static class PupilSessionDuration
{
    public const string Groep78 = "ongeveer 30 minuten";

    public const string VoLessons = "2 lesdelen van 40–45 minuten";

    /// <summary>Spelled out for the parent letter.</summary>
    public const string VoLetter = "twee lesdelen";
}
