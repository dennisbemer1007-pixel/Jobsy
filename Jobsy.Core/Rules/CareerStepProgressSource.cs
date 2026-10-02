namespace Jobsy.Core.Rules;

public static class CareerStepProgressSources
{
    public const string Manual = "Manual";
    public const string Auto = "Auto";
    public const string ManualUndo = "ManualUndo";
    public const string CarriedOver = "CarriedOver";

    public static bool IsKnown(string? value)
        => string.Equals(value, Manual, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, Auto, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, ManualUndo, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, CarriedOver, StringComparison.OrdinalIgnoreCase);

    public static bool IsCompletionStamp(string? value)
        => string.Equals(value, Manual, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, Auto, StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, CarriedOver, StringComparison.OrdinalIgnoreCase);
}
