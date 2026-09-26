namespace Jobsy.Core.Rules;

public static class CareerStepProgressSources
{
    public const string Manual = "Manual";
    public const string Auto = "Auto";
    public const string ManualUndo = "ManualUndo";

    public static bool IsKnown(string? value)
        => string.Equals(value, Manual, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, Auto, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, ManualUndo, StringComparison.OrdinalIgnoreCase);
}
