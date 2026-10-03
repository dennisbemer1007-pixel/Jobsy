namespace Jobsy.Core.Scholen;

/// <summary>Pupil code alphabet and format helpers (D5). Same behaviour as <see cref="Jobsy.Core.Rules.ShortCodeFormat"/>.</summary>
public static class PupilCodeFormat
{
    /// <summary>30 characters; no I, L, O, 0, 1.</summary>
    public const string Alphabet = Jobsy.Core.Rules.ShortCodeFormat.Alphabet;

    public const int Length = Jobsy.Core.Rules.ShortCodeFormat.Length;

    public static string Normalize(string input) => Jobsy.Core.Rules.ShortCodeFormat.Normalize(input);

    public static bool TryNormalize(string? input, out string normalized)
        => Jobsy.Core.Rules.ShortCodeFormat.TryNormalize(input, out normalized);

    public static bool IsWellFormed(string? input) => Jobsy.Core.Rules.ShortCodeFormat.IsWellFormed(input);

    /// <summary>Display form <c>K7Q-M2P</c>.</summary>
    public static string Display(string code) => Jobsy.Core.Rules.ShortCodeFormat.Display(code);
}
